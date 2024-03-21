using System.Text.RegularExpressions;

namespace IPK_Project;

public class StatesBehaviour
{
    //TADY NA KONEC ODSTRAŇ \n JE TU JENOM JAKO DEBUG
    private const string ReplyOk = @"^REPLY OK IS ([ -~]*)\n$";
    private const string ReplyNok = @"^REPLY NOK IS ([ -~]*)\n$";
    private const string ReplyMsg = @"^MSG FROM ([!-~]*) IS ([ -~]*)\n$";
    private const string HelpMsg = "\nCommands:\n" +
                            "/auth <username> <password> <secret> - authenticate user\n" +
                            "/join <channel> - join channel\n" +
                            "/rename <newname> - rename user\n" +
                            "/help - show help\n" +
                            "/bye - disconnect from server\n";
    public static (string, string) Start(out StatesEnum nextState, ref List<string> inputs)
    {
        if(inputs.Count < 1)
        {
            nextState = StatesEnum.Start;
            return ("err", "err");
        }
        
        string input = inputs[0];
        inputs.RemoveAt(0);
        
        string[] splitInput = input.ToLower().Split(" ");
        switch (splitInput[0])
        {
            case "/auth":
                if (splitInput.Length < 5 &&
                    splitInput[1].Length <= 20 && Regex.IsMatch(splitInput[1], @"^[A-z0-9-]*$") && 
                    splitInput[2].Length <= 20 && Regex.IsMatch(splitInput[2], @"^[!-~]*$") &&
                    splitInput[3].Length <= 128 && Regex.IsMatch(splitInput[3], @"^[A-z0-9-]*$"))
                {
                    nextState = StatesEnum.Auth;
                    //TADY NA KONEC ODSTRAŇ \n JE TU JENOM JAKO DEBUG
                    return (@"AUTH " + splitInput[1] + " AS " + splitInput[2] + " USING " + splitInput[3] + "\n", splitInput[2]);
                }

                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return ("err", "err");
            
            case "/help":
                Console.WriteLine(HelpMsg);
                nextState = StatesEnum.Start;
                return ("err", "err");
            
            default:
                Console.WriteLine("You need to authenticate!");
                nextState = StatesEnum.Start;
                return ("err", "err");
        }
    }
    public static string Auth(ref List<string> responses, out StatesEnum nextState)
    {
        if (responses.Count < 1)
        {
            nextState = StatesEnum.Auth;
            return "err";
        }
    
        if (responses[0] == "/help")
        {
            Console.WriteLine(HelpMsg);
            nextState = StatesEnum.Auth;
            responses.RemoveAt(0);
            return "err";
        }
        else if (Regex.IsMatch(responses[0], ReplyOk))
        {
            nextState = StatesEnum.Open;
            Console.Error.WriteLine(@"Success: " + responses[0].Split(" ")[3]);
            responses.RemoveAt(0);
            return "err";
        }
        else if (Regex.IsMatch(responses[0], ReplyNok))
        {
            nextState = StatesEnum.Auth;
            Console.Error.WriteLine(@"Failure: " + responses[0].Split(" ")[3]);
            responses.RemoveAt(0);
            return "err";
        }
        
        nextState = StatesEnum.End;
        return "err";
    }
    public static string Open(ref List<string> inputs, ref List<string> responses, out StatesEnum nextState, string displayName)
    {
        string sendToServer = "err";
        nextState = StatesEnum.Open;

        if (inputs.Count > 0)
        {
            string[] input = inputs[0].ToLower().Split(" ");
            switch (input[0])
            {
                case "/join":
                    if (input.Length == 2 && input[1].Length <= 20 && Regex.IsMatch(input[1], @"^[A-z0-9-]*$"))
                    {
                        sendToServer = @"JOIN " + input[1] + " AS " + displayName + "\n";
                    }

                    break;
                case "/help":
                    Console.WriteLine(HelpMsg);
                    nextState = StatesEnum.Start;
                    break;
                default:
                    if (Regex.IsMatch(input[0], @"^[ -~]*$"))
                    {
                        sendToServer = "MSG FROM " + displayName + " IS " + input + "\n";
                    }
                    else
                    {
                        Console.WriteLine("Invalid input");
                    }

                    break;
            }

            inputs.RemoveAt(0);
        }

        if (responses.Count > 0)
        {
            string[] responseSplit = responses[0].Split(" ");
            if (Regex.IsMatch(responses[0], ReplyOk))
            {
                Console.WriteLine("Success: " + responseSplit[3]);
            }
            else if (Regex.IsMatch(responses[0], ReplyNok))
            {
                Console.WriteLine("Failure: " + responseSplit[3]);
            }
            else if (Regex.IsMatch(responses[0], ReplyMsg))
            {
                Console.WriteLine(responseSplit[2] + ": " + string.Join(" ", responseSplit.Skip(4)));
            }
            responses.RemoveAt(0);
        }
        
        nextState = StatesEnum.Open;
        return sendToServer;
    }
    public static string Err(string input, out StatesEnum nextState)
    {
        switch (input)
        {
            case "/auth":
                break;
            case "/join":
                break;
            case "/rename":
                break;
            case "/help":
                Console.WriteLine(HelpMsg);
                nextState = StatesEnum.Start;
                break;
            default:
                //idk
                break;
        }
        nextState = StatesEnum.Err;
        return "err";
    }
}