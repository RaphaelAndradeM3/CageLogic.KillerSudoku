---
description: "Lista de tarefas da feature de dicas lógicas progressivas"
---

# Tasks: Dicas Lógicas Progressivas

**Input**: Documentos de design em `specs/003-logical-hints/`.

**Pré-requisitos**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/logical-hints.md` e `quickstart.md`.

**Testes**: Incluídos porque a especificação define testes independentes por história e validação local por fatia. Para cada fatia, escrever os testes antes da implementação correspondente e executar a validação focada indicada.

**Organização**: Fases por história de usuário e prioridade. As três fatias técnicas da spec ficam dentro da história P1; a progressão dos três níveis fica na história P2.

## Formato

Cada tarefa usa `- [ ] Tnnn [P?] [US#?] Descrição com caminho exato`. `[P]` indica arquivos distintos sem dependência pendente. `[US#]` rastreia tarefas de uma história; setup, fundação e polish não recebem esse marcador.

## Fase 1: Setup

**Propósito**: preparar a estrutura compartilhada.

**Sem tarefas de setup**: `CageLogic.slnx`, os projetos Domain/Application e seus projetos NUnit já existem. O plano não adiciona projeto nem pacote.

---

## Fase 2: Fundamentos compartilhados

**Propósito**: definir os contratos que bloqueiam ambas as histórias.

- [x] T001 [P] Escrever testes para `LogicalStepEvidence` cobrindo posições de padrão/escopo, contexto tipado opcional e dígitos válidos de 1 a 9 em `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`.
- [x] T002 [P] Escrever testes para os contratos de entrada e resultado: `UniqueSolution` obrigatória quando `Multiplicity == Unique`, pertencente ao puzzle, nula para `NoSolution` ou `Multiple`; snapshot com cages e givens correspondentes; níveis `Explanation`, `Highlights` e `Action` na ordem 1–3 em `tests/CageLogic.Application.Tests/Hints/GetHintUseCaseTests.cs`.
- [x] T003 [P] Criar `LogicalStepEvidence` sem texto localizado, com posições `CellPosition` de padrão/escopo, `RelevantDigits` limitado a 1–9 e `ScopeContext` tipado opcional para contexto de linha/coluna/bloco/cage e alvo de cage ou residual da Rule of 45, em `src/CageLogic.Domain/LogicalSteps/LogicalStepEvidence.cs`; estender `LogicalStep` em `src/CageLogic.Domain/LogicalSteps/LogicalStep.cs` para carregar a evidência preservando exatamente uma colocação ou uma ou mais eliminações e `RelatedPositions` como união ordenada row-major.
- [x] T004 [P] Criar `HintPuzzleContext`, `HintRequest`, `HintLevel`, `HintResult` e `HintStatus` em `src/CageLogic.Application/Hints/HintPuzzleContext.cs`, `src/CageLogic.Application/Hints/HintRequest.cs`, `src/CageLogic.Application/Hints/HintLevel.cs`, `src/CageLogic.Application/Hints/HintResult.cs` e `src/CageLogic.Application/Hints/HintStatus.cs`; exigir `UniqueSolution` quando `Multiplicity == Unique`, validá-la contra o puzzle e exigir nulo para `NoSolution`/`Multiple`; exigir snapshot com cages e givens correspondentes ao puzzle; aceitar apenas `Explanation`, `Highlights`, `Action` em ordem 1–3; carregar `BoardRevision` monotônica fornecida pela sessão e ecoá-la em todos os status; definir `Available`, `PuzzleSolved`, `InconsistentState`, `NoSafeHint` e `ValueNotConfirmed` conforme `data-model.md`.
- [x] T005 Executar os testes focados dos contratos em `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` (`FullyQualifiedName~LogicalTechniqueTests`) e `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` (`FullyQualifiedName~GetHintUseCaseTests`) após T003 e T004.

**Checkpoint**: os tipos compartilhados e suas invariantes podem ser usados pelas duas histórias.

---

## Fase 3: User Story 1 — Entender o próximo passo (Prioridade: P1)

**Objetivo**: escolher a primeira técnica aplicável na ordem estável de 002 e fornecer uma explicação em português, com evidência coerente com o estado atual, para as nove técnicas v1.

**Teste independente**: usando os estados de referência existentes e ampliados, cada técnica retorna seu ID, evidência tipada e explicação correspondente; o analyzer conserva a prioridade/desempate de 002; estados incompatíveis, resolvidos ou sem técnica recebem o resultado especificado.

### Fatia 1 — Singles

- [x] T006 [P] [US1] Ampliar os vetores LH-01 (Naked Single), LH-02 (Hidden Single) e LH-03 (Cage Single) para exigir evidência de dedução e papéis de destaque declarados em `specs/003-logical-hints/spec.md` em `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`.
- [x] T007 [P] [US1] Criar testes para os nomes e explicações em português dos vetores LH-01 a LH-03, comparando-os ao catálogo e sem revelar valor concreto no primeiro nível, em `tests/CageLogic.Application.Tests/Hints/LogicalHintExplanationCatalogTests.cs`.
- [x] T008 [P] [US1] Preencher `LogicalStepEvidence` nas estratégias Naked Single, Hidden Single e Cage Single em `src/CageLogic.Domain/LogicalSteps/Techniques/NakedSingleTechnique.cs`, `src/CageLogic.Domain/LogicalSteps/Techniques/HiddenSingleTechnique.cs` e `src/CageLogic.Domain/LogicalSteps/Techniques/CageSingleTechnique.cs`.
- [x] T009 [P] [US1] Adicionar nome e explicação geral das três técnicas de singles ao catálogo em `src/CageLogic.Application/Hints/LogicalHintExplanationCatalog.cs`.
- [x] T010 [P] [US1] Executar o teste focado de técnicas do Domain em `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalTechniqueTests` após T008.
- [x] T011 [P] [US1] Executar os testes do catálogo em `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalHintExplanationCatalogTests` após T009.

### Fatia 2 — Técnicas Killer Sudoku

- [x] T012 [P] [US1] Ampliar os vetores LH-04 (Cage Combination), LH-05 (Cage/Region Intersection) e LH-06 (Rule of 45) para exigir evidência suficiente ao raciocínio e os papéis de destaque declarados em `specs/003-logical-hints/spec.md` em `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`.
- [x] T013 [P] [US1] Acrescentar testes para os nomes e explicações dos vetores LH-04 a LH-06 e ausência de ação concreta no nível 1 em `tests/CageLogic.Application.Tests/Hints/LogicalHintExplanationCatalogTests.cs`.
- [x] T014 [P] [US1] Preencher evidências para Cage Combination, Cage/Region Intersection e Rule of 45 em `src/CageLogic.Domain/LogicalSteps/Techniques/CageCombinationTechnique.cs`, `src/CageLogic.Domain/LogicalSteps/Techniques/CageRegionIntersectionTechnique.cs` e `src/CageLogic.Domain/LogicalSteps/Techniques/RuleOf45Technique.cs`.
- [x] T015 [P] [US1] Adicionar ao catálogo os nomes e explicações de Cage Combination, Cage/Region Intersection e Rule of 45 em `src/CageLogic.Application/Hints/LogicalHintExplanationCatalog.cs`.
- [x] T016 [P] [US1] Executar os testes focados do Domain em `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalTechniqueTests` após T014.
- [x] T017 [P] [US1] Executar os testes focados do catálogo em `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalHintExplanationCatalogTests` após T015.

### Fatia 3 — Técnicas avançadas

- [x] T018 [P] [US1] Ampliar os vetores LH-07 (Naked Pair), LH-08 (Hidden Pair) e LH-09 (Naked Triple) para exigir evidência de padrão, escopo, efeito e papéis de destaque declarados em `specs/003-logical-hints/spec.md` em `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`.
- [x] T019 [P] [US1] Acrescentar testes para nomes e explicações em português dos vetores LH-07 a LH-09 em `tests/CageLogic.Application.Tests/Hints/LogicalHintExplanationCatalogTests.cs`.
- [x] T020 [P] [US1] Preencher evidências para Naked Pair, Hidden Pair e Naked Triple em `src/CageLogic.Domain/LogicalSteps/Techniques/NakedPairTechnique.cs`, `src/CageLogic.Domain/LogicalSteps/Techniques/HiddenPairTechnique.cs` e `src/CageLogic.Domain/LogicalSteps/Techniques/NakedTripleTechnique.cs`.
- [x] T021 [P] [US1] Adicionar ao catálogo os nomes e explicações de Naked Pair, Hidden Pair e Naked Triple em `src/CageLogic.Application/Hints/LogicalHintExplanationCatalog.cs`.
- [x] T022 [P] [US1] Executar os testes focados do Domain em `tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalTechniqueTests` após T020.
- [x] T023 [P] [US1] Executar os testes focados do catálogo em `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~LogicalHintExplanationCatalogTests` após T021.

### Caso de uso para explicação inicial

- [x] T024 [US1] Criar em `tests/CageLogic.Application.Tests/Hints/GetHintUseCaseTests.cs` os vetores determinísticos LH-01 a LH-09 com snapshot completo, multiplicidade/solução de origem, técnica esperada, efeito e mapa exato de evidências; testar também prioridade, puzzle resolvido, conflitos, origem `Multiple` cujo estado atual não tem solução compatível (`InconsistentState`), ausência de técnica, revisão ecoada e cancelamento.
- [x] T025 [US1] Implementar `GetHintUseCase.ExecuteAsync` em `src/CageLogic.Application/Hints/GetHintUseCase.cs`: validar correspondência do snapshot, conflitos e conclusão; para puzzle original único comparar entradas de jogador com `SolutionGrid`; retornar `InconsistentState` para origem `NoSolution`; para origem `Multiple`, restringir `PuzzleDefinition` às jogadas atuais e consultar `PuzzleStructureValidator`/`SudokuSolver` para detectar `NoSolution`; consultar `ILogicalStepAnalyzer` em sua ordem estável; devolver explicação de nível 1 sem posição, dígito ou ação; ecoar `BoardRevision`; executar a análise CPU-bound com `Task.Run` e propagar `CancellationToken`.
- [x] T026 [US1] Executar os testes focados do caso de uso em `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~GetHintUseCaseTests` após T025.

**Checkpoint**: o consumidor recebe explicação para qualquer técnica v1, sem a ação concreta, e resultados explícitos para estado resolvido, inconsistente ou sem dica segura.

---

## Fase 4: User Story 2 — Revelar uma resposta somente se necessário (Prioridade: P2)

**Objetivo**: expor destaques no nível 2 e a ação lógica adequada no nível 3, sem converter eliminações em colocações e sem divulgar colocação sem unicidade confirmada.

**Teste independente**: percorrer os três níveis no mesmo snapshot; conferir ausência de conteúdo futuro em cada nível; para eliminações receber somente os candidatos a remover; para colocações receber valor apenas quando compatível com solução única confirmada.

### Testes da User Story 2

- [x] T027 [US2] Ampliar `tests/CageLogic.Application.Tests/Hints/GetHintUseCaseTests.cs` com testes para projeção dos níveis 1–3, papéis de destaque e candidatos envolvidos, ações `PlaceValue`/`RemoveCandidates`, `ValueNotConfirmed` quando a origem é `Multiple` mesmo se o estado restrito tiver solução única, revisão ecoada e cancelamento.

### Implementação da User Story 2

- [x] T028 [P] [US2] Criar a união discriminada `HintAction` em `src/CageLogic.Application/Hints/HintAction.cs` com exatamente `PlaceValue` ou `RemoveCandidates`; exigir lista de remoções não vazia, ordenada row-major por posição e por dígito crescente.
- [x] T029 [P] [US2] Acrescentar a `HintResult` em `src/CageLogic.Application/Hints/HintResult.cs` destaques imutáveis de posições por papel e pares posição/dígito envolvidos, sem declarar remoção no nível 2.
- [x] T030 [US2] Estender `GetHintUseCase` em `src/CageLogic.Application/Hints/GetHintUseCase.cs`: nível 2 inclui somente destaques; nível 3 inclui exatamente `PlaceValue` ou `RemoveCandidates`; recusar valor com `ValueNotConfirmed` se o puzzle original não for único, sem bloquear ação de eliminação; manter nível 1 sem posições/dígitos/ação e ecoar a revisão em todos os status.
- [x] T031 [US2] Executar os testes focados da progressão em `tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj` com `--configuration Release --filter FullyQualifiedName~GetHintUseCaseTests` após T028–T030.

**Checkpoint**: as três projeções respeitam a ordem de revelação e as restrições de segurança para valores.

---

## Fase 5: Polish e validação transversal

**Propósito**: manter o contrato para a sessão futura e executar os gates da solution.

- [x] T032 Atualizar `specs/003-logical-hints/contracts/logical-hints.md` e `specs/003-logical-hints/quickstart.md` para refletir campos/status finais, vetores LH-01 a LH-09, eco da revisão por 003 e responsabilidade da sessão de 004 (FR-013/SC-005) de descartar revisões obsoletas e solicitar novamente a partir do nível 1. O registro do `GetHintUseCase` e dependências ocorre no composition root MAUI real da fatia 3 de 004.
- [ ] T033 Executar `dotnet build CageLogic.slnx --configuration Release --warnaserror` após concluir as histórias e os testes focados.
- [ ] T034 Executar `dotnet test CageLogic.slnx --no-build --configuration Release` em `CageLogic.slnx` somente se T033 concluir com sucesso.

---

## Dependências e ordem de execução

### Dependências entre fases

- **Setup (Fase 1)**: nenhuma tarefa; solution, projetos e dependências já existem.
- **Fundamentos (Fase 2)**: sem bloqueio externo; T001 e T002 podem ser escritos em paralelo, T003 e T004 podem ser implementados em paralelo após os testes, e T005 depende de ambos.
- **User Story 1 (Fase 3, P1)**: depende da Fase 2; as fatias singles → técnicas Killer → técnicas avançadas seguem a ordem do catálogo e os testes de cada fatia precedem sua implementação.
- **User Story 2 (Fase 4, P2)**: depende da conclusão de US1, pois amplia os modelos e o mesmo `GetHintUseCase`.
- **Polish (Fase 5)**: depende de US1 e US2; executar testes da solution somente depois de build bem-sucedido.

### Dependências dentro das histórias

- US1: testes Domain/Application de cada fatia → evidência/catálogo dessa fatia → testes focados. Após as três fatias, testes do caso de uso → implementação do caso de uso → testes focados.
- US2: testes de progressão → modelos de ação/destaque (em paralelo, arquivos diferentes) → projeção no caso de uso → testes focados.
- `BoardRevision` protege a fronteira: Application ecoa a revisão; a sessão de 004 implementa descarte e novo pedido conforme FR-013/SC-005. A multiplicidade `Multiple` da origem não muda se o snapshot restrito ficar único, portanto esse caso segue sem `PlaceValue`.

## Oportunidades de paralelismo

### User Story 1

```text
Fatia singles:
T006 (testes Domain) || T007 (testes catálogo)
T008 (evidência Domain) || T009 (textos Application)
T010 (teste Domain) || T011 (teste Application)

Fatia técnicas Killer:
T012 (testes Domain) || T013 (testes catálogo)
T014 (evidência Domain) || T015 (textos Application)
T016 (teste Domain) || T017 (teste Application)

Fatia avançada:
T018 (testes Domain) || T019 (testes catálogo)
T020 (evidência Domain) || T021 (textos Application)
T022 (teste Domain) || T023 (teste Application)
```

### User Story 2

```text
Após T027:
T028 (HintAction.cs) || T029 (HintResult.cs)
Depois: T030 (GetHintUseCase.cs) → T031 (testes de progressão)
```

## Estratégia de entrega

1. **Base**: concluir contratos e evidências compartilhados da Fase 2.
2. **MVP — User Story 1**: entregar as três fatias de explicação, cobrindo as nove técnicas e validando cada grupo; em seguida entregar o caso de uso de nível 1 e seus estados.
3. **Entrega incremental — User Story 2**: acrescentar destaques e ações em níveis 2/3, incluindo eliminações e o bloqueio de valor sem unicidade confirmada.
4. **Fechamento**: atualizar o contrato para 004, executar build Release e, se aprovado, os testes da solution.

## Notas

- Toda tarefa executável tem checkbox, ID sequencial, marcador `[P]` quando aplicável, `[US#]` nas fases de história e caminho de arquivo/projeto.
- As tarefas de sessão/UI/DI real não são incluídas porque o host MAUI e a composition root pertencem à feature 004; FR-013 e a fatia 3 dessa feature cobrem descarte/reenvio obsoleto e registro do caso de uso.
- O fluxo acima planeja a validação; a geração deste arquivo não executou build nem testes.
