using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;

namespace BingoAdmin.UI.Views
{
    public partial class SystemConfigView : UserControl
    {
        private class ConfigModel 
        {
            public string DbStatusText { get; set; } = "Verificando...";
            public Brush DbStatusColor { get; set; } = Brushes.Gray;
        }

        public SystemConfigView()
        {
            InitializeComponent();
            DataContext = new ConfigModel();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                
                var config = builder.Build();

                PixKeyBox.Text = config["PaymentSettings:PixKey"];
                MerchantNameBox.Text = config["PaymentSettings:MerchantName"];
                MerchantCityBox.Text = config["PaymentSettings:MerchantCity"];

                // Inter Config
                InterClientIdBox.Text = config["PaymentSettings:InterClientId"];
                InterClientSecretBox.Password = config["PaymentSettings:InterClientSecret"];
                InterCertBox.Text = config["PaymentSettings:InterCertPath"];
                InterKeyBox.Text = config["PaymentSettings:InterKeyPath"];

                // Check Environment Variable for Azure
                string azureConn = Environment.GetEnvironmentVariable("BINGO_AZURE_CONNECTION");
                if (string.IsNullOrEmpty(azureConn))
                {
                    DbConnectionStringBox.Text = "Nenhuma variável de ambiente encontrada (Usando SQLite Local)";
                    UpdateDbStatus("Local (SQLite)", Brushes.Blue);
                }
                else
                {
                    // Mask password
                    var masked = System.Text.RegularExpressions.Regex.Replace(azureConn, "Password=[^;]+", "Password=******");
                    DbConnectionStringBox.Text = masked;
                    UpdateDbStatus("Configurado (Azure)", Brushes.Orange);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar configurações: {ex.Message}");
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                string json = File.ReadAllText(path);
                
                var jNode = JsonNode.Parse(json);
                if (jNode != null)
                {
                    if (jNode["PaymentSettings"] == null)
                    {
                        jNode["PaymentSettings"] = new JsonObject();
                    }

                    jNode["PaymentSettings"]["PixKey"] = PixKeyBox.Text;
                    jNode["PaymentSettings"]["MerchantName"] = MerchantNameBox.Text;
                    jNode["PaymentSettings"]["MerchantCity"] = MerchantCityBox.Text;

                    // Save Inter
                    jNode["PaymentSettings"]["InterClientId"] = InterClientIdBox.Text;
                    jNode["PaymentSettings"]["InterClientSecret"] = InterClientSecretBox.Password;
                    jNode["PaymentSettings"]["InterCertPath"] = InterCertBox.Text;
                    jNode["PaymentSettings"]["InterKeyPath"] = InterKeyBox.Text;

                    File.WriteAllText(path, jNode.ToString());
                    
                    StatusMessage.Text = "Configurações salvas com sucesso! Reinicie o aplicativo.";
                    StatusMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}");
            }
        }

        private async void TestDb_Click(object sender, RoutedEventArgs e)
        {
            string azureConn = Environment.GetEnvironmentVariable("BINGO_AZURE_CONNECTION");
            if (string.IsNullOrEmpty(azureConn))
            {
                MessageBox.Show("Modo Local (SQLite). Não há banco Azure para testar.");
                return;
            }

            StatusMessage.Text = "Testando conexão...";
            StatusMessage.Foreground = Brushes.Black;
            StatusMessage.Visibility = Visibility.Visible;

            try
            {
                await System.Threading.Tasks.Task.Run(() => 
                {
                    using (var conn = new SqlConnection(azureConn))
                    {
                        conn.Open();
                    }
                });

                UpdateDbStatus("Conectado (Online)", Brushes.Green);
                StatusMessage.Text = "Sucesso! Conexão com Azure estabelecida.";
                StatusMessage.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                UpdateDbStatus("Erro de Conexão", Brushes.Red);
                StatusMessage.Text = $"Falha: {ex.Message}";
                StatusMessage.Foreground = Brushes.Red;
            }
        }

        private void SelectCert_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Certificado (.crt, .pfx)|*.crt;*.pfx|Todos os arquivos (*.*)|*.*",
                Title = "Selecione o Certificado Digital do Banco Inter"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InterCertBox.Text = openFileDialog.FileName;
            }
        }

        private void SelectKey_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Chave Privada (.key)|*.key|Todos os arquivos (*.*)|*.*",
                Title = "Selecione a Chave Privada do Banco Inter"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InterKeyBox.Text = openFileDialog.FileName;
            }
        }

        private void TestInter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InterClientIdBox.Text) || 
                string.IsNullOrEmpty(InterCertBox.Text) || 
                string.IsNullOrEmpty(InterKeyBox.Text))
            {
                MessageBox.Show("Preencha ClientID e selecione os certificados (.crt e .key)", "Configuração Incompleta", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(InterCertBox.Text))
            {
                 MessageBox.Show($"Arquivo de certificado não encontrado: {InterCertBox.Text}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                 return;
            }

            if (!File.Exists(InterKeyBox.Text))
            {
                 MessageBox.Show($"Arquivo de chave não encontrado: {InterKeyBox.Text}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                 return;
            }

            // Implementação futura da chamada real
            MessageBox.Show("Certificados localizados com sucesso locally! \nA validação completa com a API ocorrerá ao salvar e tentar gerar um Pix.", "Check Local OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateDbStatus(string text, Brush color)
        {
            if (DataContext is ConfigModel model)
            {
                model.DbStatusText = text;
                model.DbStatusColor = color;
                
                // Force UI update since we didn't implement INotifyPropertyChanged fully for simplicity
                DataContext = null;
                DataContext = model;
            }
        }
    }
}