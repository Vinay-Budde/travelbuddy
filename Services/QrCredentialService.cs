using System.Security.Cryptography;
using System.Text;
using QRCoder;
using TravelBuddy.DTOs;
using TravelBuddy.Models;

namespace TravelBuddy.Services
{
    public interface IQrCredentialService
    {
        string GenerateRawToken();
        string HashToken(string token);
        QrCodeResponse CreateDynamicQr(string rawToken, DateTime expiresAt);
    }

    public class QrCredentialService : IQrCredentialService
    {
        public string GenerateRawToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
            // URL-safe base64
            return "TB1." + Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public string HashToken(string token)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public QrCodeResponse CreateDynamicQr(string rawToken, DateTime expiresAt)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(rawToken, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            string base64 = Convert.ToBase64String(qrCodeImage);

            return new QrCodeResponse
            {
                Token = rawToken,
                QrBase64Image = $"data:image/png;base64,{base64}",
                ExpiresAtUtc = expiresAt,
                RefreshIntervalSeconds = 30
            };
        }
    }
}
