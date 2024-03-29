using System.Net.Sockets;
using System.Text;

namespace IPK_Project;

public class TcpChatClient : IClient
{
    private readonly NetworkStream _stream;
    private StatesEnum _state;
    private bool _asyncEnd;
    private Queue<string?> _inputs = [];
    private Queue<string> _responses = [];
    private string _displayName;

    public TcpChatClient(NetworkStream stream)
    {
        _stream = stream;
        _state = StatesEnum.Start;
        
        Task.Run(GetResponseAsync);
        Task.Run(GetInputAsync);
    }

    private void SendInput(string input)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(input);
        _stream.Write(buffer, 0, buffer.Length);
    }

    private async Task GetResponseAsync()
    {
        byte[] responseBuffer = new byte[1400];
        while (_state != StatesEnum.End)
        {
            int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            string[] responseArr = response.Split("\r\n");
            foreach (var res in responseArr.Where(res => !string.IsNullOrWhiteSpace(res)))
            {
                var resEnq = res + "\r\n";
                _responses.Enqueue(resEnq);
            }
        }
    }

    private async void GetInputAsync()
    {
        while (_state != StatesEnum.End)
        {
            try
            {
                Console.In.Peek();
            }
            catch (ObjectDisposedException)
            {
                _inputs.Enqueue(null);
                break;
            }

            if (Console.In.Peek() == -1 && _inputs.Count == 0)
            {
                _inputs.Enqueue(null);
                break;
            }
            string? input = await Console.In.ReadLineAsync();
            _inputs.Enqueue(input);
        }
    }

    public void MainBegin()
    {
        StatesBehaviour statesBehaviour = new StatesBehaviour();
        while (_state != StatesEnum.End)
        {
            string sendToServer = "";
            switch (_state)
            {
                case StatesEnum.Start:
                    (sendToServer, _displayName) = statesBehaviour.Start(out _state, ref _inputs);
                    break;
                case StatesEnum.Auth:
                    sendToServer = statesBehaviour.Auth(ref _responses, out _state);
                    break;
                case StatesEnum.Open:
                    sendToServer = statesBehaviour.Open(ref _inputs, ref _responses, out _state, ref _displayName);
                    break;
                case StatesEnum.Err:
                    sendToServer = statesBehaviour.Err(out _state);
                    break;
            }

            if (_state == StatesEnum.End || _asyncEnd || sendToServer == "errEnd")
            {
                SendInput(Patterns.ByePattern);
                break;
            }
            
            if (sendToServer == "err")
            {
                continue;
            }
            
            SendInput(sendToServer);
        }
        
        _stream.Close();
    }

    public void EndProgram(object? sender, ConsoleCancelEventArgs e)
    {
        if (_state != StatesEnum.Start)
        {
            e.Cancel = true;
            _asyncEnd = true;
            _state = StatesEnum.End;
        }
        else
        {
            e.Cancel = false;
        }
    }
}