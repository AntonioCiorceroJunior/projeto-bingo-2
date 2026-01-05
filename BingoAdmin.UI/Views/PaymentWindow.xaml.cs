using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using BingoAdmin.UI.Services;
using QRCoder;

namespace BingoAdmin.UI.Views
{
    public partial class PaymentWindow : Window
    {
        private readonly Usuario _usuario;
        private readonly BingoContext _context;
        private readonly EmailService _emailService;
        private readonly PaymentService _paymentService;
        private DispatcherTimer _paymentCheckTimer;
        private string _currentTransactionId = "";
        private decimal _currentAmount = 0;

        public bool PaymentSuccess { get; private set; } = false;

        public PaymentWindow(Usuario usuario, BingoContext context, EmailService emailService, PaymentService paymentService)
        {
            InitializeComponent();
            _usuario = usuario;
            _context = context;
            _emailService = emailService;
            _paymentService = paymentService;

            LoadUserData();

            _paymentCheckTimer = new DispatcherTimer();
            _paymentCheckTimer.Interval = TimeSpan.FromSeconds(5);
            _paymentCheckTimer.Tick += CheckPaymentStatus_Tick;
        }

        private void LoadUserData()
        {
            TrialNameBox.Text = _usuario.Nome;
            TrialCpfBox.Text = _usuario.Cpf;
            TrialPhoneBox.Text = _usuario.Telefone;

            if (_usuario.JaUsouTesteGratis || _usuario.SolicitouTesteGratis)
            {
                FreeTrialButton.Visibility = Visibility.Collapsed;
            }
        }

        private void PayDaily_Click(object sender, RoutedEventArgs e)
        {
            StartPixPayment(50.00m, "Acesso Diário");
        }

        private void PayMonthly_Click(object sender, RoutedEventArgs e)
        {
            StartPixPayment(400.00m, "Acesso Mensal");
        }

        private void StartPixPayment(decimal amount, string description)
        {
            PaymentPanel.Visibility = Visibility.Collapsed;
            PixPaymentPanel.Visibility = Visibility.Visible;

            PixPlanText.Text = description;
            PixAmountText.Text = $"Valor: {amount:C}";
            _currentAmount = amount;

            GeneratePixQrCode(amount);
            
            // Start Polling
            _paymentCheckTimer.Start();
        }

        private void GeneratePixQrCode(decimal amount)
        {
            try
            {
                var response = _paymentService.CreatePixPayment(amount, PixPlanText.Text);
                
                _currentTransactionId = response.TransactionId;
                PixCopyBox.Text = response.CopyPasteCode;

                // Generate QR Code Image
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(response.CopyPasteCode, QRCodeGenerator.ECCLevel.Q);
                    PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                    // Reduced size multiplier from 20 to 5 to avoid memory/display issues
                    byte[] qrCodeBytes = qrCode.GetGraphic(5);

                    using (var stream = new MemoryStream(qrCodeBytes))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                        QrCodeImage.Source = image;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar QR Code: {ex.Message}");
            }
        }

        private async void CheckPaymentStatus_Tick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTransactionId)) return;

            // In Real Scenario: call API
            var status = await _paymentService.CheckPaymentStatusAsync(_currentTransactionId);

            if (status == "approved")
            {
                ApprovePayment();
            }
            else if (status == "rejected")
            {
                _paymentCheckTimer.Stop();
                MessageBox.Show("Pagamento não identificado ou recusado.\nGerando novo código...", "Erro no Pagamento");
                GeneratePixQrCode(_currentAmount); // Regenerate
                _paymentCheckTimer.Start();
            }
        }

        private void ApprovePayment()
        {
            _paymentCheckTimer.Stop();
            
            // Grant Access based on plan
            if (_currentAmount == 50.00m)
            {
                _usuario.DataValidadeLicenca = DateTime.Now.AddDays(1);
                _usuario.StatusAssinatura = "Diaria";
            }
            else if (_currentAmount == 400.00m)
            {
                _usuario.DataValidadeLicenca = DateTime.Now.AddDays(30);
                _usuario.StatusAssinatura = "Mensal";
            }
            
            _context.Usuarios.Update(_usuario);
            _context.SaveChanges();

            MessageBox.Show($"Pagamento de {_currentAmount:C} confirmado!\nSeu acesso foi liberado.", "Sucesso");
            
            PaymentSuccess = true;
            this.Close();
        }

        private void CopyPix_Click(object sender, RoutedEventArgs e)
        {
             if (!string.IsNullOrEmpty(PixCopyBox.Text))
             {
                 try 
                 {
                    Clipboard.SetText(PixCopyBox.Text);
                    MessageBox.Show("Código Pix copiado para a área de transferência!");
                 }
                 catch (Exception ex)
                 {
                     MessageBox.Show($"Não foi possível copiar automaticamente: {ex.Message}\n\nTente selecionar o texto e copiar (Ctrl+C).");
                 }
             }
        }

        private void CreditCardButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Integração com cartão de crédito em breve.", "Em Construção");
        }

        private void FreeTrialButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentPanel.Visibility = Visibility.Collapsed;
            FreeTrialPanel.Visibility = Visibility.Visible;
        }

        private void BackToPayment_Click(object sender, RoutedEventArgs e)
        {
            _paymentCheckTimer.Stop();
            FreeTrialPanel.Visibility = Visibility.Collapsed;
            PixPaymentPanel.Visibility = Visibility.Collapsed;
            PaymentPanel.Visibility = Visibility.Visible;
        }

        private async void SubmitTrial_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TrialNameBox.Text) || 
                string.IsNullOrWhiteSpace(TrialPhoneBox.Text) || 
                string.IsNullOrWhiteSpace(TrialReasonBox.Text))
            {
                MessageBox.Show("Preencha Nome, Telefone e Motivo.", "Campos Obrigatórios");
                return;
            }

            try
            {
                // Update User Data
                _usuario.Nome = TrialNameBox.Text;
                _usuario.Cpf = TrialCpfBox.Text;
                _usuario.Telefone = TrialPhoneBox.Text;
                _usuario.MotivoTesteGratis = TrialReasonBox.Text;
                _usuario.SolicitouTesteGratis = true;
                
                // Grant 20 minutes automatically
                _usuario.DataValidadeLicenca = DateTime.Now.AddMinutes(20);
                _usuario.StatusAssinatura = "TesteGratis";
                _usuario.JaUsouTesteGratis = true;

                _context.Usuarios.Update(_usuario);
                await _context.SaveChangesAsync();

                // Send Email
                await _emailService.SendFreeTrialRequestAsync(_usuario.Nome, _usuario.Email, _usuario.Cpf, _usuario.Telefone, _usuario.MotivoTesteGratis);

                MessageBox.Show("Teste Grátis de 20 minutos liberado!\n\nEntraremos em contato para liberar mais tempo.", "Sucesso");
                
                PaymentSuccess = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar solicitação: {ex.Message}");
            }
        }

        private void WhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userInfo = _usuario != null ? $"ID: {_usuario.Id} | Nome: {_usuario.Nome}" : "Usuário Desconhecido";
                var txInfo = !string.IsNullOrEmpty(_currentTransactionId) ? $" | Transação: {_currentTransactionId}" : "";
                var message = Uri.EscapeDataString($"Olá! Estou enviando o comprovante de pagamento do Bingo Management.\n\nDados do Usuário:\n{userInfo}{txInfo}\n\n[Anexar Comprovante Aqui]");
                
                var url = $"https://wa.me/5541987733337?text={message}";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível abrir o WhatsApp: {ex.Message}");
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
