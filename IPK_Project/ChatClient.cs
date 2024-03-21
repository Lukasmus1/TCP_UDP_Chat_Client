using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace IPK_Project;

public class ChatClient
{
    private NetworkStream _stream;
    private StatesEnum _state;
    private List<string?> _inputs = [];
    private List<string> _responses = [];
    private string _displayName;
    
    private const string ErrPattern = @"ERR FROM ([!-~]*) IS ([ -~]*)\r\n";

    public ChatClient(NetworkStream stream)
    {
        _stream = stream;
        _state = StatesEnum.Start;
        
        Task.Run(GetResponseAsync);
        Task.Run(GetInputAsync);
    }

    private void SendInput(string input)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(input);
        _stream.Write(buffer, 0, buffer.Length);
    }

    private async Task GetResponseAsync()
    {
        while (_state != StatesEnum.End)
        {
            byte[] responseBuffer = new byte[1024];
            int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            //Je možné, že to sebere 2 věci najendou
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            _responses.Add(response);
        }
    }

    private async Task GetInputAsync()
    {
        while (_state != StatesEnum.End)
        {
            string? input = await Console.In.ReadLineAsync();
            _inputs.Add(input);
        }
    }
    
    //REPLY OK IS ahojky
    public void MainBegin()
    {
        string sendToServer;
        while (_state != StatesEnum.End)
        {
            Task.Delay(50).Wait();
            sendToServer = "";
            switch (_state)
            {
                case StatesEnum.Start:
                    (sendToServer, _displayName) = StatesBehaviour.Start(out _state, ref _inputs);
                    break;
                case StatesEnum.Auth:
                    sendToServer = StatesBehaviour.Auth(ref _responses, out _state);
                    break;
                case StatesEnum.Open:
                    sendToServer = StatesBehaviour.Open(ref _inputs, ref _responses, out _state, _displayName);
                    break;
                case StatesEnum.Err:
                    break;
                case StatesEnum.End:
                    continue;
            }

            if (_responses.Any(x => Regex.IsMatch(x, ErrPattern)) || _state == StatesEnum.End)
            {
                break;
            }
            
            if (sendToServer == "err")
            {
                continue;
            }
            
            SendInput(sendToServer);
        }
        
        //UKONČI VŠECHNY TASKY
    }
}