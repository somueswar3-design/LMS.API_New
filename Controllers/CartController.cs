using LMS.API.Data;
using LMS.API.DTOs;
using LMS.API.Models;
using LMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Controllers;

// ═══════════════════════════════════════════════════════════════
//  CART
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/cart"), Authorize]
public class CartController(LmsDbContext db) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        var items = await db.Carts.Include(c => c.Course).Where(c => c.UserId == userId).ToListAsync();
        return Ok(items.Select(c => new CartItemDto(c.Id, c.CourseId, c.Course.Title, c.Course.ThumbnailUrl, c.Course.Price, c.Course.IsFree, c.AddedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        if (await db.Enrollments.AnyAsync(e => e.UserId == req.UserId && e.CourseId == req.CourseId))
            return BadRequest(new { message = "Already enrolled" });
        if (await db.Carts.AnyAsync(c => c.UserId == req.UserId && c.CourseId == req.CourseId))
            return BadRequest(new { message = "Already in cart" });
        var course = await db.Courses.FindAsync(req.CourseId);
        if (course is null) return NotFound();

        if (course.IsFree)
        {
            db.Enrollments.Add(new Enrollment { UserId = req.UserId, CourseId = req.CourseId });
            await db.SaveChangesAsync();
            return Ok(new { enrolled = true, message = "Enrolled in free course" });
        }

        db.Carts.Add(new Cart { UserId = req.UserId, CourseId = req.CourseId });
        await db.SaveChangesAsync();
        return Ok(new { added = true });
    }

    [HttpDelete("{userId}/{courseId}")]
    public async Task<IActionResult> RemoveFromCart(int userId, int courseId)
    {
        var item = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);
        if (item is null) return NotFound();
        db.Carts.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{userId}/clear")]
    public async Task<IActionResult> ClearCart(int userId)
    {
        var items = await db.Carts.Where(c => c.UserId == userId).ToListAsync();
        db.Carts.RemoveRange(items);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record AddToCartRequest(int UserId, int CourseId);

// ═══════════════════════════════════════════════════════════════
//  PAYMENTS — full Razorpay integration with transaction logs
// ═══════════════════════════════════════════════════════════════
[ApiController, Route("api/payments")]
public class PaymentsController(LmsDbContext db, IRazorpayService razorpay, IEmailService email, ILogger<PaymentsController> logger) : ControllerBase
{
    // ─── Step 1: Create Razorpay order ───────────────────────
    [HttpPost("create-order")]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var user = await db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Id == req.UserId);
        if (user is null) return NotFound(new { message = "User not found" });

        var courses = await db.Courses.Where(c => req.CourseIds.Contains(c.Id) && c.Status == CourseStatus.Published).ToListAsync();
        if (!courses.Any()) return BadRequest(new { message = "No valid courses" });

        // Filter out already enrolled
        var enrolledIds = await db.Enrollments
            .Where(e => e.UserId == req.UserId && req.CourseIds.Contains(e.CourseId))
            .Select(e => e.CourseId).ToListAsync();
        courses = courses.Where(c => !enrolledIds.Contains(c.Id)).ToList();
        if (!courses.Any()) return BadRequest(new { message = "Already enrolled in all selected courses" });

        var total    = courses.Sum(c => c.Price);
        var currency = user.Organization.Currency ?? "INR";
        var receipt  = $"rcpt_{DateTime.UtcNow:yyyyMMddHHmmss}_{req.UserId}";

        // Create Razorpay order
        var rzpOrder = await razorpay.CreateOrderAsync(total, currency, receipt, new()
        {
            ["userId"]   = req.UserId.ToString(),
            ["courses"]  = string.Join(",", courses.Select(c => c.Id))
        });

        // Persist order
        var order = new Order
        {
            UserId         = req.UserId,
            TotalAmount    = total,
            Currency       = currency,
            RazorpayOrderId = rzpOrder.OrderId,
            Status         = OrderStatus.Pending
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        foreach (var c in courses)
            db.OrderItems.Add(new OrderItem { OrderId = order.Id, CourseId = c.Id, Price = c.Price });

        // Log transaction as initiated
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId          = req.UserId,
            OrderId         = order.Id,
            RazorpayOrderId = rzpOrder.OrderId,
            Amount          = total,
            Currency        = currency,
            Status          = PaymentTransactionStatus.Initiated,
            IpAddress       = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent       = Request.Headers.UserAgent.ToString()
        });
        await db.SaveChangesAsync();

        logger.LogInformation("Payment order created: {OrderId} for User:{UserId} Amount:{Amount}", order.Id, req.UserId, total);

        return Ok(new
        {
            orderId          = order.Id,
            razorpayOrderId  = rzpOrder.OrderId,
            amount           = (long)(total * 100),
            currency,
            keyId            = user.Organization.RazorpayKeyId,
            courses          = courses.Select(c => new { c.Id, c.Title, c.Price }),
            userEmail        = user.Email,
            userName         = $"{user.FirstName} {user.LastName}",
            userPhone        = user.PhoneNumber ?? ""
        });
    }

    // ─── Step 2: Verify signature & enroll ───────────────────
    [HttpPost("verify")]
    [Authorize]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Include(o => o.User).ThenInclude(u => u.Organization)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId);
        if (order is null) return NotFound(new { message = "Order not found" });
        if (order.Status == OrderStatus.Paid) return BadRequest(new { message = "Already paid" });

        // Verify Razorpay signature
        var isValid = razorpay.VerifyPaymentSignature(req.RazorpayOrderId, req.RazorpayPaymentId, req.RazorpaySignature);

        // Get transaction log
        var txn = await db.PaymentTransactions.FirstOrDefaultAsync(t => t.RazorpayOrderId == req.RazorpayOrderId);

        if (!isValid)
        {
            if (txn is not null)
            {
                txn.Status        = PaymentTransactionStatus.Failed;
                txn.FailureReason = "Signature mismatch";
                txn.CompletedAt   = DateTime.UtcNow;
            }
            order.Status = OrderStatus.Failed;
            await db.SaveChangesAsync();
            logger.LogWarning("Payment signature invalid for Order:{OrderId}", req.OrderId);
            return BadRequest(new { message = "Payment verification failed" });
        }

        // Fetch payment details from Razorpay for method info
        var paymentDetails = await razorpay.FetchPaymentAsync(req.RazorpayPaymentId);

        // Mark order paid
        order.Status              = OrderStatus.Paid;
        order.PaidAt              = DateTime.UtcNow;
        order.RazorpayPaymentId   = req.RazorpayPaymentId;
        order.RazorpaySignature   = req.RazorpaySignature;

        // Update transaction log
        if (txn is not null)
        {
            txn.RazorpayPaymentId = req.RazorpayPaymentId;
            txn.RazorpaySignature = req.RazorpaySignature;
            txn.Status            = PaymentTransactionStatus.Success;
            txn.CompletedAt       = DateTime.UtcNow;
            txn.Method            = MapMethod(paymentDetails?.Method ?? "");
            txn.MethodDetail      = paymentDetails?.Method;
        }

        // Auto-enroll
        var enrolledCourses = new List<string>();
        foreach (var item in order.Items)
        {
            if (!await db.Enrollments.AnyAsync(e => e.UserId == order.UserId && e.CourseId == item.CourseId))
            {
                db.Enrollments.Add(new Enrollment { UserId = order.UserId, CourseId = item.CourseId });
                enrolledCourses.Add(item.Course.Title);
            }
            // Clear from cart
            var cartItem = await db.Carts.FirstOrDefaultAsync(c => c.UserId == order.UserId && c.CourseId == item.CourseId);
            if (cartItem is not null) db.Carts.Remove(cartItem);
        }
        await db.SaveChangesAsync();

        // Send receipt email
        _ = email.SendPaymentReceiptAsync(
            order.User.Email,
            $"{order.User.FirstName} {order.User.LastName}",
            order.OrderNumber,
            order.TotalAmount,
            order.Currency,
            enrolledCourses,
            order.User.Organization.Name
        );

        // Send enrollment confirmation for each course
        foreach (var courseName in enrolledCourses)
            _ = email.SendEnrollmentConfirmationAsync(order.User.Email, order.User.FirstName, courseName, order.User.Organization.Name);

        logger.LogInformation("Payment verified: Order:{OrderId} Payment:{PaymentId} Amount:{Amount}", req.OrderId, req.RazorpayPaymentId, order.TotalAmount);

        return Ok(new { success = true, enrolledCourses, orderNumber = order.OrderNumber });
    }

    // ─── Razorpay Webhook (for async confirmation) ────────────
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        logger.LogInformation("Razorpay webhook received: {Payload}", payload[..Math.Min(200, payload.Length)]);

        // Store webhook for audit
        // TODO: verify webhook signature and process event
        return Ok();
    }

    // ─── Admin: all transactions ──────────────────────────────
    [HttpGet("transactions")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int? orgId, [FromQuery] string? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var q = db.PaymentTransactions
            .Include(t => t.User).ThenInclude(u => u.Organization)
            .Include(t => t.Order).ThenInclude(o => o!.Items).ThenInclude(i => i.Course)
            .AsQueryable();

        if (User.IsInRole("OrgAdmin"))
        {
            var myOrgId = int.Parse(User.FindFirst("orgId")!.Value);
            q = q.Where(t => t.User.OrganizationId == myOrgId);
        }
        else if (orgId.HasValue)
            q = q.Where(t => t.User.OrganizationId == orgId.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentTransactionStatus>(status, out var st))
            q = q.Where(t => t.Status == st);
        if (from.HasValue) q = q.Where(t => t.InitiatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(t => t.InitiatedAt <= to.Value);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.InitiatedAt).Skip((page - 1) * size).Take(size).ToListAsync();

        var totalRevenue = await q.Where(t => t.Status == PaymentTransactionStatus.Success).SumAsync(t => t.Amount);
        var totalRefunds = await q.Where(t => t.Status == PaymentTransactionStatus.Refunded).SumAsync(t => t.RefundAmount ?? 0);

        return Ok(new
        {
            items = items.Select(MapTxn),
            totalCount = total,
            page, pageSize = size,
            totalPages = (int)Math.Ceiling(total / (double)size),
            summary = new
            {
                totalRevenue,
                totalRefunds,
                netRevenue = totalRevenue - totalRefunds,
                successCount = items.Count(t => t.Status == PaymentTransactionStatus.Success),
                failedCount  = items.Count(t => t.Status == PaymentTransactionStatus.Failed),
            }
        });
    }

    // ─── Single transaction detail ────────────────────────────
    [HttpGet("transactions/{id}")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> GetTransaction(int id)
    {
        var t = await db.PaymentTransactions
            .Include(t => t.User)
            .Include(t => t.Order).ThenInclude(o => o!.Items).ThenInclude(i => i.Course)
            .FirstOrDefaultAsync(t => t.Id == id);
        return t is null ? NotFound() : Ok(MapTxn(t));
    }

    // ─── User: own transactions ───────────────────────────────
    [HttpGet("transactions/user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserTransactions(int userId)
    {
        var items = await db.PaymentTransactions
            .Include(t => t.Order).ThenInclude(o => o!.Items).ThenInclude(i => i.Course)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.InitiatedAt)
            .Take(50).ToListAsync();
        return Ok(items.Select(MapTxn));
    }

    // ─── Refund ───────────────────────────────────────────────
    [HttpPost("refund")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Refund([FromBody] RefundRequest req)
    {
        var txn = await db.PaymentTransactions.FindAsync(req.TransactionId);
        if (txn is null || txn.Status != PaymentTransactionStatus.Success)
            return BadRequest(new { message = "Transaction not eligible for refund" });

        var ok = await razorpay.RefundPaymentAsync(txn.RazorpayPaymentId!, req.Amount ?? txn.Amount);
        if (!ok) return StatusCode(500, new { message = "Refund failed" });

        txn.Status       = req.Amount.HasValue && req.Amount < txn.Amount
            ? PaymentTransactionStatus.PartialRefund
            : PaymentTransactionStatus.Refunded;
        txn.RefundAmount = req.Amount ?? txn.Amount;
        txn.RefundedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("Refund processed: Txn:{Id} Amount:{Amount}", req.TransactionId, txn.RefundAmount);
        return Ok(new { success = true, refundAmount = txn.RefundAmount });
    }

    // ─── Orders list ─────────────────────────────────────────
    [HttpGet("orders/user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserOrders(int userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Course)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt).Take(50).ToListAsync();
        return Ok(orders.Select(o => new OrderDto(o.Id, o.OrderNumber, o.TotalAmount, o.Currency, o.Status.ToString(), o.RazorpayOrderId, o.CreatedAt, o.Items.Select(i => new OrderItemDto(i.CourseId, i.Course.Title, i.Price)).ToList())));
    }

    static PaymentMethod MapMethod(string m) => m.ToLower() switch
    {
        "upi"         => PaymentMethod.UPI,
        "card"        => PaymentMethod.Card,
        "netbanking"  => PaymentMethod.NetBanking,
        "wallet"      => PaymentMethod.Wallet,
        "emi"         => PaymentMethod.EMI,
        _             => PaymentMethod.Unknown
    };

    static object MapTxn(PaymentTransaction t) => new
    {
        t.Id, t.TransactionRef, t.RazorpayOrderId, t.RazorpayPaymentId,
        t.Amount, t.Currency,
        Status = t.Status.ToString(), Method = t.Method.ToString(),
        t.MethodDetail, t.FailureReason, t.RefundId, t.RefundAmount, t.RefundedAt,
        t.InitiatedAt, t.CompletedAt, t.IpAddress,
        User = new { t.User.Id, t.User.FirstName, t.User.LastName, t.User.Email },
        Order = t.Order is null ? null : new
        {
            t.Order.Id, t.Order.OrderNumber, t.Order.TotalAmount,
            Courses = t.Order.Items.Select(i => i.Course.Title)
        }
    };
}

public record RefundRequest(int TransactionId, decimal? Amount);
