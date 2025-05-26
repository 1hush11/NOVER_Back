using System.Net.Mail;


namespace NOVER_Back.Models
{ 
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }
}
