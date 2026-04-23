# Plan de Rollout Progresivo

## Alcance

Aplicar migracion por etapas desde integraciones legacy a la biblioteca .NET 8 ARCA.

## Etapas

1. Piloto tecnico
- Consumidor: `samples/PilotConsumer` como base de referencia.
- Ambiente: homologacion.
- Criterio de salida: autenticacion WSAA correcta y ejecucion de operaciones WSFEv1 con trazabilidad.

2. Primer consumidor productivo
- Activar por feature flag o configuracion controlada.
- Monitorear errores de autenticacion, rechazo funcional y latencia por 7 dias.

3. Expansiones por lotes
- Migrar consumidores restantes en ventanas de bajo trafico.
- Mantener fallback a legacy durante la estabilizacion.

## Monitoreo operativo

- Logs estructurados con correlacion por operacion.
- Metricas de latencia, error y reintentos.
- Alertas por incrementos de error de autenticacion o timeout.

## Checklist de ejecucion

- [ ] Homologacion validada contra baseline legacy.
- [ ] Certificados verificados en cada entorno.
- [ ] Dashboards y alertas habilitadas.
- [ ] Ventana de rollback definida por equipo operativo.
- [ ] Aprobacion funcional y tecnica para cada lote.

## Evidencia operativa

Estado actual: cierre documental preparado. Completar esta seccion cuando se disponga de evidencia de ejecucion real.

- Fecha de ejecucion:
- Consumidor(es) migrado(s):
- Ambiente:
- Resultado de homologacion:
- Resultado de monitoreo inicial:
- Incidencias detectadas:
- Decision de continuidad o rollback:
