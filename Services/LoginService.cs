using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;
using System.DirectoryServices;


namespace ProductivityApi.Services
{
    public class LoginService
    {
        private readonly ADSettings _settings;
        private readonly AuthSettings _authSettings;
        private readonly JwtSettings _jwtSettings;
        private readonly JwtHelper _jwtHelper;
        private readonly UserRepository _userRepository;
        private readonly UserLogRepository _userLogRepository;
        private readonly OtpRepository _otpRepository;
        private readonly IWebHostEnvironment _env;

        public LoginService(AppSettings appSettings,  UserRepository userRepository, UserLogRepository userLogRepository, OtpRepository otpRepository, JwtHelper jwtHelper, IWebHostEnvironment env)
        {
            _settings = appSettings.ADSettings;
            _authSettings = appSettings.AuthSettings;
            _jwtSettings = appSettings.JwtSettings;
            _jwtHelper = jwtHelper;
            _userRepository = userRepository;
            _userLogRepository = userLogRepository;
            _otpRepository = otpRepository;
            _env = env;
        }

        public bool IsTwoFactorAuthEnabled => _authSettings.EnableTwoFactorAuth;

        public async Task<bool> ValidateLoginAsync(LoginRequest login)
        {
            bool success = false;
            string? message = null;

            try
            {
                success = _env.IsProduction() ? ValidateLdapLogin(login) : ValidateDbLogin(login);

                message = success ? null : "Invalid credentials";
            }
            catch (Exception ex)
            {
                message = $"Exception: {ex.GetType().Name} — {ex.Message}";
                Log.Error(ex, "Login validation error for UserId={UserId} Source={Source}", login.UserId, login.Source);
                throw;
            }
            finally
            {
                await _userLogRepository.AddAsync(new UserLog
                {
                    UserId = login.UserId,
                    Source = login.Source,
                    Action = "LOGIN",
                    Response = success ? "SUCCESS" : "FAILURE",
                    Message = message
                });

                Log.Information(
                    "Login attempt UserId={UserId} Source={Source} Env={Env} Success={Success}",
                    login.UserId,
                    login.Source,
                    _env.EnvironmentName,
                    success
                );
            }

            return success;
        }
        public bool ValidateLdapLogin(LoginRequest login)

        {
            using (var entry = new DirectoryEntry(_settings.LDAPPath, _settings.ServiceAccountUser, _settings.ServiceAccountPassword))
            {
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(sAMAccountName={login.UserId})";
                    searcher.SearchScope = SearchScope.Subtree;

                    var user = searcher.FindOne();
                    if (user == null)
                        return false; // User not found

                    // Attempt user login
                    string userDn = user.Path; // Full LDAP path of user
                    try 
                    {
                        using (var userEntry = new DirectoryEntry(userDn, login.UserId, login.Password))
                        {
                            // Force bind to check credentials
                            var obj = userEntry.NativeObject;
                            return true;
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        // wrong password
                        return false;
                    }
                }
            }
        }

        private bool ValidateDbLogin(LoginRequest login)
        {
            var user = _userRepository.GetUser(login.UserId);
            if (user == null)
                return false;

            // Simple password check (plain-text for example, should hash in real apps)
            return user.Password == login.Password;
        }

        public ADProfile GetUserProfile(string userName, string? loggedInUserId = null)
        {
            //if (_env.IsProduction())
            //{
            //    if (string.IsNullOrWhiteSpace(loggedInUserId))
            //    {
            //        throw new UnauthorizedAccessException("User identity not found.");
            //    }

            //    if (!string.Equals(
            //            userName,
            //            loggedInUserId,
            //            StringComparison.OrdinalIgnoreCase))
            //    {
            //        throw new UnauthorizedAccessException("Access denied.");
            //    }
            //}

            if (string.IsNullOrWhiteSpace(loggedInUserId))
            {
                throw new UnauthorizedAccessException("User identity not found.");
            }

            if (!string.Equals(
                    userName,
                    loggedInUserId,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access denied.");
            }

            ADProfile profile = new();

            using var dirEntry = new DirectoryEntry(
                _settings.LDAPPath,
                _settings.ServiceAccountUser,
                _settings.ServiceAccountPassword);

            using var dirSearcher = new DirectorySearcher(dirEntry)
            {
                Filter = $"(sAMAccountName={userName})",
                SearchScope = SearchScope.Subtree
            };

            dirSearcher.PropertiesToLoad.AddRange(new[]
            {
                "employeeID",
                "sAMAccountName",
                "userAccountControl",
                "displayName",
                "mail",
                "mobile",
                "title",
                "department",
                "l",
                "st",
                "manager"
            });

            var result = dirSearcher.FindOne();

            if (result != null)
            {
                int accountControl =
                    result.Properties["userAccountControl"].Count > 0
                        ? (int)result.Properties["userAccountControl"][0]
                        : 0;

                if (IsAccountActive(accountControl))
                {
                    profile.EmployeeID =
                        result.Properties["employeeID"].Count > 0
                            ? result.Properties["employeeID"][0].ToString()
                            : result.Properties["sAMAccountName"].Count > 0
                                ? result.Properties["sAMAccountName"][0].ToString()
                                : userName;

                    profile.Username =
                        result.Properties["displayName"].Count > 0
                            ? result.Properties["displayName"][0].ToString()
                            : string.Empty;

                    profile.Email =
                        result.Properties["mail"].Count > 0
                            ? result.Properties["mail"][0].ToString()
                            : string.Empty;

                    profile.Mobile =
                        result.Properties["mobile"].Count > 0
                            ? result.Properties["mobile"][0].ToString()
                            : string.Empty;

                    profile.JobTitle =
                        result.Properties["title"].Count > 0
                            ? result.Properties["title"][0].ToString()
                            : string.Empty;

                    profile.Department =
                        result.Properties["department"].Count > 0
                            ? result.Properties["department"][0].ToString()
                            : string.Empty;

                    profile.City =
                        result.Properties["l"].Count > 0
                            ? result.Properties["l"][0].ToString()
                            : string.Empty;

                    profile.State =
                        result.Properties["st"].Count > 0
                            ? result.Properties["st"][0].ToString()
                            : string.Empty;

                    var managers = new List<ManagerInfo>();

                    if (managers.Count > 0) profile.L1Manager = managers[0];
                    if (managers.Count > 1) profile.L2Manager = managers[1];
                    if (managers.Count > 2) profile.L3Manager = managers[2];
                }
            }

            return profile;
        }
        private List<ManagerInfo> GetManagerChain(SearchResult userResult)
        {
            List<ManagerInfo> managers = new List<ManagerInfo>();
            string managerDN = userResult.Properties["manager"].Count > 0 ? userResult.Properties["manager"][0].ToString() : null;

            using var rootEntry = new DirectoryEntry(_settings.LDAPPath, _settings.ServiceAccountUser, _settings.ServiceAccountPassword);

            while (!string.IsNullOrEmpty(managerDN))
            {
                using var searcher = new DirectorySearcher(rootEntry)
                {
                    Filter = $"(distinguishedName={EscapeLdapSearchFilter(managerDN)})",
                    SearchScope = SearchScope.Subtree
                };
                searcher.PropertiesToLoad.AddRange(new[] { "displayName", "mail", "title", "manager", "sAMAccountName", "employeeID" });

                var result = searcher.FindOne();
                if (result == null) break;

                ManagerInfo manager = new ManagerInfo
                {
                    Name = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : "",
                    Email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : "",
                    JobTitle = result.Properties["title"].Count > 0 ? result.Properties["title"][0].ToString() : "",
                    EmployeeID = result.Properties["employeeID"].Count > 0 ? result.Properties["employeeID"][0].ToString() :
                                 result.Properties["sAMAccountName"].Count > 0 ? result.Properties["sAMAccountName"][0].ToString() : ""
                };

                managers.Add(manager);

                managerDN = result.Properties["manager"].Count > 0 ? result.Properties["manager"][0].ToString() : null;
            }

            return managers;
        }

        //public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        //{
        //    bool success = false;
        //    string? message = null;

        //    try
        //    {
        //        using var entry = new DirectoryEntry(
        //            _settings.LDAPPath,
        //            _settings.ServiceAccountUser,
        //            _settings.ServiceAccountPassword);

        //        using var searcher = new DirectorySearcher(entry);
        //        searcher.Filter = $"(sAMAccountName={EscapeLdapSearchFilter(userId)})";
        //        searcher.SearchScope = SearchScope.Subtree;

        //        var result = searcher.FindOne();
        //        if (result == null)
        //        {
        //            message = "User not found in directory";
        //            return false;
        //        }

        //        using var userEntry = result.GetDirectoryEntry();
        //        userEntry.Invoke("SetPassword", new object[] { newPassword });
        //        userEntry.Properties["lockoutTime"].Value = 0;
        //        userEntry.CommitChanges();

        //        success = true;
        //    }
        //    catch (COMException ex)
        //    {
        //        message = $"COMException: {ex.GetType().Name} — {ex.Message} (HRESULT: {ex.ErrorCode:X})";
        //        Log.Error(ex, "LDAP password reset COMException for UserId={UserId}", userId);
        //    }
        //    catch (Exception ex)
        //    {
        //        message = $"Exception: {ex.GetType().Name} — {ex.Message}";
        //        Log.Error(ex, "Password reset error for UserId={UserId}", userId);
        //    }
        //    finally
        //    {
        //        await _userLogRepository.AddAsync(new UserLog
        //        {
        //            UserId = userId,
        //            Action = "PASSWORD_RESET",
        //            Response = success ? "SUCCESS" : "FAILURE",
        //            Message = message
        //        });

        //        Log.Information(
        //            "Password reset UserId={UserId} Success={Success}",
        //            userId,
        //            success
        //        );
        //    }

        //    return success;
        //}

        //public async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        //{
        //    bool success = false;
        //    string? message = null;

        //    try
        //    {
        //        success = _env.IsProduction()
        //            ? await ResetAdPasswordAsync(userId, newPassword)
        //            : ResetDbPassword(userId, newPassword);

        //        message = success ? null : "Password reset failed";
        //    }
        //    catch (Exception ex)
        //    {
        //        message = $"Exception: {ex.GetType().Name} — {ex.Message}";
        //        Log.Error(ex, "Password reset error for UserId={UserId}", userId);
        //        throw;
        //    }
        //    finally
        //    {
        //        await _userLogRepository.AddAsync(new UserLog
        //        {
        //            UserId = userId,
        //            Source = "ProductivityApi",
        //            Action = "PASSWORD_RESET",
        //            Response = success ? "SUCCESS" : "FAILURE",
        //            Message = message
        //        });

        //        Log.Information(
        //            "Password reset UserId={UserId} Env={Env} Success={Success}",
        //            userId,
        //            _env.EnvironmentName,
        //            success);
        //    }

        //    return success;
        //}

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            bool success = false;
            string? message = null;

            try
            {
                var tokenRecord = await _otpRepository.GetResetTokenAsync(
                    request.ResetToken,
                    request.Source);

                if (tokenRecord == null)
                {
                    throw new InvalidOperationException(
                        "Invalid or expired reset token");
                }

                if (!string.Equals(
                        tokenRecord.UserId,
                        request.UserId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Invalid reset token");
                }

                success = _env.IsProduction()
                    ? await ResetAdPasswordAsync(request.UserId, request.NewPassword)
                    : ResetDbPassword(request.UserId, request.NewPassword);

                if (success)
                {
                    await _otpRepository.DeleteOtpAsync(
                        request.UserId,
                        request.Source);
                }

                message = success ? null : "Password reset failed";
            }
            catch (Exception ex)
            {
                message = ex.Message;

                Log.Error(
                    ex,
                    "Password reset error for UserId={UserId}",
                    request.UserId);

                throw;
            }
            finally
            {
                await _userLogRepository.AddAsync(
                    new UserLog
                    {
                        UserId = request.UserId,
                        Source = request.Source,
                        Action = "PASSWORD_RESET",
                        Response = success ? "SUCCESS" : "FAILURE",
                        Message = message
                    });

                Log.Information(
                    "Password reset UserId={UserId} Success={Success}",
                    request.UserId,
                    success);
            }

            return success;
        }

        private Task<bool> ResetAdPasswordAsync(string userId, string newPassword)
        {
            using var entry = new DirectoryEntry(
                _settings.LDAPPath,
                _settings.ServiceAccountUser,
                _settings.ServiceAccountPassword);

            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(sAMAccountName={EscapeLdapSearchFilter(userId)})",
                SearchScope = SearchScope.Subtree
            };

            var result = searcher.FindOne();
            if (result == null)
                return Task.FromResult(false);

            using var userEntry = result.GetDirectoryEntry();

            userEntry.Invoke("SetPassword", new object[] { newPassword });
            userEntry.Properties["lockoutTime"].Value = 0;
            userEntry.CommitChanges();

            return Task.FromResult(true);
        }

        private bool ResetDbPassword(string userId, string newPassword)
        {
            return _userRepository.UpdatePassword(userId, newPassword) == 1;
        }

        private string EscapeLdapSearchFilter(string dn)
        {
            return dn.Replace("\\", "\\5c")
                     .Replace("*", "\\2a")
                     .Replace("(", "\\28")
                     .Replace(")", "\\29")
                     .Replace("\0", "\\00");
        }

        private bool IsAccountActive(int accountControl)
        {
            const int ADS_UF_ACCOUNTDISABLE = 0x0002; // Bit flag for disabled accounts
            return (accountControl & ADS_UF_ACCOUNTDISABLE) == 0;
        }

        public string GenerateJwtToken(string userId)
        {
            return _jwtHelper.GenerateToken(userId);
        }

    }

}   
