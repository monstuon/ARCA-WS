namespace ARCA_WS.Infrastructure.Wsaa;

public sealed record WsaaLoginResponse(string Token, string Sign, DateTimeOffset Expiration);
