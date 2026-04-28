using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;
using System.Text;


namespace ProductivityApi.Services
{
    public class PLFService
    {
        private readonly string _conStr;
        private readonly HttpClient _httpClient;
        private readonly PlfApiSettings _plfconfig;

        public PLFService(IConfiguration configuration, HttpClient httpClient, AppSettings appSettings)
        {
            _conStr = configuration.GetConnectionString("DBConn");
            _httpClient = httpClient;
            _plfconfig = appSettings.PlfApiSettings;
        }

        public Task<GetFinanceModel> GetFinance(string lan)
        {
            var endPoint = $"pff-api/services/createFinanceRest/finance/getFinance/{lan}";
            return SendRequest<GetFinanceModel>(HttpMethod.Get, endPoint, null, "getFinance", "3");
        }

        public Task<CustomerDetailsModel> GetCustomerDetails(string cif)
        {
            var endPoint = $"pff-api/services/customerRest/customerService/getCustomerDetails/{cif}";
            return SendRequest<CustomerDetailsModel>(HttpMethod.Get, endPoint, null, "getCustomerDetails", "3");
        }

        public Task<CreateAdviceResponse> CreateAdvice(CreateAdviceRequest request)
        {
            var endPoint = "pff-api/services/feePostingRest/feePostingService/createAdvise";
            return SendRequest<CreateAdviceResponse>(HttpMethod.Post, endPoint, request, "createAdvise", "3");
        }

        public Task<PaymentResponse> ManualPayment(PaymentRequest request)
        {
            var endPoint = "pff-api/services/finInstructionRest/loanInstructionService/manualPayment";

            return SendRequest<PaymentResponse>(HttpMethod.Post,endPoint,request, serviceName: "manualPayment", "45");
        }

        //public Task<StatementResponse> GetStatement(StatementRequest request)
        //{
        //    var endPoint = "pff-api/services/statementRest/statementService/getStatement";

        //    return SendRequest<StatementResponse>(HttpMethod.Get,endPoint,request, serviceName: "getStatement", "3");
        //}

        public Task<GenerateStatementResponse> GenerateStatement(GenerateStatementRequest request)
        {
            var endPoint = "pff-api/services/loanLetterRest/loanLetter/generateStatement";

            return SendRequest<GenerateStatementResponse>(HttpMethod.Post, endPoint, request, serviceName: "generateStatement", "3");
        }

        public Task<TransferLetterResponse> TransferLetter(TransferLetterRequest request)
        {
            var endPoint = "pff-api/services/loanLetterRest/loanLetter/transferLetter";

            return SendRequest<TransferLetterResponse>(HttpMethod.Post, endPoint, request, serviceName: "transferLetter", "3");
        }

        public Task<ForeclosureReportModel> GetForeclosureReport(ForeclosureReportRequest request)
        {
            var endPoint = "pff-api/services/statementRest/statementService/getForeclosureStmtV1";

            return SendRequest<ForeclosureReportModel>(HttpMethod.Post, endPoint, request, serviceName: "getForeclosureStmtV1", "3");
        }

        public Task<ReturnStatus> UpdateCustomerEmail(CustomerEmailRequest request)
        {
            var endPoint = "pff-api/services/customerRest/customerService/updateCustomerEmail";

            return SendRequest<ReturnStatus>(HttpMethod.Post, endPoint, request, serviceName: "updateCustomerEmail", "3");
        }

        public Task<ReturnStatus> AddCustomerEmail(CustomerEmailRequest request)
        {
            var endPoint = "pff-api/services/customerRest/customerService/addCustomerEmail";

            return SendRequest<ReturnStatus>(HttpMethod.Post, endPoint, request, serviceName: "addCustomerEmail", "3");
        }

        public Task<ReturnStatus> CancelLoan(CancelLoanRequest request)
        {
            var endPoint = "pff-api/services/createFinanceRest/finance/cancelLoan";

            return SendRequest<ReturnStatus>(HttpMethod.Post,endPoint,request,serviceName: "cancelLoan",serviceVersion: "3");
        }

        private async Task<TResponse> SendRequest<TResponse>(HttpMethod method, string endPoint, object requestBodyObj = null, string serviceName = "", string serviceVersion="3")
        {
            var apiUrl = $"{_plfconfig.BaseUrl}{endPoint}";
            var requestTimeStr = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var msgID = Guid.NewGuid().ToString("N");

            var log = new ApiLogModel
            {
                HttpMethod = method.Method,
                ServiceName = serviceName,
                BaseURL = _plfconfig.BaseUrl,
                Endpoint = endPoint
            };

            try
            {
                using var request = new HttpRequestMessage(method, apiUrl);

                // Load static headers from config
                foreach (var header in _plfconfig.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                // Add dynamic headers
                request.Headers.Add("RequestTime", requestTimeStr);
                request.Headers.Add("MESSAGEID", msgID);
                request.Headers.Add("ServiceName", serviceName);
                request.Headers.Add("ServiceVersion", serviceVersion);

                // Serialize request body if provided
                string requestBody = null;
                if (requestBodyObj != null)
                {
                    var settings = new JsonSerializerSettings
                    {
                        DateFormatString = "yyyy-MM-dd'T'HH:mm:ss",
                        DateTimeZoneHandling = DateTimeZoneHandling.Unspecified
                    };

                    requestBody = JsonConvert.SerializeObject(requestBodyObj, settings);
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                }

                log.RequestHeaders = JsonConvert.SerializeObject(
                    request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value))
                );
                log.RequestBody = requestBody;

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                var responseBody = await response.Content.ReadAsStringAsync();

                log.ResponseCode = (int)response.StatusCode;
                log.ResponseBody = responseBody;

                if (!response.IsSuccessStatusCode)
                {
                    log.ErrorMessage = $"Error code: {response.StatusCode}";
                    throw new HttpRequestException( $"API failed: {response.StatusCode}, Body: {responseBody}");
                }

                return JsonConvert.DeserializeObject<TResponse>(responseBody);

            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.ToString();
                throw;
            }
            finally
            {
                try
                {
                    await LogApiCall(log);
                }
                catch (Exception logEx)
                {
                    Console.WriteLine("Logging failed: " + logEx);
                }
            }
        }

        public async Task LogApiCall(ApiLogModel log)
        {
            await using var conn = new SqlConnection(_conStr);
            await using var cmd = new SqlCommand("SP_Insert_API_Logs", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@HttpMethod", log.HttpMethod ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ServiceName", log.ServiceName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@BaseURL", log.BaseURL ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Endpoint", log.Endpoint ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RequestHeaders", log.RequestHeaders ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@RequestBody", log.RequestBody ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ResponseCode", log.ResponseCode ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ResponseBody", log.ResponseBody ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ErrorMessage", log.ErrorMessage ?? (object)DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }

}
