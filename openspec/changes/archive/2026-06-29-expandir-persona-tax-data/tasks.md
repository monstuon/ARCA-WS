## 1. Modelado y contratos

- [x] 1.1 Identificar en el dominio actual los campos de `PersonaTaxData` que ya existen y definir los nuevos campos de `datosGenerales` a incorporar
- [x] 1.2 Extender los modelos/DTOs de `PersonaTaxData` con propiedades opcionales para los nuevos datos sin romper compatibilidad

## 2. Mapeo condicional por tipo de contribuyente

- [x] 2.1 Actualizar el mapper de respuesta `getPersona_v2` para poblar campos comunes de `datosGenerales`
- [x] 2.2 Implementar lógica condicional para poblar campos de Responsable Inscripto y dejar null los exclusivos de Monotributo
- [x] 2.3 Implementar lógica condicional para poblar campos de Monotributista y dejar null los exclusivos de Responsable Inscripto

## 3. Validación y pruebas

- [x] 3.1 Agregar pruebas unitarias de mapeo para escenario Responsable Inscripto
- [x] 3.2 Agregar pruebas unitarias de mapeo para escenario Monotributista
- [x] 3.3 Agregar prueba de robustez para payload parcial con nodos opcionales faltantes