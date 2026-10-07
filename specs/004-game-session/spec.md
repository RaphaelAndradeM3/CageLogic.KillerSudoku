# FEATURE SPEC: Sessão de Jogo Killer Sudoku

- **Feature Branch**: A definir na implementação
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD RF-001 a RF-013 e RF-016 a RF-020; tela e desenho do tabuleiro em PRD. Depende de 001-killer-sudoku-rules, 002-puzzle-engine e 003-logical-hints.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** O jogador precisa iniciar e completar uma partida Killer Sudoku usando os controles apropriados ao dispositivo. Uma experiência que bloqueia a tela, esconde as regras do puzzle ou perde o estado durante a interação impede o uso diário do produto.
>
> **Definição de sucesso:** O jogador pode iniciar e jogar uma partida em Windows e Android, selecionar células, inserir respostas ou candidatos, desfazer e refazer ações, acompanhar o tempo e concluir o puzzle.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; a interface coordena a partida, mas não implementa regras, cálculo de candidatos, solver ou geração.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Jogar em desktop ou dispositivo móvel (Priority: P1)

O jogador inicia um puzzle e interage com o tabuleiro por mouse/toque e controles apropriados à plataforma.

**Why this priority**: A partida é o fluxo central do aplicativo multiplataforma.

**Independent Test**: Completar o mesmo fluxo essencial em Windows e Android: abrir partida, selecionar célula, inserir valor e receber estado visual atualizado.

**Acceptance Scenarios**:

1. **Given** um puzzle disponível, **When** o jogador o inicia, **Then** a grade 9×9, os limites de cage e os alvos ficam compreensíveis.
2. **Given** uma célula editável selecionada, **When** o jogador informa um valor, **Then** o estado e o feedback refletem as regras atuais.
3. **Given** o jogador está em Windows ou Android, **When** usa o mecanismo de entrada esperado na plataforma, **Then** consegue selecionar e alterar células sem depender de conexão.

### User Story 2 - Controlar e concluir uma partida (Priority: P1)

O jogador pode usar candidatos, corrigir ações e compreender o fim da partida.

**Why this priority**: Notas, histórico e conclusão são necessários para jogar puzzles de duração longa.

**Independent Test**: Alternar entre resposta e candidato, inserir/apagar valores, desfazer/refazer, pausar e concluir um puzzle de teste.

**Acceptance Scenarios**:

1. **Given** modo resposta, **When** o jogador informa um dígito, **Then** ele é inserido como resposta; no modo candidatos, o mesmo controle altera apenas as notas.
2. **Given** uma ação reversível, **When** o jogador desfaz ou refaz, **Then** o tabuleiro retorna exatamente ao estado anterior ou posterior.
3. **Given** uma partida em andamento, **When** o jogador pausa e retoma, **Then** o cronômetro acompanha o tempo ativo da partida.
4. **Given** todas as células estão corretas, **When** o puzzle é concluído, **Then** a partida termina e um resumo é apresentado.
5. **Given** uma dica foi calculada para a revisão `r` e uma jogada altera o tabuleiro antes da exibição, **When** a resposta da revisão `r` chega, **Then** a sessão a descarta, solicita uma nova análise do snapshot atual desde o nível 1 e exibe somente o resultado da revisão atual ou a mensagem de que não há dica segura.

### Edge Cases

- Seleção de uma célula fixa do puzzle.
- Inserção de valor já conflitante, apagamento de célula vazia e undo/redo sem ações.
- Alternar modo candidato/resposta sem substituir conteúdo incorretamente.
- Pausa, suspensão do app, rotação/redimensionamento e retorno do segundo plano.
- Entrada simultânea ou repetida enquanto uma operação de geração/dica está ocupada.
- Uma jogada ocorre enquanto uma dica está sendo calculada; resultado associado a revisão anterior nunca pode ser exibido.
- Falha de desenho ou tentativa de entrada fora da grade não altera regras do jogo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir iniciar uma partida com dificuldade escolhida em Windows e Android.
- **FR-002**: O sistema MUST apresentar tabuleiro 9×9 e as cages com seus alvos sem ocultar valores e destaques.
- **FR-003**: O sistema MUST permitir selecionar uma célula editável por toque ou por entrada adequada ao Windows, incluindo teclado.
- **FR-004**: O sistema MUST permitir inserir e apagar respostas sem alterar células fixas do puzzle.
- **FR-005**: O sistema MUST permitir alternar entre modo de resposta e modo de candidato.
- **FR-006**: O sistema MUST oferecer atualização de candidatos compatíveis com o estado atual quando o jogador pedir candidatos automáticos.
- **FR-007**: O sistema MUST informar conflitos de regras sem impedir uma correção posterior.
- **FR-008**: O sistema MUST permitir desfazer e refazer mudanças de jogo de forma determinística.
- **FR-009**: O sistema MUST oferecer pausar e retomar a partida e mostrar o tempo de jogo.
- **FR-010**: O sistema MUST reconhecer a conclusão somente quando todos os valores estiverem corretos e apresentar resumo da partida.
- **FR-011**: O sistema MUST manter a interface responsiva durante validação, candidatos e dicas.
- **FR-012**: O sistema MUST oferecer controles acessíveis por toque em Android e por mouse/teclado em Windows.
- **FR-013**: A sessão MUST manter uma `BoardRevision` monotônica e associar cada pedido de dica ao snapshot dessa revisão. Antes de exibir o resultado, MUST compará-la com a revisão atual; se estiver obsoleta, MUST descartá-lo, cancelar o pedido anterior quando possível e solicitar uma nova dica para o snapshot atual no nível 1. MUST exibir somente um resultado da revisão atual, inclusive `NoSafeHint`, e reiniciar a progressão no nível 1 após cada jogada que altere o tabuleiro. O resultado 003 fornece a revisão analisada conforme `003-logical-hints` FR-005.

### Key Entities

- **Partida**: puzzle ativo, estado do tabuleiro, dificuldade e tempo.
- **Jogada**: alteração de valor ou candidato com estado anterior e novo.
- **Seleção**: célula atualmente ativa.
- **Resumo de partida**: conclusão, tempo, erros e dicas usados.
- **Candidatos manuais**: notas inseridas pelo jogador, distintas de respostas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Os fluxos de iniciar, selecionar, inserir, apagar, pausar e concluir podem ser completados nos dois sistemas-alvo.
- **SC-002**: Em 100% dos cenários de undo/redo, o estado após a operação corresponde ao estado salvo antes da mudança.
- **SC-003**: Nenhum puzzle é marcado concluído enquanto existir célula incorreta ou vazia.
- **SC-004**: As interações de teclado e de toque cobrem os controles necessários para concluir uma partida sem conexão.
- **SC-005**: Em 100% dos cenários em que o tabuleiro muda durante o cálculo da dica, a sessão não exibe resultado com revisão diferente da atual; ela solicita a revisão atual desde o nível 1 e exibe o resultado dessa revisão ou `NoSafeHint`.

## Assumptions

- As plataformas iniciais são Windows e Android; iOS, macOS e Web ficam fora do MVP.
- O estado e as regras de negócio vêm das features anteriores, não da camada de apresentação.
- A tela pode solicitar dicas de 003-logical-hints e novos puzzles de 002-puzzle-engine.
- Persistência entre encerramentos e estatísticas são especificadas em 005-offline-progression.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Application e MAUI; Domain fornece as regras; testes de aplicação e de interface conforme a solution vier a definir.
- MVVM e desenho do tabuleiro isolado conforme a constituição; a camada de desenho converte coordenadas e emite interação, sem decidir regras.
- Entrada por teclado e toque deve produzir a mesma intenção de jogada.
- Operações I/O ou demoradas são assíncronas quando necessário; cálculos longos não bloqueiam a interface.
- Regras de C#, logging, segurança e gates compartilhados estão em specs/README.md.

## 3. FATIAS VERTICAIS DE IMPLEMENTAÇÃO (máximo 3)

### Slice 1: Exibir e selecionar o tabuleiro
- **Escopo**: Iniciar um puzzle, desenhar grade e cages e selecionar células com mouse/toque e teclado.
- **Validação local**: Confirmar legibilidade, seleção correta e operação em Windows e Android.

### Slice 2: Jogar, anotar e corrigir
- **Escopo**: Inserir/apagar valores e candidatos, apresentar validação e implementar undo/redo.
- **Validação local**: Reproduzir os fluxos de entrada, conflitos, notas e histórico nos dois alvos.

### Slice 3: Pausar e concluir
- **Escopo**: Exibir tempo, pausar/retomar, solicitar dica, descartar/recalcular resultados obsoletos conforme FR-013, registrar no composition root MAUI real o `GetHintUseCase` e suas dependências e encerrar com resumo quando o tabuleiro estiver correto.
- **Validação local**: Exercitar suspensão/retorno, responsividade, resultado obsoleto e novo pedido desde o nível 1, registro do caso de uso no composition root, dica e conclusão completa.

## 4. GATES DE VALIDAÇÃO (.NET Toolchain)

Na solution existente, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build --configuration Release após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Esses comandos se aplicam à `CageLogic.slnx` e aos projetos `net10.0` existentes. Builds dos targets Windows/Android dependem do host MAUI e dos workloads que serão introduzidos nesta feature.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir o contrato desta feature.
3. Registrar no DI os contratos e serviços criados ou alterados usando o composition root MAUI real na mesma fatia; incluir o `GetHintUseCase` de 003 e suas dependências necessárias. Não criar composition root artificial em biblioteca.
4. Não mover regras de jogo, solver ou geração para ViewModels/controles visuais.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.
