#include <gtest/gtest.h>
#include <vector>
#include <cstdint>

extern "C" {
#include "../aobinc/aob.h"
}

TEST(AobInternalTest, IsMatchFallback) {
    uint8_t data[]    = {0x11, 0x22, 0x33, 0x44, 0x55};
    uint8_t pattern[] = {0x11, 0x00, 0x33, 0x44, 0x00};
    uint8_t mask[]    = {0xFF, 0x00, 0xFF, 0xFF, 0x00}; // 0x00 = wildcard

    // Совпадение с маской
    EXPECT_TRUE(is_match_fallback(data, pattern, mask, 5));

    // Несовпадение в точной части
    data[0] = 0x99;
    EXPECT_FALSE(is_match_fallback(data, pattern, mask, 5));
}

// ==========================================
// 2. Тесты функции nativec_scan_mask
// ==========================================

TEST(AobScanMaskTest, BasicAndMultipleMatches) {
    std::vector<uint8_t> buffer = {
        0xAA, 0xBB, 0xCC, 0xDD, 0x00,
        0x11, 0x22, 0x33, 0x44, 0x55,
        0xAA, 0xFF, 0xCC, 0xDD, 0x99
    };

    uint8_t patt[] = {0xAA, 0x00, 0xCC, 0xDD};
    uint8_t mask[] = {0xFF, 0x00, 0xFF, 0xFF};

    ptrdiff_t results[10];
    ptrdiff_t count = nativec_scan_mask(buffer.data(), buffer.size(), patt, mask, 4, results, 10);

    ASSERT_EQ(count, 2);
    EXPECT_EQ(results[0], 0);  // Первое совпадение на 0
    EXPECT_EQ(results[1], 10); // Второе совпадение на 10
}

TEST(AobScanMaskTest, MaxResultsLimit) {
    std::vector<uint8_t> buffer(100, 0xAA); // Буфер с повторяющимися байтами
    uint8_t patt[] = {0xAA, 0xAA};
    uint8_t mask[] = {0xFF, 0xFF};

    ptrdiff_t results[2];
    ptrdiff_t count = nativec_scan_mask(buffer.data(), buffer.size(), patt, mask, 2, results, 2);

    EXPECT_EQ(count, 2); // Не должен превысить max_results
}

// ==========================================
// 3. Тесты функции nativec_scan_solid
// ==========================================

TEST(AobScanSolidTest, ExactMatchAndNotFound) {
    std::vector<uint8_t> buffer = {0x00, 0x11, 0x22, 0x33, 0x44, 0x11, 0x22, 0x33, 0x55};
    uint8_t patt[] = {0x11, 0x22, 0x33};

    ptrdiff_t results[5];
    ptrdiff_t count = nativec_scan_solid(buffer.data(), buffer.size(), patt, 3, results, 5);

    ASSERT_EQ(count, 2);
    EXPECT_EQ(results[0], 1);
    EXPECT_EQ(results[1], 5);

    uint8_t not_found_patt[] = {0x99, 0x88};
    count = nativec_scan_solid(buffer.data(), buffer.size(), not_found_patt, 2, results, 5);
    EXPECT_EQ(count, 0);
}

// ==========================================
// 4. Тесты аналога memmem (aob_memmem)
// ==========================================

TEST(AobMemMemTest, FindSubstring) {
    const char* haystack = "Hello, AobscanFast world!";
    const char* needle = "AobscanFast";

    void* res = aob_memmem(haystack, strlen(haystack), needle, strlen(needle));
    ASSERT_NE(res, nullptr);
    EXPECT_EQ(static_cast<const char*>(res), haystack + 7);

    const char* invalid_needle = "UnknownPattern";
    res = aob_memmem(haystack, strlen(haystack), invalid_needle, strlen(invalid_needle));
    EXPECT_EQ(res, nullptr);
}

// ==========================================
// 5. Тесты пакета регионов (nativec_scan_batch_solid)
// ==========================================

TEST(AobBatchSolidTest, MultipleRegions) {
    std::vector<uint8_t> region1 = {0x00, 0x11, 0x22, 0x00};
    std::vector<uint8_t> region2 = {0xAA, 0xBB, 0xCC, 0xDD};
    std::vector<uint8_t> region3 = {0x11, 0x22, 0xFF, 0xFF};

    AobRegionTask tasks[] = {
        {region1.data(), region1.size()},
        {region2.data(), region2.size()},
        {region3.data(), region3.size()}
    };

    uint8_t patt[] = {0x11, 0x22};
    uintptr_t absolute_results[5];

    ptrdiff_t count = nativec_scan_batch_solid(tasks, 3, patt, 2, absolute_results, 5);

    ASSERT_EQ(count, 2);
    // Проверяем, что вернулись корректные абсолютные указатели
    EXPECT_EQ(absolute_results[0], reinterpret_cast<uintptr_t>(region1.data() + 1));
    EXPECT_EQ(absolute_results[1], reinterpret_cast<uintptr_t>(region3.data() + 0));
}

// ==========================================
// 6. Тесты CPUID и определения фичей CPU
// ==========================================

TEST(AobCpuTest, GetFeatures) {
    bool sse2 = false, avx2 = false, avx512 = false;
    get_cpu_features(&sse2, &avx2, &avx512);

    // В современных системах, запускающих gtest, SSE2 почти наверняка поддеживается
#if defined(__x86_64__) || defined(_M_X64)
    EXPECT_TRUE(sse2);
#endif
}

int main(int argc, char **argv) {
    ::testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}