using System;

namespace AsioAudioEngine;

internal static class Diagnostics
{
    public static Action<string>? Logger;

    public static void Log(string message)
    {
        try
        {
            string ts = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            Logger?.Invoke($"[{ts}] {message}");
        }
        catch { }
    }
}
