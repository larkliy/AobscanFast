using AobscanFast.Infrastructure.Windows;
using AobscanFast.Services;

Console.OutputEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

var handler = new WinProcessHandler();

var processId = handler.FindIdByName("notepad");

if (processId == null)
{
    Console.WriteLine($"Process ID is not found.");
    return;
}

using var handle = handler.OpenProcess(processId.Value);

var reader = new WinMemoryReader(handle);

var aobscanner = new AobScanner(reader);

var results = aobscanner.Scan("02 00 00 00 02 00 00 00 20 15");

Console.WriteLine($"Результатов: {results.Count}");

foreach (nint result in results.Take(10))
{
    Console.WriteLine($"Address: 0x{result:X2}");
}

Console.WriteLine();