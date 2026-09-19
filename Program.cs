using Hangfire;
using Hangfire.Dashboard;
using ProductivityApi.Jobs;
using ProductivityApi.Middlewares;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;
using Microsoft.Extensions.FileProviders;
using ProductivityApi.Repositories;

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
builder.Services.AddScoped<WabaService>();
builder.Services.AddScoped<LoanCancellationService>();
builder.Services.AddScoped<SOALODFCLService>();
builder.Services.AddScoped<ClosureService>();
builder.Services.AddScoped<GoldLoanLeadService>();
builder.Services.AddScoped<GoldLoanVendorService>();
builder.Services.AddScoped<SARFAESIService>();
builder.Services.AddScoped<NewBranchServices>();
builder.Services.AddScoped<KotakService>();
builder.Services.AddScoped<DeathClaimService>();
builder.Services.AddScoped<GurukulUserService>();
builder.Services.AddScoped<VCPService>();
builder.Services.AddScoped<AxisService>();
builder.Services.AddScoped<BTLettersService>();
builder.Services.AddScoped<TrainingDashboardService>();
builder.Services.AddScoped<RazorpayService>();
builder.Services.AddHttpClient<DmsDocsService>();
builder.Services.AddHttpClient<GoDigitService>();
builder.Services.AddScoped<BulkDocsService>();
builder.Services.AddScoped<BulkFclService>();
builder.Services.AddScoped<SARFAESIPhysicalFilingService>();
builder.Services.AddScoped<IndusArchivedService>();
builder.Services.AddScoped<WabaWtsappHookService>();
builder.Services.AddScoped<ZeroBalanceDayService>();

builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<AirtelabankService>();
builder.Services.AddScoped<AnnouncementService>();

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
builder.Services.AddScoped<WabaRepository>();
builder.Services.AddScoped<UserLogRepository>();
builder.Services.AddScoped<LoanCancellationRepository>();
builder.Services.AddScoped<PLFRepository>();
builder.Services.AddScoped<SOALODFCLRepository>();
builder.Services.AddScoped<ClosureRepository>();
builder.Services.AddScoped<GoldLoanLeadRepository>();
builder.Services.AddScoped<GoldLoanVendorRepository>();
builder.Services.AddScoped<SARFAESIRepository>();
builder.Services.AddScoped<NewBranchRepository>();
builder.Services.AddScoped<DeathClaimRepository>();
builder.Services.AddScoped<GurukulUserRepository>();
builder.Services.AddScoped<VCPRepository>();
builder.Services.AddScoped<AxisRepository>();
builder.Services.AddScoped<BTLettersRepository>();
builder.Services.AddScoped<TrainingDashboardRepository>();
builder.Services.AddScoped<DmsDocsRepository>();
builder.Services.AddScoped<BulkDocsRepository>();
builder.Services.AddScoped<BulkFclRepository>();
builder.Services.AddScoped<SARFAESIPhysicalFilingRepository>();
builder.Services.AddScoped<IndusArchivedRepository>();
builder.Services.AddScoped<WabaWtsappHookRepository>();
builder.Services.AddScoped<ZeroBalanceDayRepository>();
builder.Services.AddScoped<GoDigitRepository>();

builder.Services.AddScoped<PurchaseOrderRepository>();
builder.Services.AddScoped<AirtelBankRepository>();
builder.Services.AddScoped<AnnouncementRepository>();

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

builder.Services.AddScoped<CibilCorrectionRepository>();
builder.Services.AddScoped<CibilCorrectionService>();

builder.Services.AddScoped<FixedAssetRepository>();
builder.Services.AddScoped<FixedAssetService>();

builder.Services.AddScoped<ModulesRepository>();
builder.Services.AddScoped<ModulesService>();

builder.Services.AddScoped<LegalCourtsService>();
builder.Services.AddScoped<LegalCourtsRepository>();

builder.Services.AddScoped<RefundCalculatorService>();


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure cookie policy to enforce secure cookies and reasonable SameSite policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

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

//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(
//        Path.Combine(builder.Environment.WebRootPath, "wb")),
//    RequestPath = "/wb"
//});

if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<ApiKeyMiddleware>();
}

app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = Array.Empty<IDashboardAuthorizationFilter>()
});

app.MapControllers();

await app.MapJobsAsync();

app.Run();
