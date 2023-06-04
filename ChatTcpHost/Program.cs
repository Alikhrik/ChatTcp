using ChatTcpLib.Server;

namespace ChatTcpHost;
internal abstract class Program
{
    private const string MutexGuid = "9B95C874-8A6E-418F-99AD-D29537967271";
    private const string Def = @"DESKTOP-254REBR\MSSQLSERVER2";
    [Obsolete("Obsolete")]
    public static async Task Main(string[] args)
    {
        var appMutex = new Mutex(true, MutexGuid);
        if (!appMutex.WaitOne(0))
        {
            appMutex.Dispose();
            Console.Write("The application is already running.\n Press any key to exit . . .");
            Console.ReadKey();
            return;
        }
        string? sqlServerName;
        do
        {
            Console.ForegroundColor = ConsoleColor.White; Console.Write("Enter local");
            Console.ForegroundColor = ConsoleColor.Cyan;  Console.Write(" sql");
            Console.ForegroundColor = ConsoleColor.White; Console.Write(" server name: ");
            Console.ForegroundColor = ConsoleColor.Cyan;  sqlServerName = Console.ReadLine();
            if (sqlServerName is "def")
                sqlServerName = Def;
        } while (sqlServerName == null);
        var chatTcpServer = new ChatTcpServer(sqlServerName);
        Console.CursorVisible = false;
        using var server = chatTcpServer.StartAsync();
        await server;
        Console.CursorVisible = true;
        Console.Write("Press any key to exit . . .");
        Console.ReadKey();
    }
}