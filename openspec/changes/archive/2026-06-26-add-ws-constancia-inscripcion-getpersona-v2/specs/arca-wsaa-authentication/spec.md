## MODIFIED Requirements

### Requirement: Credential caching and proactive renewal
The library SHALL support hybrid credential sourcing where credentials can be provided externally per operation and MUST obtain credentials from WSAA on demand when external credentials are not available or are no longer valid. The credential subsystem MUST support resolving WSAA credentials for more than one ARCA service name, including WSFE and `ws_sr_constancia_inscripcion`.

#### Scenario: External credentials are accepted as primary source
- **WHEN** an upstream consumer provides `Token` and `Sign` for a WSFE operation
- **THEN** the authentication subsystem MUST allow the operation pipeline to use those credentials as primary source when valid
- **THEN** no forced WSAA renewal is required for that operation

#### Scenario: On-demand WSAA renewal for missing external credentials
- **WHEN** an operation requires authentication and no external credentials are provided
- **THEN** the subsystem MUST issue credentials through WSAA and return them to the caller pipeline

#### Scenario: On-demand WSAA renewal for unusable external credentials
- **WHEN** external credentials are provided but cannot be used because they are expired or invalid
- **THEN** the subsystem MUST issue fresh credentials through WSAA
- **THEN** the refreshed credentials MUST be returned to the caller pipeline for response propagation

#### Scenario: WSAA resolution for constancia service name
- **WHEN** a WS Constancia operation requires authentication
- **THEN** the subsystem MUST be able to request WSAA credentials for `ws_sr_constancia_inscripcion`
- **THEN** credentials for that service MUST be isolated from credentials resolved for other service names
