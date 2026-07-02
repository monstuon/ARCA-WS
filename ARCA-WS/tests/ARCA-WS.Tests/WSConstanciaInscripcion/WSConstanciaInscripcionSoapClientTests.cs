using System.Net;
using System.Text;
using ARCA_WS.Infrastructure.WSConstanciaInscripcion;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ARCA_WS.Tests.WSConstanciaInscripcion;

public sealed class WSConstanciaInscripcionSoapClientTests
{
    [Fact]
    public async Task GetPersonaAsync_ShouldMapResponsableInscriptoData_WhenCatalogIndicatesRi()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildResponsableInscriptoSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WSConstanciaInscripcionSoapClient(new HttpClient(handler), NullLogger<WSConstanciaInscripcionSoapClient>.Instance);

        var result = await sut.GetPersonaAsync("https://wsconstancia-homo", "tok", "sig", 23296988839, 30533319242, CancellationToken.None);

        Assert.Equal(30533319242, result.Cuit);
        Assert.Equal("ACME SA", result.Denominacion);
        Assert.Equal("Juan", result.Nombre);
        Assert.Equal("Perez", result.Apellido);
        Assert.Equal("CUIT", result.TipoClave);
        Assert.Equal(12, result.MesCierre);
        Assert.True(result.EsResponsableInscripto);
        Assert.False(result.EsMonotributista);
        Assert.Contains("IVA", result.ResponsableInscriptoImpuestos!);
        Assert.Contains("620900", result.ResponsableInscriptoActividades!);
        Assert.Contains("REGIMEN GENERAL", result.ResponsableInscriptoRegimenes!);
        Assert.Empty(result.MonotributoImpuestos!);
        Assert.Null(result.MonotributoCategoria);
    }

    [Fact]
    public async Task GetPersonaAsync_ShouldMapMonotributoData_WhenCatalogIndicatesMonotributo()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildMonotributoSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WSConstanciaInscripcionSoapClient(new HttpClient(handler), NullLogger<WSConstanciaInscripcionSoapClient>.Instance);

        var result = await sut.GetPersonaAsync("https://wsconstancia-homo", "tok", "sig", 23296988839, 27126191060, CancellationToken.None);

        Assert.Equal(27126191060, result.Cuit);
        Assert.True(result.EsMonotributista);
        Assert.False(result.EsResponsableInscripto);
        Assert.Equal("C", result.MonotributoCategoria);
        Assert.Contains("MONOTRIBUTO", result.MonotributoImpuestos!);
        Assert.Contains("SERVICIOS INFORMATICOS", result.MonotributoActividades!);
        Assert.Contains("REG SIMPLIFICADO", result.MonotributoRegimenes!);
        Assert.Empty(result.ResponsableInscriptoImpuestos!);
    }

    [Fact]
    public async Task GetPersonaAsync_ShouldHandleMissingOptionalNodes_WithoutFailing()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildPartialSoap(), Encoding.UTF8, "text/xml")
            }));
        var sut = new WSConstanciaInscripcionSoapClient(new HttpClient(handler), NullLogger<WSConstanciaInscripcionSoapClient>.Instance);

        var result = await sut.GetPersonaAsync("https://wsconstancia-homo", "tok", "sig", 23296988839, 20390833564, CancellationToken.None);

        Assert.Equal(20390833564, result.Cuit);
        Assert.Equal("CONTRIBUYENTE", result.Denominacion);
        Assert.Null(result.Nombre);
        Assert.Null(result.Apellido);
        Assert.Null(result.TipoClave);
        Assert.Null(result.MesCierre);
        Assert.False(result.EsMonotributista);
        Assert.False(result.EsResponsableInscripto);
        Assert.Empty(result.Impuestos);
        Assert.Empty(result.Actividades);
        Assert.Empty(result.Regimenes);
    }

    private static string BuildResponsableInscriptoSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
               "<soapenv:Body>" +
               "<getPersona_v2Response>" +
               "<return>" +
               "<idPersona>30533319242</idPersona>" +
               "<denominacion>ACME SA</denominacion>" +
               "<estadoClave>ACTIVO</estadoClave>" +
               "<tipoPersona>JURIDICA</tipoPersona>" +
               "<nombre>Juan</nombre>" +
               "<apellido>Perez</apellido>" +
               "<tipoClave>CUIT</tipoClave>" +
               "<mesCierre>12</mesCierre>" +
               "<fechaInscripcion>20230110</fechaInscripcion>" +
               "<impuesto><descripcionImpuesto>IVA</descripcionImpuesto></impuesto>" +
               "<actividad><descripcionActividad>620900</descripcionActividad></actividad>" +
               "<regimen><descripcionRegimen>REGIMEN GENERAL</descripcionRegimen></regimen>" +
               "</return>" +
               "</getPersona_v2Response>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildMonotributoSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
               "<soapenv:Body>" +
               "<getPersona_v2Response>" +
               "<return>" +
               "<idPersona>27126191060</idPersona>" +
               "<denominacion>MONOTRIBUTO TEST</denominacion>" +
               "<estadoClave>ACTIVO</estadoClave>" +
               "<tipoPersona>FISICA</tipoPersona>" +
               "<categoriaMonotributo>C</categoriaMonotributo>" +
               "<impuesto><descripcionImpuesto>MONOTRIBUTO</descripcionImpuesto></impuesto>" +
               "<actividad><descripcionActividad>SERVICIOS INFORMATICOS</descripcionActividad></actividad>" +
               "<regimen><descripcionRegimen>REG SIMPLIFICADO</descripcionRegimen></regimen>" +
               "</return>" +
               "</getPersona_v2Response>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private static string BuildPartialSoap()
    {
        return "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
               "<soapenv:Body>" +
               "<getPersona_v2Response>" +
               "<return>" +
               "<idPersona>20390833564</idPersona>" +
               "<denominacion>CONTRIBUYENTE</denominacion>" +
               "<estadoClave>ACTIVO</estadoClave>" +
               "<tipoPersona>FISICA</tipoPersona>" +
               "</return>" +
               "</getPersona_v2Response>" +
               "</soapenv:Body>" +
               "</soapenv:Envelope>";
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return responder(request);
        }
    }
}
