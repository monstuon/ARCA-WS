# Guia de Migracion a Biblioteca .NET 8 ARCA

## Objetivo

Migrar consumidores legacy (.NET Framework) a la biblioteca reutilizable .NET 8 para WSAA + WSFEv1.

## Estrategia de adopcion

1. Integrar la libreria por referencia de proyecto en el monorepo.
2. Configurar opciones `Arca` por ambiente (homologacion/produccion).
3. Implementar un flujo piloto con operacion de consulta y luego autorizacion.
4. Comparar resultados funcionales con la implementacion legacy en homologacion.
5. Habilitar gradualmente en produccion por consumidor.

## Ejemplo de configuracion

```json
{
  "Arca": {
    "Environment": "Homologation",
    "Endpoints": {
      "WsaaHomologation": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WsaaProduction": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WsfeHomologation": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "WsfeProduction": "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
    },
    "Wsaa": {
      "ServiceName": "wsfe",
      "TimestampToleranceSeconds": 120,
      "RenewalWindowSeconds": 120
    },
    "Resilience": {
      "Timeout": "00:01:00",
      "MaxRetries": 0
    },
    "Certificate": {
      "Source": "File",
      "FilePath": "certificado.pfx",
      "Password": "<secreto>"
    }
  }
}
```

## Manejo de errores recomendado

- `ArcaValidationException`: datos obligatorios o consistencia de importes.
- `ArcaAuthenticationException`: fallas en WSAA/certificados.
- `ArcaFunctionalException`: rechazos funcionales de ARCA (codigo/mensaje).
- `ArcaInfrastructureException`: conectividad, timeouts o errores no controlados.

## Nuevos metodos WSFE de consulta y CAEA

```csharp
var correlationId = Guid.NewGuid().ToString("N");

var puntos = await client.PuntosHabilitadosCaeaAsync(correlationId);

var comprobante = await client.ConsultarComprobanteAsync(
  new ConsultarComprobanteRequest(pointOfSale: 1, voucherType: 6, voucherNumber: 123),
  correlationId);

var caea = await client.CAEAConsultarAsync(new CaeaPeriodRequest(period: 202604, order: 1), correlationId);
var caeaSolicitado = await client.CAEASolicitarAsync(new CaeaPeriodRequest(period: 202604, order: 2), correlationId);

var detalle = new VoucherRequest(
  PointOfSale: 1,
  VoucherType: 6,
  DocumentType: 99,
  DocumentNumber: 0,
  IssueDate: new DateOnly(2026, 4, 23),
  NetAmount: 826.45m,
  NonTaxableAmount: 0m,
  ExemptAmount: 0m,
  TotalAmount: 1000m,
  CurrencyId: "PES",
  CurrencyRate: 1m,
  VoucherNumberFrom: 100,
  VoucherNumberTo: 100,
  VatBreakdown: [new VatItem(5, 826.45m, 173.55m)]);

var regInformativo = await client.CAEARegInformativoAsync(
  new CaeaRegInformativoRequest(
    PointOfSale: 1,
    VoucherType: 6,
    Caea: "61234567890123",
    Details: [detalle]),
  correlationId);
```

## Restricciones CAEA relevantes

- `Period` debe ir en formato `yyyymm` y el `Order` solo admite `1` o `2`.
- `CAEA` en registro informativo debe tener 14 digitos numericos.
- En `CAEARegInformativo`, el encabezado (`PointOfSale`, `VoucherType`) debe coincidir con cada detalle.
- Las credenciales externas (`Token`/`Sign`) se aceptan en `CAEARegInformativo`; si son rechazadas, se aplica fallback WSAA.
- Para homologacion/produccion, validar siempre que el punto de venta este habilitado via `PuntosHabilitadosCaeaAsync` antes de solicitar o informar CAEA.

## Rollback

1. Revertir referencia a la libreria y restaurar el flujo legacy por consumidor.
2. Mantener version pinneada de la libreria en cada consumidor.
3. Conservar monitoreo de incidencias durante 48 horas posteriores.
