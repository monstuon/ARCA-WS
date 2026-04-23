## ADDED Requirements

### Requirement: Voucher authorization flow
The library SHALL support voucher authorization with externally provided WSAA credentials and MUST fall back to WSAA credential issuance when external credentials are missing or unusable.

#### Scenario: Authorization with externally provided valid credentials
- **WHEN** a consumer submits a voucher authorization request including `Token` and `Sign` that are valid for the target service and not expired
- **THEN** the library MUST use those credentials to call WSFEv1
- **THEN** the library MUST NOT invoke WSAA for that operation

#### Scenario: Authorization without external credentials
- **WHEN** a consumer submits a voucher authorization request without `Token` and `Sign`
- **THEN** the library MUST obtain credentials from WSAA via the existing authentication flow
- **THEN** the library MUST continue the WSFEv1 authorization using the obtained credentials

#### Scenario: Authorization with expired or invalid external credentials
- **WHEN** a consumer submits a voucher authorization request with external `Token` and `Sign` that are expired or rejected as unusable
- **THEN** the library MUST execute fallback to WSAA to obtain fresh credentials
- **THEN** the library MUST retry the WSFEv1 authorization using the refreshed credentials

---

### Requirement: Authorization response includes refreshed credentials
The library MUST return credential metadata in the authorization response whenever credentials are generated or renewed by the API during fallback.

#### Scenario: Response includes credentials after fallback renewal
- **WHEN** fallback to WSAA is executed and fresh credentials are obtained for the authorization flow
- **THEN** the authorization response MUST include the resulting `Token`, `Sign`, and expiration timestamp
- **THEN** the response MUST allow ERP consumers to persist and reuse those credentials externally

#### Scenario: Response omits credential payload when not renewed
- **WHEN** authorization completes using valid externally provided credentials and no renewal was needed
- **THEN** the response MAY omit credential payload or leave it unchanged according to contract semantics
- **THEN** it MUST remain unambiguous for the consumer whether fallback renewal occurred

---

### Requirement: API-level stateless credential handling
The library MUST remain stateless regarding durable credential storage while still allowing short-lived in-memory optimization.

#### Scenario: No persistent token storage in API
- **WHEN** the authorization flow completes after reusing or renewing credentials
- **THEN** the library MUST NOT persist Token/Sign in durable storage managed by the API

#### Scenario: Optional short-lived in-memory optimization
- **WHEN** multiple authorization requests arrive concurrently in a single API instance
- **THEN** the library MAY use short-lived in-memory credential cache to reduce redundant WSAA calls
- **THEN** this optimization MUST NOT change the external responsibility model where ERP remains the primary credential owner