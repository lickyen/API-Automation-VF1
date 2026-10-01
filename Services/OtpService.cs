using ProductivityApi.Models;
using ProductivityApi.Repository;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace ProductivityApi.Services
{
    public class OtpService
    {
        private readonly UserRepository _userRepository;
        private readonly OtpRepository _otpRepository;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserLogRepository _userLogRepository;

        public OtpService(
            UserRepository userRepository,
            OtpRepository otpRepository,
            SmsService smsService,
            TemplateService templateService,
            UserLogRepository userLogRepository)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _smsService = smsService;
            _templateService = templateService;
            _userLogRepository = userLogRepository;
        }

        public async Task<string> SendOtp(
            string userId,
            string? source = null,
            string? smsContent = null,
            string? dltContentId = null)
        {

            var user = _userRepository.GetUser(userId)
                ?? throw new InvalidOperationException("User not found");

            if (string.IsNullOrWhiteSpace(user.Mobile))
                throw new InvalidOperationException("Contact details not found");

            await SendOtpInternal(
                userId,
                user.Mobile,
                source,
                smsContent,
                dltContentId);

            return user.Mobile;
        }

        public async Task<bool> SendOtpToMobile(
            string mobileNumber,
            string? source = null,
            string? smsContent = null,
            string? dltContentId = null)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new InvalidOperationException("Mobile number not found");

            await SendOtpInternal(
                mobileNumber,
                mobileNumber,
                source,
                smsContent,
                dltContentId);

            return true;
        }

        public async Task<Response> VerifyOtp(
            string userId,
            string otp,
            string? source = null)
        {
            bool isValid = false;
            string? message = null;

            try
            {
                var storedOtp = await _otpRepository.GetOtpByUserIdAsync(
                    userId,
                    source);

                if (storedOtp == null)
                {
                    return new Response(
                        false,
                        new OtpVerificationResult
                        {
                            MaxAttemptsExceeded = true
                        },
                        "OTP not found. Please request a new OTP.");
                }

                if (storedOtp.ExpiryTime < DateTime.UtcNow)
                {
                    await _otpRepository.DeleteOtpAsync(
                        userId,
                        source);

                    return new Response(
                        false,
                        new OtpVerificationResult
                        {
                            MaxAttemptsExceeded = true
                        },
                        "OTP expired. Please request a new OTP.");
                }

                isValid = VerifyHash(
                    otp,
                    storedOtp.OtpHash);

                if (isValid)
                {
                    var resetToken = GenerateResetToken();

                    await _otpRepository.SaveResetTokenAsync(
                        userId,
                        resetToken,
                        source);

                    return new Response(
                        true,
                        new OtpVerificationResult
                        {
                            ResetToken = resetToken,
                            MaxAttemptsExceeded = false
                        },
                        "OTP verified successfully");
                }

                storedOtp.Attempts++;
                //storedOtp.Source = source;

                await _otpRepository.UpdateAttemptAsync(storedOtp);

                if (storedOtp.Attempts >= 5)
                {
                    await _otpRepository.DeleteOtpAsync(
                        userId,
                        source);

                    return new Response(
                        false,
                        new OtpVerificationResult
                        {
                            MaxAttemptsExceeded = true
                        },
                        "Maximum OTP attempts exceeded.");
                }

                var attemptsLeft = 5 - storedOtp.Attempts;

                return new Response(
                    false,
                    new OtpVerificationResult
                    {
                        MaxAttemptsExceeded = false
                    },
                    $"Invalid OTP. {attemptsLeft} attempt(s) remaining.");
            }
            catch (Exception ex)
            {
                message = ex.Message;

                Log.Error(
                    ex,
                    "OTP verification error for UserId={UserId} Source={Source}",
                    userId,
                    source);

                throw;
            }
            finally
            {
                await _userLogRepository.AddAsync(
                    new UserLog
                    {
                        UserId = userId,
                        Source = source,
                        Action = "OTP_VERIFICATION",
                        Response = isValid ? "Success" : "Failure",
                        Message = message
                    });

                Log.Information(
                    "OTP verification UserId={UserId} Source={Source} Success={Success}",
                    userId,
                    source,
                    isValid);
            }
        }

        #region Helpers

        private async Task SendOtpInternal(
            string userId,
            string mobileNumber,
            string? source,
            string? smsContent,
            string? dltContentId)
                {
                    string otp = GenerateOtp();
                    string hashedOtp = HashOtp(otp);

                    await _otpRepository.SaveOtpAsync(
                        new OtpModel
                        {
                            UserId = userId,
                            OtpHash = hashedOtp,
                            Attempts = 0,
                            Source = source
                        });

                    if (string.IsNullOrWhiteSpace(smsContent))
                    {
                        var smsTemplate = _templateService.GetSmsTemplate("OtpSms");

                        smsContent = _templateService.ReplacePlaceholders(
                            smsTemplate.Content,
                            new Dictionary<string, string>
                            {
                                { "Name", "Vistaarian" },
                                { "OTP", otp }
                            });

                        dltContentId = smsTemplate.DltContentId;
                    }
                    else
                    {
                        smsContent = smsContent.Replace("<<OTP>>", otp);
                    }

                    await _smsService.SendSms(
                        mobileNumber,
                        smsContent,
                        dltContentId ?? string.Empty);
                }

        private string GenerateOtp()
        {
            return RandomNumberGenerator
                .GetInt32(0, 1000000)
                .ToString("D6");
        }

        private string GenerateResetToken()
        {
            return Convert.ToHexString(
                RandomNumberGenerator.GetBytes(16));
        }

        private string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();

            byte[] bytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(otp));

            return Convert.ToBase64String(bytes);
        }

        private bool VerifyHash(
            string otp,
            string hashedOtp)
        {
            var computedHash =
                Convert.FromBase64String(HashOtp(otp));

            var storedHash =
                Convert.FromBase64String(hashedOtp);

            return CryptographicOperations.FixedTimeEquals(
                computedHash,
                storedHash);
        }

        #endregion
    }
}