## ADDED Requirements

### Requirement: FEParamGetPtosVenta for CAEA-enabled points of sale
The library MUST expose WSFEv1 `FEParamGetPtosVenta` using authenticated context (`Token`, `Sign`, `Cuit`) and return typed data for enabled points of sale for CAEA operations.

#### Scenario: Query enabled points of sale
- **WHEN** a consumer requests `PuntosHabilitadosCaea`
- **THEN** the library MUST call WSFEv1 `FEParamGetPtosVenta` with valid authentication fields
- **THEN** the library MUST return the list of enabled points of sale in a typed response

#### Scenario: WSFE returns business error on point-of-sale query
- **WHEN** WSFEv1 responds with functional errors for `FEParamGetPtosVenta`
- **THEN** the library MUST surface typed error information including WSFE error code and message

---

### Requirement: FECompConsultar voucher lookup
The library MUST expose WSFEv1 `FECompConsultar` to query previously issued vouchers.

#### Scenario: Query existing voucher
- **WHEN** a consumer requests `ConsultarComprobante` with valid voucher identifiers
- **THEN** the library MUST call WSFEv1 `FECompConsultar` with authenticated context
- **THEN** the library MUST return typed voucher data including authorization status and fiscal metadata when available

#### Scenario: Voucher not found or rejected query
- **WHEN** WSFEv1 indicates the voucher does not exist or query is functionally invalid
- **THEN** the library MUST return typed functional error information preserving WSFE code/message

---

### Requirement: FECAEAConsultar CAEA lookup
The library MUST expose WSFEv1 `FECAEAConsultar` to consult an existing CAEA period/code.

#### Scenario: Query CAEA data
- **WHEN** a consumer requests `CAEAConsultar` for a specific period and order
- **THEN** the library MUST call WSFEv1 `FECAEAConsultar` with authenticated context
- **THEN** the library MUST return typed CAEA data including CAEA code and validity window when provided by WSFEv1

#### Scenario: Invalid CAEA query parameters
- **WHEN** WSFEv1 rejects `FECAEAConsultar` due to invalid period/order or business rules
- **THEN** the library MUST expose functional errors with WSFE detail codes/messages

---

### Requirement: FECAEASolicitar CAEA issuance
The library MUST expose WSFEv1 `FECAEASolicitar` to request CAEA for a valid period.

#### Scenario: Successful CAEA request
- **WHEN** a consumer requests `CAEASolicitar` with valid period/order data
- **THEN** the library MUST call WSFEv1 `FECAEASolicitar` with authenticated context
- **THEN** the library MUST return typed CAEA issuance data including the assigned CAEA and period validity

#### Scenario: Rejected CAEA request
- **WHEN** WSFEv1 rejects `FECAEASolicitar`
- **THEN** the library MUST return typed functional errors and observations from WSFEv1

---

### Requirement: FECAEARegInformativo informative CAEA reporting
The library MUST expose WSFEv1 `FECAEARegInformativo` with a request/response structure equivalent to the current voucher-emission flow, adapted to CAEA registration semantics.

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