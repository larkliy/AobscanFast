<div align="center">

  <img src="https://img.icons8.com/dusk/128/memory-slot.png" alt="logo" width="100" height="auto" />
  
  <h1>⚡ AobscanFast</h1>
  
  <p>
    <b>Blazing fast memory scanning (AOB) meets Clean Architecture.</b>
    <br>
    A modular, SIMD-accelerated memory scanner written in modern C# for high-performance applications.
  </p>

  <!-- Badges -->
  <a href="#">
    <img src="https://img.shields.io/badge/.NET-9.0%2B-512BD4?style=flat-square&logo=dotnet" alt=".NET Version" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Platform" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/Architecture-Clean-green?style=flat-square" alt="Architecture" />
  </a>
  <a href="#">
    <img src="https://img.shields.io/badge/SIMD-AVX512%20%7C%20AVX2-red?style=flat-square" alt="SIMD" />
  </a>

</div>

<br>

## 🚀 Overview

**AobscanFast** is not just another memory scanner. It is a highly optimized, thread-safe library designed for developers who need extreme performance without sacrificing code quality.

*   💎 **Hardware Intrinsics:** Optimized for `Vector512`, `Vector256`, and `Vector128`. It dynamically chooses the fastest matching engine based on your CPU.
*   🏗️ **Clean Architecture:** Fully decoupled layers (Abstractions, Services, Infrastructure). No more static spaghetti code.
*   🧵 **True Parallelism:** Concurrent scanning using `Parallel.ForEach` with optimized memory chunking.
*   🔒 **Safe Handles:** Built with `SafeHandle` and `CsWin32` source generators to ensure zero handle leaks and memory safety.
*   🛠️ **DI Ready:** Designed to work seamlessly with Dependency Injection containers.

---

## 📦 Installation

```bash
git clone https://github.com/larkliy/AobscanFast.git
```

## 🔥 Usage

The new modular API separates process management from scanning logic.

### 1. Basic Pattern Scan (Direct usage)

```csharp
using AobscanFast.Infrastructure.Windows;
using AobscanFast.Services;

// 1. Initialize infrastructure
var handler = new WinProcessHandler();
uint? pid = handler.FindIdByName("notepad");

// 2. Open process safely
using var handle = handler.OpenProcess(pid.Value);

// 3. Scan memory
var reader = new WinMemoryReader(handle);
var scanner = new AobScanner(reader);

var results = scanner.Scan("48 8B ?? ?? ?? AA");
```

### 2. Module-Specific Scan

Limit the scan range to a specific DLL to drastically increase performance.

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

---

## 🏗 Architecture

The project follows **SOLID** principles to remain maintainable and extensible:

- **Abstractions:** Interfaces like `IMemoryReader` and `IProcessHandler`.
- **Services:** Core logic, including `AobScanner` and SIMD `PatternMatchers`.
- **Infrastructure:** Platform-specific implementations (currently Windows via `CsWin32`).
- **Models:** Value types like `MemoryRange` and `AobPattern`.

---

## 🛠 Performance Tech

1.  **Smart Chunking:** Adjacent memory regions are merged and then sliced into optimal chunks (256KB) to maximize cache hits and parallel efficiency.
2.  **Zero-Allocation Paths:** Heavy use of `Span<T>`, `ReadOnlySpan<T>`, and `ArrayPool<byte>` to minimize GC pressure.
3.  **Strategy Pattern:** Automatically switches between `SolidMatcher` (direct `IndexOf`) and `MaskMatcher` (SIMD masked comparison) based on your pattern.

---

## 🤝 Contributing

Contributions are welcome! Whether it's porting to **Linux** (via `/proc/pid/maps`) or optimizing SIMD routines further.

1.  Fork the repo
2.  Create your branch: `git checkout -b feature/cool-optimization`
3.  Commit your changes
4.  Push to the branch and open a Pull Request

## ⭐ Support

If you like this project, please **give it a Star**! 🌟 It helps me stay motivated and improve the library.

---
<div align="center">
  <i>Engineered for speed, architected for humans.</i>
</div>
```
