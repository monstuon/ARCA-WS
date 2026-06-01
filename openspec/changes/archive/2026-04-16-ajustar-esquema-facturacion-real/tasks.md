## 1. Dominio — Contrato Definitivo de VoucherRequest

- [x] 1.1 Confirmar que `VoucherRequest` en `Domain/Wsfe/WsfeModels.cs` no expone `VatAmount` ni `TaxAmount` como campos escalares separados; el IVA total y el total de tributos se derivan de `VatBreakdown` y `TaxBreakdown` respectivamente.
- [x] 1.2 Confirmar que `AssociatedVouchers` es `IReadOnlyList<AssociatedVoucherInfo>?` (colección), no un único objeto opcional.
- [x] 1.3 Confirmar presencia de `NonTaxableAmount` (decimal, obligatorio, reemplaza el `ImpTotConc` hardcodeado), `CantReg` (int, default 1) y `SameCurrencyQuantity` (int?, opcional).

## 2. Validación — Migración al Nuevo Contrato

- [x] 2.1 Reemplazar la suma de totales en `WsfeRequestValidator.Validate`: `expected = NetAmount + NonTaxableAmount + VatBreakdown?.Sum(v => v.Amount) ?? 0 + ExemptAmount + TaxBreakdown?.Sum(t => t.Amount) ?? 0`. Ajustar el mensaje de error para mencionar los nuevos componentes.
- [x] 2.2 Validar que cada `VatItem` en `VatBreakdown` tenga `Id` permitido (3, 4, 5 o 6), `BaseAmount >= 0` y `Amount >= 0`. Rechazar si algún elemento no cumple.
- [x] 2.3 Validar que cada `TaxItem` en `TaxBreakdown` tenga `Id > 0`, `Description` no vacío, `BaseAmount >= 0`, `Rate >= 0` y `Amount >= 0`. Rechazar si algún elemento no cumple.
- [x] 2.4 Cambiar la validación de crédito para iterar `AssociatedVouchers`: exigir que la colección no sea nula ni vacía cuando `VoucherType` es 3, 7 u 8; validar cada elemento de la colección individualmente (Type > 0, PointOfSale > 0, Number > 0, Cuit > 0).
- [x] 2.5 Agregar/ajustar tests unitarios en `tests/ARCA-WS.Tests/Wsfe/WsfeRequestValidatorTests.cs` cubriendo: suma de totales con VatBreakdown y TaxBreakdown, VatItem con Id inválido, TaxItem con Description vacío, NC con AssociatedVouchers vacío, NC con un elemento de AssociatedVouchers inválido.

## 3. SOAP Client — Mapeo de Nuevos Campos

- [x] 3.1 En `BuildAuthorizeVoucherEnvelope`, reemplazar `"<ar:CantReg>1</ar:CantReg>"` por `$"<ar:CantReg>{request.CantReg}</ar:CantReg>"`.
- [x] 3.2 Reemplazar `"<ar:ImpTotConc>0</ar:ImpTotConc>"` por `$"<ar:ImpTotConc>{request.NonTaxableAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpTotConc>"`.
- [x] 3.3 Calcular `ImpIVA` como suma de `VatBreakdown?.Sum(v => v.Amount) ?? 0m` y mapearlo a `<ar:ImpIVA>`.
- [x] 3.4 Calcular `ImpTrib` como suma de `TaxBreakdown?.Sum(t => t.Amount) ?? 0m` y mapearlo a `<ar:ImpTrib>`.
- [x] 3.5 Generar el bloque `<ar:Iva>` iterando `VatBreakdown`: por cada `VatItem` emitir un nodo `<ar:AlicIva>` con `<ar:Id>`, `<ar:BaseImp>` e `<ar:Importe>`. Omitir el bloque si `VatBreakdown` es nula o vacía.
- [x] 3.6 Generar el bloque `<ar:Tributos>` iterando `TaxBreakdown`: por cada `TaxItem` emitir un nodo `<ar:Tributo>` con `<ar:Id>`, `<ar:Desc>` (HTML-escaped), `<ar:BaseImp>`, `<ar:Alic>` e `<ar:Importe>`. Omitir el bloque si `TaxBreakdown` es nula o vacía.
- [x] 3.7 Generar el bloque `<ar:CbtesAsoc>` iterando `AssociatedVouchers`: por cada elemento emitir un nodo `<ar:CbteAsoc>` con `<ar:Tipo>`, `<ar:PtoVta>`, `<ar:Nro>` y `<ar:Cuit>`. Omitir el bloque si la colección es nula o vacía. Eliminar el código que trata `AssociatedVoucher` en singular.
- [x] 3.8 Incluir `<ar:CanMisMonExt>{SameCurrencyQuantity}</ar:CanMisMonExt>` inmediatamente después de `<ar:MonCotiz>` cuando `SameCurrencyQuantity` tiene valor.

## 4. Tests de SOAP Client — Migración y Nuevos Casos

- [x] 4.1 Migrar todos los constructores de `VoucherRequest` en `WsfeSoapClientTests.cs` del contrato viejo (`VatAmount`, `TaxAmount`, `AssociatedVoucher` singular) al nuevo (`VatBreakdown`, `TaxBreakdown`, `AssociatedVouchers` colección, `NonTaxableAmount`).
- [x] 4.2 Agregar test: IVA con dos alícuotas (10,5 % y 21 %) — verificar que el envelope contenga dos nodos `<ar:AlicIva>` con los Ids 4 y 5, y que `<ar:ImpIVA>` sea la suma.
- [x] 4.3 Agregar test: TaxBreakdown con una percepción provincial — verificar presencia de `<ar:Tributos>`, `<ar:Id>2</ar:Id>`, `<ar:Desc>` con el texto de la percepción, `<ar:Alic>` y `<ar:Importe>`.
- [x] 4.4 Agregar test: NC con dos elementos en `AssociatedVouchers` — verificar que el envelope contenga dos nodos `<ar:CbteAsoc>` con sus respectivos números y CUIT.
- [x] 4.5 Agregar test: `NonTaxableAmount > 0` — verificar que `<ar:ImpTotConc>` refleja el valor y no `0`.
- [x] 4.6 Agregar test: `SameCurrencyQuantity` presente — verificar presencia de `<ar:CanMisMonExt>` con el valor correcto.
- [x] 4.7 Agregar test: `CantReg = 2` — verificar que `<ar:CantReg>2</ar:CantReg>` aparece en el encabezado del lote.

## 5. PilotConsumer — Escenarios Nuevos

- [x] 5.1 Migrar los constructores de `VoucherRequest` existentes en `Program.cs` del contrato viejo al nuevo: reemplazar `VatAmount` por `VatBreakdown` con un `VatItem(5, netAmount, vatAmount)`, `TaxAmount` por `TaxBreakdown: null`, `AssociatedVoucher` singular por `AssociatedVouchers: [...]`.
- [x] 5.2 Agregar escenario **IIBB-Percepcion**: Factura B a consumidor final por $1.000 neto, con IVA 21 % ($210) y percepción IIBB CABA 1,5 % ($15). Usar `NonTaxableAmount: 0`, `VatBreakdown: [VatItem(5, 1000, 210)]`, `TaxBreakdown: [TaxItem(2, "Percepción IIBB CABA", 1000, 1.5m, 15)]`, `TotalAmount: 1225`. Imprimir CAE, vencimiento y los campos nuevos `ImpTotConc` y `ImpTrib` del resultado.
- [x] 5.3 Agregar escenario **NC-Multi-Vinculo**: Nota de Crédito B que anula la factura del escenario CF-Productos Y la factura del escenario RI-Productos (dos elementos en `AssociatedVouchers`). Ambos vínculos deben informar `Type`, `PointOfSale`, `Number` y `Cuit`. Imprimir ambos comprobantes asociados en el log de resultado junto con el CAE de la NC.
- [x] 5.4 Asegurarse de que los dos nuevos escenarios llamen a `GetLastAuthorizedVoucherAsync` antes de emitir, registren la correlación en el log e incluyan su resultado en el reporte final de `anyFailed`.

## 6. Cobertura Automatizada

- [x] 6.1 Ejecutar el suite de tests existente y corregir toda regresión introducida por los cambios de modelo o mapeo.
- [x] 6.2 Verificar que los nuevos tests (secciones 2.5, 4.2–4.7) cubren las rutas afirmativas y negativas de cada nueva regla de validación y campo de mapeo.


## 2. Validación — Migración al Nuevo Contrato

- [ ] 2.1 Reemplazar la suma de totales en `WsfeRequestValidator.Validate`: `expected = NetAmount + NonTaxableAmount + VatBreakdown?.Sum(v => v.Amount) ?? 0 + ExemptAmount + TaxBreakdown?.Sum(t => t.Amount) ?? 0`. Ajustar el mensaje de error para mencionar los nuevos componentes.
- [ ] 2.2 Validar que cada `VatItem` en `VatBreakdown` tenga `Id` permitido (3, 4, 5 o 6), `BaseAmount >= 0` y `Amount >= 0`. Rechazar si algún elemento no cumple.
- [ ] 2.3 Validar que cada `TaxItem` en `TaxBreakdown` tenga `Id > 0`, `Description` no vacío, `BaseAmount >= 0`, `Rate >= 0` y `Amount >= 0`. Rechazar si algún elemento no cumple.
- [ ] 2.4 Cambiar la validación de crédito para iterar `AssociatedVouchers`: exigir que la colección no sea nula ni vacía cuando `VoucherType` es 3, 7 u 8; validar cada elemento de la colección individualmente (Type > 0, PointOfSale > 0, Number > 0, Cuit > 0).
- [ ] 2.5 Agregar/ajustar tests unitarios en `tests/ARCA-WS.Tests/Wsfe/WsfeRequestValidatorTests.cs` cubriendo: suma de totales con VatBreakdown y TaxBreakdown, VatItem con Id inválido, TaxItem con Description vacío, NC con AssociatedVouchers vacío, NC con un elemento de AssociatedVouchers inválido.

## 3. SOAP Client — Mapeo de Nuevos Campos

- [ ] 3.1 En `BuildAuthorizeVoucherEnvelope`, reemplazar `"<ar:CantReg>1</ar:CantReg>"` por `$"<ar:CantReg>{request.CantReg}</ar:CantReg>"`.
- [ ] 3.2 Reemplazar `"<ar:ImpTotConc>0</ar:ImpTotConc>"` por `$"<ar:ImpTotConc>{request.NonTaxableAmount.ToString(CultureInfo.InvariantCulture)}</ar:ImpTotConc>"`.
- [ ] 3.3 Calcular `ImpIVA` como suma de `VatBreakdown?.Sum(v => v.Amount) ?? 0m` y mapearlo a `<ar:ImpIVA>`.
- [ ] 3.4 Calcular `ImpTrib` como suma de `TaxBreakdown?.Sum(t => t.Amount) ?? 0m` y mapearlo a `<ar:ImpTrib>`.
- [ ] 3.5 Generar el bloque `<ar:Iva>` iterando `VatBreakdown`: por cada `VatItem` emitir un nodo `<ar:AlicIva>` con `<ar:Id>`, `<ar:BaseImp>` e `<ar:Importe>`. Omitir el bloque si `VatBreakdown` es nula o vacía.
- [ ] 3.6 Generar el bloque `<ar:Tributos>` iterando `TaxBreakdown`: por cada `TaxItem` emitir un nodo `<ar:Tributo>` con `<ar:Id>`, `<ar:Desc>` (HTML-escaped), `<ar:BaseImp>`, `<ar:Alic>` e `<ar:Importe>`. Omitir el bloque si `TaxBreakdown` es nula o vacía.
- [ ] 3.7 Generar el bloque `<ar:CbtesAsoc>` iterando `AssociatedVouchers`: por cada elemento emitir un nodo `<ar:CbteAsoc>` con `<ar:Tipo>`, `<ar:PtoVta>`, `<ar:Nro>` y `<ar:Cuit>`. Omitir el bloque si la colección es nula o vacía. Eliminar el código que trata `AssociatedVoucher` en singular.
- [ ] 3.8 Incluir `<ar:CanMisMonExt>{SameCurrencyQuantity}</ar:CanMisMonExt>` inmediatamente después de `<ar:MonCotiz>` cuando `SameCurrencyQuantity` tiene valor.

## 4. Tests de SOAP Client — Migración y Nuevos Casos

- [ ] 4.1 Migrar todos los constructores de `VoucherRequest` en `WsfeSoapClientTests.cs` del contrato viejo (`VatAmount`, `TaxAmount`, `AssociatedVoucher` singular) al nuevo (`VatBreakdown`, `TaxBreakdown`, `AssociatedVouchers` colección, `NonTaxableAmount`).
- [ ] 4.2 Agregar test: IVA con dos alícuotas (10,5 % y 21 %) — verificar que el envelope contenga dos nodos `<ar:AlicIva>` con los Ids 4 y 5, y que `<ar:ImpIVA>` sea la suma.
- [ ] 4.3 Agregar test: TaxBreakdown con una percepción provincial — verificar presencia de `<ar:Tributos>`, `<ar:Id>2</ar:Id>`, `<ar:Desc>` con el texto de la percepción, `<ar:Alic>` y `<ar:Importe>`.
- [ ] 4.4 Agregar test: NC con dos elementos en `AssociatedVouchers` — verificar que el envelope contenga dos nodos `<ar:CbteAsoc>` con sus respectivos números y CUIT.
- [ ] 4.5 Agregar test: `NonTaxableAmount > 0` — verificar que `<ar:ImpTotConc>` refleja el valor y no `0`.
- [ ] 4.6 Agregar test: `SameCurrencyQuantity` presente — verificar presencia de `<ar:CanMisMonExt>` con el valor correcto.
- [ ] 4.7 Agregar test: `CantReg = 2` — verificar que `<ar:CantReg>2</ar:CantReg>` aparece en el encabezado del lote.

## 5. PilotConsumer — Escenarios Nuevos

- [ ] 5.1 Migrar los constructores de `VoucherRequest` existentes en `Program.cs` del contrato viejo al nuevo: reemplazar `VatAmount` por `VatBreakdown` con un `VatItem(5, netAmount, vatAmount)`, `TaxAmount` por `TaxBreakdown: null`, `AssociatedVoucher` singular por `AssociatedVouchers: [...]`.
- [ ] 5.2 Agregar escenario **IIBB-Percepcion**: Factura B a consumidor final por $1.000 neto, con IVA 21 % ($210) y percepción IIBB CABA 1,5 % ($15). Usar `NonTaxableAmount: 0`, `VatBreakdown: [VatItem(5, 1000, 210)]`, `TaxBreakdown: [TaxItem(2, "Percepción IIBB CABA", 1000, 1.5m, 15)]`, `TotalAmount: 1225`. Imprimir CAE, vencimiento y los campos nuevos `ImpTotConc` y `ImpTrib` del resultado.
- [ ] 5.3 Agregar escenario **NC-Multi-Vinculo**: Nota de Crédito B que anula la factura del escenario CF-Productos Y la factura del escenario RI-Productos (dos elementos en `AssociatedVouchers`). Ambos vínculos deben informar `Type`, `PointOfSale`, `Number` y `Cuit`. Imprimir ambos comprobantes asociados en el log de resultado junto con el CAE de la NC.
- [ ] 5.4 Asegurarse de que los dos nuevos escenarios llamen a `GetLastAuthorizedVoucherAsync` antes de emitir, registren la correlación en el log e incluyan su resultado en el reporte final de `anyFailed`.

## 6. Cobertura Automatizada

- [ ] 6.1 Ejecutar el suite de tests existente y corregir toda regresión introducida por los cambios de modelo o mapeo.
- [ ] 6.2 Verificar que los nuevos tests (secciones 2.5, 4.2–4.7) cubren las rutas afirmativas y negativas de cada nueva regla de validación y campo de mapeo.
