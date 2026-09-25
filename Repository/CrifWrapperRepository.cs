using Dapper;
using Microsoft.Data.SqlClient;

namespace ProductivityApi.Repositories
{
    public class CrifWrapperRepository
    {
        private readonly string _conStr;

        public CrifWrapperRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")
               ?? throw new InvalidOperationException("DBConn connection string is missing.");
        }

        public void LogXmlTransaction(string action, string requestXml, string responseXml, bool isSuccess,
     string errorMessage, string requestHeaders = null, string requestJson = null, string responseJson = null)
        {
            try
            {
                using var con = new SqlConnection(_conStr);
                var sql = @"INSERT INTO CRIFXml_ApiLog
                        (Action, RequestXml, ResponseXml, IsSuccess, ErrorMessage, RequestHeaders, RequestJson, ResponseJson, CreatedDate)
                    VALUES
                        (@Action, @RequestXml, @ResponseXml, @IsSuccess, @ErrorMessage, @RequestHeaders, @RequestJson, @ResponseJson, GETDATE())";
                con.Execute(sql, new
                {
                    Action = action,
                    RequestXml = requestXml,
                    ResponseXml = responseXml,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    RequestHeaders = requestHeaders,
                    RequestJson = requestJson,
                    ResponseJson = responseJson
                });
            }
            catch { /* logging failure must never break the actual response */ }
        }
    }
}