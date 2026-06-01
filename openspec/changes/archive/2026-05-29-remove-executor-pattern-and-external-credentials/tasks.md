## 1. Descubrimiento y alcance tecnico

- [x] 1.1 Inventariar clases/interfaces/registraciones DI que implementan patron executor en WSAA, WSFE y flujos compartidos.
- [x] 1.2 Identificar modelos, flags o ramas que tratan "external credentials" como estrategia separada.

## 2. Refactor de flujo lineal sin executor

- [x] 2.1 Reemplazar en autenticacion WSAA la orquestacion basada en executor por metodos de servicio lineales.
- [x] 2.2 Reemplazar en autorizacion WSFE la orquestacion basada en executor por flujo directo de validacion, resolucion de credenciales, invocacion y mapeo.
- [x] 2.3 Eliminar wiring de executors en DI y registrar servicios directos como entry points runtime.

## 3. Simplificacion de manejo de credenciales

- [x] 3.1 Eliminar semantica interna de "external credentials mode" y tratar `Token/Sign` de request como entrada primaria normal.
- [x] 3.2 Mantener fallback a WSAA cuando falten credenciales o sean invalidas/vencidas, incluyendo reintento de autorizacion con credenciales renovadas.
- [x] 3.3 Verificar que la respuesta de autorizacion conserve metadata de credenciales renovadas cuando aplique.

## 4. Verificacion y hardening

- [x] 4.1 Actualizar/crear pruebas unitarias e integracion para cubrir flujo lineal sin executor en WSAA y WSFE.
- [x] 4.2 Ejecutar pruebas relevantes y ajustar regresiones por eliminacion de executor y ramas de credenciales externas.
- [x] 4.3 Revisar compilacion y limpieza final de codigo/DI para asegurar que no queden referencias al patron executor.