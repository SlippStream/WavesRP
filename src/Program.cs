// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Reflection;
using DiscordRPC;
using WavesRP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WavesRp
{
    public class Program
    {
        private static readonly Dictionary<string, string> _secretCache = new Dictionary<string, string>();
        static string ARTIST_SONG_DELIMITER = " - ";
        static async Task Main(string[] args)
        {
            //try { GetSecrets(); } catch (Exception ex) { Console.WriteLine("Cannot get secrets! Exiting..."); Environment.Exit(1); }
            AppSettings appSettings = GetCurrentSettings();
            Process? tidalProcess = null;
            ListeningData listeningData = new();
            DiscordClient discord = DiscordClient.Instantiate(appSettings.DiscordAppId ?? "0");
            TidalHttpService tidalHttpService = new(appSettings.TidalClientId, appSettings.TidalClientSecret);

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
                if (!GetTIDALProcess(out Process tempProcess))
                {
                    if (tidalProcess != null && !string.Empty.Equals(listeningData.SongName))
                    {
                        Console.WriteLine("TIDAL client may be paused. Waiting for unpause...");
                        SetPausedPresence(discord, listeningData);
                        DateTime pauseTime = DateTime.Now;
                        tidalProcess = await WaitForUnpause(tidalProcess);
                        listeningData.Started = DateTime.Now - (pauseTime - listeningData.Started);
                        continue;
                    }
                    Console.WriteLine("TIDAL not running, waiting for 500ms...");
                    if (discord.IsRPConnected)
                    {
                        tidalProcess = null;
                        discord.ClearPresence();
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

                //Call TIDAL's API if we haven't for this song.
                if (listeningData == null
                    || listeningData.SongName.Equals(string.Empty)
                    || !(tidalProcess.MainWindowTitle.StartsWith(listeningData.SongName) && tidalProcess.MainWindowTitle.Contains(listeningData.Artists.FirstOrDefault().Name)))
                {
                    listeningData = await tidalHttpService.SearchFromWindowTitle(tidalProcess.MainWindowTitle, ARTIST_SONG_DELIMITER);
                }

                //Check for looped song
                if (listeningData != null && listeningData.Started + listeningData.Duration < DateTime.Now)
                {
                    listeningData.Started = DateTime.Now;
                }

                //If we don't have TIDAL access, display a basic presence
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
                        Label = "Listen 🎵",
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
        public class AppSettings
        {
            public AppSettings(IConfigurationSection section)
            {
                TidalClientId = section.GetValue<string>("TIDAL_CLIENT_ID");
                TidalClientSecret = section.GetValue<string>("TIDAL_CLIENT_SECRET");
                DiscordAppId = section.GetValue<string>("DISCORD_APP_ID");
            }
            public string TidalClientId { get; set; } = string.Empty;
            public string TidalClientSecret { get; set; } = string.Empty;
            public string DiscordAppId { get; set; } = string.Empty;
        }
        public static AppSettings GetCurrentSettings()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
            IConfigurationRoot config = builder.Build();

            var settings = new AppSettings(config.GetSection("ApiKeys"));
            return settings;
        }
    }
}