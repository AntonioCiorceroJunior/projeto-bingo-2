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
        
        private const bool UseMercadoPago = false; // MUDE PARA TRUE APÓS COLOCAR O TOKEN
        private const string MpAccessToken = "SEU_ACCESS_TOKEN_AQUI"; 
        
        // Configuração Manual (Chave Pix Pessoal)
        private const string PixKey = "+5541987733337"; 
        private const string MerchantName = "Bingo Admin";
        private const string MerchantCity = "Curitiba";
        
        private readonly HttpClient _httpClient = new HttpClient();

        public PaymentResponse CreatePixPayment(decimal amount, string description)
        {
            if (UseMercadoPago)
            {
                // Implementação Real via API (Síncrona por simplicidade, idealmente Async)
                try {
                    return CreateMercadoPagoPreference(amount, description).Result;
                } catch {
                    // Fallback para manual se falhar
                    return CreateManualPix(amount);
                }
            }
            else
            {
                return CreateManualPix(amount);
            }
        }

        private PaymentResponse CreateManualPix(decimal amount)
        {
            var txId = "BINGO" + DateTime.Now.Ticks.ToString().Substring(10);
            var payload = PixGenerator.GeneratePayload(PixKey, amount, MerchantName, MerchantCity, txId);

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
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mercadopago.com/v1/payments");
            request.Headers.Add("Authorization", $"Bearer {MpAccessToken}");

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
            if (UseMercadoPago)
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