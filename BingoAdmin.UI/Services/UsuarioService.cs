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
                return usuario;
            }

            return null;
        }

        public Usuario Cadastrar(string nome, string email, string senha)
        {
            if (_context.Usuarios.Any(u => u.Email == email))
            {
                throw new Exception("E-mail já cadastrado.");
            }

            var usuario = new Usuario
            {
                Nome = nome,
                Email = email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha)
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return usuario;
        }
    }
}
