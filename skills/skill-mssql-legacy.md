# SKILL: MANEJO DE BASE DE DATOS LEGACY (SQL SERVER)

## CONTEXTO DEL SISTEMA
1. El sistema interactúa con una base de datos legacy (heredada) alimentada por el módulo ZKAccess de ZKTeco, que envia las marcas de asistencia a la tabla respectiva via trigger.
2. NUNCA inventes esquemas, tablas o columnas. Trabaja estrictamente con los scripts DDL proporcionados por el usuario.

## REGLAS DE ACCESO A DATOS (DAPPER)
1. LECTURA DE MARCAS: Las consultas a las tablas transaccionales de marcaciones deben ser estrictamente de LECTURA.
2. OPTIMIZACIÓN: Utiliza siempre la cláusula `WITH (NOLOCK)` en las consultas `SELECT` que ataquen tablas con alto volumen de registros legacy para evitar bloqueos en la base de datos de producción.
3. MAPEO: Utiliza Dapper para mapear los resultados a entidades C# (DTOs). Si los nombres de las columnas legacy son complejos, usa alias en la consulta SQL para que coincidan con las propiedades PascalCase del modelo C#.