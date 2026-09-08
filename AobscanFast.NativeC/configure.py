#!/usr/bin/env python3
import sys
import os
import platform
import subprocess
import re
import argparse


def get_cpu_flags_linux():
    flags = set()
    try:
        with open("/proc/cpuinfo", "r") as f:
            for line in f:
                if line.strip().startswith("flags"):
                    flags.update(line.split(":")[1].strip().split())
                    break
    except Exception:
        pass
    return flags


def get_compiler_info(name):
    try:
        output = subprocess.check_output([name, "--version"], stderr=subprocess.DEVNULL).decode("utf-8")
        firstln = output.splitlines()[0]

        vermatch = re.search(r"\d+\.\d+\.\d+", firstln)
        version = vermatch.group(0) if vermatch else "unknown"

        return f"{name} ({version})"
    except Exception:
        return None


def main():
    parser = argparse.ArgumentParser(description="AobscanFast Native C Configure Script")
    parser.add_argument("-debug", action="store_true", help="Build in Debug mode with symbols")
    parser.add_argument("-compiler", choices=["gcc", "clang", "cl", "icc", "g++", "clang++"], help="Force specific C compiler")
    parser.add_argument("-force-avx512", action="store_true", help="Force enable AVX-512 flag")
    parser.add_argument("-build", action="store_true", help="Build project after CMake configuration")
    parser.add_argument("-multithreaded", action="store_true", help="Build faster using multiple CPU cores")
    parser.add_argument("-test", choices=["benchmark", "googletest"], help="Run benchmark or googletest suite")
    
    args = parser.parse_args()
    if args.multithreaded and not args.build and not args.test:
        parser.error("Flag -multithreaded requires -build or -test flag")
        sys.exit(1)

    os_type = platform.system()
    print(f"\x1b[32mInfo\x1b[0m: Detected OS: \x1b[36m{os_type}\x1b[0m")

    compilers_tocheck = [args.compiler] if args.compiler else ["gcc", "clang", "icc", "cl", "g++", "clang++"]
    cxx_compilers_tocheck = ["g++", "clang++", "cl"]
    
    chosen_compiler = None
    cxx_chosen_compiler = None
    compiler_display = "none"
    cxx_compiler_display = "none"

    for c in compilers_tocheck:
        info = get_compiler_info(c)
        if info:
            chosen_compiler = c
            compiler_display = info
            break
    
    for cxx in cxx_compilers_tocheck:
        cxxinfo = get_compiler_info(cxx)
        if cxxinfo:
            cxx_chosen_compiler = cxx
            cxx_compiler_display = cxxinfo
            break

    print(f"\x1b[32mInfo\x1b[0m: Detected C Compiler: \x1b[36m{compiler_display}\x1b[0m")
    print(f"\x1b[32mInfo\x1b[0m: Detected CXX Compiler: \x1b[36m{cxx_compiler_display}\x1b[0m")
    
    if not chosen_compiler and os_type != "Windows":
        print("\x1b[31mError\x1b[0m: No usable C compiler found")
        sys.exit(1)

    c_flags = []

    has_avx512  = False
    has_sse     = False
    has_sse2    = False
    has_sse3    = False
    has_ssse3   = False
    has_sse4_1  = False
    has_sse4_2  = False
    has_avx2    = False
    has_avx     = False

    if os_type == "Linux":
        cpu_flags = get_cpu_flags_linux()
        print(f"\x1b[32mInfo\x1b[0m: CPU Flags: {', '.join(sorted(list(cpu_flags))[:10])}... (total {len(cpu_flags)})")

        if "sse" in cpu_flags:     has_sse = True
        if "sse2" in cpu_flags:    has_sse2 = True
        if "sse3" in cpu_flags or "pni" in cpu_flags: has_sse3 = True
        if "ssse3" in cpu_flags:   has_ssse3 = True
        if "sse4_1" in cpu_flags:  has_sse4_1 = True
        if "sse4_2" in cpu_flags:  has_sse4_2 = True
        if "avx" in cpu_flags:     has_avx = True
        if "avx2" in cpu_flags:    has_avx2 = True
        if "avx512f" in cpu_flags: has_avx512 = True

        if args.force_avx512:
            print("\x1b[33mWarning\x1b[0m: Force enable AVX512")
            has_sse = has_sse2 = has_sse3 = has_ssse3 = has_sse4_1 = has_sse4_2 = has_avx = has_avx2 = has_avx512 = True

        if has_avx512:
            c_flags.append("-mavx512f -mavx512bw -mavx2 -mavx -msse4.2 -msse4.1 -mssse3 -msse3 -msse2 -msse")
        elif has_avx2:
            c_flags.append("-mavx2 -mavx -msse4.2 -msse4.1 -mssse3 -msse3 -msse2 -msse")
        elif has_avx:
            c_flags.append("-mavx -msse4.2 -msse4.1 -mssse3 -msse3 -msse2 -msse")
        elif has_sse4_2:
            c_flags.append("-msse4.2 -msse4.1 -mssse3 -msse3 -msse2 -msse")
        elif has_sse4_1:
            c_flags.append("-msse4.1 -mssse3 -msse3 -msse2 -msse")
        elif has_ssse3:
            c_flags.append("-mssse3 -msse3 -msse2 -msse")
        elif has_sse3:
            c_flags.append("-msse3 -msse2 -msse")
        elif has_sse2:
            c_flags.append("-msse2 -msse")
        elif has_sse:
            c_flags.append("-msse")
        else:
            c_flags.append("-march=native")

    elif os_type == "Windows":
        try:
            cmd = "powershell -Command \"(Get-WmiObject Win32_Processor).ProcessorId + ' ' + (Get-WmiObject Win32_Processor).Name\""
            proc_info = subprocess.check_output(cmd, shell=True).decode().lower()
        except Exception:
            proc_info = ""

        if "avx512" in proc_info or args.force_avx512:
            if args.force_avx512:
                print("\x1b[33mWarning\x1b[0m: Force enable AVX512")
            has_sse = has_sse2 = has_sse3 = has_ssse3 = has_sse4_1 = has_sse4_2 = has_avx = has_avx2 = has_avx512 = True
            c_flags.append("/arch:AVX512")
        elif "core" in proc_info or "amd" in proc_info or "ryzen" in proc_info:
            has_sse = has_sse2 = has_sse3 = has_ssse3 = has_sse4_1 = has_sse4_2 = has_avx = has_avx2 = True
            c_flags.append("/arch:AVX2")
        else:
            has_sse = has_sse2 = True
            c_flags.append("/arch:SSE2")

    else:
        print(f"\x1b[31mError\x1b[0m: OS {os_type} Not supported")
        sys.exit(1)

    print(f"--    Host CPU has SSE    {'[\x1b[32m YES \x1b[0m]' if has_sse else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has SSE2   {'[\x1b[32m YES \x1b[0m]' if has_sse2 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has SSE3   {'[\x1b[32m YES \x1b[0m]' if has_sse3 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has SSSE3  {'[\x1b[32m YES \x1b[0m]' if has_ssse3 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has SSE4.1 {'[\x1b[32m YES \x1b[0m]' if has_sse4_1 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has SSE4.2 {'[\x1b[32m YES \x1b[0m]' if has_sse4_2 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has AVX    {'[\x1b[32m YES \x1b[0m]' if has_avx else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has AVX2   {'[\x1b[32m YES \x1b[0m]' if has_avx2 else '[\x1b[31m NO  \x1b[0m]'}")
    print(f"--    Host CPU has AVX512 {'[\x1b[32m YES \x1b[0m]' if has_avx512 else '[\x1b[31m NO  \x1b[0m]'}")
    
    flags_str = " ".join(c_flags)

    build_dir = "build"
    build_type = "Debug" if args.debug else "Release"
    if not os.path.exists(build_dir):
        os.makedirs(build_dir)

    cmake_cmd = [
        "cmake",
        f"-DCMAKE_BUILD_TYPE={build_type}",
        f"-DCMAKE_C_FLAGS={flags_str}",
        "-Wno-dev",
        ".."
    ]

    try:
        print(f"\x1b[32mInfo\x1b[0m: Running CMake configuration in {build_dir}...")
        subprocess.run(cmake_cmd, cwd=build_dir, check=True)

        if args.build or args.test:
            print("\x1b[32mInfo\x1b[0m: Building project binaries...")
            build_cmd = ["cmake", "--build", build_dir, "--config", build_type]
            if args.multithreaded:
                build_cmd.append("--parallel")
            subprocess.run(build_cmd, check=True)

        if args.test:
            target_file = "AobNativeBenchmark" if args.test == "benchmark" else "AobNativeTestsCpp"
            
            bin_path = os.path.join(build_dir, target_file)
            if not os.path.exists(bin_path) and os_type == "Windows":
                bin_path = os.path.join(build_dir, build_type, f"{target_file}.exe")

            print(f"\x1b[32mInfo\x1b[0m: Running test executable: {bin_path}")
            subprocess.run([bin_path], check=True)

        elif not args.build:
            print("\x1b[32mInfo\x1b[0m: Finished configuration. Run: 'cmake --build build'")

    except subprocess.CalledProcessError as e:
        print(f"\x1b[31mError\x1b[0m: Process failed with exit code {e.returncode}")
        sys.exit(1)


if __name__ == "__main__":
    main()