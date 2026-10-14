namespace SentinelOps.Application.Common.Exceptions;

public class DurableBufferUnavailableException : Exception
{
    public DurableBufferUnavailableException(string message) : base(message)
    {
    }

    public DurableBufferUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
