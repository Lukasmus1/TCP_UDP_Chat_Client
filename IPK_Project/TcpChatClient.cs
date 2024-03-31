using System.Net.Sockets;
using System.Text;

namespace IPK_Project;

public class TcpChatClient(NetworkStream stream) : IClient
{
    private StatesEnum _state = StatesEnum.Start;
    private bool _asyncEnd;
    private Queue<string?> _inputs = [];
    private Queue<string> _responses = [];
    private string _displayName = "";
    
    //Method for sending input to the server
    private void SendInput(string input)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(input);
        stream.Write(buffer, 0, buffer.Length);
    }

    //Async method for getting responses from the server and putting them into the queue
    private async Task GetResponseAsync()
    {
        byte[] responseBuffer = new byte[1400];
        while (_state != StatesEnum.End)
        {
            int bytesRead = await stream.ReadAsync(responseBuffer);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            string[] responseArr = response.Split("\r\n");
            foreach (string res in responseArr.Where(res => !string.IsNullOrWhiteSpace(res)))
            {
                string resEnq = res + "\r\n";
                _responses.Enqueue(resEnq);
            }
        }
    }

    //Async method for getting input from the user and putting it into the queue
    private async void GetInputAsync()
    {
        while (_state != StatesEnum.End)
        {
            //Check if the input stream is closed
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
        Task.Run(GetResponseAsync);
        Task.Run(GetInputAsync);
        while (_state != StatesEnum.End)
        {
            string sendToServer = "";
            
            //FSM
            switch (_state)
            {
                case StatesEnum.Start:
                    (sendToServer, _displayName) = StatesBehaviour.Start(out _state, ref _inputs);
                    break;
                case StatesEnum.Auth:
                    sendToServer = StatesBehaviour.Auth(ref _responses, out _state);
                    break;
                case StatesEnum.Open:
                    sendToServer = StatesBehaviour.Open(ref _inputs, ref _responses, out _state, ref _displayName);
                    break;
                case StatesEnum.Err:
                    sendToServer = StatesBehaviour.Err(out _state);
                    break;
            }

            //Helper for "asynchronous" end
            if (_state == StatesEnum.End || _asyncEnd || sendToServer == "errEnd")
            {
                SendInput(Patterns.ByePattern);
                break;
            }
            
            //The naming here is slightly decieving, it was just a random string I chose
            if (sendToServer == "err")
            {
                continue;
            }
            
            SendInput(sendToServer);
        }
        
        //Before the program ends, close the stream
        stream.Close();
    }

    //Method for ending the program upon pressing Ctrl+C
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