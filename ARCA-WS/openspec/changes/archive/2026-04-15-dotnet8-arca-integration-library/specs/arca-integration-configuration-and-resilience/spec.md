## ADDED Requirements

### Requirement: Centralized typed configuration
The library SHALL provide typed configuration objects for ARCA endpoints, service identifiers, certificate settings, timeout values, and resilience policies.

#### Scenario: Valid configuration binding
- **WHEN** the host binds configuration into the library options
- **THEN** the library validates required fields and exposes a ready-to-use runtime configuration model

#### Scenario: Invalid configuration startup failure
- **WHEN** required endpoint, certificate, or service configuration is missing or invalid
- **THEN** the library MUST fail initialization with a typed configuration error describing the missing or invalid fields

### Requirement: Timeout and retry policy enforcement
The library MUST enforce per-operation timeout and bounded retry behavior for transient transport failures.

#### Scenario: Transient transport failure with retry
- **WHEN** a SOAP call fails due to a transient transport condition classified as retryable
- **THEN** the library retries according to configured limits and backoff policy before returning failure

#### Scenario: Non-retryable failure
- **WHEN** a SOAP call fails with a non-retryable error classification
- **THEN** the library returns failure immediately without additional retries

### Requirement: Deterministic service endpoint selection
The library SHALL resolve WSAA and WSFEv1 endpoints deterministically by configured environment profile.

#### Scenario: Homologation profile selection
- **WHEN** runtime configuration selects homologation profile
- **THEN** the library uses homologation endpoints for WSAA and WSFEv1 invocations

#### Scenario: Production profile selection
- **WHEN** runtime configuration selects production profile
- **THEN** the library uses production endpoints for WSAA and WSFEv1 invocations
