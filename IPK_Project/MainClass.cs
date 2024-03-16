namespace IPK_Project;

class MainClass
{
    static void ArgLenCheck(int i, int len)
    {
        if (i + 1 >= len)
        {
            Console.WriteLine("Error: argument missing");
            Environment.Exit(1);
        }
    }
    static void Main(string[] args)
    {
        bool reqArgT = false;
        bool reqArgS = false;
        bool? isTcp = null; //true = TCP, false = UDP
        UInt16 argP = 4567;
        UInt16 argD = 250;
        byte argR = 3;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                    reqArgT = true;
                    isTcp = true;
                    break;
                
                case "-s":
                    reqArgS = true;
                    isTcp = false;
                    break;
                
                case "-p":
                    ArgLenCheck(i, args.Length);
                    if (!UInt16.TryParse(args[i + 1], out argP))
                    {
                        Console.WriteLine("Error: -p");
                        Environment.Exit(1);
                    }
                    i++;
                    break;
                
                case "-d":
                    ArgLenCheck(i, args.Length);
                    if (!UInt16.TryParse(args[i + 1], out argD))
                    {
                        Console.WriteLine("Error: -d");
                        Environment.Exit(1);
                    }
                    i++;
                    break;
                
                case "-r":
                    ArgLenCheck(i, args.Length);
                    if (!byte.TryParse(args[i + 1], out argR))
                    {
                        Console.WriteLine("Error: -r");
                        Environment.Exit(1);
                    }
                    i++;
                    break;
                
                case "-h":
                    Console.WriteLine("\nPomocníček :koteseni:");
                    Environment.Exit(0);
                    break;
                
                default:
                    Console.WriteLine("Error: unknown argument");
                    Environment.Exit(1);
                    break;
            }
        }
        if (!(reqArgT && reqArgS) || isTcp == null)
        {
            Console.WriteLine("Error: -t and -s are required");
            Environment.Exit(1);
        }
    }
}