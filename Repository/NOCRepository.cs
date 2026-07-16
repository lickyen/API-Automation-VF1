using System.Data;
using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;

        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            int affected = 0;
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            foreach (var lan in lans)
            {
                var result = await conn.ExecuteScalarAsync<int>(
                    "sp_NOC_InsertClosedLan",
                    new
                    {
                        Lan = lan.Lan,
                        BranchCode = lan.BranchCode,
                        CustId = lan.CustId,
                        CollId = lan.CollId,
                        DisbDate = lan.DisbDate,
                        LoanStatus = lan.LoanStatus,
                        ClosureDate = lan.ClosureDate,
                        NocStatus = lan.NocStatus,
                        IsEligible = lan.IsEligible,
                        Remarks = lan.Remarks
                    },
                    commandType: CommandType.StoredProcedure
                );

                affected += result;
            }

            return affected;
        }

        public async Task<IEnumerable<NocClosedLan>> GetClosedLansAsync(bool? isEligible)
        {
            using var conn = new SqlConnection(_conStr);
            conn.Open();

            return await conn.QueryAsync<NocClosedLan>(
                "sp_NOC_GetClosedLans",
                new
                {
                    IsEligible = isEligible
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();
            await conn.ExecuteAsync(
                "sp_NOC_InsertTransaction",
                dto,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<NocTransaction?> GetById(int id)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_NOC_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );

            var transaction = await multi.ReadFirstOrDefaultAsync<NocTransaction>();

            if (transaction == null)
                return null;

            var logs = (await multi.ReadAsync<NocTransactionLog>()).ToList();

            transaction.Logs = logs;

            return transaction;
        }

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(
            string userId,
            string role,
            string status
        )
        {
            try
            {
                using var conn = new SqlConnection(_conStr);

                var result = await conn.QueryAsync<UserLanDto>(
                    "sp_NOC_GetLansByUser",
                    new
                    {
                        UserId = userId,
                        Role = role,
                        Status = status,
                    },
                    commandType: CommandType.StoredProcedure
                );

                // 🔥 DEBUG SAFETY CHECK (add temporarily)
                if (!result.Any())
                {
                    return (result, null); // DO NOT treat as error
                }

                return (result, null);
            }
            catch (SqlException ex)
            {
                return (null, ex.Message);
            }
        }

        public async Task<(bool IsValid, string? Error)> ValidateRoleExistsAsync(string role)
        {
            using var conn = new SqlConnection(_conStr);

            var sql =
                @"
                SELECT COUNT(1)
                FROM NOC_RoleStatusMap
                WHERE Role = @Role
            ";

            var exists = await conn.ExecuteScalarAsync<int>(sql, new { Role = role });

            if (exists == 0)
                return (false, $"Invalid role: {role}");

            return (true, null);
        }

        public async Task<int> UpdateTransactionStatusAsync(UpdateNocStatusDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@Role", dto.Role);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            return await conn.ExecuteScalarAsync<int>(
                "sp_NOC_UpdateTransactionStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> UpdatePropertyOwnerAsync(UpdatePropertyOwnerDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@PropertyOwnerCustId", dto.PropertyOwnerCustId);
            parameters.Add("@PropertyOwnerName", dto.PropertyOwnerName);
            parameters.Add("@PropertyOwnerMobile", dto.PropertyOwnerMobile);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            var result = await conn.ExecuteScalarAsync<int>(
                "sp_NOC_UpdatePropertyOwner",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }

        public async Task<BranchEmailMaster?> GetBranchEmailsAsync(string branchCode)
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT TOP 1 *
                FROM BranchEmail_Master
                WHERE BranchCode = @BranchCode
                AND IsActive = 1";

            return await conn.QueryFirstOrDefaultAsync<BranchEmailMaster>(
                sql,
                new { BranchCode = branchCode }
            );
        }

        //--------------------------------------------------------
        // Pending before dispatch
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>> GetPendingBeforeDispatchTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status IN
                (
                    'Generate NOC',
                    'File Requested',
                    'File at NOC',
                    'Ready for dispatch'
                )
                AND Closure_Date IS NOT NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Delivered in Branch pending
        //--------------------------------------------------------
        public async Task<
            IEnumerable<NocTransaction>
        > GetDeliveredInBranchPendingTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Delivered in Branch'
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Returned to HO
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>> GetReturnedToHOTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Returned to HO'
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Document not collected
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>> GetDocumentNotCollectedTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status <> 'Document Collected'
                AND Closure_Date IS NOT NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Dispatched but not delivered (Courier delay)
        //--------------------------------------------------------
        public async Task<
            IEnumerable<NocTransaction>
        > GetDispatchedPendingDeliveryTransactionsAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Dispatched'
                AND POD IS NOT NULL
                AND POD <> ''
                AND DeliveryDate IS NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }

        //--------------------------------------------------------
        // Courier delivered but system status not updated
        //--------------------------------------------------------
        public async Task<IEnumerable<NocTransaction>> GetCourierDeliveredButStatusNotUpdatedAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
                SELECT *
                FROM NOC_Transactions
                WHERE Status = 'Dispatched'
                AND DeliveryDate IS NOT NULL
            ";

            return await conn.QueryAsync<NocTransaction>(sql);
        }
        public async Task<IEnumerable<string>> GetAllowedStatusesAsync(string? role)
        {
            using var conn = new SqlConnection(_conStr);

            //const string sql = @"
            //    SELECT Status
            //    FROM NOC_RoleStatusMap
            //    WHERE IsActive = 1
            //      AND (@Role IS NULL OR @Role = '' OR Role = @Role)
            //    ORDER BY Id;";

            const string sql = @"
                SELECT Status
                FROM
                (
                    SELECT Status, MIN(Id) AS Id
                    FROM NOC_RoleStatusMap
                    WHERE IsActive = 1
                      AND (@Role IS NULL OR @Role = '' OR Role = @Role)
                    GROUP BY Status
                ) AS T
                ORDER BY Id;";


            return await conn.QueryAsync<string>(sql, new { Role = role });
        }
        public async Task<IEnumerable<dynamic>> GetOneilReportDataAsync(List<int> ids)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            string query =
                @"
                SELECT
                    Id,
                    Lan,
                    Cust_Name AS CustomerName,
                    Branch_Name AS Branch,
                    Loan_Status AS LoanStatus,
                    Disb_Date AS DisbDate,
                    Closure_Date AS ClosureDate,
                    Status
                FROM NOC_Transactions
                WHERE Id IN @Ids
                ORDER BY Id
            ";

            var result = await conn.QueryAsync(query, new { Ids = ids });
            return result;
        }

        public async Task<List<SignUser>> GetSignUsersAsync()
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        SELECT
            Id,
            EmpId,
            Name
        FROM NOC_SignMaster
        ORDER BY Name";

            var result = await conn.QueryAsync<SignUser>(sql);

            return result.ToList();
        }

        public async Task SaveLeegalityDocumentAsync(
            int transactionId,
            string documentId,
            string signUrl
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        UPDATE NOC_Transactions
        SET
            LeegalityDocumentId=@DocumentId,
            LeegalitySignUrl=@SignUrl,
            LeegalityStatus='PENDING',
            Updated_Dt=GETDATE()
        WHERE Id=@TransactionId";

            await conn.ExecuteAsync(
                sql,
                new
                {
                    TransactionId = transactionId,
                    DocumentId = documentId,
                    SignUrl = signUrl,
                }
            );
        }

        public async Task<LeegalityDocument?> GetLeegalityDocumentAsync(int transactionId)
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        SELECT
            Id AS TransactionId,
            LeegalityDocumentId AS DocumentId,
            LeegalitySignUrl AS SignUrl,
            LeegalityStatus AS Status,
            LeegalityCompletedDt AS CompletedDt
        FROM NOC_Transactions
        WHERE Id=@TransactionId";

            return await conn.QueryFirstOrDefaultAsync<LeegalityDocument>(
                sql,
                new { TransactionId = transactionId }
            );
        }

        public async Task UpdateLeegalityStatusAsync(
            int transactionId,
            string status,
            DateTime? completedDt
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        UPDATE NOC_Transactions
        SET
            LeegalityStatus=@Status,
            LeegalityCompletedDt=@CompletedDt,
            Updated_Dt=GETDATE()
        WHERE Id=@TransactionId";

            await conn.ExecuteAsync(
                sql,
                new
                {
                    TransactionId = transactionId,
                    Status = status,
                    CompletedDt = completedDt,
                }
            );
        }

        //public async Task<bool> UploadNOCAndAckPdfAsync(
        //    int transactionId,
        //    string nocPdfBase64,
        //    string ackPdfBase64,
        //    string updatedBy
        //)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql =
        //        @"
        //UPDATE NOC_Transactions
        //SET
        //    NOC_PDF = @NocPdf,
        //    Ack_PDF = @AckPdf,
        //    Updated_By = @UpdatedBy,
        //    Updated_Dt = GETDATE()
        //WHERE Id = @TransactionId";

        //    return await conn.ExecuteAsync(
        //            sql,
        //            new
        //            {
        //                TransactionId = transactionId,
        //                NocPdf = nocPdfBase64,
        //                AckPdf = ackPdfBase64,
        //                UpdatedBy = updatedBy,
        //            }
        //        ) > 0;
        //}
        public async Task<bool> UploadNOCAndAckPdfAsync(
    int transactionId,
    string nocFileName,
    string ackFileName,
    string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE NOC_Transactions
        SET
            NOC_PDF    = @NocPdf,
            Ack_PDF    = @AckPdf,
            Updated_By = @UpdatedBy,
            Updated_Dt = GETDATE()
        WHERE Id = @TransactionId";

            return await conn.ExecuteAsync(sql, new
            {
                TransactionId = transactionId,
                NocPdf = nocFileName,
                AckPdf = ackFileName,
                UpdatedBy = updatedBy,
            }) > 0;
        }

        //        public async Task<(string Pdf, byte[] SignImage)> GetNocPdfForSigningAsync(
        //            int transactionId,
        //            int signUserId
        //        )
        //        {
        //            using var conn = new SqlConnection(_conStr);

        //            string sql =
        //                @"
        //        SELECT
        //    t.NOC_PDF,
        //    s.SignImage
        //FROM NOC_Transactions t
        //CROSS JOIN NOC_SignMaster s
        //WHERE t.Id = @TransactionId
        //  AND s.EmpId = @SignUserId";

        //            var row = await conn.QueryFirstOrDefaultAsync(
        //                sql,
        //                new { TransactionId = transactionId, SignUserId = signUserId }
        //            );

        //            if (row == null)
        //            {
        //                throw new Exception(
        //                    $"Transaction {transactionId} or SignUser {signUserId} not found"
        //                );
        //            }

        //            if (string.IsNullOrWhiteSpace((string?)row.NOC_PDF))
        //            {
        //                throw new Exception($"NOC_PDF is empty for transaction {transactionId}");
        //            }

        //            if (row.SignImage == null)
        //            {
        //                throw new Exception($"Signature image not found for SignUserId {signUserId}");
        //            }

        //            return ((string)row.NOC_PDF, (byte[])row.SignImage);
        //        }

        public async Task<(string Pdf, string SignImage)> GetNocPdfForSigningAsync(
    int transactionId,
    int signUserId)
        {
            using var conn = new SqlConnection(_conStr);
            const string sql = @"
        SELECT
            t.NOC_PDF,
            s.SignImage
        FROM NOC_Transactions t
        CROSS JOIN NOC_SignMaster s
        WHERE t.Id    = @TransactionId
          AND s.EmpId = @SignUserId";

            var row = await conn.QueryFirstOrDefaultAsync(
                sql,
                new { TransactionId = transactionId, SignUserId = signUserId }
            );

            if (row == null)
                throw new Exception($"Transaction {transactionId} or SignUser {signUserId} not found.");

            if (string.IsNullOrWhiteSpace((string?)row.NOC_PDF))
                throw new Exception($"NOC_PDF is empty for transaction {transactionId}.");

            if (string.IsNullOrWhiteSpace((string?)row.SignImage))
                throw new Exception($"Signature image path not found for SignUserId {signUserId}.");

            return ((string)row.NOC_PDF, (string)row.SignImage);
        }

        //public async Task<bool> SaveSignedNocAsync(
        //    int transactionId,
        //    int signUserId,
        //    string signedPdfBase64,
        //    string updatedBy
        //)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql =
        //        @"
        //UPDATE NOC_Transactions
        //SET
        //    NOC_PDF_Signed = @Pdf,
        //    SignUserId = @SignUserId,
        //    Updated_By = @UpdatedBy,
        //    Updated_Dt = GETDATE()
        //WHERE Id = @TransactionId";

        //    return await conn.ExecuteAsync(
        //            sql,
        //            new
        //            {
        //                TransactionId = transactionId,
        //                SignUserId = signUserId,
        //                Pdf = signedPdfBase64,
        //                UpdatedBy = updatedBy,
        //            }
        //        ) > 0;
        //}

        public async Task<bool> SaveSignedNocAsync(
    int transactionId,
    int signUserId,
    string fileName,
    string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE NOC_Transactions
        SET
            NOC_PDF_Signed = @FileName,
            SignUserId     = @SignUserId,
            Updated_By     = @UpdatedBy,
            Updated_Dt     = GETDATE()
        WHERE Id = @TransactionId";

            return await conn.ExecuteAsync(sql, new
            {
                TransactionId = transactionId,
                SignUserId = signUserId,
                FileName = fileName,
                UpdatedBy = updatedBy,
            }) > 0;
        }

        public async Task<(string Pdf, string PropertyOwnerName)> GetAckPdfForSigningAsync(
            int transactionId
        )
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        SELECT
            Ack_PDF,
            PropertyOwner_Name
        FROM NOC_Transactions
        WHERE Id = @TransactionId";

            var row = await conn.QueryFirstOrDefaultAsync(
                sql,
                new { TransactionId = transactionId }
            );

            if (row == null)
            {
                throw new Exception($"Transaction {transactionId} not found");
            }

            if (string.IsNullOrWhiteSpace((string?)row.Ack_PDF))
            {
                throw new Exception($"Ack_PDF is empty for transaction {transactionId}");
            }

            return ((string)row.Ack_PDF, row.PropertyOwner_Name.ToString());
        }

        //    public async Task<bool> SaveSignedAckAsync(
        //        int transactionId,
        //        string signedPdfBase64,
        //        string updatedBy
        //    )
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql =
        //            @"
        //UPDATE NOC_Transactions
        //SET
        //    Ack_PDF_Signed=@Pdf,
        //    Updated_By=@UpdatedBy,
        //    Updated_Dt=GETDATE()
        //WHERE Id=@TransactionId";

        //        return await conn.ExecuteAsync(
        //                sql,
        //                new
        //                {
        //                    TransactionId = transactionId,
        //                    Pdf = signedPdfBase64,
        //                    UpdatedBy = updatedBy,
        //                }
        //            ) > 0;
        //    }

        public async Task<bool> SaveSignedAckAsync(
    int transactionId,
    string fileName,
    string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE NOC_Transactions
        SET
            Ack_PDF_Signed = @FileName,
            Updated_By     = @UpdatedBy,
            Updated_Dt     = GETDATE()
        WHERE Id = @TransactionId";

            return await conn.ExecuteAsync(sql, new
            {
                TransactionId = transactionId,
                FileName = fileName,
                UpdatedBy = updatedBy,
            }) > 0;
        }

        //public async Task<string?> GetSignedNocPdfAsync(int transactionId)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql =
        //        @"
        //SELECT NOC_PDF_Signed
        //FROM NOC_Transactions
        //WHERE Id = @TransactionId";

        //    return await conn.ExecuteScalarAsync<string?>(
        //        sql,
        //        new { TransactionId = transactionId }
        //    );
        //}

        public async Task<string?> GetSignedNocPdfAsync(int transactionId)
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.ExecuteScalarAsync<string?>(
                "SELECT NOC_PDF_Signed FROM NOC_Transactions WHERE Id = @TransactionId",
                new { TransactionId = transactionId });
        }
        //public async Task<string?> GetSignedAckPdfAsync(int transactionId)
        //{
        //    using var conn = new SqlConnection(_conStr);

        //    string sql =
        //        @"
        //SELECT Ack_PDF_Signed
        //FROM NOC_Transactions
        //WHERE Id = @TransactionId";

        //    return await conn.ExecuteScalarAsync<string?>(
        //        sql,
        //        new { TransactionId = transactionId }
        //    );
        //}

        public async Task<string?> GetSignedAckPdfAsync(int transactionId)
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.ExecuteScalarAsync<string?>(
                "SELECT Ack_PDF_Signed FROM NOC_Transactions WHERE Id = @TransactionId",
                new { TransactionId = transactionId });
        }

        public async Task<bool> UpdateOwnerMobileNumberAsync(UpdateOwnerMobileNumberDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            string sql =
                @"
        UPDATE NOC_Transactions
        SET
            UpdatedOwnerMobileNo = @UpdatedOwnerMobileNo,
            Updated_By = @UpdatedBy,
            Updated_Dt = GETDATE()
        WHERE Id = @Id
    ";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    dto.UpdatedOwnerMobileNo,
                    dto.UpdatedBy,
                }
            );

            return rows > 0;
        }

        public async Task<bool> UpdateCustMobileNumberAsync(UpdateCustMobileNumberDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            string sql =
                @"
        UPDATE NOC_Transactions
        SET
            UpdatedCustMobileNo = @UpdatedCustMobileNo,
            Updated_By = @UpdatedBy,
            Updated_Dt = GETDATE()
        WHERE Id = @Id
    ";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    dto.Id,
                    dto.UpdatedCustMobileNo,
                    dto.UpdatedBy,
                }
            );

            return rows > 0;
        }

        //    public async Task<bool> UpdateKycPdfAsync(
        //        int transactionId,
        //        string pdfBase64,
        //        string updatedBy
        //    )
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql =
        //            @"
        //    UPDATE NOC_Transactions
        //    SET
        //        KYC_PDF = @PdfBase64,
        //        Updated_By = @UpdatedBy,
        //        Updated_Dt = GETDATE()
        //    WHERE Id = @TransactionId
        //";

        //        var rows = await conn.ExecuteAsync(
        //            sql,
        //            new
        //            {
        //                TransactionId = transactionId,
        //                PdfBase64 = pdfBase64,
        //                UpdatedBy = updatedBy,
        //            }
        //        );

        //        return rows > 0;
        //    }

        public async Task<bool> UpdateKycPdfAsync(
    int transactionId,
    string fileName,
    string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE NOC_Transactions
        SET
            KYC_PDF    = @FileName,
            Updated_By = @UpdatedBy,
            Updated_Dt = GETDATE()
        WHERE Id = @TransactionId";

            return await conn.ExecuteAsync(sql, new
            {
                TransactionId = transactionId,
                FileName = fileName,
                UpdatedBy = updatedBy,
            }) > 0;
        }

        //    public async Task<bool> UpdateAckPdfAsync(
        //        int transactionId,
        //        string pdfBase64,
        //        string updatedBy
        //    )
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql =
        //            @"
        //    UPDATE NOC_Transactions
        //    SET
        //        Ack_PDF_Signed = @PdfBase64,
        //        Updated_By = @UpdatedBy,
        //        Updated_Dt = GETDATE()
        //    WHERE Id = @TransactionId
        //";

        //        var rows = await conn.ExecuteAsync(
        //            sql,
        //            new
        //            {
        //                TransactionId = transactionId,
        //                PdfBase64 = pdfBase64,
        //                UpdatedBy = updatedBy,
        //            }
        //        );

        //        return rows > 0;
        //    }

        public async Task<bool> UpdateAckPdfAsync(
    int transactionId,
    string fileName,
    string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = @"
        UPDATE NOC_Transactions
        SET
            Ack_PDF_Signed = @FileName,
            Updated_By     = @UpdatedBy,
            Updated_Dt     = GETDATE()
        WHERE Id = @TransactionId";

            return await conn.ExecuteAsync(sql, new
            {
                TransactionId = transactionId,
                FileName = fileName,
                UpdatedBy = updatedBy,
            }) > 0;
        }

        //    public async Task<string?> GetKycPdfAsync(int transactionId)
        //    {
        //        using var conn = new SqlConnection(_conStr);

        //        string sql =
        //            @"
        //    SELECT KYC_PDF
        //    FROM NOC_Transactions
        //    WHERE Id = @TransactionId
        //";

        //        return await conn.ExecuteScalarAsync<string?>(
        //            sql,
        //            new { TransactionId = transactionId }
        //        );
        //    }

        public async Task<string?> GetKycPdfAsync(int transactionId)
        {
            using var conn = new SqlConnection(_conStr);
            return await conn.ExecuteScalarAsync<string?>(
                "SELECT KYC_PDF FROM NOC_Transactions WHERE Id = @TransactionId",
                new { TransactionId = transactionId });
        }

        public async Task<bool> UpdatePodAsync(int transactionId, string pod, string? courierVendor, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);

            string sql =
                @"
        UPDATE NOC_Transactions
        SET
            POD = @POD,
            CourierVendor = @CourierVendor,
            Updated_By = @UpdatedBy,
            Updated_Dt = GETDATE()
        WHERE Id = @TransactionId
    ";

            var rows = await conn.ExecuteAsync(
                sql,
                new
                {
                    TransactionId = transactionId,
                    POD = pod,
                    CourierVendor = courierVendor,
                    UpdatedBy = updatedBy,
                }
            );

            return rows > 0;
        }

        public async Task UpdateOtpStatus(int transactionId, string otpType, string status)
        {
            using var conn = new SqlConnection(_conStr);

            string sql = otpType.ToUpper() switch
            {
                "KYC" => @"
            UPDATE NOC_Transactions
            SET OTP_Status_KYC = @Status,
                OTP_Status_KYC_Date =
                    CASE
                        WHEN @Status = 'Verified'
                        THEN GETDATE()
                        ELSE OTP_Status_KYC_Date
                    END
            WHERE Id = @TransactionId",

                "NOC" => @"
            UPDATE NOC_Transactions
            SET OTP_Status_NOC = @Status,
                OTP_Status_NOC_Date =
                    CASE
                        WHEN @Status = 'Verified'
                        THEN GETDATE()
                        ELSE OTP_Status_NOC_Date
                    END
            WHERE Id = @TransactionId",

                _ => throw new Exception("Invalid OTP type"),
            };

            await conn.ExecuteAsync(sql, new { TransactionId = transactionId, Status = status });
        }
    }
}
