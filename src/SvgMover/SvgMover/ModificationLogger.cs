namespace ProgrammerAl.SvgMover;

public interface ISiteLogger
{
    void Log(string message);
}

public class SiteLogger : ISiteLogger
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}
