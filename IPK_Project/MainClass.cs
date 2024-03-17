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
        bool? isTcp = null; //true = TCP, false = UDP
        string? argS = null;
        UInt16 argP = 4567;
        UInt16 argD = 250;
        byte argR = 3;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                    isTcp = true;
                    ArgLenCheck(i, args.Length);
                    if (args[i + 1] == "udp")
                    {
                        isTcp = false;
                    }
                    else if (args[i + 1] == "tcp")
                    {
                        isTcp = true;
                    }
                    else
                    {
                        Console.WriteLine("Error: -t");
                        Environment.Exit(1);
                    }
                    i++;
                    break;
                
                case "-s":
                    ArgLenCheck(i, args.Length);
                    if (Uri.CheckHostName(args[i + 1]) == UriHostNameType.Unknown)
                    {
                        Console.WriteLine("Error: -s");
                        Environment.Exit(1);
                    }
                    argS = args[i + 1];
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
        if (!(isTcp == null || argS == null))
        {
            Console.WriteLine("Error: -t and -s are required");
            Environment.Exit(1);
        }
    }
}