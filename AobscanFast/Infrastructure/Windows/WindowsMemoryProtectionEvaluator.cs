using AobscanFast.Core.Models;
using Windows.Win32.System.Memory;

using static Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS;
using static Windows.Win32.System.Memory.VIRTUAL_ALLOCATION_TYPE;

namespace AobscanFast.Infrastructure.Windows;

internal static class WindowsMemoryProtectionEvaluator
{
    public static bool IsScannable(in MEMORY_BASIC_INFORMATION memoryInfo, MemoryAccess requestedAccess)
    {
        bool isCommitted = memoryInfo.State == MEM_COMMIT;
        bool isGuarded = (memoryInfo.Protect & PAGE_GUARD) != 0;
        bool isNoAccess = (memoryInfo.Protect & PAGE_NOACCESS) != 0;

        if (!isCommitted || isGuarded || isNoAccess)
            return false;

        if ((requestedAccess & MemoryAccess.Readable) != 0 && !memoryInfo.IsReadableRegion())
            return false;

        if ((requestedAccess & MemoryAccess.Writable) != 0 && !memoryInfo.IsWritableRegion())
            return false;

        if ((requestedAccess & MemoryAccess.Executable) != 0 && !memoryInfo.IsExecutableRegion())
            return false;

        return true;
    }
}
