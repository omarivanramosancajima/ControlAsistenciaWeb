# SKILL: RESTRICCIONES ESTRICTAS DE IA Y SEGURIDAD

## REGLAS INQUEBRANTABLES
1. PROHIBIDO BORRAR DATOS: Nunca generes ni ejecutes sentencias `DROP TABLE`, `TRUNCATE`, o `DELETE` sin confirmación explícita en mayúsculas del usuario.
2. NO SOBREESCRIBIR SIN AVISO: Si vas a modificar un archivo existente, muestra solo el bloque de código que cambia o advierte de la sobreescritura.
3. CERO ALUCINACIONES: Si no sabes cómo se relaciona una tabla o te falta contexto del negocio, DETENTE y pregúntale al usuario. No asumas reglas de negocio.
4. PROTECCIÓN DE CÓDIGO: Tu código debe compilar a la primera. Maneja excepciones (`try-catch`) en todos los Dapper Repositories.