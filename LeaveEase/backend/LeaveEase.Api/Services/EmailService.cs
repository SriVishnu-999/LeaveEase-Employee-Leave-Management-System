using LeaveEase.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LeaveEase.Api.Services;

public interface IEmailService
{
    Task SendLeaveSubmittedAsync(LeaveRequest request, AppUser manager, CancellationToken cancellationToken = default);
    Task SendLeaveStatusChangedAsync(LeaveRequest request, CancellationToken cancellationToken = default);
}

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendLeaveSubmittedAsync(LeaveRequest request, AppUser manager, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manager.Email))
            return;

        var subject = $"Leave request #{request.Id} from {request.Employee.FullName}";
        var body = $"""
            <h2>New leave request</h2>
            <p><strong>{Escape(request.Employee.FullName)}</strong> submitted a {Escape(request.LeaveType.Name)} request.</p>
            <table style="border-collapse:collapse">
              <tr><td style="padding:6px 12px 6px 0"><strong>Dates</strong></td><td>{request.StartDate:dd MMM yyyy} – {request.EndDate:dd MMM yyyy}</td></tr>
              <tr><td style="padding:6px 12px 6px 0"><strong>Days</strong></td><td>{request.TotalDays:0.##}</td></tr>
              <tr><td style="padding:6px 12px 6px 0"><strong>Reason</strong></td><td>{Escape(request.Reason)}</td></tr>
            </table>
            <p>Please sign in to LeaveEase to approve or reject the request.</p>
            """;

        await SendAsync(manager.Email, subject, body, cancellationToken);
    }

    public async Task SendLeaveStatusChangedAsync(LeaveRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Employee.Email))
            return;

        var subject = $"Your leave request #{request.Id} was {request.Status.ToString().ToLowerInvariant()}";
        var comment = string.IsNullOrWhiteSpace(request.ManagerComment)
            ? ""
            : $"<p><strong>Manager comment:</strong> {Escape(request.ManagerComment)}</p>";

        var body = $"""
            <h2>Leave request updated</h2>
            <p>Hello {Escape(request.Employee.FullName)},</p>
            <p>Your <strong>{Escape(request.LeaveType.Name)}</strong> request for {request.StartDate:dd MMM yyyy} – {request.EndDate:dd MMM yyyy} is now <strong>{request.Status}</strong>.</p>
            {comment}
            <p>Sign in to LeaveEase to view the complete request history and current balance.</p>
            """;

        await SendAsync(request.Employee.Email, subject, body, cancellationToken);
    }

    private async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["Smtp:Enabled"], out var enabled) || !enabled)
        {
            logger.LogInformation("SMTP disabled. Skipping email to {Recipient}: {Subject}", recipient, subject);
            return;
        }

        try
        {
            var host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is missing.");
            var port = int.TryParse(configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var username = configuration["Smtp:Username"] ?? string.Empty;
            var password = configuration["Smtp:Password"] ?? string.Empty;
            var fromEmail = configuration["Smtp:FromEmail"] ?? username;
            var fromName = configuration["Smtp:FromName"] ?? "LeaveEase";
            var useSsl = bool.TryParse(configuration["Smtp:UseSsl"], out var ssl) && ssl;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            var secureSocket = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(host, port, secureSocket, cancellationToken);
            if (!string.IsNullOrWhiteSpace(username))
                await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send LeaveEase email to {Recipient}", recipient);
        }
    }

    private static string Escape(string value) => System.Net.WebUtility.HtmlEncode(value);
}
