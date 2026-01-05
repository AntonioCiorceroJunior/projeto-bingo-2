using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BingoAdmin.UI.Services
{
    public class EmailService
    {
        // In a real scenario, these would be in appsettings.json
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "ciorcero07@gmail.com"; // Or a dedicated sender
        private const string SenderPassword = "your-app-password"; // App Password

        public async Task SendFreeTrialRequestAsync(string userName, string userEmail, string userCpf, string userPhone, string reason)
        {
            // For now, we'll just log to console/debug as we don't have the password
            // In production, uncomment the code below and configure credentials
            
            string subject = $"Solicitação de Teste Grátis - {userName}";
            string body = $@"
                Nova solicitação de teste grátis:
                
                Nome: {userName}
                Email: {userEmail}
                CPF: {userCpf}
                Telefone: {userPhone}
                
                Motivo/Indicação:
                {reason}
            ";

            // Mock sending
            await Task.Delay(1000); 
            Console.WriteLine($"[MOCK EMAIL] To: ciorcero07@gmail.com | Subject: {subject} | Body: {body}");

            /*
            using (var client = new SmtpClient(SmtpServer, SmtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(SenderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add("ciorcero07@gmail.com");

                await client.SendMailAsync(mailMessage);
            }
            */
        }
    }
}
