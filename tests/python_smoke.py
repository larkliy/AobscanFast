import ctypes
import os
import sys

from aobscanfast import AobError, AobScanner


marker = bytes.fromhex("DE AD BE EF 13 37 00 FF AA 11 22 33")
buffer = ctypes.create_string_buffer(marker)
expected = ctypes.addressof(buffer)

with AobScanner() as scanner:
    hits = scanner.scan("DE AD BE EF 13 37 00 FF AA 11 22 33", batch_size=1)
    assert expected in hits
    assert hits == sorted(hits)
    assert all(ctypes.string_at(hit, len(marker)) == marker for hit in hits)

    module_name = os.path.basename(sys.executable)
    module_pattern = "4D 5A" if os.name == "nt" else "7F 45 4C 46"
    module_hits = scanner.scan_module(module_name, module_pattern)
    module_first = scanner.scan_module_first(module_name, module_pattern)
    assert module_hits
    assert module_first in module_hits

scanner.close()
try:
    scanner.scan_first("AA")
except AobError:
    pass
else:
    raise AssertionError("closed scanner accepted a scan")
