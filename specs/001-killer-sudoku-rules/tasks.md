---
description: "Tarefas para implementar regras e candidatos de Killer Sudoku"
---

# Tasks: Regras e Candidatos de Killer Sudoku

**Input**: Artefatos de design em `/specs/001-killer-sudoku-rules/`

**Prerequisites**: [plan.md](plan.md) e [spec.md](spec.md); também foram usados `data-model.md`, `research.md` e `quickstart.md`.

**Tests**: Incluídos porque as histórias definem testes independentes, o plano exige cobertura determinística NUnit 5 e a constituição requer testes para novas regras de domínio.

**Organization**: As tarefas de implementação estão agrupadas por história de usuário e ordenadas por dependência.

## Formato

Cada tarefa usa `- [ ] [ID] [P?] [Story?] descrição com caminho de arquivo`. `[P]` identifica tarefas que podem ser executadas em paralelo; `[US1]` e `[US2]` apontam para as histórias da especificação.

## Convenções de caminhos

- Solução na raiz: `CageLogic.slnx`.
- Bibliotecas: `src/CageLogic.Domain/` e `src/CageLogic.Application/`.
- Testes NUnit: `tests/CageLogic.Domain.Tests/` e `tests/CageLogic.Application.Tests/`.
- Os projetos de domínio e aplicação usam `net10.0`; nenhum host MAUI, projeto de infraestrutura, persistência ou contrato externo entra nesta feature.

## Phase 1: Setup (Projetos)

**Purpose**: Criar a solution, bibliotecas e projetos de teste definidos no plano.

- [X] T001 [P] Criar a solution .NET 10 `CageLogic.slnx` na raiz do repositório.
- [X] T002 [P] Criar a biblioteca Domain `src/CageLogic.Domain/CageLogic.Domain.csproj` como projeto C# `net10.0` com nullable habilitado e remover o placeholder `src/CageLogic.Domain/Class1.cs` do template.
- [X] T003 [P] Criar a biblioteca Application `src/CageLogic.Application/CageLogic.Application.csproj` como projeto C# `net10.0` com nullable habilitado e remover o placeholder `src/CageLogic.Application/Class1.cs` do template.
- [X] T004 [P] Criar `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` para `net10.0` usando NUnit 5, `Microsoft.NET.Test.Sdk` e `NUnit3TestAdapter` nas versões correntes do template oficial; remover o teste de exemplo `tests/CageLogic.Domain.Tests/UnitTest1.cs`.
- [X] T005 [P] Criar `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` para `net10.0` usando NUnit 5, `Microsoft.NET.Test.Sdk` e `NUnit3TestAdapter` nas versões correntes do template oficial; remover o teste de exemplo `tests/CageLogic.Application.Tests/UnitTest1.cs`.

---

## Phase 2: Foundational (Dependências compartilhadas)

**Purpose**: Configurar a solution e as referências entre projetos antes de implementar histórias.

- [X] T006 [P] Adicionar os quatro projetos `.csproj` de `src/CageLogic.Domain/`, `src/CageLogic.Application/`, `tests/CageLogic.Domain.Tests/` e `tests/CageLogic.Application.Tests/` à solution `CageLogic.slnx`.
- [X] T007 [P] Garantir `<TargetFramework>net10.0</TargetFramework>` e `<Nullable>enable</Nullable>` em `src/CageLogic.Domain/CageLogic.Domain.csproj`, `src/CageLogic.Application/CageLogic.Application.csproj`, `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` e `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj`.
- [X] T008 [P] Adicionar a referência de `src/CageLogic.Application/CageLogic.Application.csproj` para `src/CageLogic.Domain/CageLogic.Domain.csproj`, mantendo a dependência voltada para o domínio.
- [X] T009 [P] Adicionar a referência de `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` para `src/CageLogic.Domain/CageLogic.Domain.csproj`.
- [X] T010 [P] Adicionar referências de `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` para `src/CageLogic.Application/CageLogic.Application.csproj` e `src/CageLogic.Domain/CageLogic.Domain.csproj`.

**Checkpoint**: Os projetos e as referências estão prontos; não há dependências de MAUI ou infraestrutura no Domain.

---

## Phase 3: User Story 1 — Validar valores e estrutura (Priority: P1) 🎯 MVP

**Goal**: Representar um puzzle validado e retornar separadamente estrutura inválida, conflitos de valores, tabuleiro parcial válido/incompleto e tabuleiro resolvido.

**Independent Test**: Aplicar puzzles e tabuleiros determinísticos e comparar os resultados para cobertura, conectividade, alvos, duplicatas, soma parcial, completude e estado resolvido, conforme as Acceptance Scenarios 1–5 de User Story 1 em `specs/001-killer-sudoku-rules/spec.md`.

### Testes da User Story 1

- [ ] T011 [P] [US1] Escrever testes NUnit determinísticos para definições estruturais válidas e inválidas — posições fora da grade, cages vazias ou repetidas, células sem cage ou sobrepostas, desconexão ortogonal e alvo inalcançável — em `tests/CageLogic.Domain.Tests/Structures/PuzzleStructureValidatorTests.cs`.
- [ ] T012 [P] [US1] Escrever testes NUnit determinísticos para duplicatas em linha, coluna, bloco e cage, alvo de cage inalcançável durante uma partida, validade/completude independentes e resolução em `tests/CageLogic.Domain.Tests/Validation/SudokuBoardValidatorTests.cs`.
- [ ] T013 [P] [US1] Escrever testes NUnit determinísticos para inserir, substituir e limpar valores, preservar valores fixos e retornar resultado explícito para jogadas esperadas inválidas em `tests/CageLogic.Application.Tests/Moves/ApplyMoveUseCaseTests.cs`.
- [ ] T014 [US1] Executar `dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` e `dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` após T011–T013 e registrar a falha esperada dos cenários antes de criar os tipos de produção.

### Implementação da User Story 1

- [ ] T015 [P] [US1] Criar `PuzzleDefinitionPosition` para coordenadas ainda não validadas, `CageDefinition` com alvo e posições brutas e `PuzzleDefinition` com valores fixos e definições de cages em `src/CageLogic.Domain/Puzzles/PuzzleDefinitionPosition.cs`, `src/CageLogic.Domain/Cages/CageDefinition.cs` e `src/CageLogic.Domain/Puzzles/PuzzleDefinition.cs`.
- [ ] T016 [P] [US1] Criar `CellPosition` imutável com linha e coluna de 0 a 8 e `Cage` validada, não vazia, sem posições repetidas e conectada por lados compartilhados em `src/CageLogic.Domain/Board/CellPosition.cs` e `src/CageLogic.Domain/Cages/Cage.cs`.
- [ ] T017 [US1] Criar `PuzzleStructureIssue`, `PuzzleStructureResult` e `ValidatedPuzzle` em `src/CageLogic.Domain/Puzzles/PuzzleStructureIssue.cs`, `src/CageLogic.Domain/Puzzles/PuzzleStructureResult.cs` e `src/CageLogic.Domain/Puzzles/ValidatedPuzzle.cs`; preservar coordenadas brutas nos erros e distinguir sucesso estrutural de estrutura inválida.
- [ ] T018 [US1] Implementar `CageSumFeasibility` em `src/CageLogic.Domain/Cages/CageSumFeasibility.cs` para decidir se o alvo pode ser completado exatamente com o número de casas restantes usando dígitos distintos de 1 a 9, excluindo os já usados na cage.
- [ ] T019 [P] [US1] Implementar `PuzzleStructureValidator` em `src/CageLogic.Domain/Puzzles/PuzzleStructureValidator.cs` para verificar limites das coordenadas, partição exata das 81 posições, cages vazias, duplicatas, conexão ortogonal e atingibilidade estrutural exata do alvo; retornar `PuzzleStructureResult` antes de criar um tabuleiro jogável.
- [ ] T020 [P] [US1] Criar `SudokuCell`, `SudokuBoard` e `Move` em `src/CageLogic.Domain/Board/SudokuCell.cs`, `src/CageLogic.Domain/Board/SudokuBoard.cs` e `src/CageLogic.Domain/Moves/Move.cs`; separar `GivenValue` de `PlayerValue`, modelar limpeza explícita e preservar valores fixos.
- [ ] T021 [US1] Criar `ValueConflict`, `BoardValidationResult` e `SudokuBoardValidator` em `src/CageLogic.Domain/Validation/ValueConflict.cs`, `src/CageLogic.Domain/Validation/BoardValidationResult.cs` e `src/CageLogic.Domain/Validation/SudokuBoardValidator.cs`; informar regra e posições, detectar conflitos múltiplos e manter estrutura inválida separada dos valores jogados.
- [ ] T022 [US1] Criar `MoveResult` e `ApplyMoveUseCase` em `src/CageLogic.Application/Moves/MoveResult.cs` e `src/CageLogic.Application/Moves/ApplyMoveUseCase.cs` para aplicar ou limpar valores, recusar alteração de valor fixo por resultado explícito e retornar o novo tabuleiro e sua validação.

**Checkpoint**: A estrutura do puzzle e os valores jogados podem ser avaliados sem UI; a história é independente dos candidatos.

---

## Phase 4: User Story 2 — Consultar candidatos (Priority: P2)

**Goal**: Calcular candidatos para células vazias a partir das restrições locais e das combinações de cage, sem procurar uma solução completa do tabuleiro.

**Independent Test**: Comparar conjuntos de candidatos com fixtures determinísticos para linha, coluna, bloco, valores da cage, combinação distinta que completa o alvo, célula preenchida, estado inconsistente e alteração após inserir ou limpar um valor, conforme User Story 2 em `specs/001-killer-sudoku-rules/spec.md`.

**Dependency**: Usa o tabuleiro, cages, validação e `CageSumFeasibility` entregues por User Story 1.

### Testes da User Story 2

- [ ] T023 [P] [US2] Escrever testes NUnit com fixtures determinísticos para candidatos permitidos e removidos por linha, coluna, bloco e cage, conclusão de soma com dígitos distintos, ausência de busca global, célula preenchida e conjunto vazio em `tests/CageLogic.Domain.Tests/Candidates/CandidateCalculatorTests.cs`.
- [ ] T024 [P] [US2] Escrever testes NUnit para consultar candidatos usando o tabuleiro mais recente após inserir ou limpar um valor, sem resultados desatualizados, em `tests/CageLogic.Application.Tests/Candidates/GetCandidatesUseCaseTests.cs`.
- [ ] T025 [US2] Executar `dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` e `dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` após T023–T024 e registrar a falha esperada dos cenários antes de criar o cálculo e o caso de uso de candidatos.

### Implementação da User Story 2

- [ ] T026 [US2] Criar `CandidateSet` em `src/CageLogic.Domain/Candidates/CandidateSet.cs`, associado a uma célula vazia e a um conjunto de dígitos de 1 a 9; permitir conjunto vazio como resultado válido para estado inconsistente.
- [ ] T027 [US2] Implementar `CandidateCalculator` em `src/CageLogic.Domain/Candidates/CandidateCalculator.cs` para remover dígitos presentes na linha, coluna, bloco e cage e reter apenas valores que ainda permitam completar o alvo com dígitos distintos; não executar solver global nem manter cache mutável global.
- [ ] T028 [US2] Implementar `GetCandidatesUseCase` em `src/CageLogic.Application/Candidates/GetCandidatesUseCase.cs` para consultar o tabuleiro atual e devolver candidatos somente para posições vazias; garantir que consultas após uma jogada ou limpeza usem o novo estado retornado por `ApplyMoveUseCase` e sejam recalculadas sem resultados obsoletos.

**Checkpoint**: Candidatos locais são atualizados a partir do estado atual do tabuleiro e não dependem do solver global.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Atualizar instruções de verificação e concluir os gates de build e testes definidos pelo projeto.

- [ ] T029 Atualizar `specs/001-killer-sudoku-rules/quickstart.md` com os projetos efetivamente criados, seus caminhos e os cenários determinísticos implementados para estrutura, validação, jogadas e candidatos.
- [ ] T030 Executar `dotnet build --configuration Release --warnaserror` na raiz usando `CageLogic.slnx` e corrigir erros ou warnings antes da etapa de testes.
- [ ] T031 Após o build bem-sucedido, executar `dotnet test --no-build` na raiz usando `CageLogic.slnx` e corrigir falhas nos projetos NUnit.

## Dependencies & Execution Order

### Dependências por fase

- **Setup (Phase 1)**: T001–T005 não têm dependências entre si; podem começar em paralelo.
- **Foundational (Phase 2)**: T006–T007 dependem da criação dos projetos; T008–T010 dependem da configuração dos arquivos `.csproj`. A fase bloqueia as histórias.
- **User Story 1 (Phase 3)**: Começa após a fase Foundational. T011–T013 são testes em arquivos separados; T014 registra a falha esperada antes da implementação T015–T022.
- **User Story 2 (Phase 4)**: Depende dos modelos de tabuleiro e cage e da viabilidade de soma de User Story 1. T023–T024 criam os testes e T025 registra a falha esperada antes da implementação T026–T028.
- **Polish (Phase 5)**: T029–T031 vêm após as histórias desejadas; build deve passar antes de executar testes com `--no-build`.

### Ordem dentro das histórias

- Criar primeiro os testes NUnit de cada história; mantê-los determinísticos e independentes de internet, relógio, aleatoriedade e estado global.
- Em User Story 1, modelos de entrada e valores do domínio precedem o validador estrutural; o validador do tabuleiro precede o caso de uso Application.
- Em User Story 2, `CandidateSet` precede `CandidateCalculator`, que precede o caso de uso Application.
- Não introduzir MAUI, persistência, solver global, importação ou editor de cages nesta feature.

### Oportunidades de paralelismo

- T001–T005 podem ser executadas em paralelo para criar solution e projetos independentes.
- T006 e T007 podem correr em paralelo após a criação dos projetos; T008–T010 podem correr em paralelo após a configuração dos `.csproj`.
- T011–T013 cobrem arquivos de teste separados; T015 e T016 criam modelos em arquivos separados; T019 e T020 também são independentes depois de T017–T018.
- T023 e T024 podem ser escritos em paralelo, pois pertencem a projetos de teste distintos.

## Implementation Strategy

### MVP sugerido: User Story 1

1. Concluir Setup e Foundational.
2. Escrever T011–T013 e registrar a falha esperada em T014.
3. Implementar e verificar User Story 1 (T015–T022): estrutura das cages, regras Sudoku/Killer Sudoku, conflitos, validade e completude.
4. Parar no checkpoint para validar a história com os testes determinísticos antes de iniciar candidatos.

### Entrega incremental

1. Entregar User Story 1 como base utilizável de tabuleiro e validação.
2. Escrever T023–T024 e registrar a falha esperada em T025.
3. Adicionar User Story 2 (T026–T028) reutilizando a viabilidade de soma das cages e os resultados de validação da base.
4. Atualizar o quickstart e executar build Release e todos os testes NUnit.

## Notes

- `[P]` indica tarefas que tocam arquivos diferentes e não dependem umas das outras após seus pré-requisitos.
- As tarefas de teste estão incluídas porque os critérios independentes das histórias, o plano e a constituição do projeto exigem cobertura determinística para as regras.
- A pasta `contracts/` não é necessária: esta feature não expõe API ou protocolo externo.
- Os comandos de validação pertencem à implementação futura; esta geração apenas descreve as tarefas.
