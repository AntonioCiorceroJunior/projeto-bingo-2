using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;
using BingoAdmin.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using BingoAdmin.UI.Services;

namespace BingoAdmin.UI.Views
{
    public partial class UsuariosView : UserControl
    {
        private BingoContext? _context;
        private UserSession? _userSession;
        private BingoContextService? _bingoContextService;

        public UsuariosView()
        {
            InitializeComponent();
            this.Loaded += UsuariosView_Loaded;
        }

        // Constructor for DI
        public UsuariosView(BingoContext context) : this()
        {
            _context = context;
            // LoadUsuarios(); // Wait for Loaded event to avoid issues
        }

        private void UsuariosView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_context == null || _userSession == null)
            {
                if (Application.Current is App app && app.Host != null)
                {
                    _context = app.Host.Services.GetService<BingoContext>();
                    _userSession = app.Host.Services.GetService<UserSession>();
                    _bingoContextService = app.Host.Services.GetService<BingoContextService>();
                }
            }
            
            // Refresh list on load
            LoadUsuarios(SearchBox?.Text ?? "");
        }

        private void LoadUsuarios(string filter = "")
        {
            if (_context != null)
            {
                var query = _context.Usuarios.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    // Case-insensitive filtering
                    // Note: In some DBs like SQL Server default collation is CI, but let's be safe
                    query = query.Where(u => u.Nome.Contains(filter) || u.Email.Contains(filter)); 
                }

                var usuarios = query.OrderByDescending(u => u.Id).ToList();
                UsuariosGrid.ItemsSource = usuarios;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUsuarios(SearchBox.Text);
        }

        private void SearchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoadUsuarios(SearchBox.Text);
            }
        }

        private void Impersonate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                if (_context != null && _userSession != null)
                {
                    var targetUser = _context.Usuarios.Find(userId);
                    if (targetUser != null)
                    {
                        if (MessageBox.Show($"Deseja acessar o painel como '{targetUser.Nome}'?\n\nVocê verá exatamente o que este usuário vê.", "Acesso Administrativo", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            _userSession.ImpersonateUser(targetUser);
                            
                            // Notify system updates
                            _bingoContextService?.NotifyBingoListUpdated();
                            
                            MessageBox.Show($"Você agora está acessando como {targetUser.Nome}.\nUtilize a barra superior para sair deste modo.", "Modo Espião Ativado", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Try to switch tab via VisualTree lookup or service?
                            // easier: just refresh the UI state will happen automatically due to events in other views.
                        }
                    }
                }
            }
        }

        private void Add30Days_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                UpdateUserLicense(userId, 30, "Ativa");
            }
        }

        private void Add1Day_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                UpdateUserLicense(userId, 1, "Ativa");
            }
        }

        private void BlockUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                // Disable license by setting date to past
                UpdateUserLicense(userId, -999, "Cancelada");
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                if (MessageBox.Show("Tem certeza que deseja EXCLUIR este usuário permanentemente?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (_context == null) return;
                        var user = _context.Usuarios.Find(userId);
                        if (user != null)
                        {
                            _context.Usuarios.Remove(user);
                            _context.SaveChanges();
                            LoadUsuarios(SearchBox.Text);
                            MessageBox.Show("Usuário excluído com sucesso.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao excluir: {ex.Message}");
                    }
                }
            }
        }

        private void UpdateUserLicense(int userId, int daysToAdd, string status)
        {
            try
            {
                if (_context == null) return;

                var user = _context.Usuarios.Find(userId);
                if (user != null)
                {
                    if (daysToAdd == -999) // Block logic
                    {
                        user.DataValidadeLicenca = DateTime.Now.AddDays(-1);
                    }
                    else
                    {
                        // If expired, add from now. If active, add to current expiry.
                        if (user.DataValidadeLicenca < DateTime.Now)
                            user.DataValidadeLicenca = DateTime.Now.AddDays(daysToAdd);
                        else
                            user.DataValidadeLicenca = user.DataValidadeLicenca.AddDays(daysToAdd);
                    }
                    
                    user.StatusAssinatura = status;
                    
                    _context.SaveChanges();
                    LoadUsuarios(SearchBox.Text); // Refresh list applying current filter
                    MessageBox.Show($"Usuário {user.Nome} atualizado com sucesso!", "Sucesso");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar usuário: {ex.Message}");
            }
        }
    }
}