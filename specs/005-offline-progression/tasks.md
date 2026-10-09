---
description: "Tarefas de implementação da feature Persistência Offline e Progresso"
---

# Tasks: Persistência Offline e Progresso

**Input**: artefatos de `specs/005-offline-progression/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md` e `quickstart.md`.

**Tests**: incluídos porque a spec exige testes de interrupção, falha de gravação, estatísticas, migração e gates de build/test. Os testes de cada história precedem a implementação.

**Organization**: fases por história de usuário; a US2 depende dos registros produzidos pelo fluxo persistido da US1.

## Phase 1: Setup

**Purpose**: adicionar a dependência de persistência ao projeto existente.

- [X] T001 Adicionar referência compatível e versionada de Microsoft.Data.Sqlite em `src/CageLogic.Infrastructure/CageLogic.Infrastructure.csproj`.

---

## Phase 2: Foundational

**Purpose**: estabelecer os contratos e a base transacional que bloqueiam as histórias.

- [X] T002 [P] Definir `GameProgressRecord`, `SavedGameSession`, `GameProgressStatus` e resultados de carga/gravação em `src/CageLogic.Application/Progression/GameProgressModels.cs` e o contrato `IGameProgressStore` em `src/CageLogic.Application/Progression/IGameProgressStore.cs`.
- [X] T003 [P] Escrever testes da migração v1, restrições de colunas, índice de uma partida ativa e rollback de migração com preservação de `user_version` e registros válidos anteriores em `tests/CageLogic.Infrastructure.Tests/Progression/ProgressionSchemaTests.cs`.
- [X] T004 Implementar fábrica de conexão e migração transacional com `PRAGMA user_version` em `src/CageLogic.Infrastructure/Progression/SqliteConnectionFactory.cs` e `src/CageLogic.Infrastructure/Progression/SqliteSchemaMigrator.cs` (depende de T001 e T003). Preservar estas regras do modelo: `SessionId` é chave primária; `Status` é Active, Completed ou Abandoned; `StartedAtUtc` é obrigatório; `CompletedAtUtc` só é preenchido em Completed; `AbandonedAtUtc` só é preenchido em Abandoned; `ActiveElapsedTicks`, `ErrorCount` e `DisplayedHintLevelCount` são não negativos; `SnapshotVersion` e `SnapshotJson` são obrigatórios para Active e nulos nos estados terminais; existe no máximo uma linha Active.

**Checkpoint**: os contratos de aplicação e o esquema inicial existem; nenhuma história de usuário depende de acesso direto ao SQLite.

---

## Phase 3: User Story 1 - Continuar uma partida (Priority: P1) 🎯 MVP

**Goal**: salvar cada mudança antes de confirmá-la, restaurar estado/histórico/tempo e proteger o último salvamento íntegro em falhas.

**Independent Test**: criar um puzzle válido, alterar respostas e notas, executar undo/redo, persistir, encerrar e restaurar. Comparar puzzle, solução interna, tabuleiro, notas, histórico, métricas e tempo pausado; injetar falha de escrita e dado inválido para verificar que a última transação válida continua preservada e nenhuma mudança pendente é confirmada.

### Tests for User Story 1

- [X] T005 [P] [US1] Escrever testes de captura/restauração round-trip para puzzle, solution de 81 dígitos, valores, notas, undo/redo, contadores e timer em `tests/CageLogic.Application.Tests/GameSessions/GameSessionPersistenceTests.cs`.
- [X] T006 [P] [US1] Escrever testes do store para commit atômico, rollback, falha sem substituir estado válido, snapshot inválido/incompatível, uma sessão ativa e conclusão idempotente em `tests/CageLogic.Infrastructure.Tests/Progression/SqliteGameProgressStoreTests.cs`.
- [X] T007 [P] [US1] Escrever testes da fila de comandos para ordenação, confirmação somente após commit, falha mantendo a sessão em memória, cancelamento sem resultado ambíguo e persistência do contador após exibição efetiva de dica em `tests/CageLogic.Application.Tests/GameSessions/GameProgressCommandQueueTests.cs`.
- [X] T008 [P] [US1] Escrever testes dos casos de uso para criar, carregar pausada, salvar, concluir uma única vez e abandonar uma sessão em `tests/CageLogic.Application.Tests/Progression/GameProgressLifecycleTests.cs`.

### Implementation for User Story 1

- [X] T009 [US1] Criar DTO versionado e mapeador de captura para definição do puzzle, cages, solução, tabuleiro, notas, histórico, duração ativa e contadores em `src/CageLogic.Application/GameSessions/GameSessionPersistenceSnapshot.cs` (após T005).
- [X] T010 [US1] Implementar hidratação validada e restauração pausada em `src/CageLogic.Application/GameSessions/GameSession.cs`, `src/CageLogic.Application/GameSessions/SessionHistory.cs`, `src/CageLogic.Application/GameSessions/CandidateNotes.cs` e `src/CageLogic.Application/GameSessions/ActiveGameTimer.cs`; validar givens, cages, solução, valores, notas e histórico antes de reconstruir a sessão (depende de T009).
- [X] T011 [US1] Implementar os casos de uso para criar, carregar, salvar snapshot, concluir e abandonar em `src/CageLogic.Application/Progression/CreateGameProgressUseCase.cs`, `src/CageLogic.Application/Progression/LoadActiveGameUseCase.cs`, `src/CageLogic.Application/Progression/SaveGameProgressUseCase.cs`, `src/CageLogic.Application/Progression/CompleteGameProgressUseCase.cs` e `src/CageLogic.Application/Progression/AbandonGameProgressUseCase.cs` (depende de T002, T009 e T010).
- [X] T012 [US1] Implementar `IGameProgressStore` com transação para criar partida e snapshot, salvar snapshot/escalares, concluir ou abandonar, limpar snapshot terminal e listar registros para estatísticas em `src/CageLogic.Infrastructure/Progression/SqliteGameProgressStore.cs`; configurar WAL e `synchronous=FULL` em cada conexão gravadora (depende de T004 e T006).
- [X] T013 [US1] Integrar a fila serial com captura do snapshot e `IGameProgressStore` em `src/CageLogic.Application/GameSessions/GameSessionCommandQueue.cs`; executar persistência fora da thread de UI e só retornar confirmação após commit, mantendo mudanças recentes em memória e não confirmadas se a gravação falhar (depende de T007, T011 e T012).
- [X] T014 [US1] Registrar store e casos de uso e carregar a sessão ativa antes de oferecer novo jogo em `src/CageLogic.Maui/MauiProgram.cs`, `src/CageLogic.Maui/App.xaml.cs`, `src/CageLogic.Maui/ViewModels/HomeViewModel.cs`, `src/CageLogic.Maui/Views/HomePage.xaml` e `src/CageLogic.Maui/Lifecycle/GameSessionLifecycleBehavior.cs`; restaurar pausada e persistir o tempo acumulado ao pausar/parar. Sem sessão recuperável, informar explicitamente que não há partida para continuar e oferecer novo jogo sem afirmar que não há estatísticas (depende de T011–T013).
- [X] T015 [US1] Conectar respostas, notas, apagar, undo, redo e cada nível de dica efetivamente exibido à fila persistente; apresentar os estados salvando/salvo/falha sem bloquear a interface em `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs` e `src/CageLogic.Maui/Views/GamePage.xaml`. Persistir o contador da dica exibida e não contar cancelamento, resultado obsoleto ou NoSafeHint, conforme `specs/004-game-session/spec.md` FR-010 (depende de T013).
- [X] T016 [US1] Exibir recuperação necessária para snapshot inválido, versão desconhecida ou falha sem estado íntegro e exigir confirmação antes de substituir/iniciar outra partida em `src/CageLogic.Maui/ViewModels/HomeViewModel.cs` e `src/CageLogic.Maui/Views/HomePage.xaml` (depende de T014).
- [X] T017 [US1] Integrar conclusão e abandono explícitos ao fluxo de resumo e retorno à tela inicial, garantindo transição terminal idempotente pelo SessionId em `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs`, `src/CageLogic.Maui/Views/SessionSummaryPage.xaml.cs` e `src/CageLogic.Maui/ViewModels/HomeViewModel.cs` (depende de T011, T012 e T016).

**Checkpoint**: a sessão pode ser criada, salva, restaurada pausada, concluída ou abandonada sem rede; somente commits são apresentados como confirmados.

---

## Phase 4: User Story 2 - Acompanhar progresso e preferências (Priority: P2)

**Goal**: consultar estatísticas consistentes e manter a preferência de tema após reiniciar.

**Independent Test**: fornecer registros conhecidos Active, Completed e Abandoned; comparar iniciadas, concluídas, média, melhor tempo, erros e dicas, inclusive sem conclusões. Alterar tema claro/escuro, reiniciar e confirmar a preferência salva; sem valor salvo, seguir o tema do sistema.

**Dependency**: depende dos registros duráveis e das transições da US1 para os cenários completos de estatísticas; os cálculos e preferências podem ser desenvolvidos em arquivos separados após os contratos da Fase 2.

### Tests for User Story 2

- [X] T018 [P] [US2] Escrever testes de agregação para histórico vazio, partidas concluídas, ativas e abandonadas, tempos apenas de concluídas, erros/dicas e idempotência em `tests/CageLogic.Application.Tests/Progression/GetProgressionStatisticsUseCaseTests.cs`.
- [X] T019 [P] [US2] Escrever testes da preferência para valor ausente, tema claro e tema escuro em `tests/CageLogic.Application.Tests/Progression/ThemePreferenceUseCaseTests.cs`.

### Implementation for User Story 2

- [X] T020 [P] [US2] Implementar consulta de estatísticas sem cache mutável em `src/CageLogic.Application/Progression/GetProgressionStatisticsUseCase.cs`; contar todos os registros como iniciados, somente Completed como concluídos, calcular média/melhor apenas de Completed e incluir erros/dicas dos registros iniciados (depende de T018 e T017).
- [X] T021 [P] [US2] Definir `IThemePreferenceStore` e o caso de uso para carregar Light/Dark ou acompanhar o sistema sem valor salvo em `src/CageLogic.Application/Progression/IThemePreferenceStore.cs` e `src/CageLogic.Application/Progression/ThemePreferenceUseCase.cs` (depende de T019).
- [X] T022 [US2] Implementar o adaptador de `IThemePreferenceStore` com Preferences do MAUI em `src/CageLogic.Maui/Preferences/MauiThemePreferenceStore.cs` (depende de T021).
- [X] T023 [P] [US2] Criar ViewModel e tela de estatísticas com estado vazio e rótulos acessíveis em `src/CageLogic.Maui/ViewModels/ProgressionStatisticsViewModel.cs`, `src/CageLogic.Maui/Views/ProgressionStatisticsPage.xaml` e `src/CageLogic.Maui/Views/ProgressionStatisticsPage.xaml.cs` (depende de T020).
- [X] T024 [P] [US2] Criar ViewModel e controles de tema claro/escuro que atualizam imediatamente o tema aplicado e oferecem estado acessível em `src/CageLogic.Maui/ViewModels/ThemeSettingsViewModel.cs`, `src/CageLogic.Maui/Views/ThemeSettingsPage.xaml` e `src/CageLogic.Maui/Views/ThemeSettingsPage.xaml.cs` (depende de T021).
- [X] T025 [US2] Registrar store, casos de uso e telas, carregar e aplicar a preferência salva antes de exibir a primeira página e adicionar navegação para estatísticas e tema em `src/CageLogic.Maui/MauiProgram.cs`, `src/CageLogic.Maui/App.xaml.cs` e `src/CageLogic.Maui/AppShell.xaml.cs` (depende de T022–T024).

**Checkpoint**: estatísticas refletem os registros persistidos, o estado vazio é definido e a preferência local de tema reaparece após reinício.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: validar segurança operacional, os gates automatizados e a aceitação nas duas plataformas.

- [X] T026 Revisar logs de operações de progresso para incluir contexto/correlation ID sem registrar SnapshotJson, solução, tabuleiro, notas ou valores digitados; manter a configuração Serilog diária e a retenção de 14 dias em `src/CageLogic.Maui/ViewModels/HomeViewModel.cs`, `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs` e `src/CageLogic.Maui/MauiProgram.cs`.
- [X] T027 Executar build Release com warnaserror, suíte NUnit após build, e builds MAUI Windows/Android conforme os comandos em `specs/005-offline-progression/quickstart.md`; corrigir falhas nos arquivos afetados.
- [ ] T028 Realizar aceitação manual offline em Windows e Android para encerramento/retomada, conclusão, abandono, estado sem partida retomável, tema aplicado após reinício, recuperação de falha e acessibilidade dos novos controles/status com Narrator e TalkBack conforme `specs/004-game-session/spec.md` FR-012; antes de medir p95, definir e registrar por alvo o hardware e o perfil de armazenamento/banco, executar aquecimento, medir as amostras escolhidas, corrigir gargalos se exceder 250 ms e registrar condições, quantidade de amostras e resultados em `specs/005-offline-progression/acceptance.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências; adiciona Microsoft.Data.Sqlite.
- **Foundational (Phase 2)**: depende de T001; define portas e esquema e bloqueia as histórias.
- **US1 (Phase 3)**: depende da Fase 2; entrega a capacidade MVP de salvar e retomar.
- **US2 (Phase 4)**: depende da Fase 2 e dos registros/status implementados na US1 para integração completa.
- **Polish (Phase 5)**: depende das histórias implementadas.

~~~mermaid
flowchart TD
    Setup[Setup] --> Foundation[Foundational]
    Foundation --> US1[US1: Continuar uma partida]
    US1 --> US2[US2: Progresso e preferências]
    US2 --> Polish[Polish e aceitação]
~~~

### User Story Dependencies

- **US1 (P1)**: começa após a Fase 2 e não depende de outra história.
- **US2 (P2)**: a lógica de cálculo/preferências é separável, mas a aceitação end-to-end de estatísticas depende das linhas e transições de partidas da US1.

### Parallel Opportunities

- Após T001, T002 e T003 podem avançar em paralelo; T004 aguarda T003.
- Na US1, T005–T008 são testes em arquivos distintos e podem ser escritos em paralelo após a Fase 2.
- Na US2, T018 e T019 podem ser escritos em paralelo após US1; T020 e T021, e depois T023 e T024, atuam em arquivos distintos.
- Não há paralelismo seguro entre tarefas que editam `MauiProgram.cs`, `HomeViewModel.cs` ou `GamePageViewModel.cs`.

## Parallel Example: User Story 1

Após concluir a Fase 2, distribuir os testes independentes antes da implementação:

- T005: round-trip da sessão e do histórico em `GameSessionPersistenceTests.cs`.
- T006: atomicidade e recuperação em `SqliteGameProgressStoreTests.cs`.
- T007: confirmação serializada em `GameProgressCommandQueueTests.cs`.
- T008: transições de ciclo de vida em `GameProgressLifecycleTests.cs`.

## Parallel Example: User Story 2

Depois da US1, revisar os dois conjuntos de teste separadamente; após eles, implementar cálculos e preferências em paralelo:

- T018: regras de agregação em `GetProgressionStatisticsUseCaseTests.cs`.
- T019: carregamento e gravação de tema em `ThemePreferenceUseCaseTests.cs`.
- T020: agregados de partidas em `GetProgressionStatisticsUseCase.cs`.
- T021: porta e caso de uso do tema em `IThemePreferenceStore.cs` e `ThemePreferenceUseCase.cs`.
- T023/T024: telas de estatísticas e tema em seus arquivos separados.

## Implementation Strategy

### MVP First (User Story 1)

1. Concluir Setup e Foundational.
2. Implementar e aceitar US1: gravar/retomar, inclusive falhas, sem dependência de US2.
3. Validar a história com testes de round-trip, rollback e aceitação manual nas duas plataformas.
4. Integrar US2 sobre registros já persistidos.
5. Concluir os gates Release e registrar o p95 Windows/Android.

### Incremental Delivery

1. Entregar sessão offline retomável como MVP.
2. Adicionar agregados e preferência de tema como US2.
3. Finalizar logging seguro, builds dos alvos e aceitação manual após as duas histórias.

## Notes

- `[P]` indica tarefas em arquivos diferentes, sem dependência de tarefa incompleta.
- `[US1]` e `[US2]` ligam a tarefa à história correspondente da spec.
- Tarefas de teste são escritas antes da implementação da história.
- Cada tarefa aponta os arquivos concretos a criar ou alterar; a aceitação manual registra evidência em `acceptance.md`.
