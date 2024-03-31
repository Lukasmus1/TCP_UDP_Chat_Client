using System.Net.Sockets;
using CommandLine;

namespace IPK_Project;

class MainClass
{
    static void Main(string[] args)
    {
        //Declaring variables with default values
        string? connectionType = null;
        string? server = null;
        ushort port = 4567;
        ushort data = 250;
        byte repeat = 3;

        //Arg parser
        Parser parser = new Parser(config => config.HelpWriter = TextWriter.Null);
        parser.ParseArguments<ArgParserOptions>(args)
            .WithParsed(o =>
            {
                connectionType = o.ConnectionType;
                server = o.Server;
                port = o.Port;
                data = o.Data;
                repeat = o.Repeat;

                if (o.Help)
                {
                    Console.WriteLine("\n-t Required. Transport protocol used for connection (TCP/UDP).\n\n" +
                                      "-s Required. Server IP or hostname\n\n" +
                                      "-p Server port. (Default: 4576)\n\n" +
                                      "-d UDP confirmation timeout (Default: 250)\n\n" +
                                      "-r Maximum number of UDP retransmissions (Default: 3)\n\n" +
                                      "-h Display help screen\n");
                    Environment.Exit(0);
                }
            });

        if (connectionType == null || server == null || connectionType.ToLower() is not ("tcp" or "udp"))
        {
            Console.Error.WriteLine("ERR: Missing required arguments. Use -h for help.");
            Environment.Exit(1);
        }
        
        //IClient has the MainBegin method and EndProgram method
        IClient chatClient;
        
        //Creating the client based on the connection type
        if (connectionType == "tcp")
        {
            TcpClient? client = null;
            try
            {
                client  = new TcpClient(server, port);
            }
            catch (Exception)
            {
                Console.WriteLine("ERR: Connection failed.");
                Environment.Exit(1);
            }
            chatClient = new TcpChatClient(client.GetStream());
        }
        else
        {
            UdpClient client = new UdpClient(0);
            chatClient = new UdpChatClient(client, server, port, data, repeat);
        }
        
        //End program when Ctrl+C is pressed
        Console.CancelKeyPress += chatClient.EndProgram;
        
        //Start the client
        chatClient.MainBegin();
        
    }
}