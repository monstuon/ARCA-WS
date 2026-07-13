namespace ARCA_WS.Domain.Errors;

public abstract class ArcaException : Exception
{
    protected ArcaException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public string? CorrelationId { get; init; }
}

public sealed class ArcaValidationException : ArcaException
{
    public ArcaValidationException(string message)
        : base(message)
    {
    }
}

public sealed class ArcaAuthenticationException : ArcaException
{
    public ArcaAuthenticationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class ArcaExternalCredentialsException : ArcaException
{
    public ArcaExternalCredentialsException(string message)
        : base(message)
    {
    }
}

public sealed class ArcaCredentialFallbackException : ArcaException
{
    public ArcaCredentialFallbackException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class ArcaFunctionalException : ArcaException
{
    public ArcaFunctionalException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class ArcaInfrastructureException : ArcaException
{
    public ArcaInfrastructureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
