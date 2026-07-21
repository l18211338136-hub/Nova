namespace Nova.Contracts.Exceptions;

public class NovaValidationException : Exception
{
    public NovaValidationException(string message) : base(message)
    {
    }

    public NovaValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
