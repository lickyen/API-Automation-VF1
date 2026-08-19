using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProductivityApi.Services
{
    public class OtpService
    {
        private readonly UserRepository _userRepository;
        private readonly OtpRepository _otpRepository;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserLogRepository _userLogRepository;

        public OtpService(UserRepository userRepository, OtpRepository otpRepository, SmsService smsService, TemplateService templateService, UserLogRepository userLogRepository)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _smsService = smsService;
            _templateService = templateService;
            _userLogRepository = userLogRepository;
        }
        public async Task<string> SendOtp(string userId, string? source = null)
        {
            var user = _userRepository.GetUser(userId)
                       ?? throw new InvalidOperationException("User not found");

            if (string.IsNullOrWhiteSpace(user.Mobile))
                throw new InvalidOperationException("Contact details not found");

            string otp = GenerateOtp();
            string hashedOtp = HashOtp(otp);

            await _otpRepository.SaveOtpAsync(new OtpModel
            {
                UserId = userId,
                OtpHash = hashedOtp,
                Attempts = 0,
                Source = source
            });

            var placeholders = new Dictionary<string, string>
            {
                { "OTP", otp }
            };

            var smsTemplate = _templateService.GetSmsTemplate("OtpSms");
            var smsBody = _templateService.ReplacePlaceholders(
                smsTemplate.Content,
                placeholders);

            await _smsService.SendSms(
                user.Mobile,
                smsBody,
                smsTemplate.DltContentId);

            return user.Mobile;
        }
        public async Task<bool> VerifyOtp(string userId, string otp, string? source = null)
        {
            bool isValid = false;
            string? message = null;

            try
            {
                var storedOtp = await _otpRepository.GetOtpByUserIdAsync(userId, source);

                if (storedOtp == null)
                {
                    message = "OTP not found";
                    return false;
                }

                if (storedOtp.ExpiryTime < DateTime.UtcNow)
                {
                    message = "OTP expired";
                    return false;
                }

                // Increment first, persist it, then check the limit
                storedOtp.Attempts++;
                storedOtp.Source = source;
                await _otpRepository.UpdateAttemptAsync(storedOtp);

                if (storedOtp.Attempts > 5)
                {
                    message = "Max attempts exceeded";
                    return false;
                }

                isValid = VerifyHash(otp, storedOtp.OtpHash);
                message = isValid ? null : $"Invalid OTP ({storedOtp.Attempts})";

                if (isValid)
                    await _otpRepository.DeleteOtpAsync(userId, source);
            }
            catch (Exception ex)
            {
                message = $"Exception: {ex.GetType().Name} — {ex.Message}";
                Log.Error(ex, "OTP verification error for UserId={UserId} Source={Source}", userId, source);
                throw;
            }
            finally
            {
                await _userLogRepository.AddAsync(new UserLog
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
                    isValid
                );
            }

            return isValid;
        }

        public async Task<bool> SendOtpToMobile(string referenceId, string mobileNumber, string? source = null)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
                throw new InvalidOperationException("Mobile number not found");

            string otp = GenerateOtp();
            string hashedOtp = HashOtp(otp);

            await _otpRepository.SaveOtpAsync(
                new OtpModel
                {
                    UserId = referenceId,
                    OtpHash = hashedOtp,
                    Attempts = 0,
                    Source = source
                }
            );

            var placeholders = new Dictionary<string, string> { { "OTP", otp } };

            var smsTemplate = _templateService.GetSmsTemplate("OtpSms");

            var smsBody = _templateService.ReplacePlaceholders(smsTemplate.Content, placeholders);

            await _smsService.SendSms(mobileNumber, smsBody, smsTemplate.DltContentId);

            return true;
        }

        #region Helpers
        private string GenerateOtp()
        {
            var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[4];
            rng.GetBytes(bytes);
            int otp = BitConverter.ToInt32(bytes, 0) % 1000000;
            otp = Math.Abs(otp); // ensure positive
            return otp.ToString("D6"); // 6-digit OTP
        }

        private string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyHash(string otp, string hashedOtp)
        {
            return HashOtp(otp) == hashedOtp;
        }
        #endregion
    }
}
