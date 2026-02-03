using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class DCRepository
    {

        private readonly string _conStr;

        public DCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public string AddCase(AddCaseDto AddcaseDto)
        {
            try
            {
                using (var connection = new SqlConnection(_conStr))
               // using (var command = new SqlCommand("sp_Insert_DC_Case_Details", connection))
                using (var command = new SqlCommand("sp_Insert_DC_Case", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    

                    // 3️⃣ Case-level parameters
                    command.Parameters.AddWithValue("@CaseSummary", AddcaseDto.CaseSummary);
                    command.Parameters.AddWithValue("@OffenseType", AddcaseDto.OffenseType);
                    command.Parameters.AddWithValue("@Evidence", AddcaseDto.Evidence);
                    command.Parameters.AddWithValue("@SCNToBeSent", AddcaseDto.SCNToBeSent);
                   
                    command.Parameters.AddWithValue("@CreatedBy", AddcaseDto.CreatedBy);

                    var outputParam = new SqlParameter("@NewCaseID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    connection.Open();
                    command.ExecuteNonQuery();

                    int result = (int)outputParam.Value;

                    return result > 0
                            ? $"Success. CaseID: {result}"
                            : "Error occurred";
                }
            }
            catch (Exception ex)
            {
                return $"DB Error: {ex.Message}";
            }
        }

        public string AddCaseEmployees(AddEmployeesToCaseDto addEmployeesToCaseDto)
        {
            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("sp_Insert_DC_Case_Employees", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // 1️⃣ Build TVP DataTable
                    var employeeTable = new DataTable();
                    employeeTable.Columns.Add("EmpCode", typeof(string));
                    employeeTable.Columns.Add("EmpName", typeof(string));
                    employeeTable.Columns.Add("Role", typeof(string));
                    employeeTable.Columns.Add("BranchID", typeof(string));
                    employeeTable.Columns.Add("BranchName", typeof(string));
                    employeeTable.Columns.Add("Supervisor1EmpID", typeof(string));
                    employeeTable.Columns.Add("Supervisor2EmpID", typeof(string));
                    employeeTable.Columns.Add("Supervisor3EmpID", typeof(string));

                    foreach (var emp in addEmployeesToCaseDto.Employees)
                    {
                        employeeTable.Rows.Add(
                            emp.EmpCode,
                            emp.EmpName,
                            emp.Role,
                            emp.BranchID,
                            emp.BranchName,
                            emp.Supervisor1EmpID,
                            emp.Supervisor2EmpID,
                            emp.Supervisor3EmpID
                        );
                    }

                    // 2️⃣ CaseID parameter
                    command.Parameters.AddWithValue("@CaseID", addEmployeesToCaseDto.CaseId);

                    // 3️⃣ TVP parameter
                    var empParam = command.Parameters.AddWithValue("@Employees", employeeTable);
                    empParam.SqlDbType = SqlDbType.Structured;
                    empParam.TypeName = "dbo.EmployeeBranch_TVP";

                    connection.Open();
                    command.ExecuteNonQuery();

                    return "Employees added successfully";
                }
            }
            catch (Exception ex)
            {
                return $"DB Error: {ex.Message}";
            }
        }


        public CaseDto? GetCase(int caseId)
        {
            CaseDto? caseDtl = null;

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("sp_Get_DC_Case_Details_ByCaseID", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CaseID", caseId);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // 1️⃣ Read Case Header
                        if (reader.Read())
                        {
                            caseDtl = new CaseDto
                            {
                                CaseID = reader.GetInt32(reader.GetOrdinal("CaseID")),
                                CaseSummary = reader["CaseSummary"]?.ToString(),
                                OffenseType = reader["OffenseType"]?.ToString(),
                                Evidence = reader["Evidence"]?.ToString(),
                                SCNToBeSent = reader["SCNToBeSent"]?.ToString(),

                                CaseStatus = reader["CaseStatus"] as string,
                                CreatedBy = reader["CreatedBy"] as string,
                                CreatedDate = reader["CreatedDate"] as DateTime?
                            };
                        }

                        // 2️⃣ Read Employees
                        if (caseDtl != null && reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                caseDtl.Employees.Add(new CaseEmployeeDto
                                {
                                    CaseEmpID = reader.GetInt32(reader.GetOrdinal("CaseEmpID")),
                                    EmpCode = reader["EmpCode"]?.ToString(),
                                    EmpName = reader["EmpName"]?.ToString(),
                                    Role = reader["Role"]?.ToString(),
                                    BranchID = reader["BranchID"]?.ToString(),
                                    BranchName = reader["BranchName"]?.ToString(),
                                       Supervisor1EmpID = reader["Supervisor1EmpID"] as string,
                                    Supervisor2EmpID = reader["Supervisor2EmpID"] as string,
                                    Supervisor3EmpID = reader["Supervisor3EmpID"] as string
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return caseDtl;
        }

        public List<CaseListDto> GetAllCases()
        {
            var cases = new List<CaseListDto>();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("sp_Get_DC_Case_List", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cases.Add(new CaseListDto
                            {
                                CaseID = reader.GetInt32(reader.GetOrdinal("CaseID")),
                                CaseSummary = reader["CaseSummary"]?.ToString(),
                                OffenseType = reader["OffenseType"]?.ToString(),
                                CaseStatus = reader["CaseStatus"]?.ToString(),
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                CreatedDate = reader["CreatedDate"] as DateTime?,
                                EmployeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return cases;
        }


    }
}
