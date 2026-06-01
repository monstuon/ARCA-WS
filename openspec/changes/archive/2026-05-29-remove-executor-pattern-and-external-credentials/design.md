## Context

La implementacion actual distribuye la ejecucion en un patron executor/orchestrator con ramas para credenciales externas. En la practica operativa, el consumidor siempre envia `Token` y `Sign` desde su base de datos, por lo que ese modo "externo" no representa una alternativa real sino la ruta normal. Esta diferencia entre diseno y uso real aumenta complejidad, dificulta trazabilidad y encarece mantenimiento.

El cambio debe mantener compatibilidad funcional en autorizacion WSFE y autenticacion WSAA, incluyendo fallback automatico a WSAA cuando el token recibido no sea utilizable. El alcance cruza servicios de aplicacion, DI y pruebas.

## Goals / Non-Goals

**Goals:**
- Eliminar el patron executor de los flujos WSAA/WSFE y reemplazarlo por una secuencia lineal y explicita.
- Unificar el manejo de credenciales: siempre procesar `Token/Sign` de entrada como fuente primaria del consumidor.
- Mantener fallback a WSAA cuando faltan credenciales o resultan invalidas/vencidas.
- Simplificar dependencias y branching interno sin cambiar la responsabilidad de persistencia (ERP sigue siendo owner durable).

**Non-Goals:**
- Cambiar contratos publicos principales mas alla de remover semantica interna de "external credentials mode".
- Introducir persistencia durable de credenciales en la API.
- Cambiar reglas de negocio fiscales de WSFE no relacionadas con autenticacion/flujo.

## Decisions

1. **Eliminar executors y consolidar flujo lineal por servicio**
   - Decision: cada operacion critica ejecuta una secuencia directa: validar entrada -> resolver credenciales -> invocar WSFE/WSAA -> mapear respuesta.
   - Rationale: reduce indirecciones, mejora depuracion y facilita pruebas por camino principal.
   - Alternativas consideradas:
	 - Mantener executor con simplificacion parcial: descartado por mantener overhead conceptual.
	 - Reemplazar por pipeline generico con middlewares: descartado por complejidad innecesaria para el alcance.

2. **Tratar `Token/Sign` como entrada normal, no como estrategia externa opcional**
   - Decision: remover ramas/modelos que distingan "external credentials" como modo separado.
   - Rationale: alinea implementacion con comportamiento real del sistema y elimina ambiguedad de rutas.
   - Alternativas consideradas:
	 - Mantener bandera de estrategia: descartado por duplicar caminos y costo de testing.

3. **Conservar fallback operativo a WSAA bajo condiciones de no-utilizabilidad**
   - Decision: si faltan `Token/Sign` o WSFE/validacion detecta invalidez/expiracion, renovar via WSAA y continuar en el mismo flujo lineal.
   - Rationale: preserva resiliencia sin reintroducir arquitectura compleja.
   - Alternativas consideradas:
	 - Fallar sin intentar fallback: descartado por degradar disponibilidad operativa.

4. **Mantener cache en memoria solo como optimizacion local**
   - Decision: no agregar almacenamiento durable; la cache local continua siendo opcional y no contractual.
   - Rationale: conserva simplicidad de despliegue y responsabilidad de persistencia en el ERP.

## Risks / Trade-offs

- **[Riesgo]** Eliminacion de executors puede romper extensiones internas que dependan de esas abstracciones. → **Mitigacion**: remover wiring de DI de forma controlada y actualizar pruebas de integracion/contrato.
- **[Riesgo]** Confusion en semantica de respuesta cuando se reutiliza token recibido vs token renovado. → **Mitigacion**: mantener criterio explicito en mapeo de respuesta para indicar credenciales renovadas cuando aplique.
- **[Trade-off]** Flujo lineal reduce flexibilidad de composicion generica. → **Mitigacion**: priorizar legibilidad y operaciones reales; reintroducir extension points solo si aparece necesidad concreta medida.