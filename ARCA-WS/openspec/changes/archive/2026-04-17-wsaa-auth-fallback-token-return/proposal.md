## Why

El servicio WSFE puede ser consumido por multiples instancias en paralelo, mientras que WSAA tiene limites de frecuencia. El esquema actual de cache en memoria por instancia no escala bien cuando hay varios nodos, porque cada instancia renueva credenciales de forma independiente.

Se necesita un modelo donde el ERP sea responsable principal de la gestion de Token/Sign, pero sin perder resiliencia operativa cuando el ERP no provee credenciales o provee credenciales vencidas.

## What Changes

- Permitir que las operaciones de autorizacion WSFE acepten `Token` y `Sign` como entradas opcionales por request.
- Definir al ERP como responsable principal de:
  - Obtener credenciales WSAA.
  - Persistirlas en su propio repositorio.
  - Reutilizarlas hasta su vencimiento.
- Implementar fallback en la API:
  - Si `Token/Sign` no se reciben, o son invalidos/vencidos, la API obtiene nuevas credenciales desde WSAA para completar la operacion.
- Incluir en la respuesta de autorizacion los datos de credenciales efectivamente utilizados por la API cuando se generen o renueven:
  - `Token`
  - `Sign`
  - fecha de expiracion
- Mantener la API sin persistencia obligatoria de credenciales:
  - no almacenar tokens de forma persistente en la API.
  - cache en memoria de corto plazo permitida para reducir llamadas concurrentes a WSAA.

## Capabilities

### Modified Capabilities
- `arca-wsfev1-invoicing`: extiende el contrato de autorizacion para aceptar credenciales externas opcionales y devolver credenciales renovadas cuando aplique.
- `arca-wsaa-authentication`: ajusta el flujo de emision/reuso para coexistir con credenciales provistas externamente y con fallback bajo demanda, sin requerir almacenamiento persistente en la API.

## Impact

- Codigo afectado (esperado):
  - `Domain/Wsfe/WsfeModels.cs` y/o contratos de API para entradas/salidas de autorizacion con metadata de credenciales.
  - `Application/Wsfe/Wsfev1InvoicingService.cs` para resolver politica de decision: usar credencial externa valida o renovar via WSAA.
  - `Application/Auth/WsaaAuthenticationService.cs` y `Application/Auth/CredentialCache.cs` para soportar fallback y cache corta sin persistencia.
  - `PublicApi/ArcaIntegrationClient.cs` para mantener una API simple y desacoplada para consumidores ERP.
  - tests de `tests/ARCA-WS.Tests/Wsfe` y `tests/ARCA-WS.Tests/Wsaa`.
- Impacto operativo:
  - menos llamadas innecesarias a WSAA.
  - mayor resiliencia cuando el ERP no envia token o envia token vencido.
  - sin introducir Redis/base de datos como dependencia obligatoria en la API.
- Riesgo principal:
  - evitar ambiguedades al decidir cuando una credencial externa se considera reutilizable versus cuando debe renovarse en fallback.