using ControlAsistencia.Web.Tests;

AttendanceCalculationEngineTests.Run();
AttendanceCalculationEngineValidationScenarios.Run();
await AttendanceReportServiceTests.RunAsync();
await IntegrationValidationRunner.RunAsync();
