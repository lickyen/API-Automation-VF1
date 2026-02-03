using Microsoft.Extensions.Options;
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

        public LoginService(IOptionsSnapshot<AppSettings> appSettings, UserRepository userRepository, IWebHostEnvironment env)
        {
            _settings = appSettings.Value.ADSettings;
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
            {
                using (var dirSearcher = new DirectorySearcher(dirEntry))
                {
                    dirSearcher.Filter = $"(sAMAccountName={userName})";
                    dirSearcher.PropertiesToLoad.AddRange(new[] { "mobile", "userAccountControl", "manager", "displayName", "mail", "title", "department" });

                    dirSearcher.SearchScope = SearchScope.Subtree;

                    var result = dirSearcher.FindOne();
                    if (result != null)
                    {
                        int accountControl = result.Properties["userAccountControl"].Count > 0
                            ? (int)result.Properties["userAccountControl"][0]
                            : 0;

                        bool isActive = IsAccountActive(accountControl);

                        if (isActive)
                        {
                            profile.EmployeeID = userName;
                            profile.Mobile = result.Properties["mobile"].Count > 0 ? result.Properties["mobile"][0].ToString() : "No Mobile Found";
                            profile.Username = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : "No Display Name Found";
                            profile.Email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : "No Email Found";
                            profile.JobTitle = result.Properties["title"].Count > 0 ? result.Properties["title"][0].ToString() : "No Job Title Found";
                            profile.Department = result.Properties["department"].Count > 0 ? result.Properties["department"][0].ToString() : "No Department Found";
                        }
                    }
                }
            }

            return profile;
        }
        private bool IsAccountActive(int accountControl)
        {
            const int ADS_UF_ACCOUNTDISABLE = 0x0002; // Bit flag for disabled accounts
            return (accountControl & ADS_UF_ACCOUNTDISABLE) == 0;
        }
    }
}
