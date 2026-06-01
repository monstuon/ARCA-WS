## Context

La base actual está orientada a separación por capas (API/Application/Domain/Infrastructure), con múltiples puntos de extensión internos y recorridos de llamada más largos de lo necesario para un SDK orientado a AFIP/ARCA. El problema dominante de este dominio no es la variabilidad arquitectónica, sino la robustez operativa frente a certificados, autenticación WSAA, SOAP/XML de WSFEv1 y trazabilidad de errores.

Se requiere mantener .NET 8, compatibilidad Windows/Linux, DTOs tipados, soporte WSAA + WSFEv1, cache de token/sign y logging básico, mientras se reduce complejidad estructural para mejorar mantenimiento, debugging y onboarding.

## Goals / Non-Goals

**Goals:**
- Exponer una API pública lineal mediante `ArcaClient` como punto único de entrada.
- Centralizar la autenticación WSAA en `WsaaTokenManager` con login, cache, expiración y renovación.
- Mantener operaciones WSFEv1 clave con DTOs tipados y flujo explícito SOAP -> mapper -> DTO.
- Unificar el manejo de errores en `ArcaException` con metadatos mínimos útiles para reintentos y diagnóstico.
- Reducir capas y abstractions no esenciales para acelerar trazabilidad y debugging.

**Non-Goals:**
- Rediseñar contratos funcionales de WSAA/WSFEv1 o cambiar semántica de negocio de AFIP/ARCA.
- Introducir un framework nuevo de DI, resiliencia o telemetría avanzada.
- Optimizar para múltiples implementaciones de transporte/proveedor que no existen hoy como necesidad real.
- Mantener compatibilidad binaria completa con toda API intermedia previa si bloquea la simplificación.

## Decisions

1. **Cliente principal único (`ArcaClient`)**
   - Se centraliza la API pública en una clase concreta con métodos de facturación (`AutorizarFacturaAsync`, `ConsultarComprobanteAsync`, `ObtenerUltimoComprobanteAsync`, `SolicitarCAEAAsync`, `InformarCAEAAsync`).
   - **Rationale:** reduce navegación mental, facilita onboarding y debugging end-to-end.
   - **Alternativas consideradas:**
	 - Mantener servicios públicos separados por caso de uso: mejor separación teórica, peor trazabilidad para consumidores.
	 - Fachada sobre estructura actual: reduce impacto inmediato pero conserva complejidad interna.

2. **`WsaaTokenManager` como único orquestador WSAA**
   - Se consolidan login, cache en memoria, control de expiración y renovación proactiva en un solo componente interno.
   - **Rationale:** elimina providers encadenados y reduce puntos de fallo en autenticación.
   - **Alternativas consideradas:**
	 - Mantener múltiples providers con composición: más extensible, menos claro para este dominio.
	 - Delegar cache afuera: incrementa complejidad de uso y acopla al consumidor.

3. **Flujo SOAP explícito por cliente especializado (`WsfeSoapClient`)**
   - `ArcaClient` orquesta; `WsfeSoapClient` encapsula invocaciones SOAP concretas; mapeadores traducen a DTOs tipados.
   - **Rationale:** equilibrio entre claridad y encapsulación mínima, sin sobreabstracción.
   - **Alternativas consideradas:**
	 - Exponer SOAP raw en API pública: simplifica internamente pero degrada UX del SDK.
	 - Multiplicar wrappers por operación: vuelve a fragmentar la trazabilidad.

4. **Modelo de error unificado (`ArcaException`)**
   - Excepción base con `Code`, `Message`, `IsRetryable`, `InnerException`; excepciones derivadas solo si aportan semántica clara.
   - **Rationale:** diagnóstico consistente y reglas de retry visibles.
   - **Alternativas consideradas:**
	 - Mantener jerarquía extensa actual: mayor granularidad, pero alto costo cognitivo.

5. **Estructura de carpetas simplificada orientada a lectura**
   - Organización objetivo: `Clients` (o `Services`), `Auth`, `Soap`, `Models`, `Exceptions`, `Utils`.
   - **Rationale:** flujo técnico rastreable en pocos archivos, evitando capas artificiales.
   - **Alternativas consideradas:**
	 - Conservar arquitectura por capas con renombre: mejora cosmética sin reducción real de complejidad.

## Risks / Trade-offs

- **[Riesgo] Breaking changes para consumidores actuales** → **Mitigación:** mantener adaptadores/aliases temporales y documentar migración en changelog.
- **[Riesgo] Regresiones en autenticación WSAA por consolidación** → **Mitigación:** tests focalizados en cache, expiración y renovación con casos de borde temporales.
- **[Trade-off] Menor extensibilidad futura por menos interfaces** → **Mitigación:** introducir abstracciones solo ante necesidad probada (YAGNI).
- **[Riesgo] Acoplamiento más directo entre cliente y SOAP** → **Mitigación:** límites internos claros (`ArcaClient` orquesta, `WsfeSoapClient` ejecuta transporte).

## Migration Plan

1. Introducir `ArcaClient`, `WsaaTokenManager`, `WsfeSoapClient` y `ArcaException` en paralelo con componentes existentes.
2. Redirigir internamente operaciones WSFEv1 hacia el nuevo flujo lineal, preservando comportamiento observable.
3. Mantener wrappers de compatibilidad para API anterior (si existen), marcándolos como obsoletos cuando corresponda.
4. Ejecutar suite de tests y ajustar fixtures de autenticación/certificados para la nueva centralización.
5. Retirar componentes internos redundantes una vez validada paridad funcional.
6. Publicar guía de migración corta orientada a consumo por `ArcaClient`.

**Rollback:** conservar rama/tag previo al refactor y feature flags internas de ruteo (si aplica) durante transición para volver al flujo anterior en caso de regresión crítica.

## Open Questions

- ¿Qué APIs públicas heredadas deben mantenerse temporalmente por compatibilidad y cuáles pueden retirarse en la próxima versión mayor?
- ¿La política de renovación WSAA será por ventana fija antes de expiración (por ejemplo, N minutos) o por validación bajo demanda en cada operación?
- ¿Se necesita persistencia de cache de token/sign entre procesos o alcanza cache en memoria por instancia de cliente?