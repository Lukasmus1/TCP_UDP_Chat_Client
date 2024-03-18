using System.Net.Sockets;

namespace IPK_Project;

public interface IConnection
{
    string Server { get; set; }
    ushort Port { get; set; }
    
    bool Connect(out NetworkStream stream);
}