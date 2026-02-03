using SCENetCore;

namespace SCENetGame;

internal static class Program
{
    private const string LogPath = @"C:\Users\redst\source\repos\SCENetGame\logs\";

    private static void Main()
    {
        Console.Title = "SCENetGame";

        using FileStream stream = Logging.CreateLogFile(LogPath);
        if (stream == null) return;

        Logging.Initialize(stream);

        new Game().Run();
    }
}