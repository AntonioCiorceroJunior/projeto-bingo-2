using System;
using System.Linq;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Cleaning AZURE Database...");
            
            var optionsBuilder = new DbContextOptionsBuilder<BingoContext>();
            
            // SECURITY FIX: Read from Environment Variable or prompt user. Do not hardcode credentials.
            var azureConnectionString = Environment.GetEnvironmentVariable("BINGO_AZURE_CONNECTION") 
                                      ?? "Server=tcp:YOUR_SERVER.database.windows.net;Database=BingoDB;User Id=user;Password=password;";
            
            if (azureConnectionString.Contains("YOUR_SERVER"))
            {
                Console.WriteLine("CRITICAL: Azure Connection String not configured. Please set BINGO_AZURE_CONNECTION env var.");
                return;
            }

            optionsBuilder.UseSqlServer(azureConnectionString);

            using (var context = new BingoContext(optionsBuilder.Options))
            {
                try {
                    // Force connection
                    context.Database.OpenConnection();
                    Console.WriteLine("Connected to Azure.");

                    // Manually ensure Table Exists because EF Migrations on Azure might be stuck
                    Console.WriteLine("Ensuring Kits table exists on Azure...");
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Kits]') AND type in (N'U'))
                        BEGIN
                            CREATE TABLE [dbo].[Kits](
                                [Id] [int] IDENTITY(1,1) NOT NULL,
                                [ComboId] [int] NOT NULL,
                                [NumeroKitNoCombo] [int] NOT NULL,
                                CONSTRAINT [PK_Kits] PRIMARY KEY CLUSTERED ([Id] ASC)
                            );
                            
                            ALTER TABLE [dbo].[Kits] WITH CHECK ADD CONSTRAINT [FK_Kits_Combos_ComboId] FOREIGN KEY([ComboId])
                            REFERENCES [dbo].[Combos] ([Id])
                            ON DELETE CASCADE;
                            
                            ALTER TABLE [dbo].[Kits] CHECK CONSTRAINT [FK_Kits_Combos_ComboId];
                            
                            CREATE NONCLUSTERED INDEX [IX_Kits_ComboId] ON [dbo].[Kits] ([ComboId] ASC);
                        END
                    ");

                    Console.WriteLine("Updating Bingos table schema...");
                    context.Database.ExecuteSqlRaw(@"
                        IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'TemCombos' AND Object_ID = Object_ID(N'Bingos'))
                        BEGIN
                            ALTER TABLE Bingos ADD TemCombos bit NOT NULL DEFAULT 1;
                        END

                        IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'KitsPorCombo' AND Object_ID = Object_ID(N'Bingos'))
                        BEGIN
                            ALTER TABLE Bingos ADD KitsPorCombo int NOT NULL DEFAULT 1;
                        END

                        IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'CartelasPorKit' AND Object_ID = Object_ID(N'Bingos'))
                        BEGIN
                            ALTER TABLE Bingos ADD CartelasPorKit int NOT NULL DEFAULT 1;
                        END

                        IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'QuantidadeRodadas' AND Object_ID = Object_ID(N'Bingos'))
                        BEGIN
                            ALTER TABLE Bingos ADD QuantidadeRodadas int NOT NULL DEFAULT 0;
                        END
                    ");

                    /* Lines 42-95 omitted */
                    // Delete data from child tables first to avoid FK constraints
                    Console.WriteLine("Deleting DesempateItens...");
                    context.Database.ExecuteSqlRaw("DELETE FROM DesempateItens");

                    Console.WriteLine("Deleting Ganhadores...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Ganhadores");

                    Console.WriteLine("Deleting Cartelas...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Cartelas");
                    
                    Console.WriteLine("Deleting Kits...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Kits");

                    // Rodadas often reference Sorteios, and Ganhadores reference Rodadas.
                    // Rodadas are children of Sorteios.
                    Console.WriteLine("Deleting RodadaPadroes...");
                    context.Database.ExecuteSqlRaw("DELETE FROM RodadaPadroes");

                    Console.WriteLine("Deleting Rodadas...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Rodadas");
                    
                    Console.WriteLine("Deleting Combos...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Combos");

                    // Sorteios usually root of the game instance
                    Console.WriteLine("Deleting Sorteios...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Sorteios");

                    Console.WriteLine("Deleting Usuarios...");
                    context.Database.ExecuteSqlRaw("DELETE FROM Usuarios");

                    Console.WriteLine("Azure Database Cleaned Successfully!");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
    }
}
