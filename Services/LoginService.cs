using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;
using System.DirectoryServices;


namespace ProductivityApi.Services
{
    public class LoginService
    {
        private readonly ADSettings _settings;
        private readonly UserRepository _userRepository;
        private readonly IWebHostEnvironment _env;

        public LoginService(AppSettings appSettings, UserRepository userRepository, IWebHostEnvironment env)
        {
            _settings = appSettings.ADSettings;
            _userRepository = userRepository;
            _env = env;
        }

        public bool ValidateLogin(LoginRequest login)
        {
            bool success;

            if (_env.IsProduction())
            {
                success = ValidateLdapLogin(login);
            }
            else
            {
                success = ValidateDbLogin(login);
            }

            Log.Information(
                "Login attempt UserId={UserId} Success={Success}",
                login.UserId,
                _env.EnvironmentName,
                success
            );

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

        public ADProfile GetUserProfile(string userName)
        {
            ADProfile profile = new ADProfile();

            using var dirEntry = new DirectoryEntry(_settings.LDAPPath, _settings.ServiceAccountUser, _settings.ServiceAccountPassword);
            using var dirSearcher = new DirectorySearcher(dirEntry)
            {
                Filter = $"(sAMAccountName={userName})",
                SearchScope = SearchScope.Subtree
            };

            dirSearcher.PropertiesToLoad.AddRange(new[]
            {
                 "userAccountControl", "displayName", "mail", "mobile", "title", "department", "l", "st", "manager"
            });

            var result = dirSearcher.FindOne();
            if (result != null)
            {
                int accountControl = result.Properties["userAccountControl"].Count > 0
                    ? (int)result.Properties["userAccountControl"][0]
                    : 0;

                if (IsAccountActive(accountControl))
                {
                    profile.EmployeeID = result.Properties["employeeID"].Count > 0
                        ? result.Properties["employeeID"][0].ToString()
                        : result.Properties["sAMAccountName"].Count > 0
                            ? result.Properties["sAMAccountName"][0].ToString()
                            : userName;

                    profile.Username = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : "";
                    profile.Email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : "";
                    profile.Mobile = result.Properties["mobile"].Count > 0 ? result.Properties["mobile"][0].ToString() : "";
                    profile.JobTitle = result.Properties["title"].Count > 0 ? result.Properties["title"][0].ToString() : "";
                    profile.Department = result.Properties["department"].Count > 0 ? result.Properties["department"][0].ToString() : "";
                    profile.City = result.Properties["l"].Count > 0 ? result.Properties["l"][0].ToString() : "";
                    profile.State = result.Properties["st"].Count > 0 ? result.Properties["st"][0].ToString() : "";

                    // Get flattened manager chain (L1/L2/L3)
                    var managers = GetManagerChain(result);
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

        public bool ResetPassword(string userId, string newPassword)
        {
            try
            {
                using (var entry = new DirectoryEntry(
                    _settings.LDAPPath,
                    _settings.ServiceAccountUser,
                    _settings.ServiceAccountPassword))
                {
                    using (var searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = $"(sAMAccountName={EscapeLdapSearchFilter(userId)})";
                        searcher.SearchScope = SearchScope.Subtree;

                        var result = searcher.FindOne();
                        if (result == null)
                            return false; // User not found

                        using (var userEntry = result.GetDirectoryEntry())
                        {
                            // Reset password
                            userEntry.Invoke("SetPassword", new object[] { newPassword });

                            // Unlock account (if locked)
                            userEntry.Properties["lockoutTime"].Value = 0;

                            userEntry.CommitChanges();

                            return true;
                        }
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // Password policy violation or permission issue
                return false;
            }
            catch
            {
                return false;
            }
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

    }

}   
