namespace ControlAsistencia.Web.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static OperationResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static OperationResult Fail(string message) => new() { Success = false, Message = message };
}