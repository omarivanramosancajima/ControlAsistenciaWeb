namespace ControlAsistencia.Web.Models;

public class AreaOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static AreaOperationResult Ok(string message) =>
        new() { Success = true, Message = message };

    public static AreaOperationResult Fail(string message) =>
        new() { Success = false, Message = message };
}
