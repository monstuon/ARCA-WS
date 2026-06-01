## Why

El codigo actual usa un patron executor que agrega capas de abstraccion innecesarias y complejiza el flujo en autenticacion y facturacion. Ademas, el manejo de credenciales externas no aplica al uso real: el sistema siempre envia Token y Sign desde su propia base de datos.

Se necesita simplificar la arquitectura hacia un flujo lineal y dejar una sola estrategia de credenciales, manteniendo fallback automatico a WSAA cuando las credenciales recibidas no sean utilizables.

## What Changes

- Eliminar el patron executor de toda la solucion (no solo autenticacion) y reemplazarlo por flujos directos/lineales en servicios de aplicacion.
- Quitar el manejo de "external credentials" como modo configurable o rama alternativa.
- Mantener el contrato de entrada con `Token` y `Sign` en requests, asumiendo que siempre llegan desde el repositorio del consumidor.
- Conservar fallback operativo: cuando `Token/Sign` faltan, son invalidos o estan vencidos, renovar automaticamente via WSAA y continuar la operacion.
- Simplificar orquestacion y dependencias internas para reducir branching y puntos de fallo.
- **BREAKING**: se eliminan componentes y rutas internas basadas en executor que puedan estar siendo usadas por extensiones internas.

## Capabilities

### New Capabilities
- `request-flow-linearization`: simplifica la ejecucion interna de operaciones WSAA/WSFE eliminando el patron executor y estandarizando un flujo lineal.

### Modified Capabilities
- `arca-wsaa-authentication`: ajusta el proceso de autenticacion para operar sin executor y sin modo de credenciales externas, manteniendo fallback de renovacion cuando corresponda.
- `arca-wsfev1-invoicing`: ajusta el flujo de autorizacion para usar procesamiento lineal sin executor y mantener fallback a WSAA ante credenciales no utilizables.

## Impact

- Codigo afectado (esperado):
  - Capa Application en servicios WSAA/WSFE y cualquier clase executor/orchestrator asociada.
  - Contratos y modelos que hoy expresen ramas de credenciales externas como estrategia separada.
  - Registro de dependencias (DI) para remover executors y wiring asociado.
  - Tests de autenticacion y facturacion para cubrir flujo lineal y fallback.
- Impacto funcional:
  - Menor complejidad de ejecucion y mantenimiento.
  - Comportamiento de credenciales alineado con la operacion real (siempre Token/Sign de base de datos del consumidor).
  - Se preserva resiliencia por fallback a WSAA ante credenciales no validas.