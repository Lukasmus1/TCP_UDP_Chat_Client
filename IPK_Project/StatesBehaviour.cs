using System.Text.RegularExpressions;

namespace IPK_Project;

public class StatesBehaviour
{
    //TADY NA KONEC ODSTRAŇ \n JE TU JENOM JAKO DEBUG
    private const string Reply = @"^REPLY (OK|NOK) IS .*\n$";
    private const string HelpMsg = "\nCommands:\n" +
                            "/auth <username> <password> <secret> - authenticate user\n" +
                            "/join <channel> - join channel\n" +
                            "/rename <newname> - rename user\n" +
                            "/help - show help\n" +
                            "/bye - disconnect from server\n";
    public static string Start(out bool isExpectingResponse, out StatesEnum nextState, ref List<string?> inputs)
    {
        if(inputs.Count < 1)
        {
            Task.Delay(50).Wait();
            nextState = StatesEnum.Start;
            isExpectingResponse = false;
            return "err";
        }
        
        string? input = inputs[0];
        
        //Toto tu je aby se případně nevolalo ToLower() na null
        if (input == null)
        {
            nextState = StatesEnum.Start;
            isExpectingResponse = false;
            return "err";
        }
        
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
                    isExpectingResponse = true;
                    //TADY NA KONEC ODSTRAŇ \n JE TU JENOM JAKO DEBUG
                    return @"AUTH " + splitInput[1] + " AS " + splitInput[2] + " USING " + splitInput[3] + "\n";
                }

                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                isExpectingResponse = false;
                return "err";
                
            case "/join":
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                isExpectingResponse = false;
                return "err";
                
            case "/rename":
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                isExpectingResponse = false;
                return "err";
            
            case "/help":
                Console.WriteLine(HelpMsg);
                nextState = StatesEnum.Start;
                isExpectingResponse = false;
                return "err";
            
            default:
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                isExpectingResponse = false;
                return "err";
        }
    }
    public static string Auth(ref List<string> responses, out StatesEnum nextState)
    {
        if (responses.Count < 1)
        {
            nextState = StatesEnum.Auth;
            return "err";
        }
        
        //REGEX TADY NEFUNUGJE
        if (Regex.IsMatch(responses[0], Reply))
        {
            nextState = StatesEnum.Open;
            responses.RemoveAt(0);
            Console.WriteLine("sdasd");
            return "err";
        }
        
        nextState = StatesEnum.End;
        return "err";
    }
    public static string Open(string input, out StatesEnum nextState)
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
    public static string Bye(string input, out StatesEnum nextState)
    {
        Console.WriteLine("Goodbye");
        nextState = StatesEnum.End;
        return "err";
    }
}