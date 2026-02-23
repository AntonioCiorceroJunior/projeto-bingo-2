using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace BingoAdmin.UI.Services
{
    public class BancoInterService
    {
        private readonly IConfiguration _configuration;
        private DateTime _tokenExpiration;
        private string _accessToken;

        private const string BASE_URL = "https://cdpj.partners.bancointer.com.br"; // URL de Produção
        // Para Sandbox use: "https://cdpj-sandbox.partners.bancointer.com.br"

        public BancoInterService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private HttpClient CreateHttpClient()
        {
            var certPath = _configuration["PaymentSettings:InterCertPath"];
            var keyPath = _configuration["PaymentSettings:InterKeyPath"];
            var handler = new HttpClientHandler();

            if (!string.IsNullOrEmpty(certPath) && System.IO.File.Exists(certPath) && 
                !string.IsNullOrEmpty(keyPath) && System.IO.File.Exists(keyPath))
            {
                try 
                {
                    // Tenta criar usando o par CRT + KEY (Formato PEM)
                    // Disponível no .NET 5+ (o projeto usa .NET 8)
                    var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                    handler.ClientCertificates.Add(cert);
                }
                catch (Exception ex)
                {
                    // Se falhar (ex: formato PFX antigo ou senha), logar ou tentar apenas CRT
                    Console.WriteLine($"Erro ao carregar certificado PEM: {ex.Message}");
                    try 
                    {
                         // Fallback para apenas CRT (se o KEY estiver embutido ou for PFX renomeado)
                         var cert = new X509Certificate2(certPath);
                         handler.ClientCertificates.Add(cert);
                    }
                    catch { /* Falha total de certificado */ }
                }
            }

            return new HttpClient(handler) { BaseAddress = new Uri(BASE_URL) };
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiration)
                return _accessToken;

            var clientId = _configuration["PaymentSettings:InterClientId"];
            var clientSecret = _configuration["PaymentSettings:InterClientSecret"];
            
            using var client = CreateHttpClient();
            
            var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/v2/token");
            var form = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>
            {
                new("client_id", clientId),
                new("client_secret", clientSecret),
                new("grant_type", "client_credentials"),
                new("scope", "boleto-cobranca.read boleto-cobranca.write pix.read pix.write") 
            };
            request.Content = new FormUrlEncodedContent(form);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiration = DateTime.Now.AddSeconds(expiresIn - 60);

            return _accessToken;
        }

        public async Task<(string txId, string copiaECola)> CriarCobrancaPixImediata(string cpf, string nome, decimal valor)
        {
            var token = await GetAccessTokenAsync();
            using var client = CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var txId = GenerateTxId();
            
            var payload = new 
            {
                calendario = new { expiracao = 3600 },
                devedor = new { cpf = cpf, nome = nome },
                valor = new { original = valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                chave = _configuration["PaymentSettings:PixKey"], // Chave Pix cadastrada no Inter
                solicitacaoPagador = "Cartela de Bingo"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/pix/v2/cob/{txId}", jsonContent);
            
            response.EnsureSuccessStatusCode();
            
            var respJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(respJson);
            
            // Retorna o "Pix Copy and Paste" e o TxId
            return (txId, doc.RootElement.GetProperty("pixCopiaECola").GetString());
        }

        public async Task<string> ConsultarStatusPix(string txId)
        {
            var token = await GetAccessTokenAsync();
            using var client = CreateHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/pix/v2/cob/{txId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            
            // Status possíveis: ATIVA, CONCLUIDA, REMOVIDA_PELO_USUARIO_RECEBEDOR, REMOVIDA_PELO_PSP
            return doc.RootElement.GetProperty("status").GetString();
        }

        private string GenerateTxId()
        {
            // O txId deve ter entre 26 e 35 caracteres alfanuméricos
            var guid = Guid.NewGuid().ToString("N"); // 32 chars
            return "BINGO" + guid.Substring(0, 25); 
        }
    }
}
