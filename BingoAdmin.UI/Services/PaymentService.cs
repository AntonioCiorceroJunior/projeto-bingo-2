using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Helpers;

namespace BingoAdmin.UI.Services
{
    public class PaymentService
    {
        // -------------------------------------------------------------
        // CONFIGURAÇÃO MERCADO PAGO (AUTOMÁTICO)
        // -------------------------------------------------------------
        // Para ativar o modo automático:
        // 1. Crie uma conta no Mercado Pago Developers
        // 2. Gere uma credencial de Produção
        // 3. Cole o 'Access Token' abaixo e mude UseMercadoPago = true;
        
        private readonly IConfiguration _configuration;
        private readonly BancoInterService _bancoInterService;
        private readonly HttpClient _httpClient = new HttpClient(); // For MercadoPago

        public PaymentService(IConfiguration configuration, BancoInterService bancoInterService)
        {
            _configuration = configuration;
            _bancoInterService = bancoInterService;
        }

        public async Task<PaymentResponse> CreatePixPayment(decimal amount, string description)
        {
            // 1. Tenta Banco Inter
            string interClient = _configuration["PaymentSettings:InterClientId"];
            if (!string.IsNullOrEmpty(interClient))
            {
                try 
                {
                    // Usa CPF/Nome genérico ou do usuário logado se disponível (aqui usamos genérico por simplicidade)
                    // TODO: Passar dados reais do usuário pagador
                    var (txId, copyPaste) = await _bancoInterService.CriarCobrancaPixImediata("12345678909", "Cliente Bingo", amount);
                    
                    return new PaymentResponse
                    {
                        TransactionId = "INTER-" + txId, // Prefixo INTER- pra saber a origem depois
                        CopyPasteCode = copyPaste,
                        Amount = amount,
                        Status = "pending",
                        Message = "Pagamento via Banco Inter (Automático)"
                    };
                }
                catch (Exception ex)
                {
                   System.Diagnostics.Debug.WriteLine($"Erro Banco Inter: {ex.Message}");
                }
            }

            // 2. Tenta Mercado Pago
            bool useMercadoPago = !string.IsNullOrEmpty(_configuration["PaymentSettings:MercadoPagoAccessToken"]);
            if (useMercadoPago)
            {
                try 
                {
                    return await CreateMercadoPagoPreference(amount, description);
                } 
                catch 
                {
                    // Fallback para manual se falhar
                    return CreateManualPix(amount);
                }
            }

            // 3. Fallback Manual
            return CreateManualPix(amount);
        }

        private PaymentResponse CreateManualPix(decimal amount)
        {
            var pixKey = _configuration["PaymentSettings:PixKey"];
            var merchantName = _configuration["PaymentSettings:MerchantName"];
            var merchantCity = _configuration["PaymentSettings:MerchantCity"];

            if (string.IsNullOrEmpty(pixKey) || pixKey.Contains("00000000"))
            {
                return new PaymentResponse 
                { 
                    Status = "error", 
                    Message = "Chave Pix não configurada no appsettings.json" 
                };
            }

            var txId = "BINGO" + DateTime.Now.Ticks.ToString().Substring(10);
            var payload = PixGenerator.GeneratePayload(pixKey, amount, merchantName, merchantCity, txId);

            return new PaymentResponse
            {
                TransactionId = txId,
                CopyPasteCode = payload,
                Amount = amount,
                Status = "pending",
                Message = "Pagamento via Chave Pessoal (Verificação Manual)"
            };
        }

        private async Task<PaymentResponse> CreateMercadoPagoPreference(decimal amount, string description)
        {
            var token = _configuration["PaymentSettings:MercadoPagoAccessToken"];
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/v1/payments");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var body = new
            {
                transaction_amount = amount,
                payment_method_id = "pix",
                description = description,
                payer = new {
                    email = "comprador@email.com" // Em produção, pegar do usuário
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var id = root.GetProperty("id").ToString();
                var qrCode = root.GetProperty("point_of_interaction")
                                 .GetProperty("transaction_data")
                                 .GetProperty("qr_code").GetString();
                
                return new PaymentResponse
                {
                    TransactionId = id,
                    CopyPasteCode = qrCode ?? "",
                    Amount = amount,
                    Status = "pending"
                };
            }
            
            throw new Exception("Falha ao criar Pix no Mercado Pago");
        }

        public async Task<string> CheckPaymentStatusAsync(string transactionId)
        {
            // 1. Banco Inter
            if (transactionId.StartsWith("INTER-"))
            {
                try 
                {
                    string realTxId = transactionId.Replace("INTER-", "");
                    string status = await _bancoInterService.ConsultarStatusPix(realTxId);
                    
                    // Mapear status do Inter para o app
                    if (status.ToUpper() == "CONCLUIDA") return "approved";
                    if (status.ToUpper() == "ATIVA") return "pending";
                    return "rejected";
                }
                catch 
                {
                    return "pending";
                }
            }

            // 2. Mercado Pago
            bool useMercadoPago = !string.IsNullOrEmpty(_configuration["PaymentSettings:MercadoPagoAccessToken"]);
            if (useMercadoPago)
            {
                try 
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.mercadopago.com/v1/payments/{transactionId}");
                    request.Headers.Add("Authorization", $"Bearer {MpAccessToken}");

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var doc = JsonDocument.Parse(json);
                        var status = doc.RootElement.GetProperty("status").GetString();
                        
                        // Mercado Pago retorna "approved" quando pago
                        return status ?? "pending";
                    }
                }
                catch 
                {
                    return "pending";
                }
            }

            // Modo Manual (Simulação)
            await Task.Delay(500); 
            return "pending"; 
        }
    }

    public class PaymentResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CopyPasteCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "pending";
        public string Message { get; set; } = string.Empty;
    }
}