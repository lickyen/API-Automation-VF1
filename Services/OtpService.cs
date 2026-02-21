using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


public class OtpService
{
    private readonly UserRepository _userRepository;
    private readonly OtpRepository _otpRepository;
    private readonly SmsService _smsService;
    private readonly TemplateService _templateService;

    public OtpService( UserRepository userRepository, OtpRepository otpRepository, SmsService smsService, TemplateService templateService)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _smsService = smsService;
        _templateService = templateService;
    }

    public async Task<bool> SendOtp(string userId)
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
            Attempts = 0
        });

        var placeholders = new Dictionary<string, string>
        {
            { "OTP", otp }
        };

        var smsTemplate = _templateService.GetSmsTemplate("OtpSms");
        var smsBody = _templateService.ReplacePlaceholders(smsTemplate.Content, placeholders);

        await _smsService.SendSms(user.Mobile, smsBody, smsTemplate.DltContentId);

        return true;
    }

    public async Task<bool> VerifyOtp(string userId, string otp)
    {
        var storedOtp = await _otpRepository.GetOtpByUserIdAsync(userId);

        if (storedOtp == null || storedOtp.ExpiryTime < DateTime.UtcNow || storedOtp.Attempts >= 5)
            return false;

        storedOtp.Attempts++;

        bool isValid = VerifyHash(otp, storedOtp.OtpHash);

        if (isValid)
            await _otpRepository.DeleteOtpAsync(userId);
        else
            await _otpRepository.UpdateAttemptAsync(storedOtp);

        return isValid;
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
