using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace IPK_Project;

public class UdpChatClient
{
    private UdpClient _client;
    private StatesEnum _state;
    private Queue<string> _inputs = [];
    private string _displayName;
    private string _stringToSend;
    private List<byte> _sendToServer;
    private ushort _msgCounter;
    private string _server;
    private ushort _port;
    private readonly ushort _data;
    private readonly byte _repeat;
    private List<byte[]> _confirms = [];
    private Queue<string> _responsesStr = [];
    
    
    public UdpChatClient(UdpClient client,string server, ushort port, ushort data, byte repeat)
    {
        _state = StatesEnum.Start;
        _server = server;
        _port = port;
        _data = data;
        _repeat = repeat;
        _client = client;
        
        _state = StatesEnum.Start;

        _msgCounter = 0;
        Task.Run(GetInputAsync);
        Task.Run(GetResponseAsync);
    }
    
    private void SendInput(List<byte> input)
    {
        _client.Send(input.ToArray(), input.Count);
    }
    
    private List<byte> ConvertToBytes(string input)
    {
        List<byte[]> resList = [];
        List<byte> res = [];
        input = input.Trim('\r', '\n');
        string[] splitInput = input.Split(" ");
        switch (splitInput[0].ToUpper())
        {
            case "":

                break;
            
            case "AUTH":
                resList.Add([0x02]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[1]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[3]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[5]));
                resList.Add([0x00]);
                _msgCounter++;
                break;
            
            case "JOIN":
                resList.Add([0x03]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[1]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[3]));
                resList.Add([0x00]);
                _msgCounter++;
                break;
                
            case "MSG":
                resList.Add([0x04]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[1]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(string.Join(" ", splitInput.Skip(3)).TrimEnd('\r', '\n')));
                resList.Add([0x00]);
                _msgCounter++;
                break;
            
            case "ERR":
                resList.Add([0xFE]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                resList.Add(Encoding.ASCII.GetBytes(splitInput[2]));
                resList.Add([0x00]);
                resList.Add(Encoding.ASCII.GetBytes(string.Join(" ", splitInput.Skip(4)).TrimEnd('\r', '\n')));
                resList.Add([0x00]);
                _msgCounter++;
                break;
            
            case "BYE\r\n":
                resList.Add([0xFF]);
                resList.Add([(byte)(_msgCounter >> 8), (byte)(_msgCounter & 0xFF)]);
                _msgCounter++;
                break;
        }

        foreach (byte[] item in resList)
        {
            res.AddRange(item);
        }

        return res;
    }
    private (string, byte[]) ConvertToString(byte[] input)
    {
        string res = "";
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
                res += Encoding.ASCII.GetString(input, 6, input.Length - 6) + "\r\n";
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
                i = 2;
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
                i = 2;
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
                return ("err", [0]);
        }

        return (res, input.Skip(1).Take(2).ToArray());
    }
    
    
    private async void GetInputAsync()
    {
        while (_state != StatesEnum.End)
        {
            if (Console.In.Peek() == -1 && _inputs.Count == 0)
            {
                //_asyncEnd = true;
                _state = StatesEnum.End;
                break;
            }
            string? input = await Console.In.ReadLineAsync();
            _inputs.Enqueue(input);
        }
    }
    
    private async void GetResponseAsync()
    {
        byte[] res;
        string resStr;
        byte[] msgBytes;
        IPEndPoint remoteEp = new IPEndPoint(IPAddress.Parse(_server), _port);
        while (_state != StatesEnum.End)
        {
            res = _client.Receive(ref remoteEp);
            (resStr, msgBytes) = ConvertToString(res);
            if (resStr == "confirm")
            {
                _confirms.Add(res.Skip(1).ToArray());
                if (_state == StatesEnum.Auth)
                {
                    remoteEp = new IPEndPoint(IPAddress.Any, 0);
                }
                continue;
            }
            else if (resStr == "err")
            {
                //ERR STAV
                //_state = StatesEnum.Err;
                return;
            }
            _responsesStr.Enqueue(resStr);
            SendInput([0x00, msgBytes[0], msgBytes[1]]);
        }
    }
    
    private async void HandleOutput(List<byte> input, int id)
    {
        byte[] counter = [(byte)(id >> 8), (byte)(id & 0xFF)];
        int tries = 0;
        Timer t = new Timer();
        t.Interval = _data;
        t.Elapsed += (sender, args) =>
        {
            tries += 1;
            if (tries > _repeat) return;
            t.Stop();
            t.Start();
            SendInput(input);
        };

        //Toto tady je aby se zamezilo erroru, že by se odstranilo z tohoto, když se bude vykonávat ten Contains
        List<byte[]> clone;
        SendInput(input);
        t.Start();
        while (tries <= _repeat)
        {
            clone = _confirms;
            if (clone.Any(item => item.SequenceEqual(counter)))            
            {
                t.Stop();
                break;
            }
        }
        
        if (tries > _repeat)
        {
            _state = StatesEnum.Err;
            return;
        }

        _confirms.Remove(counter);
    }
    
    public void MainBegin()
    {
        StatesBehaviour statesBehaviour = new StatesBehaviour();
        while (_state != StatesEnum.End)
        {
            switch (_state)
            {
                case StatesEnum.Start:
                    (_stringToSend, _displayName) = statesBehaviour.Start(out _state, ref _inputs);

                    break;
                case StatesEnum.Auth:
                    _stringToSend = statesBehaviour.Auth(ref _inputs, out _state);

                    break;
                case StatesEnum.Open:
                    _stringToSend = statesBehaviour.Open(ref _inputs, ref _responsesStr, out _state, _displayName);

                    break;
                case StatesEnum.Err:
                    _stringToSend = statesBehaviour.Err(out _state);

                    break;
            }

            if (_stringToSend != "err")
            {
                _sendToServer = ConvertToBytes(_stringToSend);
                Task.Run(() => HandleOutput(_sendToServer, _msgCounter - 1));
            }
        }
    }
}