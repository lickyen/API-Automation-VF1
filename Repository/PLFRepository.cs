using Dapper;
using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;

namespace ProductivityApi.Repository
{
    public class PLFRepository
    {
        private readonly string _plfConStr;
        private readonly ILogger<PLFRepository> _logger;

        private const string NeolotexDisbColumns = @"
            SELECT
                sz_loan_account_no   AS LoanAccountNo,
                applicant_type       AS ApplicantType,
                branch_code          AS BranchCode,
                customerid           AS CustomerId,
                customer_name        AS CustomerName,
                sz_collateral_id     AS CollateralId,
                loan_status          AS LoanStatus,
                loanclosure_date     AS LoanClosureDate,
                disbursement_date    AS DisbursementDate,
                noc_status           AS NocStatus
            FROM plf.neolotex_disb";

        public PLFRepository(IConfiguration configuration, ILogger<PLFRepository> logger)
        {
            _plfConStr = configuration.GetConnectionString("PlfDBConn");
            _logger = logger;
        }
        public async Task<List<PLFPayment>> GetPaymentsByLan(string lan)
        {
            try
            {
                var sql = @"SELECT 
                        HDR.RECEIPTID       AS ReceiptId,
                        HDR.RECEIPTMODE     AS ReceiptMode,
                        HDR.RECEIPTAMOUNT / 100.0    AS ReceiptAmount,
                        HDR.RECEIPTDATE     AS ReceiptDate,
                        HDR.RECEIPTPURPOSE AS ReceiptPurpose
                    FROM PLF.FINRECEIPTHEADER HDR
                    INNER JOIN PLF.FINRECEIPTDETAIL DTL ON HDR.RECEIPTID = DTL.RECEIPTID
                    WHERE HDR.REFERENCE = @reference
                    ORDER BY HDR.RECEIPTDATE DESC;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<PLFPayment>(sql, new { reference = lan });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payments for LAN: {Lan}", lan);
                return new List<PLFPayment>();
            }
        }
        public async Task<List<NeolotexDisb>> GetClosedLansAsync()
        {
            try
            {
                var sql = $@"{NeolotexDisbColumns}
                WHERE loan_status <> 'Active Loan'
                    AND loanclosure_date >= CURRENT_DATE - 1
                    AND loanclosure_date <  CURRENT_DATE;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<NeolotexDisb>(sql);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching closed loans from PLF.");
                return new List<NeolotexDisb>();
            }
        }
        public async Task<List<NeolotexDisb>> GetLoansByCollIdAsync(string collId)
        {
            try
            {
                var sql = $@"{NeolotexDisbColumns}
                    WHERE sz_collateral_id = @collId;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<NeolotexDisb>(sql, new { collId });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching loans for CollId: {CollId}", collId);
                return new List<NeolotexDisb>();
            }
        }
        public async Task<List<NeolotexDisb>> GetLoansByCustIdAsync(string custId)
        {
            try
            {
                var sql = $@"{NeolotexDisbColumns}
                    WHERE customerid = @custId;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<NeolotexDisb>(sql, new { custId });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching loans for CustId: {CustId}", custId);
                return new List<NeolotexDisb>();
            }
        }
        public async Task<List<NeolotexDisb>> GetLinkedLoansByLanAsync(string lan)
        {
            try
            {
                var sql = $@"{NeolotexDisbColumns}
                WHERE customerid IN (
                    SELECT customerid
                    FROM plf.neolotex_disb
                    WHERE sz_loan_account_no = @lan
                )
                AND applicant_type = 'B';";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<NeolotexDisb>(sql, new { lan });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching loans for LAN: {Lan}", lan);
                return new List<NeolotexDisb>();
            }
        }
        public async Task<List<NeolotexDisb>> GetApplicantsByLanAsync(string lan)
        {
            try
            {
                var sql = $@"{NeolotexDisbColumns}
                    WHERE sz_loan_account_no = @lan;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                var result = await connection.QueryAsync<NeolotexDisb>(sql, new { lan });
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers for LAN: {Lan}", lan);
                return new List<NeolotexDisb>();
            }
        }
        public async Task<List<NeolotexDisb>> GetApplicantsByLansAsync(IEnumerable<string> loanAccountNos)
        {
            try
            {
                var sql = $@"
                    {NeolotexDisbColumns}
                    WHERE sz_loan_account_no = ANY(@loanAccountNos);";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<NeolotexDisb>(
                    sql,
                    new
                    {
                        loanAccountNos = loanAccountNos.ToArray()
                    });

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching applicants for linked loans");
                return new List<NeolotexDisb>();
            }
        }
        public async Task<PlfBankDetails?> GetBankDetailsByIfscAsync(string ifsc)
        {
            try
            {
                var sql = @"
            SELECT
                bb.bankcode    AS BankCode,
                bd.bankname    AS BankName,
                bb.branchcode  AS BankBranchCode,
                bb.branchdesc  AS BranchName,
                bb.micr        AS MicrCode,
                bb.ifsc        AS IfscCode
            FROM plf.bankbranches bb
            JOIN plf.bmtbankdetail bd ON bb.bankcode = bd.bankcode
            WHERE bb.active = 1
              AND bb.ifsc = @ifsc;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<PlfBankDetails>(sql, new { ifsc });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching bank details for IFSC: {Ifsc}", ifsc);
                return null;
            }
        }
        //added by Harika
        public async Task<PlFLoanStatusModel?> GetLoanStatusByLanAsync(string lan)
        {
            try
            {
                var sql = @"
                    select distinct sz_loan_account_no as LoanNumber,loan_status as LoanStatus from plf.neolotex_disb where sz_loan_account_no=@lan
                    group by sz_loan_account_no,loan_status";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<PlFLoanStatusModel>(sql, new { lan });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Lan status by lan: {Lan}", lan);
                return null;
            }
        } 

        //added by Harika
        public async Task<PlfCashByCIFModel?> GetCashPayemntsByCIFAsync(string cif)
        {
            try
            {
                var sql = @"
            select DISTINCT (CC.CUSTID) as CUSTID,
            CAST (SUM(FP.RECEIPTAMOUNT/100) AS NUMERIC (18,2))AMOUNT
            from plf.financemain c
            join plf.customers cc
            on cc.custid=c.custid
            join plf.finreceiptheader fp
            on c.finreference=fp.reference
            JOIN PLF.NEOLOTEX_DISB ND
            ON ND.SZ_LOAN_ACCOUNT_NO=c.finreference
            AND ND.CUSTOMERID=CC.CUSTCIF
            where CC.CUSTCIF=@cif
            and FP.receiptmode='CASH'
            AND FP.RECEIPTMODESTATUS='R'
            AND RECEIPTDATE='2026-06-05'
            GROUP BY CC.CUSTID";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<PlfCashByCIFModel>(sql, new { cif });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Cash Payemnts  by CIF: {CIF}", cif);
                return null;
            }
        }

        public async Task<List<DisbLansResponseDto>> GetDisbursementsAsync(DisbLansRequestDto request)
        {
            try
            {
                var sql = @"
            SELECT  
                d.finreference      AS Lan,
                d.disbdate          AS DisbDate,
                d.disbseq           AS DisbSeq,
                f.extreference      AS LosApplicationNo
            FROM plf.findisbursementdetails d
            INNER JOIN plf.financemain f 
                ON d.finreference = f.finreference
            WHERE d.disbseq = 1
              AND d.disbdate >= @FromDate
              AND d.disbdate < @ToDate + INTERVAL '1 day'
            ORDER BY d.finreference ASC;";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();

                var result = await connection.QueryAsync<DisbLansResponseDto>(
                    sql,
                    new
                    {
                        request.FromDate,
                        request.ToDate
                    }
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching disbursement records between {FromDate} and {ToDate}",
                    request.FromDate, request.ToDate);

                return new List<DisbLansResponseDto>();
            }
        }
    }
}




