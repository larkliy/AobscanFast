#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include "aob.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

#if defined(_WIN32) || defined(_WIN64)
#include <windows.h>
static double get_time_ms(void) {
    LARGE_INTEGER freq, counter;
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&counter);
    return (double)counter.QuadPart * 1000.0 / (double)freq.QuadPart;
}
#else
#include <time.h>
static double get_time_ms(void) {
    struct timespec ts;
    clock_gettime(CLOCK_MONOTONIC, &ts);
    return (double)ts.tv_sec * 1000.0 + (double)ts.tv_nsec / 1000000.0;
}
#endif

#define BUFFER_SIZE (100 * 1024 * 1024)
#define MAX_RESULTS 1000

int main(void) 
{
    printf("=== AOB C Bandwidth Test ===\n");

    uint8_t *buffer = (uint8_t *)malloc(BUFFER_SIZE);
    if (!buffer) {
        printf("Error: Failed to allocate memory.\n");
        return 1;
    }

    memset(buffer, 0xCC, BUFFER_SIZE);

    size_t pos1 = 10 * 1024 * 1024;       
    size_t pos2 = 50 * 1024 * 1024;       
    size_t pos3 = BUFFER_SIZE - 100;      

    buffer[pos1] = 0xDE; buffer[pos1+1] = 0xAD; buffer[pos1+2] = 0xBE; buffer[pos1+3] = 0xEF;
    buffer[pos2] = 0xDE; buffer[pos2+1] = 0xAD; buffer[pos2+2] = 0xBE; buffer[pos2+3] = 0xEF;
    buffer[pos3] = 0xDE; buffer[pos3+1] = 0xAD; buffer[pos3+2] = 0xBE; buffer[pos3+3] = 0xEF;

    ptrdiff_t results[MAX_RESULTS];

    // ==========================================
    // ТЕСТ 1: Сплошной поиск (Solid Scan)
    // ==========================================
    uint8_t solid_pattern[] = {0xDE, 0xAD, 0xBE, 0xEF};
    size_t solid_len = 4;

    printf("\nSolid 1: 100 MegaBytes... ");
    
    double start_time = get_time_ms();
    ptrdiff_t count_solid = nativec_scan_solid(buffer, BUFFER_SIZE, solid_pattern, solid_len, results, MAX_RESULTS);
    double end_time = get_time_ms();

    double elapsed_solid = end_time - start_time;
    double throughput_solid = ((double)BUFFER_SIZE / (1024.0 * 1024.0)) / (elapsed_solid / 1000.0);

    printf("SUCCESS\n");
    // %td вместо %ld гарантирует правильный вывод ptrdiff_t на всех ОС
    printf("  matches found: %td (Expected: 3)\n", count_solid);
    printf("  Runtime: %.3f ms\n", elapsed_solid);
    printf("  Bandwidth: %.2f MB/s\n", throughput_solid);

    if (count_solid >= 3) {
        printf("  Pos check: %td, %td, %td -> OK\n", results[0], results[1], results[2]);
    }

    // ==========================================
    // ТЕСТ 2: Поиск с маской (Mask Scan)
    // ==========================================
    uint8_t mask_pattern[] = {0xDE, 0x00, 0xBE, 0xEF};
    uint8_t mask_bytes[]   = {0xFF, 0x00, 0xFF, 0xFF};
    size_t mask_len = 4;

    buffer[pos2+1] = 0xAA;

    printf("\nMask 2: 100 MegaBytes... ");

    start_time = get_time_ms();
    ptrdiff_t count_mask = nativec_scan_mask(buffer, BUFFER_SIZE, mask_pattern, mask_bytes, mask_len, results, MAX_RESULTS);
    end_time = get_time_ms();

    double elapsed_mask = end_time - start_time;
    double throughput_mask = ((double)BUFFER_SIZE / (1024.0 * 1024.0)) / (elapsed_mask / 1000.0);

    printf("SUCCESS\n");
    printf("  Found matches: %td (Expected: 3)\n", count_mask);
    printf("  Runtime: %.3f ms\n", elapsed_mask);
    printf("  Bandwidth: %.2f MB/s\n", throughput_mask);

    free(buffer);
    printf("\n=== Test Finished ===\n");
    return 0;
}