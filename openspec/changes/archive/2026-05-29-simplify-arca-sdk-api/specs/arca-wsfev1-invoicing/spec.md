## MODIFIED Requirements

### Requirement: Voucher authorization flow
The library SHALL support voucher authorization with externally provided WSAA credentials and MUST fall back to WSAA credential issuance when external credentials are missing or unusable.

The authorization flow MUST be invocable through `ArcaClient` as the primary public entrypoint and MUST execute through an explicit internal path to WSFEv1 SOAP operations.

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

### Requirement: API-level stateless credential handling
The library MUST remain stateless regarding durable credential storage while still allowing short-lived in-memory optimization.

#### Scenario: No persistent token storage in API
- **WHEN** the authorization flow completes after reusing or renewing credentials
- **THEN** the library MUST NOT persist Token/Sign in durable storage managed by the API

#### Scenario: Optional short-lived in-memory optimization
- **WHEN** multiple authorization requests arrive concurrently in a single API instance
- **THEN** the library MAY use short-lived in-memory credential cache to reduce redundant WSAA calls
- **THEN** this optimization MUST NOT change the external responsibility model where ERP remains the primary credential owner

### Requirement: FEParamGetPtosVenta for CAEA-enabled points of sale
The library MUST expose WSFEv1 `FEParamGetPtosVenta` using authenticated context (`Token`, `Sign`, `Cuit`) and return typed data for enabled points of sale for CAEA operations.

The operation MUST be available through `ArcaClient` with an explicit, traceable orchestration path to the SOAP request layer.

#### Scenario: Query enabled points of sale
- **WHEN** a consumer requests `PuntosHabilitadosCaea`
- **THEN** the library MUST call WSFEv1 `FEParamGetPtosVenta` with valid authentication fields
- **THEN** the library MUST return the list of enabled points of sale in a typed response

#### Scenario: WSFE returns business error on point-of-sale query
- **WHEN** WSFEv1 responds with functional errors for `FEParamGetPtosVenta`
- **THEN** the library MUST surface typed error information including WSFE error code and message

### Requirement: FECompConsultar voucher lookup
The library MUST expose WSFEv1 `FECompConsultar` to query previously issued vouchers.

The operation MUST be available through `ArcaClient` with typed request and response contracts.

#### Scenario: Query existing voucher
- **WHEN** a consumer requests `ConsultarComprobante` with valid voucher identifiers
- **THEN** the library MUST call WSFEv1 `FECompConsultar` with authenticated context
- **THEN** the library MUST return typed voucher data including authorization status and fiscal metadata when available

#### Scenario: Voucher not found or rejected query
- **WHEN** WSFEv1 indicates the voucher does not exist or query is functionally invalid
- **THEN** the library MUST return typed functional error information preserving WSFE code/message

### Requirement: FECAEAConsultar CAEA lookup
The library MUST expose WSFEv1 `FECAEAConsultar` to consult an existing CAEA period/code.

The operation MUST be available through `ArcaClient` and preserve typed CAEA result mapping.

#### Scenario: Query CAEA data
- **WHEN** a consumer requests `CAEAConsultar` for a specific period and order
- **THEN** the library MUST call WSFEv1 `FECAEAConsultar` with authenticated context
- **THEN** the library MUST return typed CAEA data including CAEA code and validity window when provided by WSFEv1

#### Scenario: Invalid CAEA query parameters
- **WHEN** WSFEv1 rejects `FECAEAConsultar` due to invalid period/order or business rules
- **THEN** the library MUST expose functional errors with WSFE detail codes/messages

### Requirement: FECAEASolicitar CAEA issuance
The library MUST expose WSFEv1 `FECAEASolicitar` to request CAEA for a valid period.

The operation MUST be invocable through `ArcaClient` while preserving typed domain DTOs for request and response.

#### Scenario: Successful CAEA request
- **WHEN** a consumer requests `CAEASolicitar` with valid period/order data
- **THEN** the library MUST call WSFEv1 `FECAEASolicitar` with authenticated context
- **THEN** the library MUST return typed CAEA issuance data including the assigned CAEA and period validity

#### Scenario: Rejected CAEA request
- **WHEN** WSFEv1 rejects `FECAEASolicitar`
- **THEN** the library MUST return typed functional errors and observations from WSFEv1

### Requirement: FECAEARegInformativo informative CAEA reporting
The library MUST expose WSFEv1 `FECAEARegInformativo` with a request/response structure equivalent to the current voucher-emission flow, adapted to CAEA registration semantics.

The operation MUST be invocable through `ArcaClient` and preserve explicit mapping traceability from DTOs to SOAP payload.

#### Scenario: Successful informative CAEA registration
- **WHEN** a consumer submits `CAEARegInformativo` with valid detail data and authentication
- **THEN** the library MUST call WSFEv1 `FECAEARegInformativo`
- **THEN** the library MUST map and return typed response fields for accepted/rejected details

#### Scenario: Structural parity with current emission flow
- **WHEN** the library builds `FECAEARegInformativo` request payload
- **THEN** it MUST preserve the same structural mapping approach currently used for emission (auth header + detail mapping + totals consistency)
- **THEN** it MUST adapt field-level semantics required by CAEA registration without breaking existing emission behavior

#### Scenario: Rejected informative registration
- **WHEN** WSFEv1 returns business errors for `FECAEARegInformativo`
- **THEN** the library MUST expose typed rejection details with WSFE codes and messages
