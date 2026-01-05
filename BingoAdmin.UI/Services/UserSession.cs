using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Services
{
    public class UserSession
    {
        public Usuario? CurrentUser { get; set; }

        public bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;
    }
}