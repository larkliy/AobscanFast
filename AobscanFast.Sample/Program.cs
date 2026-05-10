using AobscanFast.Infrastructure.Windows;
using AobscanFast.Services;
using System.Diagnostics;

Console.WriteLine($"Hello");

var processHandler = new WinProcessHandler();
var processId = processHandler.FindIdByName("notepad.exe");

if (processId == null)
{
    Console.WriteLine($"Process ID is not found.");
    return;
}

using var handle = processHandler.OpenProcess(processId.Value);
var regionEnumerator = new RemoteProcessRegionEnumerator(handle);
var memoryAccessor = new RemoteProcessMemoryAccessor(handle);
var scanner = new AobScanner(processHandler, regionEnumerator, memoryAccessor);

string pattern = "16 00 00 00 00";
int iterations = 500;

Console.WriteLine("Подготовка к сканированию...");

var results = scanner.Scan(pattern);
var firstResult = scanner.ScanFirst(pattern);
Console.WriteLine($"Первое совпадение: {(firstResult is null ? "не найдено" : $"0x{firstResult.Value:X}")}");

Console.WriteLine($"Начинаем сканирование ({iterations} итераций)...");

var stopwatch = new Stopwatch();
stopwatch.Start();

for (int i = 0; i < iterations; i++)
{
    results = scanner.Scan(pattern);
}

stopwatch.Stop();

double totalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
double averageTimeMs = totalTimeMs / iterations;

Console.WriteLine("======================================");
Console.WriteLine($"Всего найдено: {results.Count}");
Console.WriteLine($"Общее время ({iterations} раз): {totalTimeMs:F2} мс");
Console.WriteLine($"Усредненное время 1 скана: {averageTimeMs:F4} мс");
Console.WriteLine("======================================\n");

Console.WriteLine("Первые 10 адресов:");
foreach (nint result in results.Take(10))
{
    Console.WriteLine($"Address: 0x{result:X2}");
}

Console.WriteLine();
