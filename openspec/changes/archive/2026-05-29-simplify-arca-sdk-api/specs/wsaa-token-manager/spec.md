## ADDED Requirements

### Requirement: Centralized WSAA token management
The library SHALL centralize WSAA authentication concerns in a single internal component named `WsaaTokenManager`.

#### Scenario: Authentication is orchestrated by one component
- **WHEN** any operation requires WSAA credentials
- **THEN** `WsaaTokenManager` MUST handle credential acquisition, reuse, and renewal decisions
- **THEN** no parallel authentication orchestrators MUST be required for standard flows

### Requirement: In-memory cache with expiration-aware renewal
`WsaaTokenManager` MUST maintain an in-memory cache of `Token` and `Sign` and MUST renew credentials when cached values are missing, expired, or unusable.

#### Scenario: Cached credentials are reused when valid
- **WHEN** an operation requests credentials and cached values are still valid
- **THEN** `WsaaTokenManager` MUST return cached credentials without forcing a WSAA login

#### Scenario: Credentials are renewed when cache is stale
- **WHEN** cached credentials are absent, expired, or rejected as invalid
- **THEN** `WsaaTokenManager` MUST obtain fresh credentials from WSAA
- **THEN** the renewed credentials MUST replace the stale cached values

### Requirement: Correct certificate usage for WSAA login
WSAA login flows MUST use configured certificates correctly across supported platforms.

#### Scenario: Login request uses configured certificate material
- **WHEN** `WsaaTokenManager` performs WSAA login
- **THEN** the request signing process MUST use configured certificate/private key material correctly
- **THEN** the flow MUST remain compatible with Windows and Linux execution environments
