<div align="center">

  <img src="https://img.icons8.com/dusk/128/memory-slot.png" alt="logo" width="100" height="auto" />
  
  <h1>⚡ AobscanFast</h1>
  
  <p>
    <b>High-performance memory pattern (AOB) scanner for Windows and Linux.</b>
    <br>
    SIMD-accelerated, parallel, cross-platform — written in modern C#.
  </p>

  <a href="#">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET Version" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/Platform-Windows%20|%20Linux-0078D6?style=flat-square&logo=windows" alt="Platform" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/SIMD-AVX512%20|%20AVX2%20|%20SSE2-red?style=flat-square" alt="SIMD" />
  </a>

</div>

---

## Overview

**AobscanFast** scans process memory for Array-of-Bytes (AOB) signatures. It is designed for game modding, reverse engineering, debuggers, diagnostics — any tool that needs to locate byte sequences in live process memory.

**Key features:**

- **Cross-platform** — Windows (Win32 API) and Linux (`/proc/pid/mem` + `/proc/pid/maps`)
- **SIMD cascade** — automatically uses `Vector512` (AVX-512), `Vector256` (AVX2), or `Vector128` (SSE2) depending on CPU capabilities
- **Parallel scanning** — memory is split into 256 KB chunks and scanned concurrently via `Parallel.ForEach`
- **Zero-allocation paths** — `ArrayPool<byte>`, `Span<T>`, `stackalloc` throughout; no per-chunk allocations in the hot loop
- **Strategy pattern** — parsers and matchers are selected automatically based on the pattern syntax
- **DI-ready** — all dependencies injected through interfaces; `AobScannerFactory` for quick start

---

## Installation

```bash
git clone https://github.com/larkliy/AobscanFast.git
```

Or add a project reference:

```bash
dotnet add reference AobscanFast/AobscanFast.csproj
```

---

## Usage

### 1. Scan a remote process

```csharp
using AobscanFast.Infrastructure.Windows;
using AobscanFast.Services;

var handler = new WinProcessHandler();
uint? pid = handler.FindIdByName("notepad");

using var handle = handler.OpenProcess(pid.Value);

var scanner = AobScannerFactory.ForRemoteProcess(handle);
var results = scanner.Scan("48 8B ?? ?? ?? AA");
var first   = scanner.ScanFirst("48 8B ?? ?? ?? AA");
```

### 2. Module-scoped scan

Limit the range to a specific module for better performance.

```csharp
var module = handler.GetModuleInfo(pid.Value, "GameAssembly.dll");

if (module != null)
{
    var options = new AobScanOptions
    {
        MinScanAddress = module.Value.BaseAddress,
        MaxScanAddress = module.Value.BaseAddress + (nint)module.Value.Size
    };

    var results = scanner.Scan("F3 0F 10 ?? ?? ??", options);
}
```

### 3. In-process scan (injected DLL / NativeAOT)

```csharp
using AobscanFast.Services;

var scanner = AobScannerFactory.ForCurrentProcess();
var results = scanner.Scan("48 8B ?? ?? ?? AA");
```

### 4. Linux

```csharp
using AobscanFast.Infrastructure.Linux;
using AobscanFast.Services;

var handler = new LinuxProcessHandler();
uint? pid = handler.FindIdByName("bash");

using var handle = handler.OpenProcess(pid.Value);

var scanner = AobScannerFactory.ForRemoteProcess(handle);
var results = scanner.Scan("48 8B ?? ?? ?? AA");
```

---

## Pattern syntax

| Type | Example | Description |
|---|---|---|
| Solid (exact) | `AA BB CC DD` | No wildcards — uses `Span<byte>.IndexOf` |
| Byte mask | `AA ?? CC ??` | `??` matches any byte — SIMD masked comparison |
| Nibble mask | `?A B? A?B` | `?` masks a single nibble — per-nibble mask |

---

## Architecture

```
                    AobScanner (orchestrator)
                         |
          +--------------+--------------+
          |              |              |
   IProcessHandler  IRegionEnumerator  IMemoryAccessor
   (Win/Linux)      (Win/Linux)        (Win/Linux)
          |
   IMemoryRangePlanner → RegionProcessor (merge + chunk)
          |
   IPatternParserResolver → SolidParser / MaskParser / HalfMaskParser
          |
   IPatternMatcherResolver → SolidMatcher / MaskMatcher (SIMD)
```

Patterns are parsed once into an `AobPattern` (bytes, mask, search sequence). The longest contiguous unmasked sequence is extracted at parse time and used as a fast pre-filter during matching, drastically reducing the number of SIMD comparisons.

---

## Performance

- **Search-sequence pre-filter** — only positions where the longest solid run matches are verified with SIMD
- **256 KB chunks** with overlap (`patternLength - 1`) — balances parallelism with cache efficiency
- **Region merging** — adjacent memory regions are merged before chunking to minimize system calls
- **SIMD cascade** — `MaskMatcher.IsMatch()` tries AVX-512 → AVX2 → SSE2 → scalar fallback
- **Current-process zero-copy** — direct `unsafe` pointer reads when running inside the target process

---

## Project structure

```
AobscanFast/
  Core/
    Interfaces/      — all contracts
    Models/          — MemoryRange, AobScanOptions, AobPattern
    Parsing/         — SolidParser, MaskParser, HalfMaskParser
    Matching/        — SolidMatcher, MaskMatcher (SIMD)
    Helpers/         — RegionProcessor, ParserHelpers
  Services/          — AobScanner, AobScannerFactory
  Infrastructure/
    Windows/         — Win32 API implementations
    Linux/           — /proc-based implementations
AobscanFast.Sample/  — demo console app
AobscanFast.Tests/   — xUnit tests
```

---

## Building and testing

```bash
dotnet build
dotnet test
dotnet run --project AobscanFast.Sample
```

---

## Contributing

Contributions are welcome — Linux port, new SIMD routines, additional pattern formats.

1. Fork the repo
2. Create your branch: `git checkout -b feature/my-feature`
3. Commit your changes
4. Push and open a Pull Request

---

<div align="center">
  <i>Engineered for speed, architected for humans.</i>
</div>
