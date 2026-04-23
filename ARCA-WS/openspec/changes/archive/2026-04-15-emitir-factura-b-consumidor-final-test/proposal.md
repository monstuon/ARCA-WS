## Why

The current library cannot execute an end-to-end voucher authorization in homologation because the WSFE authorization SOAP mapping is still a placeholder. We need a concrete, repeatable scenario to issue a Factura B to final consumer for ARS 1000 using the provided CUIT and certificate test setup.

## What Changes

- Implement WSFEv1 `FECAESolicitar` SOAP request/response mapping in the infrastructure client used by `AuthorizeVoucherAsync`.
- Add a homologation-ready sample flow in `samples/PilotConsumer` that issues a Factura B (type 6) to final consumer and amount ARS 1000.
- Define request defaults and validation for the target scenario: document type `99` (Consumidor Final), document number `0`, currency `PES`, currency rate `1`, and internally consistent totals.
- Improve logs/output in the sample so operators can see CAE, CAE expiration, and the authorized voucher number for test evidence.
- Add/extend automated tests for SOAP mapping and scenario-level validation paths.

## Capabilities

### New Capabilities
- `wsfe-homologation-b2c-invoice-example`: Prescriptive sample workflow for issuing a homologation Factura B to final consumer with fixed test parameters.

### Modified Capabilities
- `arca-wsfev1-invoicing`: Voucher authorization requirement is extended from typed API surface to an actually mapped WSFE `FECAESolicitar` call that can approve or reject real homologation invoices.

## Impact

- Affected code:
  - `Infrastructure/Wsfe/WsfeSoapClient.cs`
  - `Application/Wsfe/Wsfev1InvoicingService.cs` (error propagation expectations)
  - `samples/PilotConsumer/Program.cs`
  - WSFE-related tests under `tests/ARCA-WS.Tests`
- Public API impact: no signature changes expected; behavior changes from placeholder response to real WSFE integration.
- Operational impact: homologation executions will produce real authorization outcomes and require valid certificate/CUIT configuration.
