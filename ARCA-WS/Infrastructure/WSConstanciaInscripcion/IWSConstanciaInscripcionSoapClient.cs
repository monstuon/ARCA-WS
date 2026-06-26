using ARCA_WS.Domain.WSConstanciaInscripcion;

namespace ARCA_WS.Infrastructure.WSConstanciaInscripcion;

public interface IWSConstanciaInscripcionSoapClient
{
    Task<PersonaTaxData> GetPersonaAsync(string endpoint, string token, string sign, long representedCuit, long cuitToQuery, CancellationToken cancellationToken);
}
