using Microsoft.Extensions.Options;
using ProductivityApi.Middlewares;
using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://productivity.vistaarfinance.com")
                .AllowAnyHeader()
                .AllowAnyMethod();
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
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30
    )
    .WriteTo.Console()
    .CreateLogger();

// Tell the Host to use Serilog
builder.Host.UseSerilog();

builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SharedService>();
builder.Services.AddScoped<BranchServices>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<DCServices>();
builder.Services.AddScoped<CersaiService>();

builder.Services.AddHttpClient<PLFService>();

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SharedRepository>();
builder.Services.AddScoped<BranchRepository>();
builder.Services.AddScoped<OtpRepository>();
builder.Services.AddScoped<SettlementRepository>();
builder.Services.AddScoped<DCRepository>();
builder.Services.AddScoped<CersaiRepository>();

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

app.Run();
