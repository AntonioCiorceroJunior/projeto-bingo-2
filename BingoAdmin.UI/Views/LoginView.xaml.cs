using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class LoginView : Page
    {
        private readonly UsuarioService? _usuarioService;
        private bool _isLoginMode = true;

        // Default constructor for XAML previewer
        public LoginView() : this(null!) { }

        public LoginView(UsuarioService? usuarioService)
        {
            InitializeComponent();
            _usuarioService = usuarioService;
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
                        // Navigate to Dashboard
                        // We need to resolve DashboardView from DI to ensure its dependencies are met if any
                        var dashboard = ((App)Application.Current).Host.Services.GetRequiredService<DashboardView>();
                        NavigationService.Navigate(dashboard);
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

                    var usuario = _usuarioService?.Cadastrar(nome, email, senha);
                    MessageBox.Show("Cadastro realizado com sucesso! Faça login.");
                    ToggleMode();
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
                ToggleModeButton.Content = "Já tem conta? Faça login";
            }
        }
    }
}
