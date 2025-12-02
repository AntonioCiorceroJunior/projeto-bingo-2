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

### 5. Reformulação de UI e Feed (Atual)
- **Contexto Global**: Implementação do `BingoContextService` para sincronizar a seleção do Bingo entre todas as abas.
- **Feed Avançado**:
  - **Interatividade**: Itens do feed agora são expansíveis (clique para ver detalhes).
  - **Histórico**: Persistência de logs e capacidade de recarregar histórico por Bingo.
  - **Layout**: Painel lateral dividido em seções funcionais.
- **Dashboard Dashboard**:
  - **Status do Jogo**: Visualização de cronômetros (Sorteio Automático/Regressiva) no topo do painel.
  - **Últimos Sorteios**: Nova seção dedicada exibindo as últimas bolas sorteadas em formato visual (`B | 05`).
- **Correções Técnicas**:
  - Substituição de propriedades inválidas (`MaxLines` -> `MaxHeight`) no XAML.
  - Refatoração de serviços para suportar injeção de dependência circular (via propriedades ou inicialização tardia).

## Estado Atual
- Branch atual: `homol_1.1v`
- O projeto está funcional com todos os módulos acima integrados.
- A interface utiliza `UniformGrid` para layouts de sorteio e `DataTriggers` para animações visuais.
