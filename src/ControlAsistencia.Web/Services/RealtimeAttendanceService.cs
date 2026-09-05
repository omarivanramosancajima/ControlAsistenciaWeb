using ControlAsistencia.Web.Hubs;
using ControlAsistencia.Web.Models;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;

namespace ControlAsistencia.Web.Services;

/// <summary>
/// Escucha CHECKINOUT mediante SQL Server Query Notifications/TableDependency
/// y publica los últimos 20 registros hacia Home mediante SignalR.
/// </summary>
public sealed class RealtimeAttendanceService : BackgroundService
{
    private const int MaxItems = 20;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<RealtimeAttendanceHub> _hubContext;
    private readonly ILogger<RealtimeAttendanceService> _logger;
    private readonly string _connectionString;

    private SqlTableDependency<CheckInOutNotificationRow>? _tableDependency;

    public RealtimeAttendanceService(
        IConfiguration configuration,
        IHubContext<RealtimeAttendanceHub> hubContext,
        ILogger<RealtimeAttendanceService> logger)
    {
        _configuration = configuration;
        _hubContext = hubContext;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("ControlAsistenciaDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ControlAsistenciaDb'.");
    }

    public async Task<IReadOnlyList<HomeRealtimeAttendanceItemViewModel>> GetLatestAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP (@MaxItems)
    C.USERID AS UserId,
    U.BADGENUMBER AS BadgeNumber,
    ISNULL(U.NAME, '') AS Name,
    C.CHECKTIME AS CheckTime,
    ISNULL(D.DEPTNAME, '') AS Area,
    CAST(U.PHOTO AS VARBINARY(MAX)) AS Photo
FROM dbo.CHECKINOUT C WITH (NOLOCK)
INNER JOIN dbo.USERINFO U WITH (NOLOCK) ON U.USERID = C.USERID
LEFT JOIN dbo.DEPARTMENTS D WITH (NOLOCK) ON D.DEPTID = U.DEFAULTDEPTID
ORDER BY C.CHECKTIME DESC;";

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<HomeRealtimeAttendanceRow>(
            new CommandDefinition(sql, new { MaxItems }, cancellationToken: cancellationToken));

        return rows.Select(ToViewModel).ToList();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _tableDependency = new SqlTableDependency<CheckInOutNotificationRow>(
                    _connectionString,
                    "CHECKINOUT");

                _tableDependency.OnChanged += TableDependency_OnChanged;
                _tableDependency.OnError += TableDependency_OnError;
                _tableDependency.Start();

                _logger.LogInformation("Monitoreo en tiempo real de CHECKINOUT iniciado.");

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "No fue posible iniciar o mantener el monitoreo de CHECKINOUT. Se reintentará.");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            finally
            {
                StopTableDependency();
            }
        }
    }

    private void TableDependency_OnChanged(
        object sender,
        RecordChangedEventArgs<CheckInOutNotificationRow> e)
    {
        if (e.ChangeType == ChangeType.None)
        {
            return;
        }

        _ = PublishLatestAsync();
    }

    private void TableDependency_OnError(
        object sender,
        TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
    {
        _logger.LogError(e.Error, "Error en el monitoreo de CHECKINOUT.");
    }

    private async Task PublishLatestAsync()
    {
        try
        {
            var items = await GetLatestAsync();
            await _hubContext.Clients.All.SendAsync(
                "attendanceUpdated",
                items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No fue posible publicar la actualización del monitoreo en tiempo real.");
        }
    }

    private void StopTableDependency()
    {
        if (_tableDependency is null)
        {
            return;
        }

        try
        {
            _tableDependency.OnChanged -= TableDependency_OnChanged;
            _tableDependency.OnError -= TableDependency_OnError;
            _tableDependency.Stop();
            _tableDependency.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al detener el monitoreo de CHECKINOUT.");
        }
        finally
        {
            _tableDependency = null;
        }
    }

    public override void Dispose()
    {
        StopTableDependency();
        base.Dispose();
    }

    private static HomeRealtimeAttendanceItemViewModel ToViewModel(HomeRealtimeAttendanceRow row)
    {
        return new HomeRealtimeAttendanceItemViewModel
        {
            UserId = row.UserId,
            BadgeNumber = row.BadgeNumber ?? string.Empty,
            Name = row.Name ?? string.Empty,
            CheckTime = row.CheckTime,
            Area = row.Area ?? string.Empty,
            PhotoBase64 = row.Photo is { Length: > 0 }
                ? $"data:image/jpeg;base64,{Convert.ToBase64String(row.Photo)}"
                : null
        };
    }

    private sealed class HomeRealtimeAttendanceRow
    {
        public int UserId { get; init; }
        public string? BadgeNumber { get; init; }
        public string? Name { get; init; }
        public DateTime CheckTime { get; init; }
        public string? Area { get; init; }
        public byte[]? Photo { get; init; }
    }

    // Debe reflejar las columnas que la prueba SqlTableDependency del usuario
    // ya confirmó para dbo.CHECKINOUT.
    private sealed class CheckInOutNotificationRow
    {
        public int USERID { get; set; }
        public DateTime CHECKTIME { get; set; }
        public string CHECKTYPE { get; set; } = string.Empty;
        public int VERIFYCODE { get; set; }
        public string SENSORID { get; set; } = string.Empty;
        public string Memoinfo { get; set; } = string.Empty;
        public string WorkCode { get; set; } = string.Empty;
        public string sn { get; set; } = string.Empty;
        public int UserExtFmt { get; set; }
    }
}
