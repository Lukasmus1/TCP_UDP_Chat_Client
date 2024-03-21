using System.ComponentModel.Design.Serialization;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace IPK_Project;

public class ChatClient
{
    private NetworkStream _stream;
    private StatesEnum _state;
    private List<string?> _inputs = [];
    private Task _mainRef;
    private SemaphoreSlim _pool;
    private bool _isExpectingResponse = false;
    private bool _badResponse = false;
    private List<string> _responses = [];

    public ChatClient(NetworkStream stream)
    {
        _pool = new SemaphoreSlim(0, 1);
        
        _stream = stream;
        _state = StatesEnum.Start;
        
        Task.Run(GetResponseAsync);
        Task.Run(GetInput);
    }

    private void SendInput(string input)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(input);
        _stream.Write(buffer, 0, buffer.Length);
    }

    private async Task<string> GetResponseAsync()
    {
        while (true)
        {
            byte[] responseBuffer = new byte[1024];
            int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            _responses.Add(Encoding.UTF8.GetString(responseBuffer, 0, bytesRead));
        }
    }

    private async Task<int> GetInput()
    {
        while (true)
        {
            string? input = await Console.In.ReadLineAsync();
            _inputs.Add(input);
        }
    }
    
    //REPLY OK IS ahojky
    public void MainBegin()
    {
        string? input = null;
        string sendToServer;
        while (true)
        {
            Task.Delay(50);
            sendToServer = "";
            switch (_state)
            {
                case StatesEnum.Start:
                    sendToServer = StatesBehaviour.Start(out _isExpectingResponse, out _state, ref _inputs);
                    break;
                case StatesEnum.Auth:
                    sendToServer = StatesBehaviour.Auth(ref _responses, out _state);
                    break;
                case StatesEnum.Open:
                    sendToServer = StatesBehaviour.Open(input, out _state);
                    break;
                case StatesEnum.End:
                    sendToServer = StatesBehaviour.Bye(input, out _state);
                    break;
                case StatesEnum.Err:
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