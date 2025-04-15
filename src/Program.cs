// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Reflection;
using DiscordRPC;
using WavesRP;
using WavesRP.Util;

namespace WavesRp
{
    public class Program
    {
        static string ARTIST_SONG_DELIMITER = " - ";
        static async Task Main()
        {
            EnvReader.Load(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"\lib\.env");
            Process? tidalProcess = null;
            ListeningData listeningData = new();
            DiscordClient discord = DiscordClient.Instantiate(Environment.GetEnvironmentVariable("DISCORD_APP_ID") ?? "0");
            TidalHttpService tidalHttpService = new();
            Process tempProcess = null;
            DateTime pauseTime = DateTime.MinValue;

            while (true)
            {
                //Check if old process is still running
                if (tidalProcess != null && tidalProcess.HasExited)
                {
                    tidalProcess = null;
                    listeningData = new();
                    discord.ClearPresence();
                    discord.DisconnectRP();
                    discord.Dispose();
                    continue;
                }
                //Check for active TIDAL process
                if (!GetTIDALProcess(out tempProcess))
                {
                    if (tidalProcess != null && !string.Empty.Equals(listeningData.SongName))
                    {
                        Console.WriteLine("TIDAL client may be paused. Waiting for unpause...");
                        SetPausedPresence(discord, listeningData);
                        pauseTime = DateTime.Now;
                        tidalProcess = await WaitForUnpause(tidalProcess);
                        listeningData.Started = DateTime.Now - (pauseTime - listeningData.Started);
                        continue;
                    }
                    Console.WriteLine("TIDAL not running, waiting for 500ms...");
                    if (discord.IsRPConnected)
                    {
                        tidalProcess = null;
                    }
                    await Task.Delay(500);
                    continue;
                }
                tidalProcess = tempProcess;
                if (discord.IsDisposed)
                {
                    discord = DiscordClient.Instantiate(Environment.GetEnvironmentVariable("DISCORD_APP_ID") ?? "0");
                }

                //Connect RP if not connected
                if (!discord.IsRPConnected) discord.ConnectRP();

                //------------------------Customizing Presence--------------------------------------
                if (listeningData == null
                    || listeningData.SongName.Equals(string.Empty)
                    || !(tidalProcess.MainWindowTitle.StartsWith(listeningData.SongName) && tidalProcess.MainWindowTitle.Contains(listeningData.Artists.FirstOrDefault().Name)))
                {
                    listeningData = await tidalHttpService.SearchFromWindowTitle(tidalProcess.MainWindowTitle, ARTIST_SONG_DELIMITER);
                }
                if (!tidalHttpService.IsAuthorized)
                {
                    SetBasicPresence(discord, tidalProcess.MainWindowTitle);
                    goto WAIT;
                }

                SetRichPresence(discord, listeningData);


            WAIT:
                await Task.Delay(1000);
            }
        }

        static bool GetTIDALProcess(out Process process)
        {
            Process[] pc = Process.GetProcessesByName("TIDAL");
            foreach (Process p in pc)
            {
                string windowName = p.MainWindowTitle;
                if (!windowName.Contains(ARTIST_SONG_DELIMITER)) continue;
                process = p;
                return true;
            }
            process = null; ;
            return false;
        }

        static async Task<Process> WaitForUnpause(Process tidalProcess)
        {
            while (true)
            {
                if (tidalProcess.HasExited) return null;
                if (GetTIDALProcess(out var p)) return p;
                await Task.Delay(250);
            }
        }

        static void SetBasicPresence(DiscordClient discord, string mainWindowTitle)
        {
            discord.SetPresence(new RichPresence()
            {
                Type = ActivityType.Listening,
                //TODO These values should be set from the ListeningData class. Splitting on the delimiter is naive
                State = mainWindowTitle.Split(ARTIST_SONG_DELIMITER)[1],
                Details = mainWindowTitle.Split(ARTIST_SONG_DELIMITER)[0]
            });
        }
        static void SetRichPresence(DiscordClient discord, ListeningData listeningData)
        {
            if (listeningData == null) return;
            discord.SetPresence(new RichPresence()
            {
                Type = ActivityType.Listening,
                State = String.Join(", ", listeningData.Artists.Select(x => x.Name)),
                Details = listeningData.SongName,
                Timestamps = new Timestamps()
                {
                    Start = listeningData.Started.ToUniversalTime(),
                    End = listeningData.Started.Add(listeningData.Duration).ToUniversalTime()
                },
                Assets = new Assets()
                {
                    LargeImageKey = listeningData.Album.CoverUrl,
                    LargeImageText = listeningData.Album.Name,
                },
                Buttons = [
                    new Button(){
                        Label = "Listen",
                        Url = listeningData.TrackUrl
                    },
                    new Button(){
                        Label = "Check out WavesRP",
                        Url = "https://github.com/SlippStream/WavesRP"
                    },
                ]
            });
        }
        static void SetPausedPresence(DiscordClient discord, ListeningData listeningData)
        {
            discord.SetPresence(new RichPresence()
            {
                Type = ActivityType.Listening,
                State = String.Join(", ", listeningData.Artists.Select(x => x.Name)),
                Details = listeningData.SongName,
                Assets = new Assets()
                {
                    LargeImageKey = listeningData.Album.CoverUrl,
                    LargeImageText = listeningData.Album.Name,
                    SmallImageText = "Paused",
                    SmallImageKey = "pause"
                },
                //Timestamps = Timestamps.FromTimeSpan(listeningData.Duration)
            });
        }
    }
}