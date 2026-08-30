using ControlAsistencia.Web.Repositories;
using ControlAsistencia.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.Cookie.Name = "ControlAsistencia.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
builder.Services.AddScoped<IHorarioRepository, HorarioRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IFeriadoRepository, FeriadoRepository>();
builder.Services.AddScoped<IExcepcionRepository, ExcepcionRepository>();
builder.Services.AddScoped<ITurnosEmpleadoRepository, TurnosEmpleadoRepository>();
builder.Services.AddScoped<IAttendanceReportRepository, AttendanceReportRepository>();
builder.Services.AddScoped<IAttendancePersonProvider, AttendancePersonProvider>();
builder.Services.AddScoped<IAttendanceScheduleProvider, AttendanceScheduleProvider>();
builder.Services.AddScoped<IAttendanceMarkProvider, AttendanceMarkProvider>();
builder.Services.AddScoped<IAttendanceParameterProvider, AttendanceParameterProvider>();
builder.Services.AddScoped<IAttendanceHolidayProvider, AttendanceHolidayProvider>();
builder.Services.AddScoped<IAttendanceExceptionProvider, AttendanceExceptionProvider>();
builder.Services.AddScoped<IAttendanceCalculationContextBuilder, AttendanceCalculationContextBuilder>();
builder.Services.AddScoped<IAttendanceCalculationEngine, AttendanceCalculationEngine>();
builder.Services.AddScoped<IAttendanceReportService, AttendanceReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
