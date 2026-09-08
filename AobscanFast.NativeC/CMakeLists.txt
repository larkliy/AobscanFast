cmake_minimum_required(VERSION 3.14)
project(AobscanFastNativeC C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)
FetchContent_Declare(
  googletest
  URL https://github.com/google/googletest/archive/refs/tags/v1.14.0.zip
)
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(googletest)

enable_testing()

if(MSVC)
    add_compile_options(/O2 /Oi /Ot /W4)
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)
else()
    add_compile_options(-O3 -Wall -Wextra -fPIC)
endif()

set(SOURCES
    aobsrc/core/match/mask.c
    aobsrc/core/match/solid.c
    aobsrc/utils/aob_memmem.c
)

add_library(AobscanFastNativeC SHARED ${SOURCES})
target_include_directories(AobscanFastNativeC PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}/aobinc")

if(WIN32)
    set(NET_RUNTIME "win-x64")
elseif(APPLE)
    set(NET_RUNTIME "osx-x64")
else()
    set(NET_RUNTIME "linux-x64")
endif()

set(OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/runtimes/${NET_RUNTIME}/native")

set_target_properties(AobscanFastNativeC PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${OUTPUT_DIR}"
    RUNTIME_OUTPUT_DIRECTORY "${OUTPUT_DIR}"
    ARCHIVE_OUTPUT_DIRECTORY "${OUTPUT_DIR}"
)

add_executable(AobNativeBenchmark tests/test.c)
target_link_libraries(AobNativeBenchmark PRIVATE AobscanFastNativeC)

add_executable(AobNativeTestsCpp tests/test.cpp)
target_link_libraries(AobNativeTestsCpp PRIVATE 
    AobscanFastNativeC 
    gtest_main
)

include(GoogleTest)
add_test(NAME AobNativeTests COMMAND AobNativeTestsCpp)
set_target_properties(AobscanFastNativeC PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS TRUE)