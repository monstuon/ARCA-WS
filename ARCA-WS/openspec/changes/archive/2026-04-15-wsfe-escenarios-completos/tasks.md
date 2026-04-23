## 1. Domain Model — Service Dates And Credit Note Association

- [x] 1.1 Add optional `ServiceDateFrom`, `ServiceDateTo`, and `ServicePaymentDueDate` fields (date strings in `yyyyMMdd` format) to `VoucherRequest` in `Domain/Wsfe/WsfeModels.cs`.
- [x] 1.2 Add optional `AssociatedVoucher` value with `Type`, `PointOfSale`, `Number`, and `Cuit` to `VoucherRequest` for credit note linkage.

## 2. Validation — Multi-Receiver And Service Concept

- [x] 2.1 Enforce that when `DocumentType` is `80` (Responsable Inscripto) or `86` (Exento), `DocumentNumber` must be a non-zero 11-digit numeric CUIT.
- [x] 2.2 Enforce that when `Concept` is `2` (Servicios) or `3` (Productos y Servicios), all three service date fields are present and form a valid date range with `ServiceDateFrom` ≤ `ServiceDateTo` ≤ `ServicePaymentDueDate`.
- [x] 2.3 Add/adjust unit tests in `tests/ARCA-WS.Tests` for the new receiver-type and service-concept validation rules, covering valid and invalid cases.

## 3. Validation — Credit Note Association And Foreign Currency

- [x] 3.1 Enforce that when `VoucherType` corresponds to a credit note (types 3, 7, and 8), `AssociatedVoucher` is present and all four association fields are non-empty.
- [x] 3.2 Enforce that when `CurrencyId` is not `PES`, `CurrencyRate` is present and greater than zero; enforce that `CurrencyRate` is exactly `1` when `CurrencyId` is `PES`.
- [x] 3.3 Add/adjust unit tests for credit note association validation and foreign currency rate validation.

## 4. SOAP Mapping — Service Dates, CbteAsoc, And Currency Rate

- [x] 4.1 Map `ServiceDateFrom`, `ServiceDateTo`, and `ServicePaymentDueDate` into the `FECAESolicitar` SOAP request detail when `Concept` is 2 or 3.
- [x] 4.2 Map the `CbteAsoc` array element in the SOAP request using `AssociatedVoucher` fields when the field is present.
- [x] 4.3 Map `MonId` and `MonCotiz` from `CurrencyId` and `CurrencyRate` in the SOAP request for all voucher types.
- [x] 4.4 Add unit tests for SOAP request generation covering service dates, `CbteAsoc`, and non-PES currency fields.

## 5. SOAP Infrastructure — XML Request And Response Logging

- [x] 5.1 Log the raw SOAP XML envelope before sending in `WsfeSoapClient` at debug level, including operation name and correlation id.
- [x] 5.2 Log the raw SOAP XML response after receiving at debug level, preserving existing error propagation behavior on malformed responses.

## 6. PilotConsumer — Full Emission Scenario Matrix

- [x] 6.1 Add scenario: Consumidor Final — Productos (VoucherType=6, DocType=99, DocNum=0, Concepto=1, ARS 1000). Retrieve last voucher before submitting.
- [x] 6.2 Add scenario: Consumidor Final — Servicios (VoucherType=6, DocType=99, DocNum=0, Concepto=2, service dates for current month, ARS 1000).
- [x] 6.3 Add scenario: Responsable Inscripto — Productos (VoucherType=1, DocType=80, DocNum=configured CUIT, Concepto=1, ARS 1210 with 21% IVA).
- [x] 6.4 Add scenario: Responsable Inscripto — Servicios (VoucherType=1, DocType=80, DocNum=configured CUIT, Concepto=2, service dates, ARS 1210 with 21% IVA).
- [x] 6.5 Add scenario: Exento — Productos (VoucherType=6, DocType=86, DocNum=configured CUIT, Concepto=1, ARS 1000 exento).
- [x] 6.6 Add scenario: Exento — Servicios (VoucherType=6, DocType=86, DocNum=configured CUIT, Concepto=2, service dates, ARS 1000 exento).
- [x] 6.7 Add scenario: Nota de Crédito B (VoucherType=7) associated to the voucher number obtained from scenario 6.1; inform Type, PointOfSale, Number, and Cuit in AssociatedVoucher.
- [x] 6.8 Add scenario: Moneda Extranjera — USD (VoucherType=6, DocType=99, CurrencyId=DOL, CurrencyRate=configured rate, TotalAmount in USD equivalent).
- [x] 6.9 Ensure each scenario calls `GetLastAuthorizedVoucherAsync` for the matching VoucherType and PointOfSale, increments by 1, and uses the result as the voucher number. Validate WSAA token cache is live before the first scenario and log token expiry.
- [x] 6.10 Print per-scenario summary: scenario name, voucher number submitted, CAE (or first rejection error), CAE expiration. Exit non-zero if any scenario fails authorization.

## 7. Automated Test Coverage

- [x] 7.1 Run existing WSFE test suite and fix any regressions introduced by model or mapping changes.
- [x] 7.2 Add integration-level service tests verifying functional error propagation for each new validation rule (RI, Exento, services, NC, foreign currency).

## 8. Documentation And Readiness

- [x] 8.1 Verify change artifacts (proposal, tasks, spec deltas) are consistent with implemented code paths and update any deviations discovered during implementation.
