## Context

La integración actual con WS Constancia de Inscripción ya consulta `getPersona_v2`, pero el modelo `PersonaTaxData` no expone varios campos de `datosGenerales` que sí están disponibles en la respuesta SOAP. Esto limita el uso de la librería para validaciones fiscales y casos de negocio que dependen del detalle de condición tributaria.

El cambio debe mantener compatibilidad con consumidores existentes: los campos actuales no deben alterarse y los nuevos datos deben agregarse como propiedades opcionales cuando aplique.

## Goals / Non-Goals

**Goals:**
- Exponer más datos de `datosGenerales` en `PersonaTaxData`.
- Mapear datos condicionales según condición fiscal del contribuyente (Responsable Inscripto o Monotributista).
- Mantener contratos compatibles hacia atrás para propiedades ya existentes.
- Cubrir con pruebas de mapeo por tipo de contribuyente.

**Non-Goals:**
- Cambiar la estrategia de autenticación WSAA.
- Rediseñar toda la respuesta de WS Constancia fuera de `PersonaTaxData`.
- Incorporar reglas de negocio externas al alcance del mapeo de datos.

## Decisions

1. Extender `PersonaTaxData` con nuevos campos opcionales
- Decision: agregar propiedades nullable para los nuevos datos de `datosGenerales`.
- Rationale: evita ruptura de compatibilidad y permite representar ausencia de datos según tipo de contribuyente.
- Alternativas consideradas:
  - Reemplazar `PersonaTaxData` por un modelo completamente nuevo: descartado por impacto en API pública.

2. Implementar mapeo condicional por tipo de contribuyente
- Decision: detectar condición fiscal y completar solo el subconjunto relevante (RI o Monotributo), dejando null el resto.
- Rationale: refleja el contrato real de ARCA y evita valores inconsistentes.
- Alternativas consideradas:
  - Poblar todos los campos con defaults: descartado por riesgo de ambigüedad funcional.

3. Mantener el mapeo centralizado en la capa de dominio/aplicación actual
- Decision: actualizar el mapper existente de WS Constancia en lugar de crear una ruta paralela.
- Rationale: minimiza cambios y conserva el patrón del proyecto.
- Alternativas consideradas:
  - Nuevo servicio de transformación dedicado: descartado para mantener alcance acotado.

## Risks / Trade-offs

- [Mapeo incorrecto por interpretación del manual] → Mitigación: usar nombres de campo alineados al manual y validar con casos de prueba representativos.
- [Cambios en formato de respuesta ARCA] → Mitigación: mantener manejo defensivo con nullables y validaciones de presencia.
- [Confusión en consumidores por nuevos campos] → Mitigación: mantener semántica de campos actuales y documentar los nuevos en la especificación.

## Migration Plan

1. Extender modelos y mapeadores con nuevos campos de `datosGenerales`.
2. Ajustar contratos públicos que exponen `PersonaTaxData`.
3. Agregar pruebas para escenarios RI y Monotributo.
4. Ejecutar suite de tests afectada y validar no regresiones.

Rollback:
- Revertir los nuevos campos de `PersonaTaxData` y su mapeo sin afectar el flujo existente.

## Open Questions

- ¿Qué lista exacta de campos de `datosGenerales` se prioriza en esta iteración para RI y Monotributo?
- ¿Se requiere incluir datos históricos o únicamente el estado vigente informado por ARCA?