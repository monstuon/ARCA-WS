## ADDED Requirements

### Requirement: Credential caching and proactive renewal
The library SHALL support hybrid credential sourcing where credentials can be provided externally per operation and MUST obtain credentials from WSAA on demand when external credentials are not available or are no longer valid.

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

---

### Requirement: Non-persistent credential ownership model
The library MUST keep ERP as the primary durable owner of WSAA credentials and MUST NOT require persistent storage in the API process.

#### Scenario: API avoids durable credential persistence
- **WHEN** the authentication subsystem issues or refreshes credentials for an operation
- **THEN** credentials MUST be exposed to the caller pipeline for outbound response usage
- **THEN** the API MUST NOT require writing those credentials to database or distributed cache

#### Scenario: In-memory cache remains optional optimization
- **WHEN** short-term in-memory cache is enabled for the authentication subsystem
- **THEN** it MUST be treated only as local optimization to reduce duplicate WSAA calls
- **THEN** correctness MUST NOT depend on cross-instance token synchronization