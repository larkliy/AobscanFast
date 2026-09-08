/* NativeC Library Export for AobscanFast, Core/Matching
    Copyright(C) 2026 Alexander Silaev <thebinaryblob@gmail.com>
    This file made for AobscanFast on 17 August 2026.
    solid is under MIT License.
    The AobscanFast is project by larkliy.
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AobscanFast.Core.Interfaces;
using AobscanFast.Core.Models;
using AobscanFast.Core.Models.Pattern;

namespace AobscanFast.Core.Matching;

internal sealed partial class NativeSolidMatcher : IPatternMatcher
{
    private const string LibName = "AobscanFastNativeC";

    [LibraryImport(LibName, EntryPoint = "nativec_scan_solid")]
    private static unsafe partial nint NativeScanSolid(
        byte *buf, nint bufLen,
        byte *patt, nint pattLen,
        nint *outResults, nint maxResults
    );

    public unsafe void ScanChunk(in MemoryRange range, AobPattern pattern, List<nint> results, ReadOnlySpan<byte> buffer, int maxResults = 0)
    {
        if (buffer.Length < pattern.Length) return;

        int maxAllocation = maxResults > 0 ? maxResults : 65536;
        nint[] offsetsBuffer = new nint[maxAllocation];

        fixed (byte *pBuffer = buffer)
        fixed (byte *pPattern = pattern.BytesSpan)
        fixed (nint *pOffsets = offsetsBuffer)
        {
            nint foundCount = NativeScanSolid(
                pBuffer, buffer.Length,
                pPattern, pattern.Length,
                pOffsets, maxAllocation
            );

            for (int i = 0; i < foundCount; i++)
                results.Add(range.BaseAddress + offsetsBuffer[i]);
        }
    }

    public unsafe void ScanMemoryDirect(in MemoryRange range, AobPattern pattern, List<nint> results, int maxResults = 0)
    {
        var buffer = new ReadOnlySpan<byte>((void*)range.BaseAddress, (int)range.Size);
        ScanChunk(range, pattern, results, buffer, maxResults);
    }
}