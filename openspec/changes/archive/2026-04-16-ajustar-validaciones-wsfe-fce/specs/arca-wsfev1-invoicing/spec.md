## MODIFIED Requirements

### Requirement: Request validation before WSFE authorization
The library MUST reject invalid voucher combinations before sending `FECAESolicitar` to WSFE whenever the rules can be determined from the request payload and official WSFE parameter catalogs.

#### Scenario: FCE request without mandatory payment due date
- **WHEN** a consumer submits an FCE voucher (`VoucherType` 201, 202, 203, 206, 207, or 208) without `ServicePaymentDueDate`
- **THEN** the library MUST reject the request with a typed validation error before calling WSFE
- **THEN** the error message MUST mention `ServicePaymentDueDate`

#### Scenario: FCE request with malformed payment due date
- **WHEN** a consumer submits an FCE voucher with `ServicePaymentDueDate` that is not a valid `yyyyMMdd` date
- **THEN** the library MUST reject the request with a typed validation error before calling WSFE

---

### Requirement: Official recipient VAT condition validation for FCE
The library MUST validate the recipient VAT condition of FCE vouchers against the official WSFE parameter catalog instead of accepting any positive integer.

#### Scenario: FCE-B uses unsupported recipient VAT condition
- **WHEN** a consumer submits an FCE-B voucher (`VoucherType` 206 or 208) with a `RecipientVatConditionId` that is not present or not enabled in the official `FEParamGetCondicionIvaReceptor` catalog for that scenario
- **THEN** the library MUST reject the request before calling WSFE
- **THEN** the error MUST identify `RecipientVatConditionId` and the FCE voucher type

#### Scenario: FCE request uses recipient VAT condition from official catalog
- **WHEN** a consumer submits an FCE voucher with a `RecipientVatConditionId` that exists in the official catalog and is compatible with the voucher type
- **THEN** the library MUST allow the request to continue to SOAP serialization

#### Scenario: Official catalog lookup is unavailable
- **WHEN** the library cannot refresh `FEParamGetCondicionIvaReceptor` from WSFEParam
- **THEN** it MUST use a still-valid cache entry if one exists for the current environment
- **THEN** otherwise it MUST fail the authorization flow with a typed error instead of silently skipping the validation

---

### Requirement: Valid associated vouchers for FCE credit notes
The library MUST validate that each FCE credit note references only voucher types allowed for that credit note.

#### Scenario: NC-FCE-A references a non-FCE-A voucher
- **WHEN** a consumer submits `VoucherType` 203 with `AssociatedVouchers` containing an entry whose `Type` is not valid for NC-FCE-A
- **THEN** the library MUST reject the request with a typed validation error before calling WSFE

#### Scenario: NC-FCE-B references a non-FCE-B voucher
- **WHEN** a consumer submits `VoucherType` 208 with `AssociatedVouchers` containing an entry whose `Type` is not valid for NC-FCE-B
- **THEN** the library MUST reject the request with a typed validation error before calling WSFE

#### Scenario: FCE credit note without associated vouchers
- **WHEN** a consumer submits `VoucherType` 203 or 208 without any associated vouchers
- **THEN** the library MUST reject the request with a typed validation error stating that at least one associated voucher is required

---

### Requirement: Reproducible FCE validations in automated tests
The library MUST cover the FCE validation rules with automated tests so that official-parameter-based behavior is reproducible without manual WSFE trials.

#### Scenario: Automated tests cover FCE validation matrix
- **WHEN** the FCE validation suite is executed
- **THEN** it MUST include negative cases for missing `ServicePaymentDueDate`, invalid `RecipientVatConditionId`, and invalid associated voucher types for NC-FCE
- **THEN** it MUST include positive cases for valid FCE-A and FCE-B requests using catalog-backed recipient VAT conditions