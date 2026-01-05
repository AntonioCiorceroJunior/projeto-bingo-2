using System;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using BCrypt.Net;

namespace BingoAdmin.UI.Services
{
    public class UsuarioService
    {
        private readonly BingoContext _context;

        public UsuarioService(BingoContext context)
        {
            _context = context;
        }

        public Usuario? Login(string email, string senha)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
            if (usuario == null) return null;

            if (BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash))
            {
                // Atualiza status de login
                usuario.IsLogado = true;
                usuario.UltimoAcesso = DateTime.Now;
                _context.SaveChanges();
                return usuario;
            }

            return null;
        }

        public void Logout(int usuarioId)
        {
            var usuario = _context.Usuarios.Find(usuarioId);
            if (usuario != null)
            {
                usuario.IsLogado = false;
                _context.SaveChanges();
            }
        }

        public Usuario Cadastrar(string nome, string email, string senha, string cpf, string telefone, 
                               string cep, string logradouro, string numero, string complemento, 
                               string bairro, string cidade, string estado)
        {
            if (_context.Usuarios.Any(u => u.Email == email))
            {
                throw new Exception("E-mail já cadastrado.");
            }

            var usuario = new Usuario
            {
                Nome = nome,
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha),
                Cpf = cpf,
                Telefone = telefone,
                Cep = cep,
                Logradouro = logradouro,
                Numero = numero,
                Complemento = complemento,
                Bairro = bairro,
                Cidade = cidade,
                Estado = estado,
                DataValidadeLicenca = DateTime.Now.AddDays(3), // 3 dias de teste grátis
                StatusAssinatura = "TesteGratis",
                IsAdmin = false
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return usuario;
        }

        public void EnsureAdminUser()
        {
            var adminEmail = "adminciorcero"; // Usando como username/email
            if (!_context.Usuarios.Any(u => u.Email == adminEmail))
            {
                var admin = new Usuario
                {
                    Nome = "Administrador Ciorcero",
                    Email = adminEmail,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("ciorcero001"),
                    IsAdmin = true,
                    StatusAssinatura = "Vitalicia",
                    DataValidadeLicenca = DateTime.MaxValue,
                    Cpf = "00000000000",
                    Telefone = "00000000000"
                };
                _context.Usuarios.Add(admin);
                _context.SaveChanges();
            }
        }
    }
}
