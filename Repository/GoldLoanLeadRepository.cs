using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class GoldLoanLeadRepository
    {
        private readonly string _conStr;

        public GoldLoanLeadRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        // =========================================
        // GET ALL GOLD LOAN LEADS
        // =========================================
        public List<GoldLoanLeadModel> GetAllGoldLoanLeads()
        {
            using var connection = new SqlConnection(_conStr);

            var leads = connection.Query<
                GoldLoanLeadModel,
                GoldLoanVendorModel,
                GoldLoanLeadModel>(
                "sp_getGoldLoanLeadsList",
                (goldLoanLead, vendor) =>
                {
                    goldLoanLead.VendorObject = vendor;
                    return goldLoanLead;
                },
                splitOn: "SplitVendorID",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return leads;
        }

        // =========================================
        // GET SINGLE GOLD LOAN LEAD
        // =========================================
        public GoldLoanLeadModel? GetGoldLoanLead(int id)
        {
            using var connection = new SqlConnection(_conStr);

            var lead = connection.Query<
                GoldLoanLeadModel,
                GoldLoanVendorModel,
                GoldLoanLeadModel>(
                "sp_getGoldLoanLead",
                (goldLoanLead, vendor) =>
                {
                    goldLoanLead.VendorObject = vendor;
                    return goldLoanLead;
                },
                new { Id = id },
                splitOn: "SplitVendorID",
                commandType: CommandType.StoredProcedure
            ).FirstOrDefault();

            return lead;
        }

        // =========================================
        // ADD GOLD LOAN LEAD
        // =========================================
        public int AddGoldLoanLead(AddGoldLoanLeadDTO addGoldLoanLeadDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@VendorID", addGoldLoanLeadDTO.VendorId);
            parameters.Add("@Name", addGoldLoanLeadDTO.Name);
            parameters.Add("@MobileNumber", addGoldLoanLeadDTO.MobileNumber);
            parameters.Add("@Caratage", addGoldLoanLeadDTO.Caratage);
            parameters.Add("@ApproximateWeight", addGoldLoanLeadDTO.ApproximateWeight);
            parameters.Add("@GoldLoanRequiredFor", addGoldLoanLeadDTO.GoldLoanRequiredFor);
            parameters.Add("@CreatedBy", addGoldLoanLeadDTO.CreatedBy);

            parameters.Add(
                "@Output",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output
            );

            connection.Execute(
                "sp_insertGoldLoanLead",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        // =========================================
        // UPDATE GOLD LOAN LEAD
        // =========================================
        public bool UpdateGoldLoanLead(
            int id,
            UpdateGoldLoanLeadDTO dto)
        {
            using var connection = new SqlConnection(_conStr);

            string query = @"
                UPDATE GoldLoanLeadMaster
                SET
                    CallingStatus = @CallingStatus,
                    Status = @Status,
                    Remarks = @Remarks,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE()
                WHERE Id = @Id";

            int rows = connection.Execute(query, new
            {
                Id = id,
                CallingStatus = dto.CallingStatus,
                Status = dto.Status,
                Remarks = dto.Remarks,
                UpdatedBy = dto.UpdatedBy
            });

            return rows > 0;
        }
    }
}