## ADDED Requirements

### Requirement: GetPersonaAsync acepta credenciales externas pre-adquiridas por el consumer
Cuando el consumer llama a GetPersonaAsync con token y sign no nulos y no vacios, el servicio SHALL intentar la llamada con esas credenciales externas antes de recurrir al WSAA interno.
Si las credenciales externas son rechazadas (ArcaFunctionalException con codigo de autenticacion), el servicio SHALL hacer fallback al WSAA con serviceNameOverride = 'ws_sr_constancia_inscripcion'.

#### Scenario: Credenciales externas validas
- **WHEN** GetPersonaAsync recibe token y sign no nulos y el servidor Constancia los acepta
- **THEN** el resultado tiene CredentialSource = 'external' y CredentialsIssuedByApi = false

#### Scenario: Credenciales externas rechazadas, fallback WSAA
- **WHEN** GetPersonaAsync recibe token y sign no nulos pero el servidor los rechaza con un error de autenticacion
- **THEN** el servicio hace fallback al WSAA y retorna el resultado con CredentialSource = 'wsaa-fallback' y CredentialsIssuedByApi = true
