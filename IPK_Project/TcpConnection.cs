using System.Net.Sockets;
using System.Text;

namespace IPK_Project;
public class TcpConnection(string server, ushort port)
{
    public string Server { get; set; } = server;
    public ushort Port { get; set; } = port;
    public NetworkStream Stream { get; private set; }
    
    private TcpClient _client;
    
    public bool Connect()
    {
        try
        {
            _client  = new TcpClient(Server, Port);
            Stream = _client.GetStream();
        }
        catch (Exception e)
        {
            return false;
        }
        return true;
    }
    
}