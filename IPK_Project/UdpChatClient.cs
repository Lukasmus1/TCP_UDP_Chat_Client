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
    private ushort _data;
    private byte _repeat;
    //Jsou tu 2 aby se neztrácel čas překonvertováváním na string, protože tam je timer
    private Queue<byte[]> _responses = [];
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
    private string ConvertToString(byte[] input)
    {
        string res = "";
        switch (input[0])
        {
            case 0x00:
                return "confirm";
            case 0x01:
                res = "NOK";
                break;
            case 0x02:
                res = "AUTH";
                break;
            case 0x03:
                res = "JOIN";
                break;
            case 0x04:
                res = "MSG";
                break;
            case 0xFE:
                res = "ERR";
                break;
            case 0xFF:
                res = "BYE";
                break;
            default:
                return "err";
        }

        return res;
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
        UdpReceiveResult res;
        string resStr;
        while (_state != StatesEnum.End)
        {
            res = await _client.ReceiveAsync();
            _responses.Enqueue(res.Buffer);

            resStr = ConvertToString(res.Buffer);
            if (resStr == "confirm")
            {
                continue;
            }
            else if (resStr == "err")
            {
                //ERR STAV
                //_state = StatesEnum.Err;
                return;
            }
            _responsesStr.Enqueue(resStr);
        }
    }
    
    private async void HandleOutput(List<byte> input)
    {
        Queue<byte[]> list = [];
        bool confirmBool = false;
        int tries = 0;
        Timer t = new Timer();
        t.Interval = _data;
        t.Elapsed += (sender, args) =>
        {
            tries += 1;
            if (tries <= _repeat)
            {
                t.Stop();
                t.Start();
            }
        };

        SendInput(input);
        t.Start();
        while (tries <= _repeat)
        {
            list = _responses;
            foreach (byte[] bytes in list)
            {
                if (bytes[0] == 0x00)
                {
                    t.Stop();
                    confirmBool = true;
                    break;
                }
            }
            
            if (confirmBool)
            {
                break;
            }
        }
        
        if (tries > _repeat + 1)
        {
            _state = StatesEnum.Err;
            return;
        }

        
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
                    if (_stringToSend != "err")
                    {
                        _sendToServer = ConvertToBytes(_stringToSend);
                    }
                    break;
                case StatesEnum.Auth:
                    _stringToSend = statesBehaviour.Auth(ref _inputs, out _state);
                    if (_stringToSend != "err")
                    {
                        _sendToServer = ConvertToBytes(_stringToSend);
                    }
                    break;
                case StatesEnum.Open:
                    _stringToSend = statesBehaviour.Open(ref _inputs, ref _responsesStr, out _state, _displayName);
                    if (_stringToSend != "err")
                    {
                        _sendToServer = ConvertToBytes(_stringToSend);
                    }
                    break;
                case StatesEnum.Err:
                    _stringToSend = statesBehaviour.Err(out _state);
                    if (_stringToSend != "err")
                    {
                        _sendToServer = ConvertToBytes(_stringToSend);
                    }
                    break;
            }


            //Task.Run(() => HandleOutput(_sendToServer));
        }
    }
}