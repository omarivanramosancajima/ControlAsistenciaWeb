# skill-arbol-departamentos.md

## Objetivo
Proveer la lógica de negocio, la estructura de datos Dapper y el componente visual en Razor para la gestión del campo `DEFAULTDEPTID` asociado a la tabla `DEPARTMENTS`, manteniendo la integridad relacional de la auto-referencia jerárquica (`DEPTID` y `SUPDEPTID`).

## 1. Modelo de Datos (DTO)
```csharp
namespace ControlAsistencia.Web.Models
{
    public class DepartamentoDTO
    {
        public int DeptID { get; set; }
        public string DeptName { get; set; }
        public int? SupDeptID { get; set; }
        public List<DepartamentoDTO> SubDepartamentos { get; set; } = new List<DepartamentoDTO>();
    }
}