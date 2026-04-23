namespace ARCA_WS.Domain;

public sealed record AuthCredentials(
    string Token,
    string Sign,
    DateTimeOffset Expiration,
    string Service,
    string Environment);
