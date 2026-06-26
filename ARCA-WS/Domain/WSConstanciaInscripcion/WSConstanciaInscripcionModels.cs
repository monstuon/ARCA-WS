namespace ARCA_WS.Domain.WSConstanciaInscripcion;

public sealed record PersonaTaxData(
    long Cuit,
    string? Denominacion,
    string? EstadoClave,
    string? TipoPersona,
    DateOnly? FechaContratoSocial,
    DateOnly? FechaInscripcion,
    IReadOnlyList<string> Impuestos,
    IReadOnlyList<string> Actividades,
    IReadOnlyList<string> Regimenes,
    IReadOnlyList<WsConstanciaError> Errors,
    string? Token = null,
    string? Sign = null,
    DateTimeOffset? ExpirationTime = null,
    bool CredentialsIssuedByApi = false,
    string? CredentialSource = null);

public sealed record WsConstanciaError(string Code, string Message);
