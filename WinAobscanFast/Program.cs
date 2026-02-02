using WinAobscanFast.Core;

var results = AobScan.ScanProcess("Godot_v4.6-stable_mono_win64.exe", "11 11 22 ?? ?? 22");

Console.WriteLine("Результатов: " + results.Count);

Console.WriteLine();