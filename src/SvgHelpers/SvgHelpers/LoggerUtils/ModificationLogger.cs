namespace ProgrammerAl.SvgHelpers.LoggerUtils;

public interface IModificationLogger
{
    void LogInfo(string message);
    void LogError(string message);
}

public class ModificationLogger : IModificationLogger
{
    public void LogInfo(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }

    public void LogError(string message)
    {
        Console.WriteLine($"ERROR: {message}");
    }
}
