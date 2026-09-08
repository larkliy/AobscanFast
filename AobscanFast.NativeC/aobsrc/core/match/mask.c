/* solid.c The mask matcher for AobscanFast.
    Copyright(C) 2026 Alexander Silaev <thebinaryblob@gmail.com>
    This file made for AobscanFast on 17 August 2026.
    solid is under MIT License.
    The AobscanFast is project by larkliy.
*/
#include "aob.h"
#include <string.h>

typedef int (*mask_match_fn)(const uint8_t*, const uint8_t*, const uint8_t*, size_t);
static int resolve_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len);
static mask_match_fn current_mask_match_impl = resolve_mask_match;

static int fallback_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len)
{
    for (size_t i = 0; i < len; i++)
        if (mask[i] && (data[i] & mask[i]) != patt[i]) return 0;
    return 1;
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("sse2")
#endif
static int sse2_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len)
{
    size_t i = 0;
    for (; i + 16 <= len; i += 16)
    {
        __m128i v_data = _mm_loadu_si128 ((const __m128i *)(data + i));
        __m128i v_patt = _mm_loadu_si128 ((const __m128i *)(patt + i));
        __m128i v_mask = _mm_loadu_si128 ((const __m128i *)(mask + i));

        __m128i v_maskd_data = _mm_and_si128 (v_mask, v_data);
        __m128i v_cmp = _mm_cmpeq_epi8 (v_maskd_data, v_patt);
        uint32_t mov_mask = (uint32_t)_mm_movemask_epi8 (v_cmp);

        if (mov_mask != 0xFFFF) return 0;
    }
    for (; i < len; i++)
        if (mask[i] && (data[i] & mask[i]) != patt[i]) return 0;
    return 1;
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif

#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("avx2")
#endif
static int avx2_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len)
{
    size_t i = 0;
    for (; i + 32 <= len; i += 32)
    {
        __m256i v_data    = _mm256_loadu_si256((const __m256i*)(data + i));
        __m256i v_patt = _mm256_loadu_si256((const __m256i*)(patt + i));
        __m256i v_mask    = _mm256_loadu_si256((const __m256i*)(mask + i));

        __m256i v_maskd_data = _mm256_and_si256(v_data, v_mask);
        __m256i v_cmp         = _mm256_cmpeq_epi8(v_maskd_data, v_patt);
        uint32_t mov_mask    = (uint32_t)_mm256_movemask_epi8(v_cmp);

        if (mov_mask != 0xFFFFFFFF)
            return 0;
    }
    // Если паттерн длиннее 32 байт, остаток сканируем через SSE2
    if (i < len)
        return sse2_mask_match(data + i, patt + i, mask + i, len - i);

    return 1;
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif


#if !defined(AOB_OS_WINDOWS)
#pragma GCC push_options
#pragma GCC target("avx512f,avx512bw")
#endif
static int avx512_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len)
{
    size_t i = 0;
    for (; i + 64 <= len; i += 64)
    {
        __m512i v_data    = _mm512_loadu_si512((const __m512i*)(data + i));
        __m512i v_patt = _mm512_loadu_si512((const __m512i*)(patt + i));
        __m512i v_mask    = _mm512_loadu_si512((const __m512i*)(mask + i));

        __m512i v_maskd_data = _mm512_and_si512(v_data, v_mask);

        // В AVX-512 сравнение сразу возвращает битовую маску __mmask64
        __mmask64 cmp_mask = _mm512_cmpeq_epi8_mask(v_maskd_data, v_patt);

        if (cmp_mask != 0xFFFFFFFFFFFFFFFFULL)
            return 0;
    }
    // Остаток добираем через AVX2
    if (i < len)
        return avx2_mask_match(data + i, patt + i, mask + i, len - i);
    return 1;
}
#if !defined(AOB_OS_WINDOWS)
#pragma GCC pop_options
#endif

static int resolve_mask_match(const uint8_t *data, const uint8_t *patt, const uint8_t *mask, size_t len)
{
    if (!data) return 0;
    bool has_sse2 = false, has_avx2 = false, has_avx512 = false;
    get_cpu_features(&has_sse2, &has_avx2, &has_avx512);

    if (has_avx512)
        current_mask_match_impl = avx512_mask_match;
    else if (has_avx2)
        current_mask_match_impl = avx2_mask_match;
    else if (has_sse2)
        current_mask_match_impl = sse2_mask_match;
    else
        current_mask_match_impl = fallback_mask_match;

    return current_mask_match_impl(data, patt, mask, len);
}

EXPORT ptrdiff_t nativec_scan_mask(
    const uint8_t *buf, size_t buf_len,
    const uint8_t *patt, const uint8_t *mask, size_t patt_len,
    ptrdiff_t *out_results, size_t max_results)
{
    if (patt_len > buf_len || patt_len == 0) return 0;

    if (current_mask_match_impl == resolve_mask_match)
    {
        bool s, a, a5;
        get_cpu_features(&s, &a, &a5);
        if (a5) current_mask_match_impl = avx512_mask_match;
        else if (a) current_mask_match_impl = avx2_mask_match;
        else if (s) current_mask_match_impl = sse2_mask_match;
        else current_mask_match_impl = fallback_mask_match;
    }

    mask_match_fn match_impl = current_mask_match_impl;
    size_t found_count = 0;

    const uint8_t *curptr = buf;
    const uint8_t *endptr = buf + buf_len - patt_len;

    if (mask[0] == 0xFF)
    {
        uint8_t target_byte = patt[0];
        while (curptr <= endptr)
        {
            curptr = (const uint8_t*)memchr(curptr, target_byte, (size_t)(endptr - curptr + 1));
            if (!curptr) break;

            if (match_impl(curptr, patt, mask, patt_len))
            {
                out_results[found_count++] = (ptrdiff_t)(curptr - buf);
                if (max_results > 0 && found_count >= max_results) break;
            }
            curptr++;
        }
    } else 
    {
        size_t scan_limit = buf_len - patt_len;
        for (size_t i = 0; i <= scan_limit; i++)
        {
            if (mask[0] && (buf[i] & mask[0]) != patt[0]) continue;

            if (match_impl(buf + i, patt, mask, patt_len))
            {
                out_results[found_count++] = (ptrdiff_t)i;
                if (max_results > 0 && found_count >= max_results) break;
            }
        }
    }

    return (ptrdiff_t)found_count;
}