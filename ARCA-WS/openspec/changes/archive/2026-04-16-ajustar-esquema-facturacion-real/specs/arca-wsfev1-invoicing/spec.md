## MODIFIED Requirements

### Requirement: Batch size declaration (CantReg)
The library MUST inform the actual batch size to AFIP in every `FECAESolicitar` request.

#### Scenario: CantReg reflects request field
- **WHEN** a consumer submits a `VoucherRequest` with `CantReg = N`
- **THEN** the library MUST emit `<ar:CantReg>N</ar:CantReg>` in the `FeCabReq` header of the SOAP envelope
- **THEN** the library MUST NOT hardcode `CantReg` to `1`

---

### Requirement: Non-taxable amount (ImpTotConc)
The library MUST map the non-taxable amount field to `ImpTotConc` in every `FECAESolicitar` request.

#### Scenario: NonTaxableAmount is mapped to ImpTotConc
- **WHEN** a consumer submits a `VoucherRequest` with `NonTaxableAmount > 0`
- **THEN** the library MUST emit `<ar:ImpTotConc>` with that value (invariant culture decimal) in the `FECAEDetRequest` node
- **THEN** the library MUST NOT emit `<ar:ImpTotConc>0</ar:ImpTotConc>` unconditionally

#### Scenario: Zero non-taxable amount
- **WHEN** a consumer submits a `VoucherRequest` with `NonTaxableAmount = 0`
- **THEN** the library MUST emit `<ar:ImpTotConc>0</ar:ImpTotConc>`

---

### Requirement: VAT breakdown as array (VatBreakdown)
The library MUST support multiple VAT aliquots in a single voucher and map each one as a separate `AlicIva` node.

#### Scenario: Single VAT aliquot
- **WHEN** a consumer submits a `VoucherRequest` with `VatBreakdown` containing one `VatItem(Id=5, BaseAmount=826.45, Amount=173.55)`
- **THEN** the library MUST emit exactly one `<ar:AlicIva>` node with `<ar:Id>5</ar:Id>`, `<ar:BaseImp>826.45</ar:BaseImp>` and `<ar:Importe>173.55</ar:Importe>` inside `<ar:Iva>`
- **THEN** `<ar:ImpIVA>` MUST equal `173.55`

#### Scenario: Mixed VAT aliquots (10.5% and 21%)
- **WHEN** a consumer submits a `VoucherRequest` with `VatBreakdown` containing two items — `VatItem(Id=4, BaseAmount=500, Amount=52.5)` and `VatItem(Id=5, BaseAmount=826.45, Amount=173.55)`
- **THEN** the library MUST emit two `<ar:AlicIva>` nodes inside `<ar:Iva>`, one with `<ar:Id>4</ar:Id>` and one with `<ar:Id>5</ar:Id>`
- **THEN** `<ar:ImpIVA>` MUST equal the sum `52.5 + 173.55 = 226.05`

#### Scenario: No VAT (exempt or zero-rated)
- **WHEN** a consumer submits a `VoucherRequest` with `VatBreakdown` null or empty
- **THEN** the library MUST omit the `<ar:Iva>` block entirely from the SOAP envelope
- **THEN** `<ar:ImpIVA>` MUST equal `0`

#### Scenario: Invalid VatItem Id
- **WHEN** a consumer submits a `VoucherRequest` with a `VatItem` whose `Id` is not in {3, 4, 5, 6}
- **THEN** the library MUST reject the request with a typed validation error referencing the invalid aliquot Id

---

### Requirement: Tax breakdown as array (TaxBreakdown)
The library MUST support multiple additional taxes (provincial perceptions, national withholdings) and map each one as a separate `Tributo` node.

#### Scenario: Provincial perception (IIBB)
- **WHEN** a consumer submits a `VoucherRequest` with `TaxBreakdown` containing `TaxItem(Id=2, Description="Percepción IIBB CABA", BaseAmount=1000, Rate=1.5, Amount=15)`
- **THEN** the library MUST emit `<ar:Tributos><ar:Tributo><ar:Id>2</ar:Id><ar:Desc>Percepción IIBB CABA</ar:Desc><ar:BaseImp>1000</ar:BaseImp><ar:Alic>1.5</ar:Alic><ar:Importe>15</ar:Importe></ar:Tributo></ar:Tributos>` in the SOAP envelope
- **THEN** `<ar:ImpTrib>` MUST equal `15`

#### Scenario: Multiple taxes
- **WHEN** a consumer submits a `VoucherRequest` with two `TaxItem` entries
- **THEN** the library MUST emit two `<ar:Tributo>` nodes inside `<ar:Tributos>` and set `<ar:ImpTrib>` to their sum

#### Scenario: No additional taxes
- **WHEN** a consumer submits a `VoucherRequest` with `TaxBreakdown` null or empty
- **THEN** the library MUST omit the `<ar:Tributos>` block entirely
- **THEN** `<ar:ImpTrib>` MUST equal `0`

#### Scenario: Invalid TaxItem
- **WHEN** a consumer submits a `VoucherRequest` with a `TaxItem` whose `Description` is empty or whose `Id`, `BaseAmount`, `Rate`, or `Amount` is negative
- **THEN** the library MUST reject the request with a typed validation error

---

### Requirement: Multiple associated vouchers (AssociatedVouchers)
The library MUST allow a credit note to reference more than one original voucher.

#### Scenario: Credit note with one associated voucher
- **WHEN** a consumer submits a credit note (`VoucherType` 3, 7, or 8) with `AssociatedVouchers` containing exactly one entry
- **THEN** the library MUST emit `<ar:CbtesAsoc>` containing one `<ar:CbteAsoc>` node with the correct `<ar:Tipo>`, `<ar:PtoVta>`, `<ar:Nro>` and `<ar:Cuit>`

#### Scenario: Credit note with multiple associated vouchers
- **WHEN** a consumer submits a credit note with `AssociatedVouchers` containing N entries (N ≥ 2)
- **THEN** the library MUST emit `<ar:CbtesAsoc>` containing N `<ar:CbteAsoc>` nodes, one per entry, preserving order

#### Scenario: Credit note with empty AssociatedVouchers
- **WHEN** a consumer submits a credit note with `AssociatedVouchers` null or empty
- **THEN** the library MUST reject the request with a typed validation error stating that at least one associated voucher is required

#### Scenario: Credit note with invalid AssociatedVoucher element
- **WHEN** a consumer submits a credit note where any element of `AssociatedVouchers` has `Type`, `PointOfSale`, `Number`, or `Cuit` equal to zero or less
- **THEN** the library MUST reject the request with a typed validation error identifying the invalid element

---

### Requirement: Foreign currency quantity (SameCurrencyQuantity / CanMisMonExt)
The library MUST include the quantity in foreign currency when provided, to support AFIP exchange rate cross-validation.

#### Scenario: SameCurrencyQuantity is present
- **WHEN** a consumer submits a `VoucherRequest` with `SameCurrencyQuantity = Q` and `CurrencyId != "PES"`
- **THEN** the library MUST emit `<ar:CanMisMonExt>Q</ar:CanMisMonExt>` immediately after `<ar:MonCotiz>` in the `FECAEDetRequest` node

#### Scenario: SameCurrencyQuantity is absent
- **WHEN** a consumer submits a `VoucherRequest` with `SameCurrencyQuantity = null`
- **THEN** the library MUST omit the `<ar:CanMisMonExt>` element entirely

---

### Requirement: Total amount consistency with new components
The library MUST validate that `TotalAmount` matches the sum of all declared components using the array-based model.

#### Scenario: Totals check with breakdown arrays
- **WHEN** a consumer submits a `VoucherRequest`
- **THEN** the library MUST reject the request if `TotalAmount ≠ NetAmount + NonTaxableAmount + sum(VatBreakdown.Amount) + ExemptAmount + sum(TaxBreakdown.Amount)`
- **THEN** the validation error MUST mention all five components by name
