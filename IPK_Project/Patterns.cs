namespace IPK_Project;

public static class Patterns
{
    public const string ReplyOk = "^REPLY OK IS ([ -~]*)\r\n$";
    public const string ReplyNok = "^REPLY NOK IS ([ -~]*)\r\n$";
    public const string ReplyMsg = "^MSG FROM ([!-~]*) IS ([ -~]*)\r\n$";
    public const string ReplyErrPattern = "ERR FROM ([!-~]*) IS ([ -~]*)\r\n";
    public const string ByePattern = "BYE\r\n";
    public const string HelpMsg = "\nCommands:\n" +
                                   "/auth <username> <password> <secret> - authenticate user\n" +
                                   "/join <channel> - join channel\n" +
                                   "/rename <newname> - rename user\n" +
                                   "/help - show help\n" +
                                   "/bye - disconnect from server\n";

    public static string GetJoinMsg(string channel, string displayName)
    {
        return "JOIN " + channel + " AS " + displayName + "\r\n";
    }
    public static string GetAuthMsg(string username, string displayName, string secret)
    {
        return "AUTH " + username + " AS " + displayName + " USING " + secret + "\r\n";
    }
    public static string GetErrMsg(string displayName, string msgContent)
    {
        return "ERR FROM " + displayName + " IS " + msgContent + "\r\n";
    }
}