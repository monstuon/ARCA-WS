## 1. Catalogos oficiales WSFE

- [x] 1.1 Identificar el catalogo WSFEParam necesario para validar condicion IVA del receptor y documentar el nombre exacto que consumira la libreria (`FEParamGetCondicionIvaReceptor` o alias equivalente ya soportado por `GetParameterCatalogAsync`).
- [x] 1.2 Incorporar una estrategia de cache por ambiente para los parametros WSFE consultados durante validacion FCE, con comportamiento definido ante cache vacia, expirada o fallo de red.
- [x] 1.3 Agregar tests de servicio o unitarios que demuestren que la validacion FCE usa parametros oficiales y no una lista hardcodeada local.

## 2. Validacion de request FCE

- [x] 2.1 Extraer en `WsfeRequestValidator` o en una fase previa equivalente una regla explicita para tipos FCE `201, 202, 203, 206, 207, 208`.
- [x] 2.2 Exigir `ServicePaymentDueDate` valido en formato `yyyyMMdd` para todos los comprobantes FCE que lo requieren y rechazar localmente requests FCE sin fecha de vencimiento.
- [x] 2.3 Validar que `DocumentType`, `DocumentNumber` y `RecipientVatConditionId` formen una combinacion admitida para el tipo FCE informado, usando catalogo oficial WSFEParam para la condicion IVA del receptor.
- [x] 2.4 Restringir especificamente los tipos FCE-B y NC-FCE-B a condiciones IVA de receptor compatibles con el catalogo oficial y cubrir los rechazos con tests automatizados.

## 3. Asociaciones NC-FCE

- [x] 3.1 Ampliar la validacion de notas de credito para contemplar `VoucherType` FCE `203` y `208`, exigiendo al menos un comprobante asociado valido.
- [x] 3.2 Definir y aplicar la matriz de asociaciones validas para NC-FCE: `203` solo puede asociar comprobantes FCE-A compatibles y `208` solo puede asociar comprobantes FCE-B compatibles.
- [x] 3.3 Rechazar localmente cualquier `AssociatedVoucherInfo` cuyo `Type` no corresponda al universo valido para la NC-FCE emisora.
- [x] 3.4 En el `PilotConsumer`, omitir de forma controlada la emision de `NC-FCE-A` o `NC-FCE-B` cuando el comprobante padre no fue autorizado o no esta disponible para asociar.

## 4. Piloto y ejemplos

- [x] 4.1 Corregir los escenarios `FCE-A`, `FCE-B`, `NC-FCE-A` y `NC-FCE-B` del `PilotConsumer` para que reflejen exactamente las reglas FCE vigentes del WSFE.
- [x] 4.2 Ajustar la condicion IVA del receptor usada por `FCE-B` y `NC-FCE-B` a un valor permitido por el catalogo oficial, eliminando la premisa inconsistente del cambio archivado previo.
- [x] 4.3 Mantener el comportamiento de omision controlada cuando un comprobante FCE base no este disponible, dejando trazabilidad en log y salida de consola.
- [x] 4.4 Actualizar la documentacion o ejemplos de homologacion afectados para que no contradigan la validacion implementada.

## 5. Cobertura automatizada

- [x] 5.1 Agregar tests de `WsfeRequestValidator` para FCE sin `ServicePaymentDueDate`, FCE con condicion IVA de receptor invalida y NC-FCE con tipo asociado invalido.
- [x] 5.2 Agregar tests del flujo de autorizacion que prueben el uso del catalogo oficial y la falla previa al envio cuando la combinacion FCE es invalida.
- [x] 5.3 Ajustar tests del `PilotConsumer` o de ejemplos para cubrir la omision controlada de `NC-FCE-A` y `NC-FCE-B`.
- [x] 5.4 Ejecutar la suite afectada y dejar en verde al menos las pruebas de `tests/ARCA-WS.Tests/Wsfe` relacionadas con FCE.