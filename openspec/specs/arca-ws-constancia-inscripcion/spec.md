# arca-ws-constancia-inscripcion Specification

## Purpose
TBD - created by archiving change add-ws-constancia-inscripcion-getpersona-v2. Update Purpose after archive.
## Requirements
### Requirement: Consulta fiscal por CUIT mediante getPersona_v2
La librería SHALL exponer una operación de consulta de persona que invoque el método `getPersona_v2` de WS Constancia de Inscripción y retorne los datos fiscales relevantes de la persona consultada.

#### Scenario: Consulta exitosa de persona
- **WHEN** un consumidor invoca `GetPersona` con un CUIT válido
- **THEN** el sistema MUST invocar `getPersona_v2` en WS Constancia de Inscripción
- **THEN** el sistema MUST devolver los datos fiscales mapeados al modelo de dominio

### Requirement: Reutilización de autenticación WSAA para WS Constancia
La operación de consulta SHALL reutilizar la autenticación WSAA existente para obtener `Token` y `Sign` del servicio `ws_sr_constancia_inscripcion` cuando no se provean credenciales externas válidas.

#### Scenario: Credenciales externas ausentes
- **WHEN** `GetPersona` se invoca sin `Token` y `Sign` externos
- **THEN** el sistema MUST obtener credenciales desde WSAA usando el service name de constancia
- **THEN** el sistema MUST ejecutar la consulta con esas credenciales

#### Scenario: Credenciales externas rechazadas
- **WHEN** `GetPersona` se invoca con credenciales externas y WS Constancia las rechaza por autenticación
- **THEN** el sistema MUST ejecutar fallback a WSAA
- **THEN** el sistema MUST reintentar la consulta con credenciales emitidas por WSAA

### Requirement: Integración directa y trazable en API pública
La librería SHALL incorporar `GetPersona` en `ArcaIntegrationClient` con firma consistente al estilo WSFE y con un flujo de ejecución directo sin patrones de indirección complejos.

#### Scenario: Llamado desde cliente público
- **WHEN** una aplicación consume `ArcaIntegrationClient.GetPersona`
- **THEN** el callstack MUST ser directo hacia `IWSConstanciaInscripcionService` y cliente SOAP de constancia
- **THEN** no se MUST introducir proxies o factories adicionales para la invocación principal

### Requirement: GetPersonaAsync acepta credenciales externas pre-adquiridas por el consumer
Cuando el consumer llama a GetPersonaAsync con token y sign no nulos y no vacíos, el servicio SHALL intentar la llamada con esas credenciales externas antes de recurrir al WSAA interno.
Si las credenciales externas son rechazadas (ArcaFunctionalException con código de autenticación), el servicio SHALL hacer fallback al WSAA con serviceNameOverride = `ws_sr_constancia_inscripcion`.

#### Scenario: Credenciales externas válidas
- **WHEN** GetPersonaAsync recibe token y sign no nulos y el servidor Constancia los acepta
- **THEN** el resultado tiene CredentialSource = `external` y CredentialsIssuedByApi = false

#### Scenario: Credenciales externas rechazadas, fallback WSAA
- **WHEN** GetPersonaAsync recibe token y sign no nulos pero el servidor los rechaza con un error de autenticación
- **THEN** el servicio hace fallback al WSAA y retorna el resultado con CredentialSource = `wsaa-fallback` y CredentialsIssuedByApi = true

