using System.Net.Sockets;
using System.Text;

namespace IPK_Project;

public class ChatClient
{
    private NetworkStream _stream;
    private StatesEnum _state;

    public ChatClient(NetworkStream stream)
    {
        _stream = stream;
        _state = StatesEnum.Start;
        Start();
    }
    
    public void SendInput(string input)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(input);
        _stream.Write(buffer, 0, buffer.Length);
    }
    
    public string GetResponse()
    {
        byte[] responseBuffer = new byte[1024];
        int bytesRead = _stream.Read(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        return response;
    }
    //REPLY OK IS ahojky\r\n
    public void Start()
    {
        while (true)
        {
            string? input = Console.ReadLine();
            if (input == null)
            {
                continue;
            }
            string sendToServer = "";
            switch (_state)
            {
                case StatesEnum.Start:
                    sendToServer = StatesBehaviour.Start(input, out _state);
                    break;
                case StatesEnum.Auth:
                    string response = GetResponse();
                    sendToServer = StatesBehaviour.Auth(response, out _state);
                    break;
                case StatesEnum.Open:
                    sendToServer = StatesBehaviour.Open(input, out _state);
                    break;
                case StatesEnum.Err:
                    sendToServer = StatesBehaviour.Err(input, out _state);
                    break;
                case StatesEnum.Bye:
                    sendToServer = StatesBehaviour.Bye(input, out _state);
                    break;
                case StatesEnum.End:
                    break;
                default:
                    break;
            }
            if (sendToServer == "err")
            {
                continue;
            }
            
            SendInput(sendToServer);
        }
    }
}