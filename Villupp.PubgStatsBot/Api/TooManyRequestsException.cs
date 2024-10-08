namespace Villupp.PubgStatsBot.Api;

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message)
           : base(message)
    {
    }

    public TooManyRequestsException(string message, Exception inner)
        : base(message, inner)
    {
    }
}