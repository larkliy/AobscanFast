"""ctypes bindings for the AobscanFast NativeAOT shared library."""

from __future__ import annotations

import ctypes
import os
from pathlib import Path
from typing import Optional

AOB_OK = 0
AOB_ERR_INVALID_ARGUMENT = -1
AOB_ERR_INVALID_HANDLE = -2
AOB_ERR_INTERNAL = -3

_ERR_NAMES = {
    AOB_ERR_INVALID_ARGUMENT: "invalid argument",
    AOB_ERR_INVALID_HANDLE: "invalid scanner handle",
    AOB_ERR_INTERNAL: "internal error",
}


class AobError(RuntimeError):
    """Raised when a native call fails."""


class _Library:
    def __init__(self, path: Optional[str] = None):
        self._cdll = ctypes.CDLL(str(Path(path) if path else self._bundled_name()))
        lib = self._cdll
        c_uint64_p = ctypes.POINTER(ctypes.c_uint64)
        c_int_p = ctypes.POINTER(ctypes.c_int)

        lib.aob_scanner_create_current.restype = ctypes.c_int
        lib.aob_scanner_create_current.argtypes = []
        lib.aob_scanner_create_remote_pid.restype = ctypes.c_int
        lib.aob_scanner_create_remote_pid.argtypes = [ctypes.c_uint]
        lib.aob_scanner_destroy.restype = None
        lib.aob_scanner_destroy.argtypes = [ctypes.c_int]
        lib.aob_find_pid_by_name.restype = ctypes.c_int
        lib.aob_find_pid_by_name.argtypes = [ctypes.c_char_p, ctypes.c_int]

        lib.aob_scan.restype = ctypes.c_int
        lib.aob_scan.argtypes = [
            ctypes.c_int, ctypes.c_char_p, ctypes.c_uint64, ctypes.c_uint64,
            ctypes.c_int64, ctypes.c_int, ctypes.c_int, c_uint64_p, ctypes.c_int,
            c_int_p,
        ]
        lib.aob_scan_first.restype = ctypes.c_int
        lib.aob_scan_first.argtypes = [
            ctypes.c_int, ctypes.c_char_p, ctypes.c_uint64, ctypes.c_uint64,
            ctypes.c_int64, ctypes.c_int, c_uint64_p,
        ]
        lib.aob_scan_module.restype = ctypes.c_int
        lib.aob_scan_module.argtypes = [
            ctypes.c_int, ctypes.c_char_p, ctypes.c_char_p,
            c_uint64_p, ctypes.c_int, c_int_p,
        ]
        lib.aob_scan_module_first.restype = ctypes.c_int
        lib.aob_scan_module_first.argtypes = [
            ctypes.c_int, ctypes.c_char_p, ctypes.c_char_p,
            c_uint64_p,
        ]
        lib.aob_last_error.restype = ctypes.c_int
        lib.aob_last_error.argtypes = [ctypes.c_char_p, ctypes.c_int]

    @staticmethod
    def _bundled_name() -> Path | str:
        native_dir = Path(__file__).resolve().parent / "_native"
        bundled = native_dir / (
            "AobscanFast.NativeExports.dll"
            if os.name == "nt" else "libAobscanFast.NativeExports.so"
        )
        if bundled.exists():
            return bundled
        return bundled.name

    def last_error(self) -> str:
        buffer = ctypes.create_string_buffer(1024)
        length = self._cdll.aob_last_error(buffer, len(buffer))
        if length <= 0:
            return ""
        return buffer.raw[: min(length, len(buffer) - 1)].decode("utf-8", "replace")

    def check(self, rc: int) -> None:
        if rc != AOB_OK:
            raise AobError(self.last_error() or _ERR_NAMES.get(rc, f"aob error {rc}"))


class AobScanner:
    """A scanner bound to the current process or a remote process by pid."""

    def __init__(self, pid: Optional[int] = None, library_path: Optional[str] = None):
        self._lib = _Library(library_path)
        handle = (
            self._lib._cdll.aob_scanner_create_current()
            if pid is None
            else self._lib._cdll.aob_scanner_create_remote_pid(pid)
        )
        if handle <= 0:
            raise AobError(self._lib.last_error() or "failed to create scanner")
        self._handle = handle
        self._pid = pid

    def __del__(self) -> None:
        try:
            self.close()
        except Exception:
            pass

    def close(self) -> None:
        """Release the native scanner and its operating-system resources."""
        handle = getattr(self, "_handle", 0)
        if handle:
            self._lib._cdll.aob_scanner_destroy(handle)
            self._handle = 0

    def __enter__(self) -> AobScanner:
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        self.close()

    def _require_open(self) -> int:
        if not self._handle:
            raise AobError("scanner is closed")
        return self._handle

    def scan(
        self, pattern: str, min_address: int = 0, max_address: int = 0,
        chunk_size: int = 0, max_parallel: int = -1, batch_size: int = 4096,
        capacity: Optional[int] = None,
    ) -> list[int]:
        """Scan memory and return all matching addresses."""
        if not pattern:
            raise ValueError("pattern must not be empty")
        if batch_size < 0 or min_address < 0 or max_address < 0:
            raise ValueError("invalid scan arguments")
        cap = batch_size if capacity is None else capacity
        if cap <= 0:
            raise ValueError("capacity must be positive")

        buffer = (ctypes.c_uint64 * cap)()
        results: list[int] = []
        cursor = min_address
        pattern_bytes = pattern.encode("utf-8")
        page_limit = min(batch_size, cap) if batch_size > 0 else cap
        while True:
            count = ctypes.c_int()
            rc = self._lib._cdll.aob_scan(
                self._require_open(), pattern_bytes, cursor, max_address, chunk_size,
                max_parallel, batch_size, buffer, cap, ctypes.byref(count),
            )
            self._lib.check(rc)
            count_value = count.value
            results.extend(int(buffer[i]) for i in range(count_value))
            if count_value < page_limit:
                return results
            cursor = max(int(buffer[i]) for i in range(count_value)) + 1

    def scan_first(
        self, pattern: str, min_address: int = 0, max_address: int = 0,
        chunk_size: int = 0, max_parallel: int = -1,
    ) -> Optional[int]:
        """Scan memory and return the first matching address, or ``None``."""
        if not pattern:
            raise ValueError("pattern must not be empty")
        if min_address < 0 or max_address < 0:
            raise ValueError("addresses must be >= 0")
        output = ctypes.c_uint64()
        rc = self._lib._cdll.aob_scan_first(
            self._require_open(), pattern.encode("utf-8"), min_address, max_address,
            chunk_size, max_parallel, ctypes.byref(output),
        )
        self._lib.check(rc)
        return int(output.value) if output.value else None

    def scan_module(self, module: str, pattern: str) -> list[int]:
        """Scan a named module and return matching addresses."""
        if not module or not pattern:
            raise ValueError("module and pattern must not be empty")
        while True:
            required = ctypes.c_int()
            rc = self._lib._cdll.aob_scan_module(
                self._require_open(), module.encode(), pattern.encode(),
                None, 0, ctypes.byref(required),
            )
            self._lib.check(rc)
            buffer = (ctypes.c_uint64 * max(required.value, 1))()
            count = ctypes.c_int()
            rc = self._lib._cdll.aob_scan_module(
                self._require_open(), module.encode(), pattern.encode(),
                buffer, len(buffer), ctypes.byref(count),
            )
            self._lib.check(rc)
            if count.value <= len(buffer):
                return [int(buffer[i]) for i in range(count.value)]

    def scan_module_first(self, module: str, pattern: str) -> Optional[int]:
        """Scan a named module and return the first matching address."""
        if not module or not pattern:
            raise ValueError("module and pattern must not be empty")
        output = ctypes.c_uint64()
        rc = self._lib._cdll.aob_scan_module_first(
            self._require_open(), module.encode(), pattern.encode(), ctypes.byref(output)
        )
        self._lib.check(rc)
        return int(output.value) if output.value else None

    @staticmethod
    def find_pid_by_name(
        name: str, index: int = 0, library_path: Optional[str] = None
    ) -> Optional[int]:
        """Find a process by executable name."""
        if not name:
            raise ValueError("name must not be empty")
        lib = _Library(library_path)
        pid = lib._cdll.aob_find_pid_by_name(name.encode(), index)
        if pid < 0:
            raise AobError(lib.last_error() or "find failed")
        return pid or None
