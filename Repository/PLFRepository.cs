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

        private const string NeolotexDisbDistinctColumns = @"
        SELECT DISTINCT ON (sz_loan_account_no)
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

        private const string DistinctLoanOrderBy = @"
        ORDER BY
            sz_loan_account_no,
            CASE WHEN applicant_type = 'B' THEN 0 ELSE 1 END";

        public PLFRepository(IConfiguration configuration, ILogger<PLFRepository> logger)
        {
            _plfConStr = configuration.GetConnectionString("PlfDBConn");
            _logger = logger;
        }

        public async Task<List<ClosureCamLmsValues>> GetClosureCamLmsAsync(string lan)
        {
            const string sql = @"
WITH PRI_PFT_Receipts AS
(
    SELECT DISTINCT receiptid
    FROM plf.receiptallocationdetail
    WHERE allocationtype IN ('PRI', 'PFT')
),
LatestAdjustedReceipt AS
(
    SELECT
        fm.finreference,
        frh.receiptid,
        frh.valuedate,
        ROW_NUMBER() OVER
        (
            PARTITION BY fm.finreference
            ORDER BY frh.valuedate DESC, frh.receiptid DESC
        ) AS rn
    FROM plf.financemain fm
    JOIN plf.finreceiptheader frh
        ON fm.finreference = frh.reference
    JOIN PRI_PFT_Receipts ppr
        ON frh.receiptid = ppr.receiptid
    WHERE fm.finisactive IN (0, 1)
)
SELECT
    cu.custsector,
    bmts.sectordesc AS custsector_desc,
    ROUND(cid.income / 100, 2) AS income,
    ROUND(fp.tdclosureamt / 100, 2) AS current_pos,
    (fp.totaltenor - fp.futureinst) AS current_mob,
    ROUND((fm.finassetvalue - fm.fincurrassetvalue) / 100, 2) AS undisbursed_amt,
    ROUND(fm.fincurrassetvalue / 100, 2) AS disbursed_amt,
    fm.promotioncode AS product,
    rmtf.fintypedesc AS product_desc,
    rmtp.description AS promotion_desc,
    cre.cibilscore AS cibil_score,
    fm.numberofterms AS tenure,
    ROUND(fsd.repayamount / 100, 2) AS current_emi,
    fsd.duedate AS due_date,
    cpe.LTVREQUESTEDPOVO AS current_ltv_per,
    ROUND(cpe.unitprice / 100, 2) AS current_ltv_amt,
    ca.collateralref,
    ROUND(fm.finassetvalue / 100, 2) AS sanction_amt,
    ROUND(cpe.fairmrktvalpovo / 100, 2) AS valuation,
    fp.finpurpose AS end_use,
    lp.loanpurposedesc AS end_use_desc,
    cpe.COLLATERALTYPE AS type_of_property,
    ROUND(fp.curreducingrate, 2) AS current_roi,
    lr.valuedate AS last_emi_paid_date
FROM plf.customers cu
LEFT JOIN plf.bmtsectors bmts
    ON cu.custsector = bmts.sectorcode
LEFT JOIN plf.customer_income_details_aview cid
    ON cu.custid = cid.custid
LEFT JOIN plf.financemain fm
    ON fm.custid = cu.custid
LEFT JOIN plf.rmtpromotions rmtp
    ON fm.promotioncode = rmtp.code
LEFT JOIN plf.rmtfinancetypes rmtf
    ON fm.promotioncode = rmtf.fintype
    OR rmtf.fintype = fm.promotioncode || '_' || fm.fintype
LEFT JOIN plf.finpftdetails fp
    ON fm.finreference = fp.finreference
LEFT JOIN plf.loanpurposes lp
    ON fp.finpurpose = lp.loanpurposecode
LEFT JOIN plf.collateralassignment ca
    ON ca.reference = fm.finreference
LEFT JOIN plf.collateral_property_ed cpe
    ON cpe.reference = ca.collateralref
LEFT JOIN plf.customer_retail_ed cre
    ON cu.custcif = cre.reference
LEFT JOIN LatestAdjustedReceipt lr
    ON fm.finreference = lr.finreference
    AND lr.rn = 1
LEFT JOIN plf.receiptallocationdetail rad
    ON lr.receiptid = rad.receiptid
    AND rad.allocationtype IN ('PRI', 'PFT')
LEFT JOIN plf.finscheduledetails fsd
    ON fm.finreference = fsd.finreference
    AND fsd.duedate =
    (
        SELECT MIN(fsd1.duedate)
        FROM plf.finscheduledetails fsd1
        WHERE fsd1.finreference = fm.finreference
          AND fsd1.duedate > CURRENT_DATE
    )
WHERE fm.finreference = @lan
GROUP BY
    cu.custsector,
    cid.income,
    bmts.sectordesc,
    fp.tdclosureamt,
    fp.totaltenor,
    fp.futureinst,
    fsd.repayamount,
    fsd.duedate,
    fm.finassetvalue,
    fm.fincurrassetvalue,
    fm.promotioncode,
    rmtf.fintypedesc,
    rmtp.description,
    ca.reference,
    cpe.reference,
    cre.cibilscore,
    fm.numberofterms,
    cpe.LTVREQUESTEDPOVO,
    cpe.unitprice,
    ca.collateralref,
    cpe.fairmrktvalpovo,
    fp.finpurpose,
    lp.loanpurposedesc,
    cpe.COLLATERALTYPE,
    lr.receiptid,
    fp.curreducingrate,
    lr.valuedate;";

            await using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<ClosureCamLmsValues>(sql, new { lan });
            return rows.ToList();
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
            //var sql = $@"{NeolotexDisbDistinctColumns}
            //    WHERE loan_status <> 'Active Loan'
            //        AND loanclosure_date >= CURRENT_DATE - 1
            //        AND loanclosure_date <  CURRENT_DATE;";

            var sql = $@"
                {NeolotexDisbDistinctColumns}
                WHERE loan_status <> 'Active Loan'
                  AND loanclosure_date >= CURRENT_DATE - 1
                  AND loanclosure_date < CURRENT_DATE
                {DistinctLoanOrderBy}";

            //var sql = $@"
            //    {NeolotexDisbDistinctColumns}
            //    WHERE loan_status <> 'Active Loan'
            //      AND loanclosure_date >= DATE '2026-07-15'
            //      AND loanclosure_date < DATE '2026-07-16'
            //    {DistinctLoanOrderBy}";

            await using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<NeolotexDisb>(sql);
            return result.ToList();
        }
        public async Task<List<NeolotexDisb>> GetLoansByCollIdAsync(string collId)
        {
            //var sql = $@"{NeolotexDisbDistinctColumns}
            //        WHERE sz_collateral_id = @collId;";

            var sql = $@"
                {NeolotexDisbDistinctColumns}
                WHERE sz_collateral_id = @collId
                {DistinctLoanOrderBy}";

            await using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<NeolotexDisb>(sql, new { collId });
            return result.ToList();
        }
        public async Task<List<NeolotexDisb>> GetLoansByCustIdAsync(string custId)
        {
            //var sql = $@"{NeolotexDisbDistinctColumns}
            //        WHERE customerid = @custId;";

            var sql = $@"
                {NeolotexDisbDistinctColumns}
                WHERE customerid = @custId
                {DistinctLoanOrderBy}";

            await using var connection = new NpgsqlConnection(_plfConStr);
            await connection.OpenAsync();
            var result = await connection.QueryAsync<NeolotexDisb>(sql, new { custId });
            return result.ToList();
        }
        public async Task<List<NeolotexDisb>> GetLinkedLoansByLanAsync(string lan)
        {
            try
            {
                //var sql = $@"{NeolotexDisbDistinctColumns}
                //WHERE customerid IN (
                //    SELECT customerid
                //    FROM plf.neolotex_disb
                //    WHERE sz_loan_account_no = @lan
                //);";

                //var sql = $@"
                //    {NeolotexDisbDistinctColumns}
                //    WHERE customerid IN
                //    (
                //        SELECT customerid
                //        FROM plf.neolotex_disb
                //        WHERE sz_loan_account_no = @lan
                //    )
                //    {DistinctLoanOrderBy}";

                var sql = $@"
                    {NeolotexDisbDistinctColumns}
                    WHERE customerid IN
                    (
                        SELECT customerid
                        FROM plf.neolotex_disb
                        WHERE sz_loan_account_no = @lan
                          AND customerid IS NOT NULL
                    )
                    OR sz_collateral_id IN
                    (
                        SELECT sz_collateral_id
                        FROM plf.neolotex_disb
                        WHERE sz_loan_account_no = @lan
                          AND sz_collateral_id IS NOT NULL
                    )
                    {DistinctLoanOrderBy}";

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
            SELECT DISTINCT ON (sz_loan_account_no)
                sz_loan_account_no AS LoanNumber,
                loan_status AS LoanStatus,
                customer_name
            FROM plf.neolotex_disb
            WHERE sz_loan_account_no = @lan
            ORDER BY
                sz_loan_account_no,
                CASE WHEN applicant_type = 'B' THEN 0 ELSE 1 END;";

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

        //public async Task<PlFLoanStatusModel?> GetLoanStatusByLanAsync(string lan)
        //{
        //    try
        //    {
        //        //var sql = @"
        //        //    select distinct sz_loan_account_no as LoanNumber,loan_status as LoanStatus from plf.neolotex_disb where sz_loan_account_no=@lan
        //        //    group by sz_loan_account_no,loan_status";

        //        var sql = @"
        //             select distinct sz_loan_account_no as LoanNumber,
        //        loan_status as LoanStatus, customer_name
        //        from plf.neolotex_disb
        //        where sz_loan_account_no = @lan and applicant_type = 'B'
        //        group by sz_loan_account_no,loan_status,customer_name";



        //        await using var connection = new NpgsqlConnection(_plfConStr);
        //        await connection.OpenAsync();
        //        return await connection.QueryFirstOrDefaultAsync<PlFLoanStatusModel>(sql, new { lan });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching Lan status by lan: {Lan}", lan);
        //        return null;
        //    }
        //} 

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
            AND RECEIPTDATE=@ReceiptDate
            GROUP BY CC.CUSTID";

                await using var connection = new NpgsqlConnection(_plfConStr);
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<PlfCashByCIFModel>(sql, 
                    new { cif, ReceiptDate = DateTime.Today });
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




