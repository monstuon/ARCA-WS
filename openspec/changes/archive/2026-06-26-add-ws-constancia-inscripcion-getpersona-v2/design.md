## Context

La solución actual integra WSAA y WSFEv1 con una estructura por capas (`Domain`, `Application`, `Infrastructure`, `PublicApi`) y registro por DI en `ServiceCollectionExtensions`. El nuevo alcance requiere incorporar WS Constancia de Inscripción (`getPersona_v2`) sin agregar complejidad de arquitectura (sin proxies/factories adicionales) y reutilizando el flujo de autenticación WSAA ya implementado.

## Goals / Non-Goals

**Goals:**
- Incorporar una consulta fiscal por CUIT usando `getPersona_v2`.
- Mantener firma pública consistente con operaciones WSFE en `ArcaIntegrationClient`.
- Reutilizar resolución de credenciales WSAA con soporte para el service name de constancia.
- Preservar trazabilidad del callstack con un flujo directo: cliente público → servicio aplicación → cliente SOAP.
- Cubrir el flujo con pruebas para los CUIT proporcionados.

**Non-Goals:**
- Implementar otros métodos del manual de WS Constancia.
- Introducir patrones de abstracción extra que oculten el flujo de ejecución.
- Cambiar reglas funcionales de autorización WSFE existentes.

## Decisions

1. **Nuevo capability dedicado `arca-ws-constancia-inscripcion`**
   - Se define un módulo paralelo a WSFE con modelos, servicio de aplicación y cliente SOAP propios.
   - Rationale: mantiene cohesión por servicio ARCA y evita mezclar contratos SOAP heterogéneos.

2. **Extender uso de WSAA para múltiples servicios ARCA**
   - Se mantiene el mismo componente de autenticación, permitiendo resolver credenciales por `serviceName` para `ws_sr_constancia_inscripcion`.
   - Rationale: evita duplicación de lógica criptográfica/renovación y preserva comportamiento conocido.
   - Alternativa descartada: crear un segundo autenticador WSAA específico para constancia (duplica código y mantenimiento).

3. **API pública directa en `ArcaIntegrationClient.GetPersona`**
   - Método asincrónico con `correlationId`, `CancellationToken` y token/sign opcionales para alineación con WSFE.
   - Rationale: consistencia para consumidores y menor fricción de adopción.

4. **Cliente SOAP explícito para `getPersona_v2`**
   - Construcción manual de envelope, SOAPAction y parseo robusto de errores/faults.
   - Rationale: control fino del contrato SOAP y simplificación del debugging.

## Risks / Trade-offs

- **[Riesgo] Diferencias de payload XML entre ambientes** → Mitigación: parser tolerante por `LocalName` y validaciones explícitas de nodos críticos.
- **[Riesgo] Credenciales externas inválidas** → Mitigación: fallback a WSAA siguiendo patrón de `Wsfev1InvoicingService`.
- **[Trade-off] Menor abstracción para mantener callstack simple** → Se acepta para priorizar operabilidad y diagnóstico.
