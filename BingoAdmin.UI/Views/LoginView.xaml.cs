using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BingoAdmin.UI.Services;
using BingoAdmin.Infra.Data;
using Microsoft.Extensions.DependencyInjection;

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
            InitializeComponent();
            _usuarioService = usuarioService;
            _context = context;
            _emailService = emailService;
            _serviceProvider = serviceProvider;
            _userSession = userSession ?? serviceProvider?.GetService<UserSession>();
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

                        // Check License
                        if (usuario.DataValidadeLicenca < DateTime.Now)
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
                MessageBox.Show($"Erro: {ex.Message}");
            }
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
                ToggleModeButton.Content = "Não tem conta? Cadastre-se";
            }
            else
            {
                NamePanel.Visibility = Visibility.Visible;
                ActionButton.Content = "Cadastrar";
                ToggleModeButton.Content = "Já tem conta? Entre";
            }
        }
    }
}
