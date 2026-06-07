using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LMS.API.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string to, string name, string orgName);
    Task SendEnrollmentConfirmationAsync(string to, string name, string courseTitle, string orgName);
    Task SendPaymentReceiptAsync(string to, string name, string orderNumber, decimal amount, string currency, List<string> courseNames, string orgName);
    Task SendCertificateIssuedAsync(string to, string name, string courseTitle, string certNumber, string orgName);
    Task SendPasswordResetAsync(string to, string name, string resetToken);
    Task SendOtpAsync(string to, string name, string otp);
    Task SendCoursePublishedAsync(List<string> instructorEmails, string courseTitle, string orgName);
    Task SendExamResultAsync(string to, string name, string examTitle, int score, bool passed, string orgName);
    Task SendLiveClassNotificationAsync(string to, string name, string title, DateTime scheduledAt, int durationMins, string platform, string meetingLink, string meetingId, string meetingPassword, string courseTitle);
    Task SendAssignmentNotificationAsync(string to, string name, string assignmentTitle, DateTime dueDate, string courseTitle);
    Task SendGradeNotificationAsync(string to, string name, string assignmentTitle, int marks, int maxMarks, string feedback, string courseTitle);
    Task SendInterviewNotificationAsync(string to, string name, string title, DateTime scheduledAt, int durationMins, string platform, string meetingLink, string interviewer, string notes);
}

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    readonly string _host      = config["Smtp:Host"]!;
    readonly int    _port      = int.Parse(config["Smtp:Port"] ?? "587");
    readonly string _user      = config["Smtp:User"]!;
    readonly string _password  = config["Smtp:Password"]!;
    readonly string _from      = config["Smtp:From"]!;
    readonly string _fromName  = config["Smtp:FromName"]!;

    async Task SendAsync(string to, string toName, string subject, string html)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _from));
        message.To.Add(new MailboxAddress(toName, to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = html };

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_host, _port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_user, _password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
            logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
    }

    static string BaseTemplate(string brandColor, string orgName, string content)
    {
        var year = DateTime.UtcNow.Year;
        var css = $@"
            * {{ box-sizing: border-box; margin: 0; padding: 0; }}
            body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f4f4f8; color: #333; }}
            .wrapper {{ max-width: 600px; margin: 32px auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }}
            .header {{ background: linear-gradient(135deg, {brandColor}, {brandColor}cc); padding: 32px 40px; text-align: center; }}
            .header h1 {{ color: white; font-size: 24px; font-weight: 800; letter-spacing: -0.5px; }}
            .header p {{ color: rgba(255,255,255,0.8); font-size: 13px; margin-top: 4px; }}
            .body {{ padding: 40px; }}
            .greeting {{ font-size: 18px; font-weight: 700; color: #111; margin-bottom: 16px; }}
            .text {{ font-size: 15px; color: #555; line-height: 1.7; margin-bottom: 16px; }}
            .btn {{ display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, {brandColor}, {brandColor}cc); color: white; text-decoration: none; border-radius: 10px; font-weight: 700; font-size: 15px; margin: 16px 0; }}
            .card {{ background: #f8f9ff; border-radius: 12px; padding: 20px; margin: 20px 0; border-left: 4px solid {brandColor}; }}
            .card h3 {{ font-size: 13px; color: #888; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px; }}
            .card p {{ font-size: 16px; font-weight: 700; color: #111; }}
            .table {{ width: 100%; border-collapse: collapse; margin: 16px 0; }}
            .table th {{ background: #f1f2f8; padding: 10px 14px; text-align: left; font-size: 12px; color: #666; text-transform: uppercase; letter-spacing: 0.5px; }}
            .table td {{ padding: 12px 14px; border-bottom: 1px solid #f0f0f0; font-size: 14px; }}
            .badge {{ display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 700; }}
            .badge-green {{ background: #d1fae5; color: #065f46; }}
            .badge-blue  {{ background: #dbeafe; color: #1e40af; }}
            .badge-red   {{ background: #fee2e2; color: #991b1b; }}
            .footer {{ background: #f8f9fa; padding: 24px 40px; text-align: center; border-top: 1px solid #eee; }}
            .footer p {{ font-size: 12px; color: #999; line-height: 1.6; }}";

        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <style>{css}</style>
</head>
<body>
  <div class=""wrapper"">
    <div class=""header"">
      <h1>{orgName}</h1>
      <p>Learning Management System</p>
    </div>
    <div class=""body"">
      {content}
    </div>
    <div class=""footer"">
      <p>© {year} {orgName}. All rights reserved.</p>
      <p>This is an automated email. Please do not reply directly.</p>
    </div>
  </div>
</body>
</html>";
    }

    public async Task SendWelcomeEmailAsync(string to, string name, string orgName)
    {
        var content = $"""
            <p class="greeting">👋 Welcome to {orgName}, {name}!</p>
            <p class="text">Your account has been created successfully. You're now part of a community of learners committed to growing their skills.</p>
            <div class="card">
              <h3>Your account</h3>
              <p>{to}</p>
            </div>
            <p class="text">Start exploring our course catalog and enroll in courses that match your goals.</p>
            <a href="#" class="btn">Browse Courses →</a>
            <p class="text" style="margin-top: 24px; font-size: 13px; color: #999;">If you didn't create this account, please ignore this email.</p>
            """;
        await SendAsync(to, name, $"Welcome to {orgName}! 🎓", BaseTemplate("#f97316", orgName, content));
    }

    public async Task SendEnrollmentConfirmationAsync(string to, string name, string courseTitle, string orgName)
    {
        var content = $"""
            <p class="greeting">🎉 You're enrolled, {name}!</p>
            <p class="text">Congratulations! You've successfully enrolled in:</p>
            <div class="card">
              <h3>Course</h3>
              <p>{courseTitle}</p>
            </div>
            <p class="text">You can start learning right now. Track your progress and complete all lessons to earn your certificate.</p>
            <a href="#" class="btn">Start Learning →</a>
            """;
        await SendAsync(to, name, $"Enrollment Confirmed: {courseTitle}", BaseTemplate("#10b981", orgName, content));
    }

    public async Task SendPaymentReceiptAsync(string to, string name, string orderNumber, decimal amount, string currency, List<string> courseNames, string orgName)
    {
        var courseRows = string.Join("", courseNames.Select(c => $"<tr><td>{c}</td><td style='text-align:right; font-weight:600'>✅ Enrolled</td></tr>"));
        var symbol = currency == "INR" ? "₹" : "$";
        var content = $"""
            <p class="greeting">✅ Payment Successful, {name}!</p>
            <p class="text">Your payment has been processed successfully. Here's your receipt:</p>
            <table class="table">
              <tr>
                <th>Order Number</th>
                <th>Amount Paid</th>
                <th>Date</th>
              </tr>
              <tr>
                <td><strong>{orderNumber}</strong></td>
                <td><strong style="color:#f97316; font-size:18px">{symbol}{amount:N2}</strong></td>
                <td>{DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC</td>
              </tr>
            </table>
            <p class="text" style="margin-top:20px; font-weight:700;">Courses enrolled:</p>
            <table class="table">
              <tr><th>Course</th><th>Status</th></tr>
              {courseRows}
            </table>
            <a href="#" class="btn">Go to My Courses →</a>
            <p class="text" style="font-size:13px; color:#999; margin-top:20px;">Save this email as your payment confirmation. Order ID: {orderNumber}</p>
            """;
        await SendAsync(to, name, $"Payment Receipt — {symbol}{amount:N2} | Order #{orderNumber}", BaseTemplate("#6366f1", orgName, content));
    }

    public async Task SendCertificateIssuedAsync(string to, string name, string courseTitle, string certNumber, string orgName)
    {
        var content = $"""
            <p class="greeting">🏆 Congratulations, {name}!</p>
            <p class="text">You've successfully completed <strong>{courseTitle}</strong> and earned your certificate!</p>
            <div class="card" style="text-align:center; border-left: none; border: 3px solid #f59e0b; background: linear-gradient(135deg, #fffbeb, #fef3c7);">
              <h3 style="color:#92400e;">Certificate of Completion</h3>
              <p style="font-size:20px; letter-spacing:2px; color:#78350f; font-family: monospace;">{certNumber}</p>
              <p style="font-size:13px; color:#a16207; margin-top:8px;">Issued on {DateTime.UtcNow:MMMM dd, yyyy}</p>
            </div>
            <p class="text">Download your certificate from your dashboard and share it on LinkedIn to showcase your achievement.</p>
            <a href="#" class="btn">Download Certificate →</a>
            """;
        await SendAsync(to, name, $"🏆 Certificate Issued: {courseTitle}", BaseTemplate("#f59e0b", orgName, content));
    }

    public async Task SendPasswordResetAsync(string to, string name, string resetToken)
    {
        var content = $"""
            <p class="greeting">🔐 Password Reset Request</p>
            <p class="text">Hi {name}, we received a request to reset your password. Use the token below:</p>
            <div class="card" style="text-align:center;">
              <h3>Reset Token (valid 15 minutes)</h3>
              <p style="font-size:28px; letter-spacing:6px; font-family:monospace; color:#6366f1;">{resetToken}</p>
            </div>
            <p class="text">If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
            """;
        await SendAsync(to, name, "Password Reset Request", BaseTemplate("#6366f1", "LMS Portal", content));
    }

    public async Task SendOtpAsync(string to, string name, string otp)
    {
        var content = $"""
            <p class="greeting">🔢 Your OTP</p>
            <p class="text">Hi {name}, here is your one-time password:</p>
            <div class="card" style="text-align:center;">
              <h3>OTP (valid 10 minutes)</h3>
              <p style="font-size:36px; letter-spacing:10px; font-family:monospace; color:#f97316; font-weight:900;">{otp}</p>
            </div>
            <p class="text" style="font-size:13px; color:#999;">Never share this OTP with anyone.</p>
            """;
        await SendAsync(to, name, $"Your OTP: {otp}", BaseTemplate("#f97316", "LMS Portal", content));
    }

    public async Task SendCoursePublishedAsync(List<string> instructorEmails, string courseTitle, string orgName)
    {
        foreach (var email in instructorEmails)
        {
            var content = $"""
                <p class="greeting">📢 Your Course is Live!</p>
                <p class="text">Your course <strong>{courseTitle}</strong> has been published and is now visible to students.</p>
                <div class="card">
                  <h3>Course</h3>
                  <p>{courseTitle}</p>
                </div>
                <p class="text">Students can now discover, enroll, and start learning from your course.</p>
                <a href="#" class="btn">View Course →</a>
                """;
            await SendAsync(email, "Instructor", $"Your course '{courseTitle}' is now live!", BaseTemplate("#10b981", orgName, content));
        }
    }

    public async Task SendExamResultAsync(string to, string name, string examTitle, int score, bool passed, string orgName)
    {
        var badge = passed ? "<span class='badge badge-green'>PASSED ✓</span>" : "<span class='badge badge-red'>FAILED ✗</span>";
        var msg   = passed
            ? "You've passed the exam! Your certificate will be issued shortly."
            : "Don't give up! Review the course material and try again.";
        var content = $"""
            <p class="greeting">📝 Exam Results: {examTitle}</p>
            <p class="text">Hi {name}, your exam has been graded.</p>
            <div class="card" style="text-align:center;">
              <h3>Your Score</h3>
              <p style="font-size:48px; font-weight:900; color:{(passed ? "#10b981" : "#ef4444")}">{score}%</p>
              <p style="margin-top:8px">{badge}</p>
            </div>
            <p class="text">{msg}</p>
            <a href="#" class="btn">View Results →</a>
            """;
        await SendAsync(to, name, $"Exam Result: {examTitle} — {score}%", BaseTemplate(passed ? "#10b981" : "#ef4444", orgName, content));
    }

    public async Task SendLiveClassNotificationAsync(string to, string name, string title, DateTime scheduledAt, int durationMins, string platform, string meetingLink, string meetingId, string meetingPassword, string courseTitle)
    {
        var content = $@"
            <p class=""greeting"">📅 Live Class Scheduled!</p>
            <p class=""text"">Hi {name}, a live class has been scheduled for <strong>{courseTitle}</strong>.</p>
            <div class=""card"">
              <h3>Class Details</h3>
              <table style=""width:100%;margin-top:8px"">
                <tr><td style=""color:#888;padding:4px 0"">Title</td><td><strong>{title}</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Date & Time</td><td><strong>{scheduledAt:dddd, MMMM dd yyyy 'at' HH:mm} UTC</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Duration</td><td><strong>{durationMins} minutes</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Platform</td><td><strong>{platform}</strong></td></tr>
                {(string.IsNullOrEmpty(meetingId) ? "" : $"<tr><td style='color:#888;padding:4px 0'>Meeting ID</td><td><strong>{meetingId}</strong></td></tr>")}
                {(string.IsNullOrEmpty(meetingPassword) ? "" : $"<tr><td style='color:#888;padding:4px 0'>Password</td><td><strong>{meetingPassword}</strong></td></tr>")}
              </table>
            </div>
            {(string.IsNullOrEmpty(meetingLink) ? "" : $"<a href='{meetingLink}' class='btn'>Join Class →</a>")}
            <p class=""text"" style=""font-size:13px;color:#999"">Please join 5 minutes before the scheduled time.</p>";

        await SendAsync(to, name, $"Live Class: {title} on {scheduledAt:MMM dd}", BaseTemplate("#6366f1", "LMS Portal", content));
    }

    public async Task SendAssignmentNotificationAsync(string to, string name, string assignmentTitle, DateTime dueDate, string courseTitle)
    {
        var content = $@"
            <p class=""greeting"">📝 New Assignment Posted!</p>
            <p class=""text"">Hi {name}, a new assignment has been posted for <strong>{courseTitle}</strong>.</p>
            <div class=""card"">
              <h3>Assignment</h3>
              <p>{assignmentTitle}</p>
              <p style=""margin-top:8px;font-size:13px;color:#e11d48"">⏰ Due: <strong>{dueDate:MMMM dd, yyyy 'at' HH:mm} UTC</strong></p>
            </div>
            <a href=""#"" class=""btn"">View Assignment →</a>";

        await SendAsync(to, name, $"New Assignment: {assignmentTitle}", BaseTemplate("#f97316", "LMS Portal", content));
    }

    public async Task SendGradeNotificationAsync(string to, string name, string assignmentTitle, int marks, int maxMarks, string feedback, string courseTitle)
    {
        var pct = maxMarks > 0 ? (int)Math.Round(marks * 100.0 / maxMarks) : 0;
        var color = pct >= 75 ? "#10b981" : pct >= 50 ? "#f59e0b" : "#ef4444";
        var content = $@"
            <p class=""greeting"">✅ Assignment Graded!</p>
            <p class=""text"">Hi {name}, your submission for <strong>{assignmentTitle}</strong> in <strong>{courseTitle}</strong> has been graded.</p>
            <div class=""card"" style=""text-align:center"">
              <h3>Your Score</h3>
              <p style=""font-size:40px;font-weight:900;color:{color}"">{marks}/{maxMarks}</p>
              <p style=""font-size:18px;color:{color}"">{pct}%</p>
            </div>
            {(string.IsNullOrEmpty(feedback) ? "" : $"<div class='card'><h3>Feedback</h3><p>{feedback}</p></div>")}
            <a href=""#"" class=""btn"">View Details →</a>";

        await SendAsync(to, name, $"Assignment Graded: {marks}/{maxMarks} — {assignmentTitle}", BaseTemplate(color, "LMS Portal", content));
    }

    public async Task SendInterviewNotificationAsync(string to, string name, string title, DateTime scheduledAt, int durationMins, string platform, string meetingLink, string interviewer, string notes)
    {
        var content = $@"
            <p class=""greeting"">🎯 Interview Scheduled!</p>
            <p class=""text"">Hi {name}, an interview has been scheduled for you. Please be prepared!</p>
            <div class=""card"">
              <h3>Interview Details</h3>
              <table style=""width:100%;margin-top:8px"">
                <tr><td style=""color:#888;padding:4px 0"">Title</td><td><strong>{title}</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Date & Time</td><td><strong>{scheduledAt:dddd, MMMM dd yyyy 'at' HH:mm} UTC</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Duration</td><td><strong>{durationMins} minutes</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Platform</td><td><strong>{platform}</strong></td></tr>
                <tr><td style=""color:#888;padding:4px 0"">Interviewer</td><td><strong>{interviewer}</strong></td></tr>
              </table>
            </div>
            {(string.IsNullOrEmpty(notes) ? "" : $"<div class='card'><h3>Notes</h3><p>{notes}</p></div>")}
            {(string.IsNullOrEmpty(meetingLink) ? "" : $"<a href='{meetingLink}' class='btn'>Join Interview →</a>")}
            <p class=""text"" style=""color:#555"">Tips: Be on time, test your internet connection, and keep your documents ready. Best of luck! 🍀</p>";

        await SendAsync(to, name, $"Interview Scheduled: {title} on {scheduledAt:MMM dd}", BaseTemplate("#7c3aed", "LMS Portal", content));
    }

}