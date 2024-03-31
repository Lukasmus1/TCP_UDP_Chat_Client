using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IPK_Project;

public class UdpChatClient : IClient
{
    private readonly UdpClient _client;
    private StatesEnum _state;
    private Queue<string?> _inputs = [];
    private string _displayName = null!;
    private string _stringToSend = null!;
    private List<byte> _sendToServer = null!;
    private ushort _msgCounter;
    private readonly ushort _data;
    private readonly byte _repeat;
    private List<byte[]> _confirms = new();
    private Queue<string> _responsesStr = new();
    private IPEndPoint _endPoint;
    private bool _asyncEnd;
    
    public UdpChatClient(UdpClient client,string server, ushort port, ushort data, byte repeat)
    {
        _state = StatesEnum.Start;
        _data = data;
        _repeat = repeat;
        _client = client;
        
        //Get the IP address of the server from DNS 
        _endPoint = new IPEndPoint(Array.Find(
            Dns.GetHostEntry(server).AddressList,
            a => a.AddressFamily == AddressFamily.InterNetwork)!, port);
        _state = StatesEnum.Start;

        _msgCounter = 0;
    }
    
    //Method for sending input to the server
    private void SendInput(List<byte> input)
    {
        _client.Send(input.ToArray(), _endPoint);
    }
    
    //Method for converting the input string to bytes according to the protocol
    private List<byte> ConvertToBytes(string input)
    {
        List<byte[]> resList = [];
        List<byte> res = [];
        input = input.Trim('\r', '\n');
        string[] splitInput = input.Split(" ");
        switch (splitInput[0].ToUpper())
        {    
            case "AUTH":
                resList.Add([0x02]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[1]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[3]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[5]));
                resList.Add([0x00]);
                break;
            
            case "JOIN":
                resList.Add([0x03]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[1]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[3]));
                resList.Add([0x00]);
                break;
                
            case "MSG":
                resList.Add([0x04]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[2]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(string.Join(" ", splitInput.Skip(4)).TrimEnd('\r', '\n')));
                resList.Add([0x00]);
                break;
            
            case "ERR":
                resList.Add([0xFE]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[2]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(string.Join(" ", splitInput.Skip(4)).TrimEnd('\r', '\n')));
                resList.Add([0x00]);
                break;
            
            case "BYE":
                resList.Add([0xFF]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                break;
        }

        foreach (byte[] item in resList)
        {
            res.AddRange(item);
        }

        _msgCounter++;
        return res;
    }
    
    //Method for converting the input bytes to string according to the TCP protocol so I can reuse the same methods
    private (string, byte[]) ConvertToString(byte[] input)
    {
        string res;
        int i;
        switch (input[0])
        {
            case 0x00:
                return ("confirm", input.Skip(1).ToArray());
            case 0x01:
                res = "REPLY ";
                if (input[3] == 1)
                {
                    res += "OK ";
                }
                else
                {
                    res += "NOK ";
                }

                res += "IS ";
                res += Encoding.ASCII.GetString(input, 6, input.Length - 7) + "\r\n";
                break;
            case 0x02:
                res = "AUTH ";
                i = 0;
                while(input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += " AS ";
                i++;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += " USING ";
                i++;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += "\r\n";
                break;
            case 0x03:
                res = "JOIN ";
                i = 2;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += " AS ";

                i++;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += "\r\n";
                break;
            case 0x04:
                res = "MSG FROM ";
                i = 3;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += " IS ";
                i++;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += "\r\n";
                break;
            case 0xFE:
                res = "ERR FROM ";
                i = 3;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += " IS ";
                i++;
                while (input[i] != 0x00)
                {
                    res += Convert.ToChar(input[i]);
                    i++;
                }
                res += "\r\n";
                break;
            case 0xFF:
                res = "BYE\r\n";
                break;
            default:
                Console.Error.WriteLine("ERR: Unknown response");
                Task.Run(() => HandleOutput(ConvertToBytes(Patterns.GetErrMsg(_displayName, "Chybný vstup ze serveru")), _msgCounter - 1));
                return ("errEnd", [0]);
        }

        return (res, input.Skip(1).Take(2).ToArray());
    }
    
    
    //Async method for getting input from the user and putting it into the queue
    private async void GetInputAsync()
    {
        while (_state != StatesEnum.End)
        {
            if (Console.In.Peek() == -1 && _inputs.Count == 0)
            {
                _asyncEnd = true;
                _state = StatesEnum.End;
                break;
            }
            string? input = await Console.In.ReadLineAsync();
            _inputs.Enqueue(input);
        }
    }
    
    //Async method for getting response from the server
    private async void GetResponseAsync()
    {
        UdpReceiveResult res;
        string resStr;
        byte[] msgBytes;
        bool switchPort = true;
        while (_state != StatesEnum.End)
        {
            res =  await _client.ReceiveAsync();

            if (res.Buffer[0] == 0x00)
            {
                _confirms.Add(res.Buffer.Skip(1).ToArray());
                continue;
            }
            
            (resStr, msgBytes) = ConvertToString(res.Buffer);
            if (resStr == "err")
            {
                //ERR STAV
                _asyncEnd = true;
                _state = StatesEnum.Err;
                return;
            }
            
            if (resStr[..5].Equals("REPLY") && switchPort)
            {
                _endPoint = res.RemoteEndPoint;
                switchPort = false;
            }
            
            _responsesStr.Enqueue(resStr);
            SendInput([0x00, msgBytes[0], msgBytes[1]]);
        }
    }
    
    //Method for waiting for the confirmation from the server
    private void HandleOutput(List<byte> input, int id)
    {
        byte[] counter = [(byte)(id >> 8), (byte)(id & 0xFF)];
        Stopwatch sw = new();
        int currTries = 0;
        List<byte[]> clone = [];
        
        //Works by starting a stopwatch and checking its elapsed time
        SendInput(input);
        sw.Start();
        while (currTries < _repeat)
        {
            //Creating a clone because the list might be modified while iterating
            //Since the clone is created inside a loop, I need to add this check so I don't create a new clone every time and waste memory
            if (!clone.SequenceEqual(_confirms))
            {
                clone = [.._confirms];
            }
        
            //If the confirms list contains the message ID, the message was confirmed
            if (clone.Any(item => item.SequenceEqual(counter)))
            {
                sw.Stop();
                break;
            }
            
            //If the time elapsed, resend the message
            if (sw.ElapsedMilliseconds >= _data)
            {
                SendInput(input);
                sw.Restart();
                currTries++;
            }
        }
        
        //If the message wasn't confirmed after the maximum number of tries, set the state to error
        sw.Stop();
        if (currTries > _repeat)
        {
            _state = StatesEnum.Err;
            _asyncEnd = true;
            return;
        }
        
        //If the message was confirmed, remove the message ID from the list
        _confirms.Remove(counter);
    }
    
    public void MainBegin()
    {
        Task.Run(GetInputAsync);
        Task.Run(GetResponseAsync);
        while (_state != StatesEnum.End)
        {
            //FSM
            switch (_state)
            {
                case StatesEnum.Start:
                    (_stringToSend, _displayName) = StatesBehaviour.Start(out _state, ref _inputs);
                    break;
                case StatesEnum.Auth:
                    _stringToSend = StatesBehaviour.Auth(ref _responsesStr, out _state);
                    break;
                case StatesEnum.Open:
                    _stringToSend = StatesBehaviour.Open(ref _inputs, ref _responsesStr, out _state, ref _displayName);
                    break;
                case StatesEnum.Err:
                    _stringToSend = StatesBehaviour.Err(out _state);
                    break;
            }
                
            //Helper for "asynchronous" end
            if (_state == StatesEnum.End || _asyncEnd)
            {
                _sendToServer = ConvertToBytes(Patterns.ByePattern);
                Task.Run(() => HandleOutput(_sendToServer, _msgCounter - 1));
                break;
            }

            if (_stringToSend == "err")
            {
                continue;
            }
            else if (_stringToSend == "errEnd")
            {
                Environment.Exit(1);
            }
            
            //Send the input to the server
            _sendToServer = ConvertToBytes(_stringToSend);
            Task.Run(() => HandleOutput(_sendToServer, _msgCounter - 1));
        }
    }
    
    //Method for handling the Ctrl+C event
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