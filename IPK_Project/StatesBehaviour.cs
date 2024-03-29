using System.Text.RegularExpressions;

namespace IPK_Project;

public class StatesBehaviour
{
    public (string, string) Start(out StatesEnum nextState, ref Queue<string?> inputs)
    {
        if(inputs.Count < 1)
        {
            nextState = StatesEnum.Start;
            return ("err", "err");
        }

        string? input = inputs.Dequeue();
        
        string[] splitInput = input!.ToLower().Split(" ");
        switch (splitInput[0])
        {
            case "/auth":
                if (splitInput.Length is < 5 and > 3 &&
                    splitInput[1].Length <= 20 && Regex.IsMatch(splitInput[1], "^[A-z0-9-]{1,20}$") && 
                    splitInput[2].Length <= 20 && Regex.IsMatch(splitInput[2], "^[!-~]{1,20}$") &&
                    splitInput[3].Length <= 128 && Regex.IsMatch(splitInput[3], "^[A-z0-9-]{1,128}$"))
                {
                    nextState = StatesEnum.Auth;
                    return (Patterns.GetAuthMsg(splitInput[1], splitInput[3], splitInput[2]), splitInput[3]);
                }

                Console.Error.WriteLine("ERR: Invalid input");
                nextState = StatesEnum.Err;
                return ("errEnd", "err");
            
            case "/help":
                Console.WriteLine(Patterns.HelpMsg);
                nextState = StatesEnum.Start;
                return ("errEnd", "err");
            
            default:
                Console.Error.WriteLine("ERR: Invalid input");
                nextState = StatesEnum.Err;
                return ("errEnd", "err");
        }
    }
    public string Auth(ref Queue<string> responses, out StatesEnum nextState)
    {
        if (responses.Count < 1)
        {
            nextState = StatesEnum.Auth;
            return "err";
        }

        string input = responses.Dequeue();
        
        if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyOk))
        {
            nextState = StatesEnum.Open;
            Console.Error.WriteLine("Success: " + string.Join(" ", input.Split(" ").Skip(3)));
            return "err";
        }
        else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyNok))
        {
            nextState = StatesEnum.Start;
            Console.Error.WriteLine("Failure: " + string.Join(" ", input.Split(" ").Skip(3)));
            return "err";
        }
        else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyErrPattern))
        {
            Console.Error.WriteLine("ERR FROM " + input.Split(" ")[2] + ": " + input.Split(" ")[4]);
            nextState = StatesEnum.End;
            return Patterns.ByePattern;
        }
        
        nextState = StatesEnum.End;
        return Patterns.ByePattern;
    }
    public string Open(ref Queue<string?> inputs, ref Queue<string> responses, out StatesEnum nextState, ref string displayName)
    {
        string sendToServer = "err";
        nextState = StatesEnum.Open;

        if (inputs.Count > 0)
        {
            if(string.IsNullOrEmpty(inputs.Peek()))
            {
                return "errEnd";
            }
            string[] input = inputs.Dequeue()!.Split(" ");
            switch (input[0])
            {
                case "/rename":
                    if (input.Length == 2 && input[1].Length <= 20 && Regex.IsMatch(input[1], @"^[A-z0-9-]*$"))
                    {
                        displayName = input[1];
                    }
                    break;
                case "/join":
                    if (input.Length == 2 && input[1].Length <= 20 && Regex.IsMatch(input[1], @"^[A-z0-9-]*$"))
                    {
                        sendToServer = Patterns.GetJoinMsg(input[1], displayName);
                    }

                    break;
                case "/help":
                    Console.WriteLine(Patterns.HelpMsg);
                    nextState = StatesEnum.Start;
                    break;
                case "/auth":
                    Console.Error.WriteLine("ERR: Already authenticated");
                    nextState = StatesEnum.Err;
                    return "err";
                default:
                    if (Regex.IsMatch(input[0], "^[ -~]*$"))
                    {
                        return "MSG FROM " + displayName + " IS " + string.Join(" ", input) + "\r\n";
                    }
                    
                    Console.Error.WriteLine("ERR: Invalid input");
                    return "errEnd";
            }
        }

        if (responses.Count > 0)
        {
            string input = responses.Dequeue();
            string[] responseSplit = input.Split(" ");
            if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyOk))
            {
                Console.Error.Write("Success: " + string.Join(" ", responseSplit.Skip(3)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyNok))
            {
                Console.Error.Write("Failure: " + string.Join(" ", responseSplit.Skip(3)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyMsg))
            {
                Console.Write(responseSplit[2] + ": " + string.Join(" ", responseSplit.Skip(4)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyErrPattern))
            {
                Console.Error.WriteLine("ERR FROM " + responseSplit[2] + ": " + string.Join(" ", responseSplit.Skip(4)));
                nextState = StatesEnum.End;
                return Patterns.ByePattern;
            }
            else if (input.ToUpper() == Patterns.ByePattern)
            {
                nextState = StatesEnum.End;
                return Patterns.ByePattern;
            }
            else
            {
                Console.Error.WriteLine("ERR: Server sent an error message");
                nextState = StatesEnum.Err;
                return Patterns.GetErrMsg(displayName, "Server poslal error");
            }
        }
        
        nextState = StatesEnum.Open;
        return sendToServer;
    }
    public string Err(out StatesEnum nextState)
    {
        nextState = StatesEnum.End;
        return Patterns.ByePattern;
    }
}