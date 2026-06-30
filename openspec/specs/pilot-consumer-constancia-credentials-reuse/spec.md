# pilot-consumer-constancia-credentials-reuse Specification

## Requirements
### Requirement: PilotConsumer mantiene snapshot de credenciales Constancia en memoria
El PilotConsumer SHALL mantener una variable local constanciaCredentials (ErpCredentialSnapshot?) separada de erpCredentials, dedicada al servicio ws_sr_constancia_inscripcion.

#### Scenario: Credenciales nulas al iniciar
- **WHEN** el PilotConsumer inicia y no hay snapshot previo
- **THEN** constanciaCredentials es null

### Requirement: Helper ApplyConstanciaCredentials provee Token y Sign vigentes
El helper ApplyConstanciaCredentials SHALL retornar el Token y Sign del snapshot si constanciaCredentials no es null y su Expiration es mayor a DateTimeOffset.UtcNow mas 30 segundos.
Si el snapshot es null o esta vencido, SHALL retornar (null, null).

#### Scenario: Snapshot vigente disponible
- **WHEN** constanciaCredentials tiene Token, Sign y Expiration futura valida
- **THEN** ApplyConstanciaCredentials retorna (Token, Sign) del snapshot

#### Scenario: Snapshot vencido
- **WHEN** constanciaCredentials.Expiration <= DateTimeOffset.UtcNow.AddSeconds(30)
- **THEN** ApplyConstanciaCredentials establece constanciaCredentials = null y retorna (null, null)

#### Scenario: Sin snapshot
- **WHEN** constanciaCredentials es null
- **THEN** ApplyConstanciaCredentials retorna (null, null)

### Requirement: Helper CaptureConstanciaCredentialsFromResult captura credenciales emitidas
El helper CaptureConstanciaCredentialsFromResult SHALL actualizar constanciaCredentials con Token, Sign y ExpirationTime.Value cuando PersonaTaxData.CredentialsIssuedByApi es true y Token y Sign no estan vacios.

#### Scenario: Resultado con credenciales emitidas por API
- **WHEN** persona.CredentialsIssuedByApi es true y Token/Sign no son nulos ni vacios
- **THEN** constanciaCredentials se actualiza con el nuevo ErpCredentialSnapshot

#### Scenario: Resultado sin credenciales emitidas por API
- **WHEN** persona.CredentialsIssuedByApi es false
- **THEN** constanciaCredentials no se modifica

### Requirement: Escenario 16 utiliza credenciales capturadas
El Escenario 16 (WS-Constancia-GetPersona) SHALL llamar a ApplyConstanciaCredentials para obtener el par (token, sign) y pasarlo a constanciaService.GetPersonaAsync.
Shall llamar a CaptureConstanciaCredentialsFromResult con el resultado si la llamada fue exitosa.

#### Scenario: Primera llamada sin snapshot
- **WHEN** constanciaCredentials es null al ejecutar Escenario 16
- **THEN** GetPersonaAsync se llama con token=null, sign=null y el servicio gestiona WSAA internamente

#### Scenario: Llamada con snapshot vigente
- **WHEN** constanciaCredentials tiene credenciales vigentes al ejecutar Escenario 16
- **THEN** GetPersonaAsync se llama con el Token y Sign del snapshot
