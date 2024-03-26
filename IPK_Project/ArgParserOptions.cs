using CommandLine;

namespace IPK_Project;

public class ArgParserOptions
{
    [Option('t')]
    public string? ConnectionType { get; set; }

    [Option('s')]
    public string? Server { get; set; }

    [Option('p')]
    public ushort Port { get; set; } = 4567;

    [Option('d')]
    public ushort Data { get; set; } = 250;

    [Option('r')]
    public byte Repeat { get; set; } = 3;
        
    [Option('h')]
    public bool Help { get; set; }
}