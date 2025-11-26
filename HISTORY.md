# Histórico do Projeto BingoAdmin

## Visão Geral
Este documento rastreia a evolução do projeto BingoAdmin, detalhando as funcionalidades implementadas e as decisões técnicas tomadas ao longo do desenvolvimento.

## Cronologia de Desenvolvimento

### 1. Configuração Inicial e Estrutura
- **Arquitetura**: Solução dividida em camadas (Domain, Infra, UI) seguindo princípios de separação de responsabilidades.
- **Tecnologia**: .NET 8, WPF (Windows Presentation Foundation).
- **Banco de Dados**: SQLite com Entity Framework Core.
- **Injeção de Dependência**: Configurada no `App.xaml.cs` para serviços e janelas.

### 2. Funcionalidades Principais (Core)
- **Configuração de Bingo**: Tela para configurar os parâmetros gerais do bingo.
- **Gerenciamento de Rodadas**: Criação e edição de rodadas.
- **Combos e Padrões**: Lógica para definir combinações vencedoras e padrões de cartela.
- **Pedra Maior**: Implementação da lógica do jogo "Pedra Maior" (Highest Card), incluindo janela específica (`PedraMaiorWindow`).

### 3. Funcionalidades Avançadas
- **Desempate**: Sistema para gerenciar empates entre jogadores (`DesempateView`, `DesempateService`).
- **Financeiro**: Módulo para controle de despesas e fluxo de caixa (`FinanceiroView`, `FinanceiroService`, Entidade `Despesa`).
- **Resultados e Relatórios**: Telas para visualização de resultados passados e geração de relatórios (`ResultadosView`, `RelatoriosView`).

### 4. Mini Games (Implementação Recente)
- **Objetivo**: Adicionar jogos rápidos de sorteio para entretenimento extra.
- **Jogos Incluídos**:
  - **Jogo do Bicho**: Sorteio entre 25 a 50 animais.
  - **Jogo dos Nomes**: Sorteio entre uma lista de nomes (fixos + adicionais).
- **Experiência de Usuário (UX)**:
  - Fluxo de duas etapas: Configuração -> Sorteio Visual.
  - **Animação**: Interface estilo "Programa de TV" com grid de cartões.
  - **Lógica de Sorteio**:
    - Duração fixa de **10 segundos**.
    - **Curva de Velocidade**: Começa devagar, acelera para alta velocidade, e desacelera gradualmente até parar no vencedor.
    - Feedback visual com destaque (highlight) e indicação clara do vencedor.

## Estado Atual
- Branch atual: `homol`
- O projeto está funcional com todos os módulos acima integrados.
- A interface utiliza `UniformGrid` para layouts de sorteio e `DataTriggers` para animações visuais.
