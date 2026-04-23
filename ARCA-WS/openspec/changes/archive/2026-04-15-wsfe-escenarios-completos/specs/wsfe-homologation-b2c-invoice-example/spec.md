## ADDED Requirements

### Requirement: Sample covers all receiver type and concept combinations
The sample consumer SHALL provide runnable homologation flows for all three receiver types (Consumidor Final, Responsable Inscripto, Exento) with both product and service concepts.

#### Scenario: Consumidor Final — Productos authorization
- **WHEN** the operator runs the sample with valid homologation configuration
- **THEN** the sample MUST submit a voucher with `VoucherType=6`, `DocumentType=99`, `DocumentNumber=0`, `Concept=1`, `CurrencyId=PES`, `CurrencyRate=1`, and `TotalAmount=1000`
- **THEN** the sample MUST retrieve the last authorized voucher number for this type before submission and use the incremented value as the voucher number

#### Scenario: Consumidor Final — Servicios authorization
- **WHEN** the operator runs the sample
- **THEN** the sample MUST submit a voucher with `VoucherType=6`, `DocumentType=99`, `DocumentNumber=0`, `Concept=2`, service dates covering the current calendar month, and `TotalAmount=1000`

#### Scenario: Responsable Inscripto — Productos authorization
- **WHEN** the operator runs the sample with a configured CUIT for RI scenarios
- **THEN** the sample MUST submit a voucher with `VoucherType=1`, `DocumentType=80`, `DocumentNumber` set to the configured CUIT, `Concept=1`, and totals consistent with 21% net IVA

#### Scenario: Responsable Inscripto — Servicios authorization
- **WHEN** the operator runs the sample
- **THEN** the sample MUST submit a voucher with `VoucherType=1`, `DocumentType=80`, `DocumentNumber` set to the configured CUIT, `Concept=2`, and service dates covering the current calendar month

#### Scenario: Exento — Productos authorization
- **WHEN** the operator runs the sample with a configured CUIT for Exento scenarios
- **THEN** the sample MUST submit a voucher with `VoucherType=6`, `DocumentType=86`, `DocumentNumber` set to the configured CUIT, `Concept=1`, and the total amount classified as exempt

#### Scenario: Exento — Servicios authorization
- **WHEN** the operator runs the sample
- **THEN** the sample MUST submit a voucher with `VoucherType=6`, `DocumentType=86`, `DocumentNumber` set to the configured CUIT, `Concept=2`, and service dates covering the current calendar month

### Requirement: Sample issues a credit note associated to an existing voucher
The sample SHALL include a credit note scenario that correctly links to a previously authorized voucher from the same run.

#### Scenario: Nota de Crédito B authorization
- **WHEN** the operator runs the sample after at least one Factura B has been authorized in the same run
- **THEN** the sample MUST submit a voucher with `VoucherType=7` and `AssociatedVoucher` populated with the type, point-of-sale, number, and CUIT of the previously authorized Factura B
- **THEN** the sample MUST print the CAE and expiration of the credit note when approved

### Requirement: Sample issues a voucher in foreign currency
The sample SHALL include a USD invoice scenario that provides `CurrencyId` and `CurrencyRate`.

#### Scenario: USD voucher authorization
- **WHEN** the operator runs the sample with a configured USD exchange rate
- **THEN** the sample MUST submit a voucher with `VoucherType=6`, `DocumentType=99`, `CurrencyId=DOL`, `CurrencyRate` set to the configured rate, and a total amount expressed in USD

### Requirement: Sample output covers all scenario results and is actionable for test evidence
The sample SHALL emit per-scenario output sufficient for homologation evidence and troubleshooting.

#### Scenario: Per-scenario result is logged
- **WHEN** each scenario completes
- **THEN** the sample MUST log the scenario name, voucher number submitted, and either the CAE with expiration date (when approved) or the first error code and message (when rejected)

#### Scenario: WSAA token state is reported before scenarios execute
- **WHEN** the sample starts
- **THEN** the sample MUST log whether WSAA credentials were retrieved from cache or freshly issued, and MUST log the token expiration timestamp

#### Scenario: Sample exits non-zero on any failure
- **WHEN** any scenario results in a rejected authorization or an unhandled exception
- **THEN** the sample MUST exit with a non-zero exit code after completing all other scenarios
