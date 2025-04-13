// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using DiscordRPC;
using WavesRP;
const string ARTIST_SONG_DELIMITER = " - ";

async Task main()
{
    Process? tidalProcess = null;
    ListeningData listeningData = new();
    DiscordClient discord = new("1248068953184534549");

    while (true)
    {
        //Check for active TIDAL process
        if (!GetTIDALProcess(ref tidalProcess))
        {
            await Task.Delay(500);
            continue;
        }
        //Check if old process is still running
        if (tidalProcess.HasExited)
        {
            tidalProcess = null;
            discord.ClearPresence();
            discord.DisconnectRP();
            continue;
        }
        //Connect RP if not connected
        if (!discord.IsRPConnected) discord.ConnectRP();

        //------------------------Customizing Presence--------------------------------------
        listeningData = ListeningData.FromWindowTitle(tidalProcess.MainWindowTitle, ARTIST_SONG_DELIMITER);
        discord.SetPresence(new RichPresence()
        {
            Type = ActivityType.Listening,
            //TODO These values should be set from the ListeningData class. Splitting on the delimiter is naive
            State = tidalProcess.MainWindowTitle.Split(ARTIST_SONG_DELIMITER)[1],
            Details = tidalProcess.MainWindowTitle.Split(ARTIST_SONG_DELIMITER)[0],
            Timestamps = new Timestamps()
            {
                Start = DateTimeOffset.UtcNow.UtcDateTime,
                End = DateTimeOffset.UtcNow.AddMinutes(3).UtcDateTime
            }
        });
        await Task.Delay(1000);
    }
}
await main();

bool GetTIDALProcess(ref Process process)
{
    Process[] pc = Process.GetProcessesByName("TIDAL");
    foreach (Process p in pc)
    {
        string windowName = p.MainWindowTitle;
        if (!windowName.Contains(ARTIST_SONG_DELIMITER)) continue;
        process = p;
        return true;
    }
    return false;
}