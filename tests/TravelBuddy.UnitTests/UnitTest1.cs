using Microsoft.Extensions.Configuration;
using TravelBuddy.Services;
using Xunit;

namespace TravelBuddy.UnitTests
{
    public class TravelBuddyPolicyTests
    {
        [Fact]
        public void QrCredentialService_Generates_Opaque_URLSafe_Token()
        {
            var service = new QrCredentialService();
            var token = service.GenerateRawToken();

            Assert.NotNull(token);
            Assert.StartsWith("TB1.", token);
            Assert.True(token.Length > 20);

            var hash = service.HashToken(token);
            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length); // SHA-256 hex string length
        }

        [Fact]
        public void RazorpayService_Simulated_Signature_Verification_Works()
        {
            var config = new ConfigurationBuilder().Build();
            var razorpay = new RazorpayService(config);

            bool result = razorpay.VerifyPaymentSignature("order_123", "pay_sim_999", "simulated_signature");
            Assert.True(result);

            bool invalidResult = razorpay.VerifyPaymentSignature("order_123", "pay_invalid", "bad_signature");
            Assert.False(invalidResult);
        }

        [Fact]
        public void QrCredentialService_Generates_Valid_Base64_QrCode()
        {
            var service = new QrCredentialService();
            var token = service.GenerateRawToken();
            var qr = service.CreateDynamicQr(token, DateTime.UtcNow.AddSeconds(30));

            Assert.NotNull(qr);
            Assert.Equal(token, qr.Token);
            Assert.StartsWith("data:image/png;base64,", qr.QrBase64Image);
            Assert.Equal(30, qr.RefreshIntervalSeconds);
        }
    }
}
