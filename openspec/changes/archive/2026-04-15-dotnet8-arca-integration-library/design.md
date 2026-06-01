## Context

La organizacion mantiene integraciones ARCA (WSAA + WSFEv1) en aplicaciones legacy .NET Framework con acoplamiento alto entre logica de negocio, transporte SOAP y manejo de certificados. Esto genera problemas de mantenimiento, despliegue y soporte en produccion. Se requiere una biblioteca .NET 8 reusable, desacoplada del host y con contratos estables para adopcion progresiva.

Restricciones relevantes:
- Debe funcionar en entornos Windows y Linux compatibles con .NET 8.
- Debe soportar certificados X.509 provistos por archivo o store del sistema.
- Debe manejar expiracion de Token/Sign de WSAA sin obligar a los consumidores a conocer detalles del protocolo.
- Debe permitir trazabilidad y diagnostico de errores funcionales y tecnicos.

## Goals / Non-Goals

**Goals:**
- Encapsular autenticacion WSAA y operaciones WSFEv1 en una API publica simple.
- Separar claramente dominio, infraestructura ARCA y adaptadores de consumo.
- Proveer resiliencia basica (timeouts, reintentos acotados e idempotencia en operaciones de consulta).
- Estandarizar manejo de errores y eventos de observabilidad.
- Facilitar pruebas unitarias y de integracion sin dependencias del codigo legacy.

**Non-Goals:**
- Reescribir ni migrar automaticamente todos los sistemas legacy en este cambio.
- Cubrir todos los web services de ARCA fuera de WSAA y WSFEv1.
- Definir UI, flujos de negocio comerciales o reglas fiscales especificas de cada cliente.
- Garantizar compatibilidad binaria directa con librerias .NET Framework existentes.

## Decisions

1. Arquitectura en capas dentro de una sola class library
- Decision: organizar en capas `Public API`, `Application`, `Domain` e `Infrastructure.ArcaSoap`.
- Rationale: permite desacople entre contratos expuestos y detalles SOAP/certificados.
- Alternativas consideradas:
  - Exponer clientes SOAP directamente: descartado por alto acoplamiento y baja mantenibilidad.
  - Multiples paquetes desde el inicio: descartado para reducir complejidad inicial de versionado.

2. Contratos tipados para requests/responses y errores
- Decision: exponer DTOs y resultados tipados, con excepciones de dominio para errores funcionales y tecnicos.
- Rationale: simplifica consumo y evita parseo de mensajes SOAP por cada consumidor.
- Alternativas consideradas:
  - Retornar XML crudo: descartado por baja ergonomia y mayor riesgo de errores.

3. Gestion de credenciales WSAA con cache y renovacion anticipada
- Decision: implementar proveedor de credenciales (Token/Sign) con cache en memoria y renovacion antes de expiracion.
- Rationale: reduce latencia y llamadas innecesarias a WSAA, minimiza fallas por expiracion.
- Alternativas consideradas:
  - Solicitar login para cada llamada WSFEv1: descartado por costo operativo y performance.
  - Persistencia distribuida obligatoria: descartado en fase inicial; se deja extension futura via interfaz.

4. Transporte SOAP abstraido por interfaces
- Decision: encapsular clientes SOAP WSAA y WSFEv1 detras de interfaces internas para facilitar mocking y cambios de implementacion.
- Rationale: mejora testabilidad y aislamiento de dependencias externas.
- Alternativas consideradas:
  - Acoplar implementacion SOAP concreta al dominio: descartado por rigidez de evolucion.

5. Observabilidad estandar
- Decision: incluir logging estructurado, correlacion por operacion y metricas basicas (latencia, tasa de error, reintentos).
- Rationale: reduce tiempo de diagnostico en incidentes productivos.
- Alternativas consideradas:
  - Logging libre por consumidor: descartado por inconsistencia y baja trazabilidad.

## Risks / Trade-offs

- [Intermitencia de endpoints ARCA] -> Mitigacion: retries acotados con backoff, timeouts explicitos y errores enriquecidos.
- [Manejo incorrecto de certificado o clave privada] -> Mitigacion: validaciones tempranas y mensajes de error accionables.
- [Desfase de reloj entre host y ARCA para WSAA] -> Mitigacion: tolerancia configurable en timestamps del TRA y alertas.
- [Sobreingenieria en v1] -> Mitigacion: iniciar con alcance WSAA+WSFEv1 minimo y extensiones por interfaces.
- [Riesgo de migracion en consumidores] -> Mitigacion: estrategia de adopcion incremental y adaptadores temporales.

## Migration Plan

1. Publicar la libreria .NET 8 con version inicial y guia de integracion.
2. Integrar en un consumidor piloto con cobertura de escenarios de autenticacion y autorizacion.
3. Comparar resultados funcionales contra integracion legacy en entorno de homologacion.
4. Migrar progresivamente consumidores priorizados, monitoreando errores y latencia.
5. Mantener fallback temporal al flujo legacy durante la ventana de estabilizacion.
6. Retirar dependencias legacy una vez cumplidos criterios de aceptacion operativa.

Rollback:
- Revertir referencia de paquete/proyecto en consumidor y restaurar ruta legacy.
- Mantener versionado semantico para evitar upgrades involuntarios.

## Open Questions

- Se requiere cache distribuida de Token/Sign para despliegues multi-instancia desde la primera version?
No
- El empaquetado inicial sera NuGet interno firmado o referencia de proyecto en monorepo?
referencia a proyecto en monorepo
- Que politica exacta de retries/timeouts se alinea con SRE para servicios externos?
Sin retries, timeout de 1 minuto.
- Se incorporara firma de comprobantes adicionales o alcance exclusivo de WSFEv1 CAE?
si. En un futuro se agregaran otros servicios que utilizan el mismo mecanismo de autenticación.
