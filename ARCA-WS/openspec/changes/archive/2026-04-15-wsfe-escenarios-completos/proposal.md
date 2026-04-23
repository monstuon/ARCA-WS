## Why

The library can currently authorize a single scenario: Factura B to final consumer for ARS products. The full homologation validation matrix requires covering all relevant receiver types (Consumidor Final, Responsable Inscripto, Exento), both product and service concepts with correct service date fields, credit note issuance with voucher association, and foreign currency invoicing (USD). Without these scenarios validated end-to-end, the emission workflow cannot be considered production-ready for the typical Argentine B2B and B2C sales mix.

## What Changes

- Extend `VoucherRequest` to carry optional service date fields (`ServiceDateFrom`, `ServiceDateTo`, `ServicePaymentDueDate`) and an optional `AssociatedVoucher` value for credit note linkage.
- Extend `WsfeRequestValidator` to enforce: CUIT-based document type/number for Responsable Inscripto (DocType 80) and Exento (DocType 86); mandatory service dates when Concepto is 2 or 3; mandatory `AssociatedVoucher` data when voucher type is a credit note; and mandatory non-zero `CurrencyRate` when `CurrencyId` is not PES.
- Extend `WsfeSoapClient` to map the new fields — service dates, `CbteAsoc` array, and non-PES currency rate — into the `FECAESolicitar` SOAP request, and to log the raw XML envelope sent and received for each operation.
- Expand `samples/PilotConsumer` with eight scenario functions covering the full emission matrix: three receiver types × two concepts, one credit note, and one USD invoice. Each scenario retrieves the last authorized voucher number before submitting, logs correlation id, CAE, and expiry, and exits non-zero on failure.

## Capabilities

### Modified Capabilities

- `arca-wsfev1-invoicing`: Validation and SOAP mapping are extended from a single consumer-final/products/ARS shape to the full emission matrix — multi-receiver types, services concept with date fields, credit note association, and foreign currency.
- `wsfe-homologation-b2c-invoice-example`: Sample is expanded from one fixed scenario to the complete eight-scenario emission matrix demonstrating all supported receiver types, concepts, credit note, and foreign currency.

## Impact

- Affected code:
  - `Domain/Wsfe/WsfeModels.cs`
  - `Application/Wsfe/WsfeRequestValidator.cs`
  - `Application/Wsfe/Wsfev1InvoicingService.cs`
  - `Infrastructure/Wsfe/WsfeSoapClient.cs`
  - `samples/PilotConsumer/Program.cs`
  - WSFE-related tests under `tests/ARCA-WS.Tests`
- Public API impact: `VoucherRequest` gains optional fields; no breaking changes to existing callers. `AuthorizeVoucherAsync` signature is unchanged.
- Operational impact: homologation executions will exercise all code paths; valid WSAA token with sufficient permissions and a configured certificate/CUIT are required for each scenario run.
