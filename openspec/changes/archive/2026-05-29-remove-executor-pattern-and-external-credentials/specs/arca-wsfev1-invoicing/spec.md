## MODIFIED Requirements

### Requirement: Voucher authorization flow
The library SHALL process consumer-provided WSAA `Token` and `Sign` as the normal primary credentials for voucher authorization and MUST fall back to WSAA credential issuance when those credentials are missing or unusable.

#### Scenario: Authorization with consumer-provided valid credentials
- **WHEN** a consumer submits a voucher authorization request including `Token` and `Sign` that are valid for the target service and not expired
- **THEN** the library MUST use those credentials to call WSFEv1
- **THEN** the library MUST NOT invoke WSAA for that operation

#### Scenario: Authorization without credentials in request
- **WHEN** a consumer submits a voucher authorization request without `Token` and `Sign`
- **THEN** the library MUST obtain credentials from WSAA via the existing authentication flow
- **THEN** the library MUST continue the WSFEv1 authorization using the obtained credentials

#### Scenario: Authorization with expired or invalid credentials
- **WHEN** a consumer submits a voucher authorization request with `Token` and `Sign` that are expired or rejected as unusable
- **THEN** the library MUST execute fallback to WSAA to obtain fresh credentials
- **THEN** the library MUST retry the WSFEv1 authorization using the refreshed credentials

---

## ADDED Requirements

### Requirement: Voucher authorization orchestration without executor pattern
The library MUST execute WSFE voucher authorization through linear service orchestration and MUST NOT require executor abstractions to coordinate authorization stages.

#### Scenario: Authorization path is resolved by direct service logic
- **WHEN** a consumer requests voucher authorization
- **THEN** the invoicing service MUST perform validation, credential resolution, WSFE invocation and response mapping in a direct ordered flow
- **THEN** no executor component MUST be required in the authorization path