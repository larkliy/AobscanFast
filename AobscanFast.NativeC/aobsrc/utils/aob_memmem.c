/* win_memmem The memmem for Windows for AobscanFast.
    Copyright(C) 2026 Alexander Silaev <thebinaryblob@gmail.com>
    This file made for AobscanFast on 17 August 2026.
    aob.h is under MIT License.
    The AobscanFast is project by larkliy.
*/
#include <string.h>
#include <stdint.h>
#include "aob.h"

typedef void* (*memmem_fn)(const void*, size_t, const void*, size_t);
static void* resolve_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len);
static memmem_fn current_memmem_impl = resolve_memmem;

// Fallback для старых CPU
static void *fallback_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len)
{
    if (needle_len == 0) return (void*)haystack;
    if (haystack_len < needle_len) return NULL;
    const uint8_t* h = (const uint8_t*)haystack;
    const uint8_t* n = (const uint8_t*)needle;
    const uint8_t* end = h + haystack_len - needle_len;
    for (; h <= end; h++) {
        if (*h == *n && memcmp(h, n, needle_len) == 0) return (void*)h;
    }
    return NULL;
}

// Реализация SSE2
#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("sse2")
#endif
static void *sse2_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len)
{
    if (needle_len == 0) return (void*)haystack;
    if (haystack_len < needle_len) return NULL;
    if (haystack_len < 16 + needle_len) return fallback_memmem(haystack, haystack_len, needle, needle_len);

    const uint8_t *h = (const uint8_t*)haystack;
    const uint8_t *n = (const uint8_t*)needle;

    __m128i first_vec = _mm_set1_epi8((char)n[0]);
    __m128i last_vec  = _mm_set1_epi8((char)n[needle_len - 1]);
    const uint8_t *end_scan = h + haystack_len - needle_len - 15;

    while (h <= end_scan)
    {
        __m128i h_first = _mm_loadu_si128((const __m128i*)h);
        __m128i h_last  = _mm_loadu_si128((const __m128i*)(h + needle_len - 1));
        __m128i cmp_res = _mm_and_si128(_mm_cmpeq_epi8(h_first, first_vec), _mm_cmpeq_epi8(h_last, last_vec));
        uint32_t mask = (uint32_t)_mm_movemask_epi8(cmp_res);

        while (mask != 0)
        {
            uint32_t bit_index = find_first_bit32(mask);
            if (memcmp(h + bit_index, n, needle_len) == 0) return (void*)(h + bit_index);
            mask &= mask - 1;
        }
        h += 16;
    }
    return fallback_memmem(h, (size_t)((const uint8_t*)haystack + haystack_len - h), n, needle_len);
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif


// Реализация AVX2
#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("avx2")
#endif
static void *avx2_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len)
{
    if (needle_len == 0) return (void*)haystack;
    if (haystack_len < needle_len) return NULL;
    if (haystack_len < 32 + needle_len) return sse2_memmem(haystack, haystack_len, needle, needle_len);

    const uint8_t *h = (const uint8_t*)haystack;
    const uint8_t *n = (const uint8_t*)needle;

    __m256i first_vec = _mm256_set1_epi8((char)n[0]);
    __m256i last_vec  = _mm256_set1_epi8((char)n[needle_len - 1]);
    const uint8_t *end_scan = h + haystack_len - needle_len - 31;

    while (h <= end_scan)
    {
        __m256i h_first = _mm256_loadu_si256((const __m256i*)h);
        __m256i h_last  = _mm256_loadu_si256((const __m256i*)(h + needle_len - 1));
        __m256i cmp_res = _mm256_and_si256(_mm256_cmpeq_epi8(h_first, first_vec), _mm256_cmpeq_epi8(h_last, last_vec));
        uint32_t mask = (uint32_t)_mm256_movemask_epi8(cmp_res);

        while (mask != 0)
        {
            uint32_t bit_index = find_first_bit32(mask);
            if (memcmp(h + bit_index, n, needle_len) == 0) return (void*)(h + bit_index);
            mask &= mask - 1;
        }
        h += 32;
    }
    return sse2_memmem(h, (size_t)((const uint8_t*)haystack + haystack_len - h), n, needle_len);
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif


// Реализация AVX-512
#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("avx512f,avx512bw")
#endif
static void *avx512_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len)
{
    if (needle_len == 0) return (void*)haystack;
    if (haystack_len < needle_len) return NULL;
    if (haystack_len < 64 + needle_len) return avx2_memmem(haystack, haystack_len, needle, needle_len);

    const uint8_t *h = (const uint8_t*)haystack;
    const uint8_t *n = (const uint8_t*)needle;

    // В AVX-512 маски сравнения возвращаются сразу в виде битовых масок (__mmask64)
    __m512i first_vec = _mm512_set1_epi8((char)n[0]);
    __m512i last_vec  = _mm512_set1_epi8((char)n[needle_len - 1]);
    const uint8_t *end_scan = h + haystack_len - needle_len - 63;

    while (h <= end_scan)
    {
        __m512i h_first = _mm512_loadu_si512((const __m512i*)h);
        __m512i h_last  = _mm512_loadu_si512((const __m512i*)(h + needle_len - 1));

        // Получаем 64-битные маски совпадений
        __mmask64 cmp_first = _mm512_cmpeq_epi8_mask(h_first, first_vec);
        __mmask64 cmp_last  = _mm512_cmpeq_epi8_mask(h_last, last_vec);
        uint64_t mask = (uint64_t)(cmp_first & cmp_last);

        while (mask != 0)
        {
            uint32_t bit_index = find_first_bit64(mask);
            if (memcmp(h + bit_index, n, needle_len) == 0) return (void*)(h + bit_index);
            mask &= mask - 1;
        }
        h += 64;
    }
    return avx2_memmem(h, (size_t)((const uint8_t*)haystack + haystack_len - h), n, needle_len);
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif

static void *resolve_memmem(const void *haystack, size_t haystack_len, const void *needle, size_t needle_len)
{
    bool has_sse2 = false, has_avx2 = false, has_avx512 = false;
    get_cpu_features(&has_sse2, &has_avx2, &has_avx512);

    if (has_avx512) {
        current_memmem_impl = avx512_memmem;
    } else if (has_avx2) {
        current_memmem_impl = avx2_memmem;
    } else if (has_sse2) {
        current_memmem_impl = sse2_memmem;
    } else {
        current_memmem_impl = fallback_memmem;
    }

    return current_memmem_impl(haystack, haystack_len, needle, needle_len);
}

EXPORT void *aob_memmem(const void* haystack, size_t haystack_len, const void* needle, size_t needle_len)
{
    return current_memmem_impl(haystack, haystack_len, needle, needle_len);
}