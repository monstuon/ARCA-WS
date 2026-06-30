## Why

El Escenario 16 del `PilotConsumer` llama a `constanciaService.GetPersonaAsync` con `token: null` y `sign: null`, lo que fuerza una autenticación WSAA en cada ejecución. El servicio `ws_sr_constancia_inscripcion` usa un `serviceNameOverride` distinto al de WSFE, por lo que necesita su propio ciclo de caching de credenciales. Se requiere un helper análogo a `ApplyErpCredentials` que permita reutilizar Token/Sign ya obtenidos para este servicio, evitando llamadas redundantes al WSAA.

## What Changes

- Nuevo helper local `ApplyConstanciaCredentials((string? token, string? sign) creds)` en `Program.cs` de `PilotConsumer` que retorna `(string? token, string? sign)` con las credenciales vigentes del snapshot o `(null, null)` si no hay o están vencidas.
- Nueva variable de estado `constanciaCredentials` (de tipo `ErpCredentialSnapshot?`) para persisitir el Token/Sign del servicio Constancia Inscripción, independiente de `erpCredentials` (WSFE).
- Nuevo helper local `CaptureConstanciaCredentialsFromResult(string scenario, PersonaTaxData result)` análogo a `CaptureErpCredentialsFromResult`, que captura las credenciales emitidas por la API en la variable `constanciaCredentials`.
- El Escenario 16 pasa las credenciales capturadas en lugar de `null, null`.

## Capabilities

### New Capabilities
- `pilot-consumer-constancia-credentials-reuse`: Capacidad del PilotConsumer de reutilizar Token/Sign WSAA ya obtenidos para el servicio `ws_sr_constancia_inscripcion` entre llamadas a `GetPersonaAsync`.

### Modified Capabilities
- `arca-ws-constancia-inscripcion`: El consumer ahora puede suministrar credenciales externas al servicio, habilitando el path `external` que ya existe en `WSConstanciaInscripcionService`.

## Impact

- `ARCA-WS/samples/PilotConsumer/Program.cs`: adición de helpers y actualización del Escenario 16.
- No hay cambios a la librería principal (`ARCA-WS.csproj`), solo al proyecto de ejemplo.
- No hay breaking changes.
