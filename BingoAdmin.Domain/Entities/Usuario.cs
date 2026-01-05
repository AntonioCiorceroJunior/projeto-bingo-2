namespace BingoAdmin.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        
        // Dados de Acesso
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;

        // Dados Pessoais
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;

        // Endereço
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // Controle de Licença e Sessão
        public DateTime DataValidadeLicenca { get; set; } = DateTime.MinValue; // Se < Hoje, bloqueado
        public string StatusAssinatura { get; set; } = "Pendente"; // Pendente, Ativa, Cancelada, TesteGratis
        public bool IsLogado { get; set; } = false;
        public DateTime? UltimoAcesso { get; set; }
        
        // Controle de Teste Grátis
        public bool JaUsouTesteGratis { get; set; } = false;
        public bool SolicitouTesteGratis { get; set; } = false;
        public string MotivoTesteGratis { get; set; } = string.Empty;
    }
}
