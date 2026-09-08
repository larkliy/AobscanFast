/* solid.c The solid matcher for AobscanFast.
    Copyright(C) 2026 Alexander Silaev <thebinaryblob@gmail.com>
    This file made for AobscanFast on 17 August 2026.
    solid is under MIT License.
    The AobscanFast is project by larkliy.
*/

#include "aob.h"
#include <string.h>
#if defined(AOB_OS_LINUX_WITH_GLIBC)
#include <sys/syscall.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#endif
EXPORT ptrdiff_t nativec_scan_solid(
    const uint8_t *buf, size_t buf_len,
    const uint8_t *patt, size_t patt_len,
    ptrdiff_t *out_results, size_t max_results)
{
    if (patt_len > buf_len || patt_len == 0) return 0;

    size_t found = 0;
    const uint8_t *cur_pos = buf;
    size_t remaining = buf_len;

    while (remaining >= patt_len)
    {
        const uint8_t *hit = (const uint8_t*)aob_memmem(cur_pos, remaining, patt, patt_len);

        if (!hit) break; // если совпадений нет.

        // вычесляем отступ от начала буфера
        ptrdiff_t offset = hit - buf;
        out_results[found++] = offset;

        if (max_results > 0 && found >= max_results) break;

        // сдвигаем указатель вперед на 1 байт от места находки для продолжения поиска
        cur_pos = hit + 1;
        remaining = buf_len - (cur_pos - buf);
    }
    return (ptrdiff_t)found;
}
/* Если жизнь одаривает вас лимонами - не делайте лимонад. 
    Заставьте жизнь забрать их обратно! Разозлитесь,
    МНЕ НЕ НУЖНЫ ТВОИ ПРОКЛЯТЫЕ ЛИМОНЫ, ЧТО МНЕ С НИМИ ДЕЛАТЬ?! 
    Требуйте встречи с менеджером, отвечающим за жизнь.
    Заставьте жизнь пожалеть о том дне, когда она решила одарить Кейва Джонсона лимонами! 
    Вы знаете, кто я? Я тот, кто сожжёт ваш дом! Я заставлю своих инженеров изобрести зажигательный лимон,
    чтобы спалить ваш дом до тла!
*/

static inline int is_address_readable(const void *addr) {
    if (!addr) return 0;
#if defined(AOB_OS_LINUX_WITH_GLIBC)
    static int null_fd = -1;
    if (null_fd == -1) {
        null_fd = open("/dev/null", O_WRONLY);
        if (null_fd == -1) return 1;
    }
    
    ssize_t result = write(null_fd, addr, 1);
    if (result < 0 && errno == EFAULT) {
        return 0;
    }
    return 1; // Память валидна
#elif defined(AOB_OS_WINDOWS)
    MEMORY_BASIC_INFORMATION mbi;
    if (VirtualQuery(addr, &mbi, sizeof(mbi))) {
        if (mbi.State == MEM_COMMIT && !(mbi.Protect & (PAGE_NOACCESS | PAGE_GUARD))) {
            return 1;
        }
    }
    return 0;
#else
    return 1;
#endif
}
EXPORT ptrdiff_t nativec_scan_batch_solid(
    const AobRegionTask *tasks, size_t task_count,
    const uint8_t *patt, size_t patt_len,
    uintptr_t *out_absolute_addresses, size_t max_results)
{
    if (patt_len == 0 || task_count == 0 || tasks == NULL) return 0;
    size_t found = 0;

    for (size_t t = 0; t < task_count; t++)
    {
        const uint8_t *buf = tasks[t].base_address;
        size_t buflen = tasks[t].size;

        if (patt_len > buflen) continue;
        //if (!is_address_readable(buf) || !is_address_readable(buf + buflen - 1)) 
          //  continue;

        const uint8_t *cur_pos = buf;
        size_t remaining = buflen;

        while (remaining >= patt_len)
        {
            const uint8_t *hit = (const uint8_t *)aob_memmem(cur_pos, remaining, patt, patt_len);
            if (!hit) break;

            out_absolute_addresses[found++] = (uintptr_t)hit;

            if (max_results > 0 && found >= max_results) return (ptrdiff_t)found;

            cur_pos = hit + 1;
            remaining = buflen - (cur_pos - buf);
        }
    }
    return (ptrdiff_t)found;
}