# Guia de ejecucion: Factura B homologacion a consumidor final

## Objetivo

Emitir una Factura B de prueba por ARS 1000 en ambiente de homologacion usando el certificado y CUIT configurados en el sample `PilotConsumer`.

## Prerrequisitos

- Certificado homologacion disponible en `Certificado/isfhomo.p12`.
- CUIT emisor configurado en `samples/PilotConsumer/Program.cs`.
- Entorno WSAA/WSFE en homologacion (ya definido por defecto en el sample).
- .NET 8 SDK instalado.

## Flujo implementado

1. Consulta el ultimo comprobante autorizado para `PtoVta=1` y `CbteTipo=6`.
2. Calcula el proximo numero (`ultimo + 1`).
3. Solicita `FECAESolicitar` para Factura B consumidor final con:
- `VoucherType=6`
- `DocumentType=99`
- `DocumentNumber=0`
- `TotalAmount=1000`
- `CurrencyId=PES`
- `CurrencyRate=1`
4. Muestra resultado de autorizacion, CAE y vencimiento de CAE.

## Ejecucion

Desde la raiz del repo:

```powershell
dotnet run --project samples/PilotConsumer/PilotConsumer.csproj
```

## Salidas esperadas

- Aprobado:
  - Muestra `CorrelationId`
  - Muestra numero de comprobante
  - Muestra `CAE` y fecha de vencimiento
- Rechazado:
  - Muestra `CorrelationId`
  - Muestra numero de comprobante
  - Muestra primer `Code` y `Message` de rechazo
  - Finaliza con codigo de salida no cero

## Notas

- El sample mantiene una configuracion de SSL relajada solo para testing/homologacion.
- No usar esta configuracion en produccion.
