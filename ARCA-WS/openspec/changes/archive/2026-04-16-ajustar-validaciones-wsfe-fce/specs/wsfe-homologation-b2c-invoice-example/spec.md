## MODIFIED Requirements

### Requirement: PilotConsumer FCE scenarios reflect real WSFE rules
The sample MUST keep its FCE scenarios aligned with the same request validation rules enforced by the library.

#### Scenario: FCE-A scenario includes mandatory FCE fields
- **WHEN** the pilot runs the `FCE-A` scenario
- **THEN** it MUST populate `ServicePaymentDueDate`
- **THEN** it MUST use an identified recipient and a recipient VAT condition accepted by the library for `VoucherType` 201
- **THEN** it MUST include only data that would pass the pre-send FCE validation

#### Scenario: FCE-B scenario uses catalog-compatible recipient VAT condition
- **WHEN** the pilot runs the `FCE-B` scenario
- **THEN** it MUST use a `RecipientVatConditionId` that is compatible with `VoucherType` 206 according to the official WSFE recipient VAT condition catalog
- **THEN** it MUST NOT rely on assumptions from older examples that contradict the live validation rules

#### Scenario: NC-FCE scenarios are skipped when parent voucher is unavailable
- **WHEN** `FCE-A` or `FCE-B` was not authorized previously in the pilot session
- **THEN** the corresponding `NC-FCE-A` or `NC-FCE-B` scenario MUST be skipped before building an invalid association payload
- **THEN** the pilot MUST log and print that the scenario was omitted because the parent voucher is unavailable

#### Scenario: NC-FCE scenario uses only valid association types
- **WHEN** the pilot runs `NC-FCE-A` or `NC-FCE-B`
- **THEN** it MUST build `AssociatedVouchers` only with voucher types accepted by the library for the NC-FCE type being issued
- **THEN** it MUST NOT send association data for unavailable or incompatible voucher types

---

### Requirement: Example and tests stay aligned for FCE
The sample documentation and automated tests MUST describe the same FCE assumptions used by the runtime code.

#### Scenario: FCE assumptions are documented consistently
- **WHEN** the FCE example, pilot code, and automated tests are reviewed together
- **THEN** they MUST agree on mandatory `ServicePaymentDueDate`, recipient identification, recipient VAT condition constraints, and NC-FCE association rules
- **THEN** no example MUST describe `FCE-B` with a receiver configuration that the validator would reject