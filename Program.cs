using Hangfire;
using Hangfire.Dashboard;
using ProductivityApi.Jobs;
using ProductivityApi.Middlewares;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;
using ProductivityApi.Repositories;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
                      .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();

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

builder.Services.AddScoped<AirtelabankService>();
builder.Services.AddScoped<AirtelBankRepository>();

builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<AnnouncementRepository>();

builder.Services.AddScoped<AxisService>();
builder.Services.AddScoped<AxisRepository>();

builder.Services.AddScoped<BranchServices>();
builder.Services.AddScoped<BranchRepository>();

builder.Services.AddScoped<BranchVisitService>();
builder.Services.AddScoped<BranchVisitRepository>();

builder.Services.AddScoped<BTLettersService>();
builder.Services.AddScoped<BTLettersRepository>();

builder.Services.AddScoped<BulkDocsService>();
builder.Services.AddScoped<BulkDocsRepository>();

builder.Services.AddScoped<BulkFclService>();
builder.Services.AddScoped<BulkFclRepository>();

builder.Services.AddScoped<CersaiService>();
builder.Services.AddScoped<CersaiRepository>();

builder.Services.AddScoped<CibilCorrectionService>();
builder.Services.AddScoped<CibilCorrectionRepository>();

builder.Services.AddScoped<ClosureService>();
builder.Services.AddScoped<ClosureRepository>();

builder.Services.AddHttpClient<CRIFService>();
builder.Services.AddScoped<CrifWrapperRepository>();

builder.Services.AddScoped<CustEmailUpdateService>();
builder.Services.AddScoped<CustEmailUpdateRepository>();

builder.Services.AddScoped<DCServices>();
builder.Services.AddScoped<DCRepository>();

builder.Services.AddScoped<DeathClaimService>();
builder.Services.AddScoped<DeathClaimRepository>();

builder.Services.AddHttpClient<DmsDocsService>();
builder.Services.AddScoped<DmsDocsRepository>();

builder.Services.AddScoped<FcRefundService>();
builder.Services.AddScoped<FcRefundRepository>();

builder.Services.AddScoped<FixedAssetService>();
builder.Services.AddScoped<FixedAssetRepository>();

builder.Services.AddHttpClient<GoDigitService>();
builder.Services.AddScoped<GoDigitRepository>();

builder.Services.AddScoped<GoldLoanLeadService>();
builder.Services.AddScoped<GoldLoanLeadRepository>();

builder.Services.AddScoped<GoldLoanVendorService>();
builder.Services.AddScoped<GoldLoanVendorRepository>();

builder.Services.AddScoped<GurukulUserService>();
builder.Services.AddScoped<GurukulUserRepository>();

builder.Services.AddScoped<IndusArchivedService>();
builder.Services.AddScoped<IndusArchivedRepository>();

builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<InvoiceRepository>();

builder.Services.AddScoped<LcRefundService>();
builder.Services.AddScoped<LcRefundRepository>();

builder.Services.AddScoped<LegalBillingService>();
builder.Services.AddScoped<LegalBillingRepository>();

builder.Services.AddScoped<LegalCourtsService>();
builder.Services.AddScoped<LegalCourtsRepository>();

builder.Services.AddScoped<LegalNoticeService>();
builder.Services.AddScoped<LegalNoticeRepository>();

builder.Services.AddScoped<LoanCancellationService>();
builder.Services.AddScoped<LoanCancellationRepository>();

builder.Services.AddScoped<ModService>();
builder.Services.AddScoped<ModRepository>();

builder.Services.AddScoped<ModulesService>();
builder.Services.AddScoped<ModulesRepository>();

builder.Services.AddScoped<NewBranchServices>();
builder.Services.AddScoped<NewBranchRepository>();

builder.Services.AddScoped<NOCService>();
builder.Services.AddScoped<NOCRepository>();

builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<OtpRepository>();

builder.Services.AddHttpClient<PLFService>();
builder.Services.AddScoped<PLFRepository>();

builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<PurchaseOrderRepository>();

builder.Services.AddScoped<SARFAESIPhysicalFilingService>();
builder.Services.AddScoped<SARFAESIPhysicalFilingRepository>();

builder.Services.AddScoped<SARFAESIService>();
builder.Services.AddScoped<SARFAESIRepository>();

builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<SettlementRepository>();

builder.Services.AddScoped<SharedService>();
builder.Services.AddScoped<SharedRepository>();

builder.Services.AddScoped<SOALODFCLService>();
builder.Services.AddScoped<SOALODFCLRepository>();

builder.Services.AddScoped<TrainingDashboardService>();
builder.Services.AddScoped<TrainingDashboardRepository>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<VCPService>();
builder.Services.AddScoped<VCPRepository>();

builder.Services.AddScoped<VOTService>();
builder.Services.AddScoped<VOTRepository>();

builder.Services.AddScoped<WabaService>();
builder.Services.AddScoped<WabaRepository>();

builder.Services.AddScoped<WabaWtsappHookService>();
builder.Services.AddScoped<WabaWtsappHookRepository>();

builder.Services.AddScoped<ZeroBalanceDayService>();
builder.Services.AddScoped<ZeroBalanceDayRepository>();

// Standalone services
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<KotakService>();
builder.Services.AddScoped<RazorpayService>();
builder.Services.AddScoped<RefundCalculatorService>();

// Standalone repositories
builder.Services.AddScoped<UserLogRepository>();

// Helper
builder.Services.AddScoped<JwtHelper>();

// Jobs
builder.Services.AddScoped<IJob, BranchVisitJob>();
builder.Services.AddScoped<IJob, FcRefundJob>();
builder.Services.AddScoped<IJob, LcRefundJob>();
builder.Services.AddScoped<IJob, CustEmailUpdateJob>();
builder.Services.AddScoped<IJob, LegalBillingJob>();
builder.Services.AddScoped<IJob, NOCJob>();
builder.Services.AddScoped<IJob, NOC_SMS_Job>();
builder.Services.AddScoped<IJob, ZeroBalanceDayNotificationEmailJob>();
builder.Services.AddScoped<IJob, ZeroBalanceDayNotSubmittedReminder1000Job>();
builder.Services.AddScoped<IJob, ZeroBalanceDayNotSubmittedReminder1100Job>();
builder.Services.AddScoped<IJob, ZeroBalanceDayNotSubmittedReminder1130Job>();
builder.Services.AddScoped<IJob, ZeroBalanceDayNotSubmittedReminder1155Job>();
builder.Services.AddScoped<IJob, ZeroBalanceDayRchZchEscalationJob>();


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
QuestPDF.Settings.License = LicenseType.Community;
// Configure cookie policy to enforce secure cookies and reasonable SameSite policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings!.Issuer,
                ValidAudience = jwtSettings.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key))
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.GetCultureInfo("en-IN");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        // Silence Hangfire dashboard polling noise
        if (httpContext.Request.Path.StartsWithSegments("/hangfire"))
            return Serilog.Events.LogEventLevel.Verbose;

        return ex != null || httpContext.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
    };
});

app.UseHttpsRedirection(); //HTTPS Only
app.UseHsts(); // enables HTTP Strict Transport Security (HSTS).

app.UseCors("AllowFrontend");

app.UseStaticFiles(); // keeps default wwwroot working

if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<ApiKeyMiddleware>();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = Array.Empty<IDashboardAuthorizationFilter>()
});

app.MapControllers();

await app.MapJobsAsync();

app.Run();
