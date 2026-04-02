using AobscanFast.Infrastructure.Windows;
using AobscanFast.Services;
using System.Diagnostics;

var processHandler = new WinProcessHandler();
var processId = processHandler.FindIdByName("HD-Player");

if (processId == null)
{
    Console.WriteLine($"Process ID is not found.");
    return;
}

using var handle = processHandler.OpenProcess(processId.Value);
var reader = new WinMemoryReader(handle);
var aobscanner = new AobScanner(processHandler, reader);

string pattern = "17 00 00";
int iterations = 10;

Console.WriteLine("Подготовка к сканированию...");

var results = aobscanner.Scan(pattern);

Console.WriteLine($"Начинаем сканирование ({iterations} итераций)...");

var stopwatch = new Stopwatch();
stopwatch.Start();

for (int i = 0; i < iterations; i++)
{
    results = aobscanner.Scan(pattern);
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