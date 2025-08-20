using BBS2K;
using BBS2K.Network;

class Program
{
    static async Task Main(string[] args)
    {
        var initialization = new InitializationHelper();
        await initialization.Initialize();

        var peer = new Peer(initialization.GetNickname(), initialization.GetPort(), initialization.GetLogger());
        try
        {
            await peer.StartAsync(initialization.GetInitialPeer());

            while (true)
            {
                string? input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }
                if (string.Equals(input, "/exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
                if (string.Equals(input, "/help", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine(InitializationHelper.GetGreeting());
                    continue;
                }
                if (string.Equals(input, "/myaddress", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine($"Your public address is {initialization.GetAddress()}:{initialization.GetPort()}. Share it with your friends to let them connect.\n\n");
                    continue;
                }
                if (string.Equals(input, "/peers", StringComparison.InvariantCultureIgnoreCase))
                {
                    peer.PrintPeers();
                    continue;
                }
                await peer.BroadcastChatMessageAsync(input);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occoured: {ex.Message}.\nThe appication will close.");
        }
        finally
        {
            await peer.StopAsync();
            Console.WriteLine("You have been disconnected.\n\n");
        }        
    }
}