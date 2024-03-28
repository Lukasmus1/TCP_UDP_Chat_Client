using System.Net;
using System.Net.Sockets;

namespace IPK_Project;

public class UdpConnection
{
    public string Server { get; set; }
    public ushort Port { get; set; }
    public ushort Data { get; set; }
    public byte Repeat { get; set; }
    public UdpClient Client { get; private set; }
    
    private IPEndPoint _endPoint;
    
    public UdpConnection(string server, ushort port, ushort data, byte repeat)
    {
        Server = server;
        Port = port;
        Data = data;
        Repeat = repeat;
        
        Client = new UdpClient(server, port);
        //_endPoint = new IPEndPoint(IPAddress.Parse(server), port);
    }
    
    public bool Connect()
    {
        try
        {
            Client  = new UdpClient();
        }
        catch (Exception e)
        {
            return false;
        }
        return true;
    }
}