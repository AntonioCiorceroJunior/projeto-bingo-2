# BingoAdmin

Sistema completo para gestão e execução de Bingos, com interface WPF, backend .NET 8, persistência local em SQLite e funcionalidades de controle financeiro, geração de cartelas, sorteio automatizado e emissão de PDFs.

## Sumário
- [Visão Geral](#visão-geral)
- [Funcionalidades](#funcionalidades)
- [Arquitetura](#arquitetura)
- [Como Rodar](#como-rodar)
- [Estrutura de Pastas](#estrutura-de-pastas)
- [Principais Riscos](#principais-riscos)
- [Roadmap e Dúvidas](#roadmap-e-dúvidas)

---

## Visão Geral
O BingoAdmin é uma solução desktop para criação, venda e execução de bingos, voltada para operadores que precisam de controle total sobre combos, kits, cartelas, pagamentos e sorteios. O sistema é auditado, com documentação técnica e operacional detalhada.

## Funcionalidades
- **Login seguro** com hash de senha (BCrypt)
- **Criação de eventos de bingo** com múltiplos modos de jogo (Clássico, Acumulado, Pé Frio)
- **Geração automática de combos, kits e cartelas** (com hash único por cartela)
- **Controle financeiro**: reserva, confirmação de pagamento (Pix manual), emissão de PDFs
- **Execução do bingo**: sorteio automatizado, conferência instantânea de ganhadores, desempate via Pedra Maior
- **Persistência local**: todos os dados salvos em SQLite
- **Recuperação de sessão**: crash recover automático

## Arquitetura
- **Frontend**: WPF (.NET 8)
- **Backend**: Serviços C# injetados via DI
- **Banco de Dados**: SQLite local (`bingoadmin.db`)
- **PDF**: QuestPDF
- **Utilitário extra**: BingoCleaner (manutenção e integração Azure)

### Principais Pastas
- `BingoAdmin.UI/` — Interface WPF, Views, ViewModels, Services
- `BingoAdmin.Domain/` — Entidades de domínio (Bingo, Cartela, Rodada, Usuario, etc)
- `BingoAdmin.Infra/` — Contexto EF Core, Migrations, Repositórios
- `BingoCleaner/` — Utilitário de manutenção (atenção: contém credenciais Azure)

## Como Rodar
1. **Pré-requisitos**
   - .NET 8 SDK
   - Windows 10+
2. **Restaurar dependências**
   ```sh
   dotnet restore
   ```
3. **Aplicar migrations (opcional, se for rodar do zero)**
   ```sh
   dotnet ef database update --project BingoAdmin.Infra --startup-project BingoAdmin.UI
   ```
4. **Executar o app**
   ```sh
   dotnet run --project BingoAdmin.UI
   ```
5. **(Opcional) Utilitário BingoCleaner**
   - Executar manualmente para manutenção/backup Azure.

## Estrutura de Pastas
```
BingoAdmin.sln
BingoAdmin.UI/         # Interface WPF
BingoAdmin.Domain/     # Entidades de domínio
BingoAdmin.Infra/      # Infraestrutura, EF Core, Migrations
BingoCleaner/          # Utilitário de manutenção
```

## Principais Riscos
- **Credenciais sensíveis** em arquivos de código (ex: AzureConnectionString, PixKey)
- **Senhas salvas em texto plano** (`uistate.json`)
- **Chave Pix hardcoded** (alterar para configuração dinâmica)

## Roadmap e Dúvidas
- [ ] Sincronização com nuvem/Azure (atualmente só local)
- [ ] Bloqueio de login por licença vencida
- [ ] Configuração dinâmica de chaves Pix e Mercado Pago
- [ ] Melhorias de segurança (armazenamento de credenciais)

---

> Para detalhes técnicos completos, consulte o arquivo `DOCUMENTACAO_TECNICA_BINGO_ADMIN.txt`.
