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
    
    public async Task<int> SendInput(string input)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(input);
        await _stream.WriteAsync(buffer, 0, buffer.Length);
        return 0;
    }
    
    public async Task<string> GetResponseAsync()
    {
        byte[] responseBuffer = new byte[1024];
        int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
        string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
        return response;
    }
    
    public async Task<string?> GetInput()
    {
        string? input = await Console.In.ReadLineAsync();
        return input;
    }
    
    //REPLY OK IS ahojky\r
    public async void Start()
    {
        bool isWatingForResponse = false;
        string response = "";
        string? input = null;
        string sendToServer;
        while (true)
        {
            if (!isWatingForResponse)
            { 
                input = GetInput();
                Console.WriteLine("input: " + input);
            }
            isWatingForResponse = false;
            
            if (input == null)
            {
                continue;
            }
            
            sendToServer = "";
            switch (_state)
            {
                case StatesEnum.Start:
                    sendToServer = StatesBehaviour.Start(input, out isWatingForResponse, out _state);
                    break;
                case StatesEnum.Auth:
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
                //Task.Delay(10);
                continue;
            }
            
            await SendInput(sendToServer);
            response = await GetResponseAsync();
        }
    }
}