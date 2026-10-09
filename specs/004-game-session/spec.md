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

## Clarifications

### Session 2026-10-08

- Q: Quando o app vai para segundo plano ou o sistema suspende a partida, como deve se comportar o cronômetro ao retornar? → A: Ao sair do primeiro plano, a partida pausa e o cronômetro para. Ao voltar, o jogador precisa tocar em “Retomar”.
- Q: Quais mudanças devem entrar no histórico de desfazer e refazer? → A: Inserir/apagar respostas e alterar candidatos entram no histórico; uma atualização automática de candidatos inteira conta como uma única ação. Seleção, modo e pausa ficam fora do histórico.
- Q: Quando o jogador insere um valor que conflita com as regras, o jogo deve mantê-lo no tabuleiro ou rejeitar a jogada? → A: Manter o valor inserido, destacar o conflito e permitir que o jogador o corrija ou apague depois; enquanto o estado for inconsistente, não oferecer dica segura.
- Q: Quando o jogador pede o preenchimento automático de candidatos, o que deve acontecer às notas existentes nas células vazias? → A: Substituir as notas de todas as células vazias pelos candidatos permitidos pelas regras naquele momento. A atualização inteira é uma ação do histórico.
- Q: Se um valor respeita as regras de linha, coluna, bloco e cage, mas difere da solução confirmada, o jogo deve sinalizá-lo imediatamente? → A: Não sinalizar divergências da solução durante a partida; destacar apenas conflitos com as regras e verificar a solução ao tentar concluir.
- Q: Como contar erros e dicas no resumo? → A: Contar um erro por cada entrada de resposta que viola uma ou mais regras locais; uma mesma entrada conta uma vez, mesmo que cause vários conflitos. Limpar/corrigir depois não apaga o erro, e Undo/Redo não altera a contagem histórica. Uma resposta apenas divergente da solução não conta como erro. Contar cada nível progressivo de dica efetivamente exibido; cancelamentos, resultados obsoletos e `NoSafeHint` não contam.
- Q: Quais comandos de teclado e suporte a leitor de tela são necessários? → A: Setas movem a seleção entre células sem circular nas bordas; 1–9 insere resposta ou alterna nota conforme o modo; Backspace/Delete limpa a resposta ou as notas da célula conforme o modo; Ctrl+Z desfaz e Ctrl+Y refaz. As 81 células e os controles da partida ficam acessíveis individualmente por TalkBack no Android e Narrator no Windows, com posição, valor, estado fixo/editável, notas, seleção, conflito e dica anunciados semanticamente.
- Q: O que ocorre às notas de uma célula quando uma resposta é inserida e depois apagada? → A: Inserir resposta limpa as notas da célula na mesma ação de histórico; desfazer essa inserção restaura as notas. Se o jogador inserir a resposta e depois apagá-la como uma nova ação, as notas permanecem vazias.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Jogar em desktop ou dispositivo móvel (Priority: P1)

O jogador inicia um puzzle e interage com o tabuleiro por mouse/toque e controles apropriados à plataforma.

**Why this priority**: A partida é o fluxo central do aplicativo multiplataforma.

**Independent Test**: Completar o mesmo fluxo essencial em Windows e Android: abrir partida, repetir o comando durante uma geração e confirmar que não há fila, iniciar uma partida após uma falha/cancelamento, selecionar célula, inserir valor e receber estado visual atualizado. Percorrer as 81 células e controles com mouse/toque, teclado e TalkBack/Narrator.

**Acceptance Scenarios**:

1. **Given** um puzzle disponível, **When** o jogador o inicia, **Then** a grade 9×9, os limites de cage e os alvos ficam compreensíveis.
2. **Given** uma célula editável selecionada, **When** o jogador informa um valor, **Then** o estado e o feedback refletem as regras atuais; se o valor gerar conflito, ele permanece no tabuleiro e o conflito é destacado para que possa ser corrigido.
3. **Given** o jogador está em Windows ou Android, **When** usa o mecanismo de entrada esperado na plataforma, **Then** consegue selecionar e alterar células sem depender de conexão.
4. **Given** um valor inserido respeita as regras locais, mas diverge da solução confirmada, **When** o jogador o informa durante a partida, **Then** o jogo não o sinaliza como incorreto até o jogador tentar concluir o puzzle.
5. **Given** uma geração está em andamento, **When** o jogador repetir o comando, cancelar ou a geração falhar, **Then** o comando repetido não fica enfileirado e a tela mantém o estado de espera ou apresenta o resultado explícito com possibilidade de nova tentativa.

### User Story 2 - Controlar e concluir uma partida (Priority: P1)

O jogador pode usar candidatos, corrigir ações e compreender o fim da partida.

**Why this priority**: Notas, histórico e conclusão são necessários para jogar puzzles de duração longa.

**Independent Test**: Alternar entre resposta e candidato, inserir/apagar valores, desfazer/refazer, pausar e concluir um puzzle de teste.

**Acceptance Scenarios**:

1. **Given** modo resposta, **When** o jogador informa um dígito, **Then** ele é inserido como resposta; no modo candidatos, o mesmo controle altera apenas as notas.
2. **Given** uma alteração de resposta ou candidato, **When** o jogador desfaz ou refaz, **Then** o conteúdo do tabuleiro retorna exatamente ao estado anterior ou posterior; uma atualização automática completa de candidatos conta como uma única ação.
3. **Given** uma partida em andamento, **When** o jogador pausa e retoma, **Then** o cronômetro acompanha o tempo ativo da partida.
4. **Given** todas as células estão corretas, **When** o puzzle é concluído, **Then** a partida termina e um resumo com dificuldade, tempo ativo, erros e dicas é apresentado conforme FR-010.
5. **Given** uma dica foi calculada para a revisão `r` e uma jogada altera o tabuleiro antes da exibição, **When** a resposta da revisão `r` chega, **Then** a sessão a descarta, solicita uma nova análise do snapshot atual desde o nível 1 e exibe somente o resultado da revisão atual ou a mensagem de que não há dica segura.
6. **Given** uma dica está sendo calculada e o jogador altera somente candidatos manuais, **When** a resposta chega, **Then** a sessão mantém a revisão do board e pode exibir o resultado se ele ainda pertencer à revisão atual.
7. **Given** uma partida em andamento, **When** o app vai para segundo plano ou é suspenso e depois retorna, **Then** a partida permanece pausada, o tempo fora do primeiro plano não é contado e o jogador precisa retomá-la explicitamente.
8. **Given** o jogador altera seleção, modo ou estado de pausa, **When** desfaz ou refaz uma alteração do tabuleiro, **Then** somente respostas e candidatos são restaurados; seleção, modo e pausa não são ações do histórico.
9. **Given** um valor inserido conflita com as regras, **When** o jogo valida o estado, **Then** mantém o valor, destaca o conflito e permite corrigi-lo ou apagá-lo.
10. **Given** existem candidatos manuais em células vazias, **When** o jogador solicita candidatos automáticos, **Then** as notas de todas as células vazias e editáveis são substituídas pelos candidatos permitidos pelas regras, e uma única ação de desfazer restaura todas as notas anteriores.
11. **Given** uma célula tem notas manuais, **When** o jogador insere uma resposta, **Then** as notas são limpas como parte da mesma ação; desfazer restaura o estado anterior completo, mas apagar a resposta depois como nova ação deixa as notas vazias.
12. **Given** o jogador usa teclado ou leitor de tela, **When** percorre e edita o tabuleiro, **Then** consegue alcançar cada uma das 81 células e os controles da partida, ouvir a posição/valor/estado semântico e realizar as mesmas intenções disponíveis por toque.
13. **Given** a partida é concluída corretamente, **When** o resumo aparece, **Then** ele apresenta um erro por cada entrada que violou regra local e uma dica por cada nível progressivo exibido, sem contar respostas apenas divergentes da solução, cancelamentos, resultados obsoletos ou `NoSafeHint`.
14. **Given** o tabuleiro está completo e consistente localmente, mas diverge da solução, **When** o jogador tenta concluir, **Then** a sessão permanece em andamento e só então informa que a solução não está correta.

### Edge Cases

- Seleção de uma célula fixa do puzzle.
- Inserção de valor já conflitante, apagamento de célula vazia e undo/redo sem ações.
- Manter uma resposta conflitante visível e corrigível, sem oferecer dica segura enquanto o estado permanecer inconsistente.
- Não sinalizar durante a partida respostas que respeitam as regras locais mas divergem da solução; verificar a solução na tentativa de conclusão.
- Alternar modo candidato/resposta sem substituir conteúdo incorretamente.
- Inserir resposta em célula com notas, desfazer a inserção e, separadamente, apagar uma resposta sem restaurar notas antigas.
- Percorrer células fixas e editáveis com leitor de tela; anunciar mudanças de valor, seleção, conflito e dica sem depender somente de cor.
- Desfazer/refazer alterações de resposta e candidatos; uma atualização automática de candidatos é uma ação única e seleção, modo e pausa ficam fora do histórico.
- A atualização automática recalcula os candidatos permitidos para todas as células vazias e editáveis, substitui as notas existentes e pode ser desfeita como uma única ação.
- Pausa, suspensão do app, rotação/redimensionamento e retorno do segundo plano.
- Sair do primeiro plano ou suspender o app pausa a partida; ao retornar, ela permanece pausada até o jogador retomar e o intervalo fora do primeiro plano não conta no cronômetro.
- Entrada simultânea ou repetida enquanto uma operação de geração/dica está ocupada.
- Uma jogada ocorre enquanto uma dica está sendo calculada; resultado associado a revisão anterior nunca pode ser exibido.
- Falha de desenho ou tentativa de entrada fora da grade não altera regras do jogo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir iniciar uma partida com dificuldade escolhida em Windows e Android. Durante a geração, MUST apresentar estado de carregamento e permitir cancelamento pelo jogador; se a geração falhar ou for cancelada, MUST apresentar um resultado explícito e permitir nova tentativa. Pedidos repetidos enquanto uma geração estiver ativa MUST NOT ser enfileirados.
- **FR-002**: O sistema MUST apresentar tabuleiro 9×9 e as cages com seus alvos sem ocultar valores e destaques.
- **FR-003**: O sistema MUST permitir selecionar qualquer célula, fixa ou editável, por toque ou por entrada adequada ao Windows, incluindo teclado. Somente células editáveis aceitam alterações.
- **FR-004**: O sistema MUST permitir inserir e apagar respostas sem alterar células fixas do puzzle. Inserir uma resposta MUST limpar as notas da célula na mesma ação do histórico; desfazer essa inserção MUST restaurar as notas. Apagar a resposta em uma ação posterior MUST deixar a célula sem as notas anteriores.
- **FR-005**: O sistema MUST permitir alternar entre modo de resposta e modo de candidato.
- **FR-006**: Quando o jogador pedir candidatos automáticos, o sistema MUST recalcular os candidatos permitidos pelas regras para todas as células vazias e editáveis e substituir as notas de candidatos existentes nessas células. A atualização completa MUST ser desfeita e refeita como uma única ação do histórico.
- **FR-007**: O sistema MUST manter no tabuleiro valores inseridos que conflitem com as regras, destacar os conflitos e permitir que o jogador corrija ou apague esses valores. MUST NOT sinalizar durante a partida se um valor que respeita as regras locais diverge da solução confirmada; essa verificação ocorre quando o jogador tenta concluir. Enquanto o estado for inconsistente, MUST informar que as jogadas impedem uma conclusão válida e não oferecer dica segura, conforme 003-logical-hints.
- **FR-008**: O sistema MUST permitir desfazer e refazer deterministicamente alterações do conteúdo editável da sessão, incluindo inserir/apagar respostas e alterar candidatos manuais ou automáticos. Uma atualização automática completa de candidatos MUST contar como uma única ação do histórico. Seleção, modo, pausa/retomada e contagem histórica de erros MUST ficar fora do histórico.
- **FR-009**: O sistema MUST permitir pausar e retomar a partida e mostrar o tempo ativo de jogo. Ao sair do primeiro plano ou ser suspenso, MUST pausar automaticamente; ao retornar, MUST permanecer pausado até o jogador retomar explicitamente, sem contar o tempo fora do primeiro plano.
- **FR-010**: Quando o jogador tentar concluir, o sistema MUST reconhecer a conclusão somente se todos os valores estiverem corretos e apresentar resumo da partida. Se houver célula vazia ou conflito, MUST manter a partida em andamento e informar o estado incompleto/inconsistente. Se todas as células estiverem preenchidas e localmente consistentes mas divergirem da solução, MUST manter a partida em andamento e informar a divergência somente após a tentativa de conclusão. O resumo MUST mostrar tempo ativo, dificuldade, contagem de erros e contagem de dicas. A contagem de erros MUST aumentar uma vez por cada entrada de resposta que viole uma ou mais regras locais, mesmo que essa entrada cause vários conflitos; corrigir/apagar depois não decrementa a contagem, e Undo/Redo MUST NOT alterar essa contagem histórica. Um valor apenas divergente da solução não conta como erro. A contagem de dicas MUST aumentar por cada nível progressivo efetivamente exibido; cancelamentos, resultados obsoletos e `NoSafeHint` não contam.
- **FR-011**: O sistema MUST manter a interface responsiva durante validação, candidatos, geração e dicas: MUST apresentar carregamento antes de aguardar geração/dica, não bloquear a thread de interface com esses cálculos, e manter a edição disponível enquanto uma dica estiver sendo calculada.
- **FR-012**: O sistema MUST oferecer controles acessíveis por toque em Android e por mouse/teclado em Windows, com cada uma das 81 células e cada controle da partida acessíveis semanticamente por TalkBack no Android e Narrator no Windows. A descrição acessível de uma célula MUST informar posição, valor ou vazio, fixa/editável, notas, seleção, conflito e destaque de dica. Setas movem a seleção uma célula na direção indicada, sem circular nas bordas; 1–9 insere resposta ou alterna nota conforme o modo; Backspace/Delete limpa a resposta ou as notas da célula conforme o modo; Ctrl+Z desfaz e Ctrl+Y refaz. As mesmas intenções MUST estar disponíveis por controles acessíveis de toque.
- **FR-013**: A sessão MUST manter uma `BoardRevision` monotônica e associar cada pedido de dica ao snapshot dos valores do `SudokuBoard` nessa revisão. Antes de exibir o resultado, MUST compará-la com a revisão atual; se estiver obsoleta, MUST descartá-lo, cancelar o pedido anterior quando possível e solicitar uma nova dica para o snapshot atual no nível 1. MUST exibir somente um resultado da revisão atual, inclusive `NoSafeHint`, e reiniciar a progressão no nível 1 após cada jogada que altere os valores do tabuleiro. Alterações somente em candidatos MUST NOT alterar a revisão nem invalidar o cálculo. O resultado 003 fornece a revisão analisada conforme `003-logical-hints` FR-005.

### Key Entities

- **Partida**: puzzle ativo, estado do tabuleiro, dificuldade e tempo.
- **Jogada**: alteração de valor ou candidato com estado anterior e novo.
- **Seleção**: célula atualmente ativa.
- **Resumo de partida**: conclusão, tempo, erros e dicas usados.
- **Candidatos manuais**: notas inseridas pelo jogador, distintas de respostas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em ambos os sistemas-alvo, todos os fluxos primários especificados para iniciar, selecionar, inserir, apagar, pausar e concluir podem ser completados sem conexão; falhas/cancelamentos de geração deixam a tela em estado explícito que permite nova tentativa.
- **SC-002**: Em 100% dos cenários de undo/redo, respostas e candidatos retornam exatamente ao estado anterior ou posterior; uma atualização automática completa de candidatos é desfeita/refeita como uma única ação, e seleção, modo e pausa/retomada não alteram o histórico.
- **SC-003**: Nenhum puzzle é marcado concluído enquanto houver célula vazia, conflito local ou divergência da `SolutionGrid`.
- **SC-004**: As interações de teclado e de toque cobrem os controles necessários para concluir uma partida sem conexão: setas navegam entre células sem circular nas bordas, 1–9 edita conforme o modo, Backspace/Delete limpa conforme o modo e Ctrl+Z/Ctrl+Y desfaz/refaz.
- **SC-005**: Em 100% dos cenários em que o tabuleiro muda durante o cálculo da dica, a sessão não exibe resultado com revisão diferente da atual; ela solicita a revisão atual desde o nível 1 e exibe o resultado dessa revisão ou `NoSafeHint`.
- **SC-006**: Em 100% dos cenários de segundo plano ou suspensão, a partida retorna pausada, o intervalo fora do primeiro plano não é contado e nenhuma retomada ocorre sem ação explícita do jogador.
- **SC-007**: Em 100% dos cenários de valores conflitantes, o valor permanece visível e corrigível, o conflito é destacado e nenhuma dica segura é oferecida enquanto o estado continuar inconsistente.
- **SC-008**: Em 100% das atualizações automáticas de candidatos, cada célula vazia e editável mostra exatamente os candidatos permitidos pelas regras, as notas anteriores são restauradas por um único desfazer e o refazer restaura integralmente o resultado calculado.
- **SC-009**: Valores localmente válidos que divergem da solução não são sinalizados durante a edição; em 100% das tentativas completas porém incorretas, a divergência só é informada após `TryComplete` e a sessão continua em andamento.
- **SC-010**: Em 100% dos resumos, a contagem corresponde às regras de FR-010: entradas conflitantes contam uma vez por entrada, correções/Undo/Redo não alteram a contagem histórica, respostas apenas divergentes da solução não contam, e cada nível progressivo exibido conta uma dica; `NoSafeHint`, cancelamentos e resultados obsoletos não contam.
- **SC-011**: TalkBack no Android e Narrator no Windows permitem percorrer e operar todas as 81 células e controles, anunciar os estados semânticos de FR-012 e usar os comandos definidos sem depender apenas de cor.
- **SC-012**: Em 100% dos pedidos assíncronos de geração/dica, o estado de carregamento é apresentado sem bloquear a interface; enquanto uma dica está pendente, seleção e edição continuam processáveis.

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
