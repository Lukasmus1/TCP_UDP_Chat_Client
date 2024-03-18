using System.Net.Sockets;
using System.Text;

namespace IPK_Project;
public class TcpConnection(string server, ushort port) : IConnection
{
    public string Server { get; set; } = server;
    public ushort Port { get; set; } = port;

    private TcpClient _client;
    private NetworkStream _stream;
    
    public bool Connect(out NetworkStream stream)
    {
        try
        {
            _client  = new TcpClient(Server, Port);
            _stream = _client.GetStream();
            Console.WriteLine("Connected");
        }
        catch (Exception e)
        {
            stream = _stream;
            return false;
        }
        stream = _stream;
        return true;
    }
    
}