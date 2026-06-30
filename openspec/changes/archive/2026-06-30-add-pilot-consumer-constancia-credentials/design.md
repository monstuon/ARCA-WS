## Context

El PilotConsumer ya implementa el patron ApplyErpCredentials para reutilizar credenciales WSAA del servicio wsfe entre llamadas. El servicio WSConstanciaInscripcion usa un serviceNameOverride = 'ws_sr_constancia_inscripcion', cuyo ciclo de autenticacion WSAA es completamente independiente. En el estado actual el Escenario 16 pasa token/sign nulos obligando a una llamada WSAA en cada ejecucion, incluso cuando ya se obtuvo un token valido.

## Goals / Non-Goals

**Goals:**

- Agregar en Program.cs un helper local ApplyConstanciaCredentials que provea Token/Sign validos al servicio de Constancia evitando llamadas redundantes al WSAA.
- Agregar CaptureConstanciaCredentialsFromResult para persistir en memoria las credenciales emitidas por la API en PersonaTaxData.
- Conectar ambos helpers al Escenario 16.

**Non-Goals:**

- Persistencia en disco de las credenciales de Constancia (solo en memoria, a diferencia de erpCredentials).
- Cambios en la libreria principal ARCA-WS.csproj.
- Cambios en WSConstanciaInscripcionService ni en su interfaz.

## Decisions

**D1: Variable de estado separada para Constancia**

Usar ErpCredentialSnapshot? constanciaCredentials en lugar de reutilizar erpCredentials. Razon: cada servicio WSAA tiene distinto serviceNameOverride y ciclo de vida del token. Mezclarlos produciria errores de autenticacion.

**D2: Misma firma conceptual que ApplyErpCredentials**

El helper retorna (string? token, string? sign) y verifica expiracion con margen de 30 segundos, identico al patron WSFE. Razon: consistencia con el codigo existente y menor curva de aprendizaje.

**D3: Solo captura en memoria (sin archivo)**

No se persiste en disco. Razon: el PilotConsumer es un ejecutable de corta vida; la complejidad de un segundo archivo de credenciales no aporta valor en el contexto de pruebas.

**D4: PersonaTaxData ya expone Token, Sign, ExpirationTime, CredentialsIssuedByApi**

No se necesita ningun cambio en el modelo de respuesta; los datos ya estan disponibles para CaptureConstanciaCredentialsFromResult.

## Risks / Trade-offs

[Risk] Si la ejecucion del PilotConsumer dura menos que el TTL del token, la captura en memoria no tiene beneficio real. -> Mitigation: La logica es identica a la de WSFE; el costo es minimo y el beneficio se da en ejecuciones largas o batches.

[Risk] El helper aplica credenciales ciegamente si constanciaCredentials no es null y no esta vencido; si el token fue invalidado externamente se producira un fallo y el servicio hara fallback a WSAA internamente. -> Mitigation: El propio WSConstanciaInscripcionService ya maneja el fallback via catch ArcaFunctionalException cuando las credenciales externas son rechazadas.
