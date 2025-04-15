using DiscordRPC;
using DiscordRPC.Logging;

namespace WavesRP
{
    public class DiscordClient
    {
        private DiscordRpcClient client;
        public bool IsRPConnected => client.IsInitialized;
        public bool IsDisposed => client.IsDisposed;
        public DiscordClient(string appID)
        {
            client = new DiscordRpcClient(appID);
            //Set the logger
            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            //Subscribe to events
            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! Listening to {0} by {1}", e.Presence.Details, e.Presence.State);
            };
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