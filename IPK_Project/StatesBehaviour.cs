﻿using System.Text.RegularExpressions;

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
                if (splitInput.Length < 5 &&
                    splitInput.Length > 3 &&
                    splitInput[1].Length <= 20 && Regex.IsMatch(splitInput[1], "^[A-z0-9-]*$") && 
                    splitInput[2].Length <= 20 && Regex.IsMatch(splitInput[2], "^[!-~]*$") &&
                    splitInput[3].Length <= 128 && Regex.IsMatch(splitInput[3], "^[A-z0-9-]*$"))
                {
                    nextState = StatesEnum.Auth;
                    return (Patterns.GetAuthMsg(splitInput[1], splitInput[2], splitInput[3]), splitInput[2]);
                }

                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return ("err", "err");
            
            case "/help":
                Console.WriteLine(Patterns.HelpMsg);
                nextState = StatesEnum.Start;
                return ("err", "err");
            
            default:
                Console.WriteLine("You need to authenticate!");
                nextState = StatesEnum.Start;
                return ("err", "err");
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
        
        if (input == "/help")
        {
            Console.WriteLine(Patterns.HelpMsg);
            nextState = StatesEnum.Auth;
            return "err";
        }
        else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyOk))
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
    public string Open(ref Queue<string?> inputs, ref Queue<string> responses, out StatesEnum nextState, string displayName)
    {
        string sendToServer = "err";
        nextState = StatesEnum.Open;

        if (inputs.Count > 0)
        {
            string[] input = inputs.Dequeue()!.Split(" ");
            switch (input[0])
            {
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
                default:
                    if (Regex.IsMatch(input[0], @"^[ -~]*$"))
                    {
                        return "MSG FROM " + displayName + " IS " + string.Join(" ", input) + "\r\n";
                    }
                    
                    Console.WriteLine("Invalid input");
                    break;
            }
        }

        if (responses.Count > 0)
        {
            string input = responses.Dequeue();
            string[] responseSplit = input.Split(" ");
            if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyOk))
            {
                Console.Write("Success: " + string.Join(" ", responseSplit.Skip(3)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyNok))
            {
                Console.Write("Failure: " + string.Join(" ", responseSplit.Skip(3)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyMsg))
            {
                Console.Write(responseSplit[2] + ": " + string.Join(" ", responseSplit.Skip(4)));
            }
            else if (Regex.IsMatch(input.ToUpper(), Patterns.ReplyErrPattern))
            {
                Console.WriteLine("ERR FROM " + responseSplit[2] + ": " + string.Join(" ", responseSplit.Skip(4)));
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
                Console.WriteLine("ERR: Unknown response");
                nextState = StatesEnum.Err;
                return Patterns.GetErrMsg(displayName, "Chybicka se vbloudila");
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