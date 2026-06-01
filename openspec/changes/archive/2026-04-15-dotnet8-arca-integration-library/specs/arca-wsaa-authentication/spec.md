## ADDED Requirements

### Requirement: WSAA authentication lifecycle management
The library SHALL generate a valid LoginTicketRequest (TRA), sign it with the configured X.509 certificate, invoke WSAA loginCms, and return usable authentication credentials (Token and Sign) for ARCA services.

#### Scenario: Successful WSAA credential issuance
- **WHEN** a consumer requests authentication credentials for an ARCA service and no valid cached credentials exist
- **THEN** the library creates and signs the TRA, invokes WSAA loginCms, validates the response, and returns Token/Sign with expiration metadata

#### Scenario: WSAA authentication failure surface
- **WHEN** WSAA returns a protocol, validation, or transport error during login
- **THEN** the library MUST return a typed authentication error including operation context and actionable diagnostics

### Requirement: Credential caching and proactive renewal
The library SHALL cache Token/Sign per service and environment and MUST proactively renew credentials before expiration using a configurable renewal window.

#### Scenario: Cached credentials reuse
- **WHEN** a consumer requests credentials and a non-expired Token/Sign is available in cache
- **THEN** the library returns cached credentials without invoking WSAA

#### Scenario: Proactive renewal before expiration
- **WHEN** credentials are within the configured renewal window
- **THEN** the library obtains new credentials from WSAA and replaces the cache entry atomically

### Requirement: Certificate source abstraction
The library SHALL support loading certificates from file path and operating system certificate store using configuration-only selection.

#### Scenario: Certificate from file
- **WHEN** certificate configuration indicates file-based loading
- **THEN** the library loads the certificate and private key from the configured file source and validates usability for signing

#### Scenario: Certificate from store
- **WHEN** certificate configuration indicates store-based loading
- **THEN** the library resolves the certificate by configured selector and validates it can sign TRA payloads
