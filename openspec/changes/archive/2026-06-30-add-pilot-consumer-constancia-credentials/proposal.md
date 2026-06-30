## Why

El Escenario 16 del PilotConsumer llama a constanciaService.GetPersonaAsync con token: null y sign: null, forzando autenticacion WSAA en cada ejecucion. El servicio ws_sr_constancia_inscripcion usa serviceNameOverride distinto al de WSFE y necesita su propio ciclo de caching. Se requiere un helper analogo a ApplyErpCredentials para reutilizar Token/Sign obtenidos para este servicio.

## What Changes

- Nuevo helper local ApplyConstanciaCredentials en Program.cs de PilotConsumer que retorna (string? token, string? sign) con las credenciales vigentes del snapshot o (null, null) si no hay o estan vencidas.
- Nueva variable de estado constanciaCredentials (ErpCredentialSnapshot?) para persistir Token/Sign del servicio Constancia, independiente de erpCredentials (WSFE).
- Nuevo helper CaptureConstanciaCredentialsFromResult analogo a CaptureErpCredentialsFromResult, que captura credenciales emitidas por la API.
- El Escenario 16 pasa las credenciales capturadas en lugar de null, null.

## Capabilities

### New Capabilities

- pilot-consumer-constancia-credentials-reuse: Capacidad del PilotConsumer de reutilizar Token/Sign WSAA para ws_sr_constancia_inscripcion entre llamadas a GetPersonaAsync.

### Modified Capabilities

- arca-ws-constancia-inscripcion: El consumer puede suministrar credenciales externas al servicio, habilitando el path 'external' ya existente en WSConstanciaInscripcionService.

## Impact

- ARCA-WS/samples/PilotConsumer/Program.cs: adicion de helpers y actualizacion del Escenario 16.
- Sin cambios a la libreria principal. Sin breaking changes.
