# AobscanFast.NativeC 
A AobscanFast module to improve performance. Written in C.

## Compiling type
Shared library, compiles into AobscanFast.NativeC/runtimes/win-x64/native/Release/AobscanNativeC.dll

## Features
- Fast.
- Low Level.
- Support AVX2, AVX512 and SSE2.

## Compile
Use configure.py script, that automatically find all supported archs and setup CMake.

## Structure
```
AobscanFast.NativeC
├─ aobinc - Include directory
├─ aobsrc - Source directory
├─ CMakeLists.txt - CMake file
└─ README.md - You reading this.
```