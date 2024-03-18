using System.Net.Sockets;

namespace IPK_Project;

public class UdpConnection : IConnection
{
    public string Server { get; set; }
    public ushort Port { get; set; }
    public ushort Data { get; set; }
    public byte Repeat { get; set; }
    
    private UdpClient client;
    
    public UdpConnection(string server, ushort port, ushort data, byte repeat)
    {
        Server = server;
        Port = port;
        Data = data;
        Repeat = repeat;
        
        client = new UdpClient(server, port);
    }
    
    public bool Connect(out NetworkStream stream)
    {
        stream = null;
        return true;
    }
}