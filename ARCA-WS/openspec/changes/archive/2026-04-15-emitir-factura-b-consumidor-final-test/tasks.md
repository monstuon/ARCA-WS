## 1. WSFE Authorization SOAP Mapping

- [x] 1.1 Implement SOAP envelope builder for `FECAESolicitar` in `Infrastructure/Wsfe/WsfeSoapClient.cs` using `VoucherRequest` fields and authenticated `Token/Sign/Cuit`.
- [x] 1.2 Execute HTTP POST with correct SOAPAction for authorization and parse response into `VoucherAuthorizationResult`.
- [x] 1.3 Map WSFE business errors (`Errors`) to typed functional outcomes and preserve infrastructure exception wrapping for malformed/unexpected responses.

## 2. Domain Validation And Request Consistency

- [x] 2.1 Verify `WsfeRequestValidator` accepts the target consumer-final scenario (`DocumentType=99`, `DocumentNumber=0`, total consistency) and adjust rules if needed.
- [x] 2.2 Add/adjust tests for voucher total consistency and scenario-specific validation boundaries.

## 3. PilotConsumer Homologation Scenario

- [x] 3.1 Update `samples/PilotConsumer/Program.cs` to build a Factura B request for final consumer with total ARS 1000 and invoke `AuthorizeVoucherAsync`.
- [x] 3.2 Keep current setup for certificate/CUIT/environment and add clear console logging for correlation id, approval flag, CAE, CAE expiration, and rejection details.
- [x] 3.3 Ensure sample exits with non-zero code on failed authorization or unhandled exception.

## 4. Automated Test Coverage

- [x] 4.1 Add unit tests for WSFE authorization SOAP request generation and response parsing (approved and rejected paths).
- [x] 4.2 Extend integration-level service tests to verify functional error propagation for WSFE rejection and success mapping.
- [x] 4.3 Run test suite for affected WSFE modules and fix regressions.

## 5. Documentation And Readiness

- [x] 5.1 Document how to run the homologation authorization sample, including required certificate file and CUIT configuration.
- [x] 5.2 Verify the change artifacts and code references are aligned with the OpenSpec capability deltas.
