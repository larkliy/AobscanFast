/* aob.h The C header for AobscanFast.
    Copyright(C) 2026 Alexander Silaev <thebinaryblob@gmail.com>
    This file made for AobscanFast on 17 August 2026.
    aob.h is under MIT License.
    The AobscanFast is project by larkliy.
*/

#ifndef AOBSCANFAST_NATIVEC_H
#define AOBSCANFAST_NATIVEC_H

#if defined(_WIN32)|| defined(_WIN64) || defined(_MSC_VER)
    #include <windows.h>
    #include <intrin.h>
    #define EXPORT __declspec(dllexport)
    #define AOB_OS_WINDOWS 1
#elif defined(__linux__) && defined(_GNU_SOURCE) || defined(__clang__) || defined(__GNUC__)
    #define _GNU_SOURCE
    #include <cpuid.h>
    #define AOB_OS_LINUX_WITH_GLIBC 1
    // для компиляторов GCC/Clang
    #define EXPORT __attribute__((visibility("default")))
#endif

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>
#include <immintrin.h>

/* Фолбэк для старых процессоров неподдерживающих AVX2 */
static inline int is_match_fallback(const uint8_t *data, const uint8_t *pattern, const uint8_t *mask, size_t len)
{
    for (size_t i = 0; i < len; i++)
        if (mask[i] && (data[i] & mask[i]) != pattern[i]) return 0;
    return 1;
}

#if defined(__AVX2__) || defined(_M_AMD64)
/* Проверка, включен ли AVX2 компилятором*/
static inline int is_match_avx2(const uint8_t *data, const uint8_t *pattern, const uint8_t *mask, size_t len)
{
    size_t i;

    for (; i + 32 <= len; i += 32)
    {
        __m256i v_data = _mm256_loadu_si256((const __m256i*)(data + i));
        __m256i v_patt = _mm256_loadu_si256((const __m256i*)(pattern + i));
        __m256i v_mask = _mm256_loadu_si256((const __m256i*)(mask + i));

        // применяем маску: (data & mask)
        __m256i v_maskd_data = _mm256_and_si256(v_data, v_mask);

        // сравниваем результат с паттерном: v_maskd_data == v_patt
        __m256i v_cmp = _mm256_cmpeq_epi8(v_maskd_data, v_patt);

        // получаем битовую маску результатов сравнения
        int mov_mask = _mm256_movemask_epi8(v_cmp);

        // если один байт из 32 не совпал, mov_mask не будет равен 0xFFFFFFFF
        if ((uint32_t)mov_mask != 0xFFFFFFFF)
            return 0;
    }

    // добираем оставшиеся байты (если длина паттерна не кратна 32)
    for (; i < len; i++) {
        if ((mask[i] && data[i] & mask[i]) != pattern[i]) return 0;
    }
    return 1;
}
#endif

static inline void get_cpuid(int info[4], int type)
{
#if defined(AOB_OS_WINDOWS)
    __cpuid(info, type);
#elif defined(AOB_OS_LINUX_WITH_GLIBC)
    __cpuid_count(type, 0, info[0], info[1], info[2], info[3]);
#endif
}

static inline void get_cpu_features(bool *has_sse2, bool *has_avx2, bool *has_avx512)
{
    int cpuInfo[4] = {0};
#if defined(AOB_OS_WINDOWS)
    __cpuid(cpuInfo, 1);
    *has_sse2 = (cpuInfo[3] & (1 << 26)) != 0;
    __cpuidex(cpuInfo, 7, 0);
    *has_avx2 = (cpuInfo[1] & (1 << 5)) != 0;
    *has_avx512 = (cpuInfo[1] & (1 << 16)) != 0;
#elif defined(AOB_OS_LINUX_WITH_GLIBC)
    unsigned int eax, ebx, ecx, edx;
    if (__get_cpuid(1, &eax, &ebx, &ecx, &edx))
        *has_sse2 = (edx & (1 << 26)) != 0;

    if (__get_cpuid_count(7, 0, &eax, &ebx, &ecx, &edx))
    {
        *has_avx2 = (ebx & (1 << 5)) != 0;
        *has_avx512 = (ebx & (1 << 16)) != 0;
    }
#endif
}

static inline uint32_t find_first_bit32(uint32_t mask)
{
#if defined(AOB_OS_WINDOWS)
    unsigned long index;
    _BitScanForward(&index, mask);
    return (uint32_t)index;
#elif defined(AOB_OS_LINUX_WITH_GLIBC)
    return (uint32_t)__builtin_ctz(mask);
#endif
}

static inline uint32_t find_first_bit64(uint64_t mask)
{
#if defined(AOB_OS_WINDOWS)
    unsigned long index;
    _BitScanForward64(&index, mask);
    return (uint32_t)index;
#elif defined(AOB_OS_LINUX_WITH_GLIBC)
    return (uint32_t)__builtin_ctzll(mask);
#endif
}

typedef struct {
    const uint8_t *base_address;
    size_t size;
} AobRegionTask;

/*=== Функции ===*/
/**
 * @brief Выполняет поиск сигнатур с использованием маски (вайлдкардов).
 *
 * Функция сканирует переданный буфер памяти, применяя битовую маску к каждому байту,
 * и сравнивает результат с целевым паттерном. Поддерживает динамическое SIMD-ускорение.
 *
 * @param[in]  buf           Указатель на сканируемый массив байт.
 * @param[in]  buf_len       Общий размер сканируемого буфера в байтах.
 * @param[in]  patt          Массив целевых байт, которые нужно найти.
 * @param[in]  mask          Массив битовых масок (Используйте 0x00 для '??', 0xFF для точного байта).
 * @param[in]  patt_len      Длина сигнатуры (размер паттерна и маски).
 * @param[out] out_results   Массив для записи найденных отступов (ну или смещений).
 * @param[in]  max_results   Максимальное количество результатов (0 — поиск до конца буфера).
 *
 * @return Количество успешно найденных совпадений, 0 при ошибке в аргументах.
 */
EXPORT ptrdiff_t nativec_scan_mask(
    const uint8_t *buf, size_t buf_len,
    const uint8_t *patt, const uint8_t *mask, size_t patt_len,
    ptrdiff_t *out_results, size_t max_results);

/**
 * @brief Выполняет быстрый поиск сплошных сигнатур (без маски).
 *
 * Использует кастомный векторный Runtime Dispatch движок для поиска точной подстроки.
 *
 * @param[in]  buf           Указатель на сканируемый массив байт.
 * @param[in]  buf_len       Общий размер сканируемого буфера в байтах.
 * @param[in]  patt          Массив байт точного паттерна, который нужно найти.
 * @param[in]  patt_len      Длина паттерна в байтах.
 * @param[out] out_results   Массив для записи найденных смещений (офсетов).
 * @param[in]  max_results   Максимальное количество результатов (0 — без лимита).
 *
 * @return Количество успешно найденных совпадений.
 */
EXPORT ptrdiff_t nativec_scan_solid(
    const uint8_t *buf, size_t buf_len,
    const uint8_t *patt, size_t patt_len,
    ptrdiff_t *out_results, size_t max_results);
/**
 * @brief Замена glibc'шному memmem.
 *
 * Выполняет поиск подстроки (needle) в реальном времени в блоке памяти (haystack).
 * При первом вызове автоматически определяет архитектуру процессора через CPUID
 * и переключает глобальный указатель на наиболее быстрый векторный движок.
 *
 * @param[in] haystack        Указатель на исследуемую область памяти.
 * @param[in] haystack_len    Размер исследуемой области памяти в байтах(size_t).
 * @param[in] needle          Указатель на искомую последовательность байт.
 * @param[in] needle_len      Размер искомой последовательнойсти байт.
 *
 * @return Указатель на первое совпадение в памяти или NULL, если ничего не найдено.
*/
EXPORT void *aob_memmem(const void* haystack, size_t haystack_len, const void* needle, size_t needle_len);

/**
 * @brief Выполняет быстрый поиск сплошных сигнатур (без маски), но принимает структуру AobRegionTask.
 *
 * Использует кастомный векторный Runtime Dispatch движок для поиска точной подстроки.
 * А также 
 *
 * @param[in] haystack        Указатель на исследуемую область памяти.
 * @param[in] haystack_len    Размер исследуемой области памяти в байтах(size_t).
 * @param[in] needle          Указатель на искомую последовательность байт.
 * @param[in] needle_len      Размер искомой последовательнойсти байт.
 *
 * @return Указатель на первое совпадение в памяти или NULL, если ничего не найдено.
*/
EXPORT ptrdiff_t nativec_scan_batch_solid(
    const AobRegionTask *tasks, size_t task_count,
    const uint8_t *patt, size_t patt_len,
    uintptr_t *out_absolute_addresses, size_t max_results);

#endif /* AOBSCANFAST_NATIVEC_H */