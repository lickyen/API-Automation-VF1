using Microsoft.IdentityModel.Tokens;
using ProductivityApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtHelper
{
    private readonly JwtSettings _jwtSettings;

    public JwtHelper(AppSettings appSettings)
    {
        _jwtSettings = appSettings.JwtSettings;
    }

    public string GenerateToken(string userId)
    {
        var claims = new List<Claim>
        {
            new("UserId", userId)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}