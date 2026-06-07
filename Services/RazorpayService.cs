using System.Security.Cryptography;
using System.Text;
using Razorpay.Api;

namespace LMS.API.Services;

public interface IRazorpayService
{
    Task<RazorpayOrderResult> CreateOrderAsync(decimal amount, string currency, string receiptId, Dictionary<string, string>? notes = null);
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);
    Task<RazorpayPaymentDetails?> FetchPaymentAsync(string paymentId);
    Task<bool> RefundPaymentAsync(string paymentId, decimal amount);
}

public record RazorpayOrderResult(string OrderId, long AmountPaise, string Currency, string Status);
public record RazorpayPaymentDetails(string Id, long Amount, string Currency, string Status, string? Method, string? Email, string? Contact, DateTime CreatedAt);

public class RazorpayService(IConfiguration config, ILogger<RazorpayService> logger) : IRazorpayService
{
    readonly string _keyId     = config["Razorpay:KeyId"]!;
    readonly string _keySecret = config["Razorpay:KeySecret"]!;

    RazorpayClient Client() => new(_keyId, _keySecret);

    public async Task<RazorpayOrderResult> CreateOrderAsync(
        decimal amount, string currency, string receiptId,
        Dictionary<string, string>? notes = null)
    {
        try
        {
            var opts = new Dictionary<string, object>
            {
                ["amount"]          = (long)(amount * 100),
                ["currency"]        = currency,
                ["receipt"]         = receiptId,
                ["payment_capture"] = 1
            };
            if (notes != null) opts["notes"] = notes;

            var order   = Client().Order.Create(opts);
            var orderId = (string)order["id"];          // cast dynamic → string
            var status  = (string)order["status"];

            logger.LogInformation("Razorpay order created: {OrderId} ₹{Amount}", orderId, amount);
            return new RazorpayOrderResult(orderId, (long)(amount * 100), currency, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Razorpay order for {Receipt}", receiptId);
            throw;
        }
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
    {
        try
        {
            var payload = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
            var hash    = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
            var isValid = hash == signature.ToLower();

            logger.LogInformation("Signature verification {PaymentId}: {Result}",
                paymentId, isValid ? "PASS" : "FAIL");
            return isValid;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Signature verification error for {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task<RazorpayPaymentDetails?> FetchPaymentAsync(string paymentId)
    {
        try
        {
            var payment   = Client().Payment.Fetch(paymentId);
            var createdAt = DateTimeOffset
                .FromUnixTimeSeconds(long.Parse(payment["created_at"].ToString()!))
                .UtcDateTime;

            return new RazorpayPaymentDetails(
                (string)payment["id"],
                long.Parse(payment["amount"].ToString()!),
                (string)payment["currency"],
                (string)payment["status"],
                payment["method"]?.ToString(),
                payment["email"]?.ToString(),
                payment["contact"]?.ToString(),
                createdAt
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<bool> RefundPaymentAsync(string paymentId, decimal amount)
    {
        try
        {
            // The Razorpay SDK Refund.Create() is the correct API.
            // Payment.Refund() only exists in older SDK versions and takes no extra args.
            var opts = new Dictionary<string, object>
            {
                ["amount"]     = (long)(amount * 100),
                ["payment_id"] = paymentId,
                ["speed"]      = "normal"
            };

            var refund   = Client().Refund.Create(opts);
            var refundId = (string)refund["id"];        // cast dynamic → string

            logger.LogInformation("Refund created: {RefundId} for payment {PaymentId} ₹{Amount}",
                refundId, paymentId, amount);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refund failed for payment {PaymentId}", paymentId);
            return false;
        }
    }
}
