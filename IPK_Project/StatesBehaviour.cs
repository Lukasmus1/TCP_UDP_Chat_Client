using System.Text.RegularExpressions;

namespace IPK_Project;


//REGEXY MOŽNÁ KOMPLET ŠPATNĚ
public class StatesBehaviour
{
    private const string helpMsg = "Commands:\n" +
                            "/auth <username> <password> <secret> - authenticate user\n" +
                            "/join <channel> - join channel\n" +
                            "/rename <newname> - rename user\n" +
                            "/help - show help\n" +
                            "/bye - disconnect from server\n";
    public static string Start(string input, out StatesEnum nextState)
    {
        string[] splitInput = input.ToLower().Split(" ");
        switch (splitInput[0])
        {
            case "/auth":
                if (splitInput.Length < 4 &&
                    splitInput[1].Length <= 20 && Regex.IsMatch(splitInput[1], @"^[A-z0-9-]*$") && 
                    splitInput[2].Length <= 20 && Regex.IsMatch(splitInput[2], @"^[\\x21-\\x7E]*$") &&
                    splitInput[3].Length <= 128 && Regex.IsMatch(splitInput[3], @"^[\\x20-\\x7E]*$"))
                {
                    nextState = StatesEnum.Auth;
                    return @"AUTH " + splitInput[1] + "AS" + splitInput[2] + "USING" + splitInput[3];
                }

                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return "err";
                
            case "/join":
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return "err";
                
            case "/rename":
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return "err";
            
            case "/help":
                Console.WriteLine(helpMsg);
                nextState = StatesEnum.Start;
                return "err";
            
            default:
                Console.WriteLine("Invalid input");
                nextState = StatesEnum.Start;
                return "err";
        }
    }
    public static string Auth(string input, out StatesEnum nextState)
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
                Console.WriteLine(helpMsg);
                nextState = StatesEnum.Start;
                break;
            default:
                //idk
                break;
        }
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
                Console.WriteLine(helpMsg);
                nextState = StatesEnum.Start;
                break;
            default:
                //idk
                break;
        }
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
                Console.WriteLine(helpMsg);
                nextState = StatesEnum.Start;
                break;
            default:
                //idk
                break;
        }
    }
    public static string Bye(string input, out StatesEnum nextState)
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
                Console.WriteLine(helpMsg);
                nextState = StatesEnum.Start;
                break;
            default:
                //idk
                break;
        }
    }
}