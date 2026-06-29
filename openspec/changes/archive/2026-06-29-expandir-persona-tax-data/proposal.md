## Why

`PersonaTaxData` actualmente devuelve un subconjunto de información fiscal del contribuyente. Para consumidores que necesitan validaciones y reporting, faltan datos de `datosGenerales` del servicio WS Constancia de Inscripción, especialmente los específicos según condición fiscal (Responsable Inscripto vs Monotributista).

## What Changes

- Ampliar el mapeo de `PersonaTaxData` para incluir más campos del bloque `datosGenerales` documentados en `manual_ws_sr_ws_constancia_inscripcion.pdf`.
- Incorporar el mapeo condicional de datos según tipo de contribuyente:
  - Responsable Inscripto: completar datos impositivos y de inscripción aplicables.
  - Monotributista: completar datos de régimen simplificado aplicables.
- Ajustar contratos/modelos de dominio y serialización pública para exponer los nuevos campos sin romper compatibilidad de los existentes.
- Agregar pruebas para validar el llenado condicional de datos por tipo de contribuyente.

## Capabilities

### New Capabilities
- `arca-ws-constancia-persona-tax-data`: Enriquecimiento de `PersonaTaxData` con datos generales del contribuyente y mapeo condicional según condición fiscal.

### Modified Capabilities
- Ninguna.

## Impact

- Código afectado: modelos y mapeadores en `Domain/WSConstanciaInscripcion`, servicio de aplicación que construye `PersonaTaxData`, y API pública que devuelve estos datos.
- API pública: se agregan propiedades opcionales en `PersonaTaxData` para nuevos campos, preservando los actuales.
- Testing: nuevas pruebas unitarias/integración para escenarios Responsable Inscripto y Monotributista.