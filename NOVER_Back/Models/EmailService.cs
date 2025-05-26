using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NOVER_Back.Models;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) =>
        _config = config;

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:Smtp:SenderName"],
            _config["Email:Smtp:SenderEmail"]
        ));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["Email:Smtp:Host"],
            int.Parse(_config["Email:Smtp:Port"]),
            SecureSocketOptions.SslOnConnect
        );
        await smtp.AuthenticateAsync(
            _config["Email:Smtp:Username"],
            _config["Email:Smtp:Password"]
        );
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
