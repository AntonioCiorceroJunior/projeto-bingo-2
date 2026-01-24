using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BingoAdmin.UI.Services
{
    public class EmailService
    {
        // Configurações do Gmail
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SenderEmail = "bingodociorcero@gmail.com";
        private const string SenderPassword = "aoat jbgz agws mkil"; // App Password

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

        public async Task SendPasswordResetCodeAsync(string email, string code)
        {
            try 
            {
                string subject = "Código de Redefinição de Senha - Bingo Admin";
                string body = $@"
                    Utilize o código abaixo para redefinir sua senha:
                    
                    CÓDIGO: {code}
                    
                    Este código expira em 15 minutos.
                ";

                using (var client = new SmtpClient(SmtpServer, SmtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                    
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(SenderEmail, "Bingo Admin Security"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = false
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }
            } 
            catch (Exception ex) 
            {
                // Fallback para debug se falhar (ex: bloqueio de segurança do Gmail)
                Console.WriteLine("Erro ao enviar email real: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"[ERRO EMAIL] {ex.Message}");
                throw; // Relança para o UI tratar
            }
        }
    }
}
