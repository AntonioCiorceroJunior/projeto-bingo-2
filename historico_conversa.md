# Histórico de Conversa e Desenvolvimento

## 25/11/2025 - Implementação dos Mini Games

### Funcionalidades Implementadas:
1.  **Jogo do Bicho**:
    - Interface para gerar lista de animais (25 a 50 opções).
    - Opção de inserir nome do comprador para cada animal.
    - Sorteio visual com grade de cartões.

2.  **Jogo dos Nomes**:
    - Interface para gerar lista de nomes (baseado em lista fixa de 100 nomes).
    - Opção de adicionar novos nomes à lista.
    - Sorteio visual com grade de cartões.

3.  **Animação de Sorteio**:
    - **Duração**: 10 segundos fixos.
    - **Curva de Velocidade**:
        - Início (0-20%): Aceleração gradual (400ms -> 50ms).
        - Meio (20-70%): Velocidade máxima constante (50ms).
        - Fim (70-100%): Desaceleração gradual até parar (50ms -> 600ms).
    - **Feedback Visual**:
        - Item sorteado pisca em Dourado (#FFD700).
        - Vencedor final destacado em Verde (#28a745).

### Arquivos Criados/Modificados:
- `BingoAdmin.UI/Views/MiniGamesView.xaml` (Nova View)
- `BingoAdmin.UI/Views/MiniGamesView.xaml.cs` (Lógica da View)
- `BingoAdmin.UI/App.xaml.cs` (Registro de rotas/serviços se necessário)

### Status:
- Código compilado e testado.
- Alterações salvas na branch `homol`.
