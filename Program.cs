// See https://aka.ms/new-console-template for more information
using System.ComponentModel;
using DiscordRPC;
using DiscordRPC.Logging;

Something st = new();
st.Initialize();

while (true)
{

}

public class Something
{
    public DiscordRpcClient client;
    public void Initialize()
    {
        /*
    Create a Discord client
    */
        client = new DiscordRpcClient("1248068953184534549");

        //Set the logger
        client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

        //Subscribe to events
        client.OnReady += (sender, e) =>
        {
            Console.WriteLine("Received Ready from user {0}", e.User.Username);
        };

        client.OnPresenceUpdate += (sender, e) =>
        {
            Console.WriteLine("Received Update! {0}", e.Presence);
        };

        //Connect to the RPC
        client.Initialize();
        //Set the rich presence
        //Call this as many times as you want and anywhere in your code.
        client.SetPresence(new RichPresence()
        {
            Type = ActivityType.Listening,
            Details = "Example Project",
            State = "csharp example",
            Assets = new Assets()
            {
                LargeImageKey = "image_large",
                LargeImageText = "Lachee's Discord IPC Library",
                SmallImageKey = "image_small"
            },
        });
    }
}