using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Services
{
    public class UserSession
    {
        private Usuario? _originalAdminUser;

        public Usuario? CurrentUser { get; set; }

        // Se estiver impersonando, mantemos as permissões de Admin para poder sair, 
        // mas o "CurrentUser" é o alvo.
        // Espere, o UserSession.IsAdmin é usado para filtrar dados?
        // Sim, usamos '!isAdmin' para filtrar.
        // Se eu impersonar, eu QUERO ser visto como usuário comum (não admin) para ver os dados DELE.
        // Então IsAdmin deve refletir o CurrentUser (o impersonado).
        // Mas a UI precisa saber se sou Admin REAL para mostrar o botão de voltar.
        // Criarei uma propriedade separada IsRealAdmin.

        public bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;
        
        public bool IsRealAdmin => _originalAdminUser != null ? _originalAdminUser.IsAdmin : IsAdmin;

        public bool IsImpersonating => _originalAdminUser != null;

        public event Action? OnSessionChanged;

        public void ImpersonateUser(Usuario targetUser)
        {
            if (_originalAdminUser == null)
            {
                _originalAdminUser = CurrentUser;
            }
            CurrentUser = targetUser;
            OnSessionChanged?.Invoke();
        }

        public void StopImpersonation()
        {
            if (_originalAdminUser != null)
            {
                CurrentUser = _originalAdminUser;
                _originalAdminUser = null;
                OnSessionChanged?.Invoke();
            }
        }
    }
}