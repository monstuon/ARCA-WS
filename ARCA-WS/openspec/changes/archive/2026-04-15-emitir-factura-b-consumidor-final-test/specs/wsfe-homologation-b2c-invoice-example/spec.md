## ADDED Requirements

### Requirement: Homologation sample can issue Factura B to final consumer
The sample consumer SHALL provide a runnable homologation flow that authorizes a Factura B for final consumer using configured certificate and CUIT, with total ARS 1000.

#### Scenario: Successful sample authorization
- **WHEN** the operator runs the sample with valid homologation certificate and CUIT configuration
- **THEN** the sample MUST submit a voucher authorization request with `VoucherType=6`, `DocumentType=99`, `DocumentNumber=0`, `CurrencyId=PES`, `CurrencyRate=1`, and `TotalAmount=1000`
- **THEN** the sample MUST print whether authorization was approved and include CAE and CAE expiration when present

### Requirement: Sample output is actionable for test evidence
The sample SHALL provide explicit output that can be used as homologation evidence and troubleshooting context.

#### Scenario: Authorization response is logged
- **WHEN** WSFE returns an authorization response
- **THEN** the sample MUST log correlation id, voucher number intent, approval flag, and first error code/message when rejected
