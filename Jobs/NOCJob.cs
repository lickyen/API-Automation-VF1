using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Services;
using Serilog;

namespace ProductivityApi.Jobs
{
    public class NOCJob : IJob
    {

        public string JobId => "noc-job";

        private readonly Serilog.ILogger _logger;
        private readonly NOCService _service;
        private readonly string _plfConStr;
        public NOCJob(NOCService service, IConfiguration configuration)
        {
            _service = service;
            _plfConStr = configuration.GetConnectionString("PlfDBConn")!;

            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/NOCJob/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.Information("NOCJob started at {Time}", DateTimeOffset.Now);

            try
            {
                await InsertLansAsync(ct);
                _logger.Information("NOCJob completed at {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in NOCJob.ExecuteAsync");
            }
        }

        private async Task InsertLansAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var dtos = await FetchLansFromPlfAsync(ct);

                if (!dtos.Any())
                {
                    _logger.Information("No  LANs found for yesterday");
                    return;
                }

                _logger.Information("Fetched {Count} LAN(s) from PLF", dtos.Count);

                var result = await _service.AddLanAsync(dtos);

                _logger.Information(
                    "LAN insert complete — Added: {Added}, Skipped: {Skipped}",
                    result.Added, result.Skipped
                );

                if (result.AddedLans.Any())
                    _logger.Information("Added LANs: {Lans}", string.Join(", ", result.AddedLans));

                if (result.SkippedLans.Any())
                    _logger.Information("Skipped LANs (already exist): {Lans}", string.Join(", ", result.SkippedLans));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to insert LANs");
            }
        }
        private async Task<List<NeolotexDisb>> FetchLansFromPlfAsync(CancellationToken ct)
        {
            

            const string query = @"SELECT * FROM PLF.neolotex_disb";

            var dtos = new List<NeolotexDisb>();

            try
            {
                using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync(ct);

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync(ct);

                while (await reader.ReadAsync(ct))
                {
                    dtos.Add(new NeolotexDisb
                    {
                        //sz_loan_account_no = reader.GetString(0),
                        //applicant_type = reader.GetString(1),
                        //branch_code = reader.GetString(2),
                        //branchname = reader.GetString(3),
                        //cluster_name = reader.GetString(4),
                        //state_name = reader.GetString(5),

                        //customerid = reader.GetString(6),
                        //customer_name = reader.GetString(7),
                        //sz_collateral_id = reader.GetString(8),

                        //loan_status = reader.GetString(9),
                        //loanclosure_date = reader.GetDateTime(10),
                        //disbursement_date = reader.GetDateTime(11),
                        //dt_correction = reader.GetDateTime(12),
                        //noc_status = reader.GetString(13),

                        //extraction_dt = reader.GetDateTime(14),
                        //external_reference = reader.GetString(15)
                        //  CreatedBy = "SYSTEM"
                        sz_loan_account_no = reader.IsDBNull(0) ? null : reader.GetString(0),
                        applicant_type = reader.IsDBNull(1) ? null : reader.GetString(1),
                        branch_code = reader.IsDBNull(2) ? null : reader.GetString(2),
                        branchname = reader.IsDBNull(3) ? null : reader.GetString(3),
                        cluster_name = reader.IsDBNull(4) ? null : reader.GetString(4),
                        state_name = reader.IsDBNull(5) ? null : reader.GetString(5),

                        customerid = reader.IsDBNull(6) ? null : reader.GetString(6),
                        customer_name = reader.IsDBNull(7) ? null : reader.GetString(7),
                        sz_collateral_id = reader.IsDBNull(8) ? null : reader.GetString(8),

                        loan_status = reader.IsDBNull(9) ? null : reader.GetString(9),
                        loanclosure_date = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                        disbursement_date = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
                        dt_correction = reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12),
                        noc_status = reader.IsDBNull(13) ? null : reader.GetString(13),

                        extraction_dt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14),
                        external_reference = reader.IsDBNull(15) ? null : reader.GetString(15)
                    });
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return dtos;

        }


    }
}
