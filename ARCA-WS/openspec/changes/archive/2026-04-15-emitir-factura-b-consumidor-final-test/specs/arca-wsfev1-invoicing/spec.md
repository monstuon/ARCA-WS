## ADDED Requirements

### Requirement: FECAESolicitar mapped authorization request
The library MUST map `VoucherRequest` data to WSFEv1 `FECAESolicitar` SOAP structures and return typed authorization outcomes.

#### Scenario: Successful mapped authorization
- **WHEN** a consumer submits a valid voucher authorization request
- **THEN** the library MUST invoke WSFEv1 `FECAESolicitar` with authenticated credentials and mapped voucher data
- **THEN** the library MUST return approval data including CAE and CAE expiration date

#### Scenario: Rejected mapped authorization
- **WHEN** WSFEv1 rejects the voucher request
- **THEN** the library MUST expose typed rejection details including WSFE error code and message

#### Scenario: Factura B consumer-final request shape
- **WHEN** a consumer submits a homologation request for Factura B to final consumer
- **THEN** the library MUST map document type `99` and document number `0` in WSFE request detail
- **THEN** the library MUST preserve monetary consistency so the total equals net plus VAT plus exempt plus taxes
