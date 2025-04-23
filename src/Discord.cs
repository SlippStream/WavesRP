using System.Text;
using DiscordRPC;
using DiscordRPC.Logging;
using Pastel;

namespace WavesRP
{
    public class DiscordClient
    {
        static readonly string THEME_DARK = "#245ABD";
        static readonly string THEME_LIGHT = "#27ADC3";
        static readonly string THEME_WHITE = "#A0B0DD";
        static readonly string THEME_BLACK = "#0B2443";
        private DiscordRpcClient client;
        public bool IsRPConnected => client.IsInitialized;
        public bool IsDisposed => client.IsDisposed;
        public DiscordClient(string appID)
        {
            if (appID == null || appID == "0" || appID == string.Empty)
            {
                Console.WriteLine("No app ID provided. Exiting...");
                Environment.Exit(1);
            }
            client = new DiscordRpcClient(appID);
            //Set the logger
            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            //Subscribe to events
            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += static (sender, e) =>
            {
                Console.WriteLine();
                Console.WriteLine("Received Discord Presence Update".Pastel(THEME_LIGHT));
                Console.WriteLine("Listening to \x1b[1m{0} — \x1b[1m{1}".Pastel(THEME_LIGHT), e.Presence.Details.Pastel(THEME_WHITE), e.Presence.State.Pastel(THEME_WHITE));
                if (e.Presence.HasTimestamps())
                {
                    var progress = (DateTime.UtcNow - e.Presence.Timestamps.Start).GetValueOrDefault();
                    var songLength = (e.Presence.Timestamps.End - e.Presence.Timestamps.Start).GetValueOrDefault();
                    Console.WriteLine("\x1b[3mon \x1b[1m{0}".Pastel(THEME_LIGHT), e.Presence.Assets.LargeImageText.Pastel(THEME_DARK));
                    Console.WriteLine("{0} {2} {1}", GetTimestamp(progress).Pastel(THEME_LIGHT), GetTimestamp(songLength).Pastel(THEME_LIGHT), GetProgressBar(progress, songLength, 30).Pastel(THEME_BLACK));
                    return;
                }
            };
        }
        private static string GetTimestamp(TimeSpan ts)
        {
            var hours = ts.Hours.ToString();
            var minutes = ts.Minutes.ToString();
            var seconds = ts.Seconds.ToString();
            if (hours.Length < 2) hours = hours.Insert(0, "0");
            if (minutes.Length < 2) minutes = minutes.Insert(0, "0");
            if (seconds.Length < 2) seconds = seconds.Insert(0, "0");
            if (int.Parse(hours) == 0)
                return $"{minutes}:{seconds}";

            return $"{hours}:{minutes}:{seconds}";
        }
        private static string GetProgressBar(TimeSpan progress, TimeSpan length, int size)
        {
            var progressBar = new StringBuilder();
            var percent = (int)(progress.TotalSeconds / length.TotalSeconds * size);
            for (int i = 0; i < size; i++)
            {
                if (i < percent) progressBar.Append('█');
                else progressBar.Append('░');
            }
            return progressBar.ToString();
        }
        public void ConnectRP()
        {
            client.Initialize();
        }
        public void DisconnectRP()
        {
            client.Deinitialize();
        }
        public void Dispose()
        {
            client.Dispose();
        }
        public void ClearPresence()
        {
            client.ClearPresence();
        }
        public void SetPresence(RichPresence presence)
        {
            client.SetPresence(presence);
        }
        public static DiscordClient Instantiate(string appID) => new(appID);
    }
}