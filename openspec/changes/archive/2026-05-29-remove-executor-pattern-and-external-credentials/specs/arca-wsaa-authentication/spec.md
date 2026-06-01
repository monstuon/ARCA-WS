## MODIFIED Requirements

### Requirement: Credential caching and proactive renewal
The library SHALL process consumer-provided `Token` and `Sign` as the normal primary credentials for each operation and MUST obtain credentials from WSAA on demand when received credentials are missing or no longer valid.

#### Scenario: Consumer credentials are accepted as primary source
- **WHEN** an upstream consumer provides `Token` and `Sign` for a WSFE operation
- **THEN** the authentication subsystem MUST allow the operation pipeline to use those credentials as primary source when valid
- **THEN** no forced WSAA renewal is required for that operation

#### Scenario: On-demand WSAA renewal for missing consumer credentials
- **WHEN** an operation requires authentication and no `Token` and `Sign` are provided
- **THEN** the subsystem MUST issue credentials through WSAA and return them to the caller pipeline

#### Scenario: On-demand WSAA renewal for unusable consumer credentials
- **WHEN** `Token` and `Sign` are provided but cannot be used because they are expired or invalid
- **THEN** the subsystem MUST issue fresh credentials through WSAA
- **THEN** the refreshed credentials MUST be returned to the caller pipeline for response propagation

---

## ADDED Requirements

### Requirement: Authentication flow without executor pattern
The library MUST execute authentication and renewal logic through linear service methods and MUST NOT require executor abstractions in WSAA authentication processing.

#### Scenario: Authentication service handles renewal directly
- **WHEN** authentication flow determines that credential renewal is required
- **THEN** the WSAA authentication service MUST perform issuance and return credentials in the same linear flow
- **THEN** no executor component MUST be used to dispatch authentication stages