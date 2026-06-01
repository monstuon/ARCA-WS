## Por qué

Con el cambio anterior se eliminaron los escalares `VatAmount` (ImpIVA) y `TaxAmount` (ImpTrib) de `VoucherRequest` y se reemplazaron por los arrays `VatBreakdown` y `TaxBreakdown`, desde los cuales el SOAP client deriva los totales sumando los importes de cada ítem. Esto funciona correctamente para la generación del envelope.

Sin embargo, en ciertos contextos operacionales es necesario disponer de esos totales directamente en el objeto de request, sin tener que iterar los arrays:

- **Logging y auditoría**: el código que registra la petición antes de enviarla a AFIP quiere mostrar los totales de IVA y Tributos como campos directos, sin calcularlos ad-hoc en cada punto de uso.
- **Consistencia con la respuesta de AFIP**: la respuesta del webservice (FECAEResponse) devuelve los totales en la cabecera; tener los mismos campos en el request facilita la comparación y el diagnóstico de diferencias.
- **Callers que ya conocen los totales**: integraciones que provienen de sistemas contables ya tienen calculados los totales y quieren informarlos explícitamente en lugar de depender de que la librería los recalcule.
- **Control ante diferencias de redondeo**: cuando la suma de los ítems difiere en centavos del total declarado (escenarios de redondeo por tipo de cambio o porcentaje), el caller necesita poder informar el total exacto de cabecera de forma independiente del desglose.

La solución debe mantener los arrays (que son requeridos para los nodos `<ar:Iva>` y `<ar:Tributos>` del envelope SOAP) y agregar los escalares de cabecera como campos opcionales que, si se informan, reemplazan el valor calculado; si se omiten, la librería sigue derivándolos de los arrays.

## Qué Cambia

- **Dominio** (`WsfeModels.cs`): agregar dos parámetros opcionales a `VoucherRequest`: `decimal? VatTotal = null` (ImpIVA explícito) y `decimal? TaxTotal = null` (ImpTrib explícito). Si son `null`, el valor efectivo se calcula sumando los arrays; si tienen valor, ese valor se usa en el envelope y en la validación de totales. Agregar además el record `OptionalItem(int Id, string Value)` y el campo `IReadOnlyList<OptionalItem>? Optionals = null`, requerido por los tipos FCE MiPyME (201, 202, 203, 206, 207, 208) para informar datos como el CBU del emisor (Id=2911).
- **Validación** (`WsfeRequestValidator.cs`): actualizar la suma de `expected` para usar `VatTotal ?? VatBreakdown?.Sum(v => v.Amount) ?? 0m` y `TaxTotal ?? TaxBreakdown?.Sum(t => t.Amount) ?? 0m`. No se valida la coherencia entre el escalar y el array; si ambos están informados, AFIP es quien determina si los valores son consistentes.
- **SOAP client** (`WsfeSoapClient.cs`): reemplazar el cálculo inline de `vatTotal` y `taxTotal` por el fallback con escalar; generar el bloque `<ar:Opcionales>` iterando `Optionals` (cada item como `<ar:Opcional><ar:Id/><ar:Valor/></ar:Opcional>`); omitir el bloque si la colección es nula o vacía.
- **Tests unitarios**: agregar casos en `WsfeSoapClientTests.cs` y `WsfeRequestValidatorTests.cs` que ejerciten la precedencia del escalar sobre el array, y el error de validación cuando el escalar difiere del array más allá de la tolerancia.
- **PilotConsumer**: actualizar los escenarios existentes para informar `VatTotal` y `TaxTotal` explícitamente cuando tienen `VatBreakdown`/`TaxBreakdown`; agregar cuatro escenarios de **Factura de Crédito Electrónica MiPyME (FCE)**: FCE-A (tipo 201, RI→RI, productos), FCE-B (tipo 206, CF, productos), NC FCE-A (tipo 203, anula la FCE-A anterior) y NC FCE-B (tipo 208, anula la FCE-B anterior).

## Capacidades

### Capacidades Modificadas

- `arca-wsfev1-invoicing`: `VoucherRequest` acepta ahora `VatTotal` y `TaxTotal` como escalares opcionales de cabecera coexistiendo con los arrays de desglose. La lógica de validación y mapeo SOAP usa el escalar cuando está presente y el array derivado cuando no.

## Impacto

- **Archivos afectados**:
  - `Domain/Wsfe/WsfeModels.cs` — `OptionalItem` record + campo `Optionals`; `VatTotal` y `TaxTotal` opcionales
  - `Application/Wsfe/WsfeRequestValidator.cs` — suma de totales y validación cruzada escalar vs. array
  - `Infrastructure/Wsfe/WsfeSoapClient.cs` — uso del escalar con fallback al array para ImpIVA e ImpTrib
  - `tests/ARCA-WS.Tests/Wsfe/WsfeRequestValidatorTests.cs` — nuevos tests de precedencia y validación cruzada
  - `tests/ARCA-WS.Tests/Wsfe/WsfeSoapClientTests.cs` — nuevos tests de mapeo con escalar explícito
  - `samples/PilotConsumer/Program.cs` — `VatTotal`/`TaxTotal` en escenarios existentes; cuatro escenarios nuevos de FCE MiPyME (FCE-A, FCE-B, NC FCE-A, NC FCE-B)
- **Impacto en API pública**: cambio aditivo (nuevos parámetros opcionales con default `null`). Todos los callers existentes siguen compilando y funcionando sin modificación.
- **Impacto operativo**: ninguno. El comportamiento actual (derivar totales de arrays) se mantiene como default.
