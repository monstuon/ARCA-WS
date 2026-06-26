## Why

La librería ya resuelve autenticación WSAA y facturación WSFE, pero no expone una consulta fiscal de padrón por CUIT. Se necesita incorporar `getPersona_v2` de WS Constancia de Inscripción para obtener datos fiscales reutilizando la autenticación existente y manteniendo una API consistente.

## What Changes

- Agregar soporte de integración para WS Constancia de Inscripción con un flujo directo de llamada al método `getPersona_v2`.
- Exponer un método público `GetPersona` en `ArcaIntegrationClient` con firma alineada al estilo de los métodos WSFE.
- Incorporar `IWSConstanciaInscripcionService` y `WSConstanciaInscripcionService` siguiendo la estructura de `Wsfev1InvoicingService`.
- Añadir cliente SOAP específico para WS Constancia con request/response y manejo de errores funcionales/infraestructura.
- Reutilizar autenticación WSAA existente para obtener token/sign del servicio `ws_sr_constancia_inscripcion`.
- Agregar pruebas para los CUIT de validación solicitados: `30-53331924-2`, `55-00000410-2`, `20-20490252-7`, `33-61307092-9`, `27-12619106-0`, `27-22630583-7`, `20-39083356-4`.

## Capabilities

### New Capabilities
- `arca-ws-constancia-inscripcion`: Consulta de datos fiscales por CUIT mediante `getPersona_v2`, con autenticación WSAA reutilizada y API pública integrada.

### Modified Capabilities
- `arca-wsaa-authentication`: Uso de WSAA para múltiples servicios ARCA, incluyendo `ws_sr_constancia_inscripcion` además de WSFE.

## Impact

- Código afectado: capas `Domain`, `Application`, `Infrastructure`, `PublicApi`, `Configuration` y `ServiceCollectionExtensions`.
- API pública: nuevo método `GetPersona` en `ArcaIntegrationClient`.
- Configuración: nuevos endpoints para WS Constancia (homologación/producción).
- Testing: nuevas pruebas unitarias/integración para servicio y cliente público del nuevo flujo.
