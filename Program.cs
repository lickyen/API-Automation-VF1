using Microsoft.Extensions.Options;
using ProductivityApi.Middlewares;
using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using ProductivityApi.Jobs;
using Serilog;
using Hangfire;
using Hangfire.SqlServer;
using ProductivityApi;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var env = builder.Environment;

        if (env.IsProduction())
        {
            policy.WithOrigins("http://localhost:3001", "https://productivity.vistaarfinance.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else // UAT/DEV
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3002", "https://uatproductivity.vistaarfinance.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

//Dapper mapping
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

//select config based on env
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

//map config to AppSettings Model
var appSettings = builder.Configuration.Get<AppSettings>() ?? throw new InvalidOperationException("Failed to load configuration");
builder.Services.AddSingleton(appSettings);

//configure logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(
        path: "logs/appLogs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30
    )
    .WriteTo.Console()
    .CreateLogger();

// Tell the Host to use Serilog
builder.Host.UseSerilog();

// Hangfire configuration
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DBConn")));

builder.Services.AddHangfireServer();

builder.Services.AddHttpClient<PLFService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<SharedService>();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<BranchServices>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<DCServices>();
builder.Services.AddScoped<CersaiService>();
builder.Services.AddScoped<BranchVisitService>();
builder.Services.AddScoped<LegalNoticeService>();
builder.Services.AddScoped<NOCService>();
builder.Services.AddScoped<ModService>();
builder.Services.AddScoped<FcRefundService>();
builder.Services.AddScoped<LcRefundService>();
builder.Services.AddScoped<LegalBillingService>();
builder.Services.AddScoped<CustEmailUpdateService>();
builder.Services.AddScoped<LoanCancellationService>();

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SharedRepository>();
builder.Services.AddScoped<BranchRepository>();
builder.Services.AddScoped<OtpRepository>();
builder.Services.AddScoped<SettlementRepository>();
builder.Services.AddScoped<DCRepository>();
builder.Services.AddScoped<CersaiRepository>();
builder.Services.AddScoped<BranchVisitRepository>();
builder.Services.AddScoped<LegalNoticeRepository>();
builder.Services.AddScoped<NOCRepository>();
builder.Services.AddScoped<ModRepository>();
builder.Services.AddScoped<FcRefundRepository>();
builder.Services.AddScoped<LcRefundRepository>();
builder.Services.AddScoped<LegalBillingRepository>();
builder.Services.AddScoped<CustEmailUpdateRepository>();
builder.Services.AddScoped<UserLogRepository>();
builder.Services.AddScoped<LoanCancellationRepository>();


builder.Services.AddScoped<IJob, BranchVisitJob>();
builder.Services.AddScoped<IJob, FcRefundJob>();
builder.Services.AddScoped<IJob, LcRefundJob>();
builder.Services.AddScoped<IJob, CustEmailUpdateJob>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

// Serilog Request Logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex != null || httpContext.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
});

//app.UseMiddleware<LoggingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<ApiKeyMiddleware>();
}

app.UseAuthorization();

app.MapControllers();

// Hangfire Jobs
app.UseHangfireDashboard("/hangfire");
await app.MapJobsAsync();

app.Run();
