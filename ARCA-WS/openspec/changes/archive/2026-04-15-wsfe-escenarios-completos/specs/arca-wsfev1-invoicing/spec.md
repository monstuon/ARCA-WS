## ADDED Requirements

### Requirement: Multi-receiver voucher authorization
The library MUST support voucher authorization for all three receiver types used in the Argentine electronic invoicing matrix.

#### Scenario: Responsable Inscripto receiver
- **WHEN** a consumer submits a voucher request with `DocumentType=80` (CUIT)
- **THEN** the library MUST validate that `DocumentNumber` is a non-zero 11-digit numeric CUIT
- **THEN** the library MUST map the document type and number into the `FECAESolicitar` SOAP request detail and return a typed authorization outcome

#### Scenario: Exento receiver
- **WHEN** a consumer submits a voucher request with `DocumentType=86` (CUIT Exento)
- **THEN** the library MUST validate that `DocumentNumber` is a non-zero 11-digit numeric CUIT
- **THEN** the library MUST map the document type and number into the `FECAESolicitar` SOAP request detail and return a typed authorization outcome

### Requirement: Services concept with mandatory date fields
The library MUST require and map service date fields when the voucher concept includes services.

#### Scenario: Services concept date validation
- **WHEN** a consumer submits a voucher request with `Concept=2` (Servicios) or `Concept=3` (Productos y Servicios)
- **THEN** the library MUST reject the request with a typed validation error if any of `ServiceDateFrom`, `ServiceDateTo`, or `ServicePaymentDueDate` is absent
- **THEN** the library MUST reject the request if `ServiceDateFrom` is after `ServiceDateTo`

#### Scenario: Services concept SOAP mapping
- **WHEN** a consumer submits a valid voucher request with services concept and all date fields present
- **THEN** the library MUST map `ServiceDateFrom`, `ServiceDateTo`, and `ServicePaymentDueDate` into the corresponding `FECAESolicitar` SOAP fields (`FchServDesde`, `FchServHasta`, `FchVtoPago`)

### Requirement: Credit note voucher association
The library MUST require and map credit note association data when the voucher type is a credit note.

#### Scenario: Credit note association validation
- **WHEN** a consumer submits a voucher request with a credit note type (3, 7, or 8)
- **THEN** the library MUST reject the request with a typed validation error if `AssociatedVoucher` is absent or any of its fields — `Type`, `PointOfSale`, `Number`, or `Cuit` — is missing

#### Scenario: Credit note association SOAP mapping
- **WHEN** a consumer submits a valid credit note request with `AssociatedVoucher` populated
- **THEN** the library MUST map all four association fields into the `CbteAsoc` array element of the `FECAESolicitar` SOAP request

### Requirement: Foreign currency voucher support
The library MUST require a non-zero currency rate and map the currency fields for all non-ARS vouchers.

#### Scenario: Foreign currency rate validation
- **WHEN** a consumer submits a voucher request with `CurrencyId` other than `PES`
- **THEN** the library MUST reject the request with a typed validation error if `CurrencyRate` is absent or not greater than zero

#### Scenario: ARS currency rate consistency
- **WHEN** a consumer submits a voucher request with `CurrencyId=PES`
- **THEN** the library MUST reject the request if `CurrencyRate` is present and not equal to 1

#### Scenario: Foreign currency SOAP mapping
- **WHEN** a valid voucher request specifies a non-PES currency and a positive rate
- **THEN** the library MUST map `CurrencyId` and `CurrencyRate` to `MonId` and `MonCotiz` in the `FECAESolicitar` SOAP request

### Requirement: SOAP XML diagnostic logging
The library MUST log the raw SOAP XML envelope for each `FECAESolicitar` operation to support diagnostic traceability.

#### Scenario: Request XML is logged
- **WHEN** the library is about to send a `FECAESolicitar` SOAP call
- **THEN** the library MUST emit the full XML request envelope at debug log level including the operation name and correlation id

#### Scenario: Response XML is logged
- **WHEN** the library receives a `FECAESolicitar` SOAP response
- **THEN** the library MUST emit the raw XML response at debug log level before parsing, and MUST NOT suppress existing error propagation on malformed responses
