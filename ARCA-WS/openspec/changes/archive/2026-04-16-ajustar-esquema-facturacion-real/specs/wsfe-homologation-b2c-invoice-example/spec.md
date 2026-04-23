## ADDED Requirements

### Requirement: Factura con percepción de Ingresos Brutos
The sample MUST demonstrate a complete end-to-end authorization of a voucher that includes a provincial perception (IIBB CABA) alongside a standard 21 % VAT aliquot, covering the `VatBreakdown`, `TaxBreakdown`, and `NonTaxableAmount` fields.

#### Scenario: IIBB perception voucher is authorized
- **WHEN** the pilot runs the "IIBB-Percepcion" scenario
- **THEN** it MUST build a `VoucherRequest` (Factura B, Consumidor Final, Productos) with:
  - `NetAmount: 1000`, `NonTaxableAmount: 0`, `ExemptAmount: 0`, `TotalAmount: 1225`
  - `VatBreakdown: [VatItem(Id=5, BaseAmount=1000, Amount=210)]`
  - `TaxBreakdown: [TaxItem(Id=2, Description="Percepción IIBB CABA", BaseAmount=1000, Rate=1.5m, Amount=15)]`
- **THEN** it MUST retrieve the last authorized voucher number for that type and point of sale before submitting
- **THEN** on authorization success it MUST print: scenario name, voucher number, CAE, CAE expiration, `ImpTrib` (15), and `ImpTotConc` (0)
- **THEN** on rejection it MUST print the first error code and message, set `anyFailed = true`, and continue to the next scenario

---

### Requirement: Nota de Crédito con múltiples comprobantes asociados
The sample MUST demonstrate a credit note that cancels two previously authorized vouchers by populating `AssociatedVouchers` with two entries.

#### Scenario: Multi-link credit note is authorized
- **WHEN** the pilot runs the "NC-Multi-Vinculo" scenario and both CF-Productos and RI-Productos have been authorized
- **THEN** it MUST build a `VoucherRequest` (Nota de Crédito B, VoucherType=7, Consumidor Final) with `AssociatedVouchers` containing:
  - Entry 1: `AssociatedVoucherInfo(Type=6, PointOfSale=pointOfSale, Number=cfProdVoucherNum, Cuit=issuerCuit)`
  - Entry 2: `AssociatedVoucherInfo(Type=1, PointOfSale=pointOfSale, Number=riProdVoucherNum, Cuit=issuerCuit)`
- **THEN** it MUST retrieve the last authorized voucher number for type 7 before submitting
- **THEN** on authorization success it MUST print: scenario name, NC voucher number, CAE, CAE expiration, and both associated voucher numbers
- **WHEN** either CF-Productos or RI-Productos was not authorized
- **THEN** the scenario MUST be skipped with a warning logged and `anyFailed = true`

---

### Requirement: Sample exit code and scenario summary
The sample MUST print a final summary of all scenarios after the new ones are added, preserving the existing exit-code behavior.

#### Scenario: New scenarios appear in the final summary
- **WHEN** all scenarios have run (including "IIBB-Percepcion" and "NC-Multi-Vinculo")
- **THEN** the pilot MUST print the total count of scenarios run, the count of failures, and exit with code `1` if `anyFailed` is true
