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

/// <summary>
/// Se produce cuando WSAA rechaza un loginCms porque ya existe un Ticket de Acceso (TA)
/// vigente para el mismo certificado + servicio (fault coe.alreadyAuthenticated).
/// No es un fallo de autenticación real: el TA existe, solo que en otro lugar
/// (otro proceso, o una corrida anterior cuyo caché en memoria se perdió).
/// </summary>
public sealed class ArcaTokenAlreadyExistsException : ArcaException
{
    public ArcaTokenAlreadyExistsException(string message) : base(message)
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
