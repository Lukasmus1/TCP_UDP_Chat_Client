using System.Net;
using CommandLine;

namespace IPK_Project;

public class ArgParserOptions
{
    [Option('t')]
    public string? ConnectionType { get; set; }

    [Option('s')]
    public string? Server { get; set; }

    [Option('p')]
    public ushort Port { get; set; }

    [Option('d')]
    public ushort Data { get; set; }

    [Option('r')]
    public byte Repeat { get; set; }
        
    [Option('h')]
    public bool Help { get; set; }
}