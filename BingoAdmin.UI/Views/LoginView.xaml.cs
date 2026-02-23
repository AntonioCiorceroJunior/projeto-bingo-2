using System;
using System.IO; // Added
using System.Text.Json; // Added
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Security.Cryptography; // Added for DPAPI
using System.Text; // Added for Encoding
using BingoAdmin.UI.Services;
using BingoAdmin.Infra.Data;
using Microsoft.Extensions.DependencyInjection;
using BingoAdmin.UI; // For UiState

namespace BingoAdmin.UI.Views
{
    public partial class LoginView : Page
    {
        private readonly UsuarioService? _usuarioService;
        private readonly BingoContext? _context;
        private readonly EmailService? _emailService;
        private readonly IServiceProvider? _serviceProvider;
        private readonly UserSession? _userSession;
        private bool _isLoginMode = true;

        // Default constructor for XAML previewer
        public LoginView() : this(null!, null!, null!, null!, null!) { }

        public LoginView(UsuarioService? usuarioService, BingoContext? context, EmailService? emailService, IServiceProvider? serviceProvider, PaymentService? paymentService = null, UserSession? userSession = null)
        {
            try
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - LoginView Constructor Started.\n");
                InitializeComponent();
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - LoginView InitializeComponent Done.\n");

                this.Loaded += LoginView_Loaded;

                _usuarioService = usuarioService;
                _context = context;
                _emailService = emailService;
                _serviceProvider = serviceProvider;
                _userSession = userSession ?? serviceProvider?.GetService<UserSession>();
                
                LoadCredentials(); // Load saved fields
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - LoginView Constructor Completed.\n");
            }
            catch (Exception ex)
            {
                 File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - ERROR IN LOGINVIEW CTOR: {ex}\n");
                 throw;
            }
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
             try 
             {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - LoginView_Loaded FIRED. UI should be visible.\n");
             } 
             catch (Exception ex)
             {
                 File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - ERROR IN LOGINVIEW LOADED: {ex}\n");
             }
        }

        private void LoadCredentials()
        {
            try
            {
                if (File.Exists("uistate.json"))
                {
                    var json = File.ReadAllText("uistate.json");
                    var state = JsonSerializer.Deserialize<UiState>(json);
                    
                    if (state != null && state.RememberMe)
                    {
                        EmailBox.Text = state.SavedEmail;
                        
                        if (!string.IsNullOrEmpty(state.SavedPassword))
                        {
                            try
                            {
                                // Decrypt password using Windows DPAPI
                                byte[] protectedBytes = Convert.FromBase64String(state.SavedPassword);
                                byte[] bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                                PasswordBox.Password = Encoding.UTF8.GetString(bytes);
                            }
                            catch
                            {
                                // If decryption fails (e.g. valid old plain text or corrupted), clear it for safety
                                PasswordBox.Password = "";
                            }
                        }
                        
                        RememberMeCheckBox.IsChecked = true;
                    }
                }
            }
            catch { }
        }

        private void SaveCredentials(string email, string password, bool remember)
        {
            try
            {
                UiState state = new UiState();
                if (File.Exists("uistate.json"))
                {
                    var json = File.ReadAllText("uistate.json");
                    state = JsonSerializer.Deserialize<UiState>(json) ?? new UiState();
                }

                state.RememberMe = remember;
                state.SavedEmail = remember ? email : "";
                
                if (remember && !string.IsNullOrEmpty(password))
                {
                    try
                    {
                        // Encrypt password using Windows DPAPI
                        byte[] bytes = Encoding.UTF8.GetBytes(password);
                        byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                        state.SavedPassword = Convert.ToBase64String(protectedBytes);
                    }
                    catch
                    {
                        state.SavedPassword = "";
                    }
                }
                else
                {
                    state.SavedPassword = "";
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var newJson = JsonSerializer.Serialize(state, options);
                File.WriteAllText("uistate.json", newJson);
            }
            catch { }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text;
            string senha = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show("Preencha todos os campos.");
                return;
            }

            try
            {
                if (_isLoginMode)
                {
                    var usuario = _usuarioService?.Login(email, senha);
                    if (usuario != null)
                    {
                        if (_userSession != null)
                        {
                            _userSession.CurrentUser = usuario;
                        }

                        // Save Credentials if "Remember Me" is checked
                        SaveCredentials(email, senha, RememberMeCheckBox.IsChecked == true);

                        // Check License (Skip for Admins)
                        if (!usuario.IsAdmin && usuario.DataValidadeLicenca < DateTime.Now)
                        {
                            if (_context != null && _emailService != null && _serviceProvider != null)
                            {
                                var paymentService = _serviceProvider.GetRequiredService<PaymentService>();
                                var paymentWindow = new PaymentWindow(usuario, _context, _emailService, paymentService);
                                paymentWindow.ShowDialog();

                                if (!paymentWindow.PaymentSuccess)
                                {
                                    return; // User closed without paying
                                }
                            }
                        }

                        // Navigate to Dashboard
                        if (_serviceProvider != null)
                        {
                            var dashboard = _serviceProvider.GetRequiredService<DashboardView>();
                            NavigationService.Navigate(dashboard);
                        }
                    }
                    else
                    {
                        MessageBox.Show("E-mail ou senha inválidos.");
                        ForgotPasswordButton.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    string nome = NameBox.Text;
                    if (string.IsNullOrWhiteSpace(nome))
                    {
                        MessageBox.Show("Preencha o nome.");
                        return;
                    }

                    // Passing empty strings for now as requested, will be replaced by full registration form later
                    var usuario = _usuarioService?.Cadastrar(nome, email, senha, "", "", "", "", "", "", "", "", "");
                    
                    if (usuario != null)
                    {
                        // Force expired to show modal on first login
                        usuario.DataValidadeLicenca = DateTime.Now.AddDays(-1); 
                        usuario.StatusAssinatura = "Pendente";
                        _context?.Usuarios.Update(usuario);
                        _context?.SaveChanges();

                        MessageBox.Show("Cadastro realizado! Faça login para ativar sua conta.");
                        ToggleMode();
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    msg += $"\n ---> {inner.Message}";
                    inner = inner.InnerException;
                }

                try
                {
                    File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - ERROR IN LOGIN CLICK: {msg}\nSTACK: {ex.StackTrace}\n");
                }
                catch { }

                MessageBox.Show($"Erro Detalhado:\n{msg}\n\nStack Trace:\n{ex.StackTrace}", "Erro de Login");
            }
        }

        private async void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text;
            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Por favor, informe seu e-mail para recuperar a senha.");
                return;
            }

            string? token = null;
            try 
            {
                token = _usuarioService?.GerarTokenRecuperacao(email);

                if (token == null)
                {
                    MessageBox.Show("Esse email ainda não foi cadastrado.");
                }
                else
                {
                    if (_emailService != null)
                    {
                         await _emailService.SendPasswordResetCodeAsync(email, token);
                         MessageBox.Show($"Código de verificação enviado para {email}.");
                         
                         // Switch UI
                         PasswordPanel.Visibility = Visibility.Collapsed;
                         ActionButton.Visibility = Visibility.Collapsed;
                         ForgotPasswordButton.Visibility = Visibility.Collapsed;
                         RememberMeCheckBox.Visibility = Visibility.Collapsed;
                         ResetPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                         MessageBox.Show("Serviço de email indisponível.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback para testes: Mostra o código na tela caso o envio de e-mail falhe (comum com Gmail sem App Password)
                MessageBox.Show($"Falha no envio de e-mail (Bloqueio do Google/Erro SMTP).\n\nCÓDIGO DE RECUPERAÇÃO (TESTE): {token}\n\nDetalhe do erro: {ex.Message}");
                
                // Permite continuar o fluxo mesmo sem e-mail
                PasswordPanel.Visibility = Visibility.Collapsed;
                ActionButton.Visibility = Visibility.Collapsed;
                ForgotPasswordButton.Visibility = Visibility.Collapsed;
                RememberMeCheckBox.Visibility = Visibility.Collapsed;
                ResetPanel.Visibility = Visibility.Visible;
            }
        }

        private void ConfirmResetButton_Click(object sender, RoutedEventArgs e)
        {
             string email = EmailBox.Text;
             string code = CodeBox.Text;
             string newPass = NewPasswordBox.Password;

             bool success = _usuarioService?.ValidarTokenEAlterarSenha(email, code, newPass) ?? false;

             if (success)
             {
                 MessageBox.Show("Senha redefinida com sucesso! Faça login.");
                 CancelResetButton_Click(null, null);
             }
             else
             {
                 MessageBox.Show("Código inválido ou expirado.");
             }
        }

        private void CancelResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPanel.Visibility = Visibility.Collapsed;
            PasswordPanel.Visibility = Visibility.Visible;
            ActionButton.Visibility = Visibility.Visible;
            ForgotPasswordButton.Visibility = Visibility.Collapsed;
            RememberMeCheckBox.Visibility = Visibility.Visible;
            
            CodeBox.Text = "";
            NewPasswordBox.Password = "";
        }

        private void ToggleModeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMode();
        }

        private void ToggleMode()
        {
            _isLoginMode = !_isLoginMode;
            if (_isLoginMode)
            {
                NamePanel.Visibility = Visibility.Collapsed;
                ActionButton.Content = "Entrar";
                ToggleButton.Content = "Não tem conta? Cadastre-se";
            }
            else
            {
                NamePanel.Visibility = Visibility.Visible;
                ActionButton.Content = "Cadastrar";
                ToggleButton.Content = "Já tem conta? Entre";
            }
        }
    }
}
