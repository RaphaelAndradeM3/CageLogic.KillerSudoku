# Pesquisa — Motor de puzzles

Esta pesquisa foi feita para a árvore atual da branch `002-puzzle-engine`. A pasta `.specify/memory/constitution.md` ainda é somente o template; os guardrails usados estão em [`constitution.md`](../../constitution.md) e [`specs/README.md`](../README.md).

## Baseline local

- A solution já existe como `CageLogic.slnx`; os projetos `CageLogic.Domain` e `CageLogic.Application` e seus testes NUnit 5 já estão presentes, todos em `net10.0`. A Application referencia somente Domain. As referências antigas à ausência da solution foram corrigidas nos documentos do projeto.
- `PuzzleDefinition` e `CageDefinition` representam entrada ainda não validada; `PuzzleStructureValidator.Validate` produz `ValidatedPuzzle`. O tipo validado contém givens e cages e pode criar um `SudokuBoard`.
- `CandidateCalculator.Calculate` aplica restrições de linha, coluna, bloco e cage, incluindo viabilidade parcial de soma, mas não prova que exista uma solução global. `SudokuBoardValidator` valida o estado e a solução completa. `SudokuBoard.WithPlayerValue` é imutável e não altera givens.
- `Cage` exige posições distintas e conectadas ortogonalmente; `CageSumFeasibility` valida se a soma é alcançável por dígitos distintos. Não há modelo de solução completa, solver, gerador, classificador ou composition root MAUI.
- PRD §§10–13 separam solver computacional, hints e geração; a dificuldade deve refletir técnicas e não apenas número/formato das cages. A spec 003 requer Single evidente, Single oculto, combinação de cage e interação cage/região ou regra dos 45. `Ideia.md` também lista pares e triplas como técnicas possíveis, sem rubrica de dificuldade definida.

## Decisões

### 1. Solver: CSP com backtracking, MRV e propagação pelos candidatos existentes

**Decision**: implementar em Application um solver síncrono e puro de CPU que recebe `ValidatedPuzzle`, atualiza um ramo por `WithPlayerValue`, recalcula candidatos e escolhe a próxima célula vazia com menos opções (MRV). Candidato vazio encerra o ramo; cada grade completa precisa passar pelo validador de tabuleiro. A API de contagem retorna `NoSolution`, `Unique` ou `Multiple` (pelo menos duas), guarda a primeira solução encontrada e encerra a busca assim que encontra a segunda.

**Rationale**: reutiliza as regras de Domain como fonte única, evita pacote externo e dá ao gerador prova de unicidade. Seleção da variável mais restrita e forward checking são técnicas estabelecidas para reduzir busca CSP; a implementação inicial deve priorizar clareza e ser medida antes de duplicar regras em estado de bitmask. O modelo de solução será próprio e imutável, separado de `SudokuBoard`, que representa tabuleiro de jogo.

**Cancellation**: observar `CancellationToken` antes da busca e durante nós/ramificações; propagar `OperationCanceledException`, nunca traduzir cancelamento em zero soluções. Application executa a operação CPU-bound fora da UI thread via `Task.Run`, passando o mesmo token ao delegate e à task. Não criar `async` artificial no algoritmo de domínio.

**Alternatives considered**:

- Estado privado mutável com máscaras e undo: potencialmente mais rápido, porém repete regras já existentes; só considerar se benchmark demonstrar gargalo.
- Dancing Links / exact cover: técnica eficiente para as restrições de cobertura clássicas; as somas e a distinção de dígitos das cages adicionariam estado/propagação e aumentariam a complexidade inicial.
- OR-Tools CP-SAT: suportaria modelagem de restrições, mas acrescenta dependência e superfície de distribuição nativa sem necessidade demonstrada para um tabuleiro 9×9 offline.

### 2. Gerador: grade válida → cages → validação → unicidade → dificuldade

**Decision**: gerar uma grade solucionada por busca CSP com ordem de candidatos controlada por seed; particionar as 81 posições em cages conectadas por lados, sem repetir dígitos da solução em uma cage; usar a soma da solução como alvo e construir `PuzzleDefinition` sem valores fixos. As 81 células começam vazias e os alvos de cage são as pistas visíveis. Validar por `PuzzleStructureValidator`, contar soluções até 2 e analisar a dificuldade. Só aceitar o candidato se a contagem for `Unique` e o perfil lógico corresponder ao pedido.

Cada tentativa rejeitada consome o orçamento e não é publicada. `GenerationBudget` controla tentativas e duração; ao se esgotar, retornar `Unavailable` com métricas/motivo, sem puzzle e sem fallback de nível. Cancelamento propaga separadamente e não produz resultado parcial.

**Rationale**: segue a ordem de validação da constituição e usa as APIs estruturais existentes. As cages são derivadas da grade conhecida e, portanto, começam com conectividade, soma e ausência de repetição controladas; o validador e o solver ainda verificam o puzzle publicado de forma independente. Manter a solução separada evita revelar respostas como givens e preserva a proposta de um Killer Sudoku cujas pistas são as cages.

**Alternatives considered**: transformar uma grade canônica é simples, mas limita a variedade; dataset fixo ou solver remoto adiciona dependência/dados e não atende ao fluxo offline autônomo pretendido; gerar cages sem solução conhecida torna a construção e validação mais complexas.

### 3. Rubrica de dificuldade versionada e cumulativa

**Decision**: definir um catálogo versionado de IDs estáveis de técnica e perfis cumulativos, conforme o catálogo v1 normativo registrado na spec:

| Perfil | Técnicas habilitadas no perfil |
|---|---|
| Easy | Naked Single, Hidden Single, Cage Single |
| Medium | Todas de Easy + Cage Combination |
| Hard | Todas de Medium + Cage/Region Intersection, Rule of 45 |
| Expert | Todas de Hard + Naked Pair, Hidden Pair, Naked Triple |

Cada perfil tenta resolver o puzzle desde o início com prioridade e desempate determinísticos. A classificação é o primeiro perfil que conclui a resolução lógica completa. Se nenhum perfil concluir, retornar `Unclassifiable`. O solver de backtracking valida solução e unicidade, mas não pode completar a trilha lógica nem reduzir artificialmente a dificuldade. O catálogo e o número da versão acompanham a análise; a tabela é política explícita do produto, não padrão universal de Sudoku. A feature 003 explica as mesmas nove técnicas para que qualquer passo usado na classificação tenha ajuda pedagógica correspondente.

As deduções compartilháveis residem em Domain como passos sem explicação textual; `DifficultyAnalyzer` fica em Application e mantém a política de perfil. 003 pode reaproveitar essas deduções e produzir `HintResult`, textos e destaques sem fazer 002 depender do motor de dicas.

**Rationale**: a spec exige técnicas necessárias e dificuldade Expert, mas não define os conjuntos. Os documentos citam Single, cages, interseção/regra dos 45, pares e triplas. Avaliar o perfil cumulativo menos permissivo e congelar a ordem torna a categoria repetível, explicitável e testável.

**Alternatives considered**: pontuação por quantidade/tamanho de cages é incompatível com FR-006; contar técnicas sem executar um traço completo não comprova que o puzzle seja resolvível por elas; pedir a 003 um nível/`HintResult` criaria dependência arquitetural inversa; permitir backtracking no classificador misturaria solver computacional e raciocínio humano.

### 4. Seed e orçamento reproduzível

**Decision**: aceitar uma seed explícita nas opções de geração, usar a mesma ordenação estável em células, cages e candidatos e fixar o algoritmo usado nos fixtures. Testes usam orçamento por número de tentativas/operações, não timeout de relógio. A seed pode acompanhar métricas/resultados de diagnóstico, sem registrar tabuleiros completos nos logs.

**Rationale**: a constituição proíbe aleatoriedade não controlada em testes. A documentação de `System.Random` alerta que sequências geradas com a mesma seed não são garantidas como iguais entre versões da implementação; portanto a expectativa mínima é replay na versão .NET suportada e, caso estabilidade entre versões vire requisito de produto, o gerador deverá versionar um PRNG próprio antes de prometer esse contrato.

**Alternatives considered**: aleatoriedade do sistema em todos os caminhos impede replay; dependência de duração real torna testes variáveis por máquina; fixar agora um PRNG customizado não traz benefício demonstrado para o requisito atual.

### 5. Limites de produção definidos com medições

**Decision**: representar tentativas e duração como limites configuráveis. Não há número atual para inserirmos com evidência: o pipeline ainda não foi implementado, não há linha de base nem especificação de aparelho Android mínimo. Antes de liberar o gerador, medir Release em Windows e no Android mínimo escolhido, com corpus fixo de seeds; registrar duração de cada etapa, tentativas/rejeições, p50/p95/máxima e latência de cancelamento. Então definir/documentar valores padrão e verificar que o orçamento não cause fallback de nível.

**Rationale**: cumprir a clarificação do usuário e a constituição de performance mensurável sem tratar um timeout estimado em máquina diferente como resultado. As medições também decidirão se alguma otimização de busca é necessária.

**Alternatives considered**: limite arbitrário sem medição; só um limite de tempo (pouco reproduzível em fixtures); só um limite de tentativas em produção (pode não proteger responsividade num aparelho lento).

## Fontes consultadas

### Repositório

- `PRD.md` §§10–13 e 19.
- `constitution.md` e `specs/README.md`.
- `specs/001-killer-sudoku-rules/{plan.md,data-model.md,quickstart.md}` e `specs/003-logical-hints/spec.md`.
- `specs/002-puzzle-engine/spec.md`, `Ideia.md`, fontes em `src/CageLogic.Domain` e `src/CageLogic.Application` e projetos de teste.

### Fontes primárias externas

- Haralick e Elliott, *Increasing Tree Search Efficiency for Constraint Satisfaction Problems*, IJCAI 1979; versão de periódico em *Artificial Intelligence* 14 (1980): [artigo original em IJCAI](https://www.ijcai.org/Proceedings/79-1/Papers/004.pdf), [DOI](https://doi.org/10.1016/0004-3702(80)90051-X). Referência para forward checking e escolha de variáveis restritivas em busca CSP; não implica benchmark do nosso puzzle.
- Donald Knuth, [*Dancing Links*](https://arxiv.org/abs/cs/0011047). Alternativa de busca exact cover considerada.
- Microsoft Learn, [cancelamento cooperativo em threads gerenciadas](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) e [cancelamento de tasks](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation). Referência para propagar token, observar cancelamento e representar cancelamento de task.
- Microsoft Learn, [classe `System.Random` (.NET 10)](https://learn.microsoft.com/en-us/dotnet/api/system.random?view=net-10.0). Referência para aleatoriedade com seed e limites de compatibilidade entre versões.
- Google, [OR-Tools para .NET](https://developers.google.com/optimization/install/dotnet/). Fonte oficial da alternativa externa considerada; não selecionada nesta implementação.

## Medição local da implementação (2026-10-05)

O fixture `PuzzleGeneratorPerformanceTests` foi executado em Release no Windows 10 x64 (`10.0.19045`), SDK .NET `10.0.401` e runtime `10.0.12`, com dez seeds fixas Easy (`20261001` a `20261010`). Latência total: p50 `160,55 ms`, p95 `687,57 ms`, máximo `687,57 ms`. Durações p50/p95/máximo: grade `0,03/4,02/4,02 ms`; cages `0,25/9,26/9,26 ms`; validação estrutural `0,51/16,45/16,45 ms`; busca de unicidade `63,87/501,60/501,60 ms`; análise lógica `87,37/179,30/179,30 ms`. A carga controlada registrou três rejeições `InvalidStructure` em três tentativas; resposta ao token já cancelado foi `11,51 ms` nesta execução.

Isso é uma linha de base de ambiente compartilhado e puzzles Easy com cages singleton, não um orçamento de produção nem uma meta de latência. O repositório contém workloads Android e MAUI Windows, mas não contém host MAUI e não havia dispositivo Android selecionado/conectado para a medição. `GenerationBudget` permanece explícito e sem default; selecionar o aparelho/runtime Android mínimo e repetir o corpus continua como gate de release.

### Re-medição local (2026-10-06)

Após substituir a varredura completa por máscaras incrementais de linha, coluna, bloco e cage no solver, o benchmark opt-in foi executado em Release no Windows 10 x64 (`10.0.19045.0`), SDK .NET `10.0.401` e runtime `10.0.12`. No corpus Easy fixo (`20261001`–`20261010`), a latência total foi p50/p95/máxima `218,09/613,02/613,02 ms`; a busca de unicidade foi `13,21/95,93/95,93 ms`. Como comparação medida neste host, a linha de base de 2026-10-05 para unicidade era `63,87/501,60/501,60 ms`; o benchmark atual confirma a redução, embora as medições variem com a carga do ambiente.

Um fixture Hard (`408863218`) concluiu em `1.854,39 ms`: particionamento `2,93 ms`, unicidade `12,72 ms` e análise lógica `1.836,43 ms`. Um fixture Expert (`1597463005`) concluiu em `146,87 ms`: particionamento `6,09 ms`, unicidade `2,56 ms` e análise lógica `135,61 ms`. Hard e Expert têm uma amostra cada; os tempos não são percentis. A análise lógica foi o estágio dominante nesses fixtures, então a busca de unicidade não se mostrou o gargalo indicado para esses casos. Nenhuma medição Android foi feita; selecionar aparelho/runtime e executar o corpus nele segue como gate antes de definir orçamentos de produção.
