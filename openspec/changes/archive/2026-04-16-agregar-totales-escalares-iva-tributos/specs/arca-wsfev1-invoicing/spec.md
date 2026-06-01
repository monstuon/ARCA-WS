# Spec: arca-wsfev1-invoicing — Totales Escalares de Cabecera (VatTotal / TaxTotal)

## Contexto

`VoucherRequest` expone `VatBreakdown` y `TaxBreakdown` como arrays de desglose de IVA y tributos. Los nuevos campos opcionales `VatTotal` y `TaxTotal` permiten al caller declarar los totales de cabecera de forma explícita. Cuando están presentes, tienen precedencia sobre la suma derivada de los arrays. Cuando son `null`, el comportamiento es idéntico al anterior.

---

## Escenarios de Validación

### Escenario: VatTotal explícito sin VatBreakdown — pasa validación de totales

```
Dado un VoucherRequest con:
  NetAmount = 826.45, ExemptAmount = 0, NonTaxableAmount = 0
  VatTotal = 173.55, TaxTotal = null
  VatBreakdown = null, TaxBreakdown = null
  TotalAmount = 1000
Cuando se llama a WsfeRequestValidator.Validate
Entonces no se lanza excepción
```

### Escenario: VatTotal y VatBreakdown ambos informados — no se valida coherencia entre ellos

```
Dado un VoucherRequest con:
  NetAmount = 826.45, NonTaxableAmount = 0, ExemptAmount = 0
  VatTotal = 174.00   (distinto de VatBreakdown.Sum)
  VatBreakdown = [VatItem(5, 826.45, 173.55)]
  TotalAmount = 1000.45   (= NetAmount + VatTotal)
Cuando se llama a WsfeRequestValidator.Validate
Entonces no se lanza excepción
  (la coherencia entre escalar y array la verifica AFIP, no la librería)
```

---

## Escenarios de Mapeo SOAP

### Escenario: VatTotal explícito sin VatBreakdown — ImpIVA usa el escalar

```
Dado un VoucherRequest con VatTotal = 210 y VatBreakdown = null
Cuando se genera el envelope SOAP con BuildAuthorizeVoucherEnvelope
Entonces el envelope contiene <ar:ImpIVA>210</ar:ImpIVA>
  Y no contiene el bloque <ar:Iva>
```

### Escenario: TaxTotal explícito sin TaxBreakdown — ImpTrib usa el escalar

```
Dado un VoucherRequest con TaxTotal = 15 y TaxBreakdown = null
Cuando se genera el envelope SOAP
Entonces el envelope contiene <ar:ImpTrib>15</ar:ImpTrib>
  Y no contiene el bloque <ar:Tributos>
```

### Escenario: VatTotal explícito con VatBreakdown presente — escalar en ImpIVA, array en AlicIva

```
Dado un VoucherRequest con:
  VatTotal = 226.05
  VatBreakdown = [VatItem(4, 826.45, 86.78), VatItem(5, 663.00, 139.23)]   // suma = 226.01
Cuando se genera el envelope SOAP
Entonces el envelope contiene <ar:ImpIVA>226.05</ar:ImpIVA>   (usa el escalar)
  Y contiene dos nodos <ar:AlicIva> (uno con Id=4, otro con Id=5)
```

### Escenario: VatTotal y TaxTotal omitidos — comportamiento anterior sin cambios

```
Dado un VoucherRequest con:
  VatBreakdown = [VatItem(5, 826.45, 173.55)]
  TaxBreakdown = [TaxItem(2, "IIBB", 1000, 1.5m, 15)]
  VatTotal = null, TaxTotal = null
Cuando se genera el envelope SOAP
Entonces <ar:ImpIVA> = 173.55   (suma de VatBreakdown)
  Y <ar:ImpTrib> = 15            (suma de TaxBreakdown)
```
