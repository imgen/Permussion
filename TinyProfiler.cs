using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace Permussion;

public static class TinyProfiler
{
    public static T Profile<T>(string operationName, Func<T> func, Action<TimeSpan, string> printer = null)
    {
        var watch = new Stopwatch();
        watch.Start();
        try
        {
            var result = func();
            PrintProfileMessage(watch, operationName, printer);
            return result;
        }
        catch
        {
            PrintProfileMessage(watch, operationName, printer);
            throw;
        }
    }

    public static void Profile(string operationName, Action action, Action<TimeSpan, string> printer = null) =>
        Profile<object>(operationName, () =>
        {
            action();
            return null;
        }, printer);

    public static async Task<T> ProfileAsync<T>(string operationName, Func<ValueTask<T>> func, Action<TimeSpan, string> printer = null)
    {
        var watch = new Stopwatch();
        watch.Start();
        try
        {
            var result = await func();
            PrintProfileMessage(watch, operationName, printer);
            return result;
        }
        catch
        {
            PrintProfileMessage(watch, operationName, printer);
            throw;
        }
    }

    public static async Task<T> ProfileAsync<T>(string operationName, Func<ConfiguredTaskAwaitable<T>> func, Action<TimeSpan, string> printer = null)
    {
        var watch = new Stopwatch();
        watch.Start();
        try
        {
            var result = await func();
            PrintProfileMessage(watch, operationName, printer);
            return result;
        }
        catch
        {
            PrintProfileMessage(watch, operationName, printer);
            throw;
        }
    }

    public static Task ProfileAsync(string operationName, Func<Task> func, Action<TimeSpan, string> printer = null) =>
        ProfileAsync<object>(operationName,
            async () =>
            {
                await func();
                return null;
            }, printer);

    public static Task ProfileAsync(string operationName, Func<ConfiguredTaskAwaitable> func, Action<TimeSpan, string> printer = null) =>
        ProfileAsync<object>(operationName,
            async () =>
            {
                await func();
                return null;
            }, printer);

    private static void PrintProfileMessage(Stopwatch watch, string operationName, Action<TimeSpan, string> printer = null)
    {
        watch.Stop();
        var isDefault = printer is null;
        printer ??= (_, message) => Console.WriteLine(message);
        var oldForeColor = Console.ForegroundColor;
        var duration = FormatTimeSpan(watch.Elapsed);
        var message = $"\tThe operation {operationName} took {duration}";
        var newLine = Environment.NewLine;
        if (isDefault) 
            Console.ForegroundColor = ConsoleColor.Yellow;
        printer(watch.Elapsed, newLine + newLine + "PROFILING MESSAGE START" + newLine + message + newLine + "PROFILING MESSAGE END" + newLine + newLine);
        if (isDefault) 
            Console.ForegroundColor = oldForeColor;
    }

    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        var (minutes, seconds, milliseconds, microseconds) = (
            (int)timeSpan.TotalMinutes,
            timeSpan.Seconds,
            timeSpan.Milliseconds,
            int.Parse(timeSpan.ToString("ffffff")[3..])
        );
        return $"{minutes}m {seconds}s {milliseconds}ms {microseconds}μs";
    }
}