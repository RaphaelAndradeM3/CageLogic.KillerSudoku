# Modelo de dados — Dicas Lógicas Progressivas

Este documento registra os contratos das bibliotecas Domain e Application para a sessão de jogo MAUI de 004.

## Tipos de entrada

### `HintPuzzleContext`

Vincula a definição original do puzzle à confirmação de solução produzida por 002.

| Campo | Tipo | Regra |
|---|---|---|
| `Puzzle` | `ValidatedPuzzle` | Estrutura e cages já passaram por `PuzzleStructureValidator`. |
| `Multiplicity` | `SolutionMultiplicity` | Resultado da busca limitada a duas soluções para o puzzle original. |
| `UniqueSolution` | `SolutionGrid?` | Obrigatória se `Multiplicity == Unique`; deve pertencer ao puzzle. Nula para `NoSolution` ou `Multiple`. |

O contexto não transforma uma busca restrita por jogadas do usuário em prova de unicidade do puzzle original. Para mostrar um valor como resposta certa, `Multiplicity` precisa ser `Unique`.

### `HintRequest`

| Campo | Tipo | Regra |
|---|---|---|
| `PuzzleContext` | `HintPuzzleContext` | Mantém a solução do puzzle original separada do tabuleiro editável. |
| `CurrentBoard` | `SudokuBoard` | Snapshot imutável com givens e jogadas atuais; suas cages e givens devem corresponder ao puzzle do contexto. |
| `Level` | `HintLevel` | Um de `Explanation`, `Highlights` ou `Action`, na ordem 1–3. |
| `BoardRevision` | `long` | Versão monotônica fornecida pela sessão para identificar o snapshot. |

`LogicalState.Create(CurrentBoard)` recalcula candidatos a partir dos valores e das regras. Candidatos anotados manualmente na interface não fazem parte da prova lógica.

## Evidência de dedução

### `LogicalStepEvidence` (Domain)

Dados tipados e sem texto para explicar por que a estratégia selecionou o passo.

| Campo | Tipo | Regra |
|---|---|---|
| `PatternPositions` | posições | Células que estabelecem o padrão lógico. |
| `ScopePositions` | posições | Linha, coluna, bloco, cage ou região considerados pela estratégia. |
| `RelevantDigits` | dígitos 1–9 | Dígitos que participam do raciocínio; nunca são texto localizado. |
| `PatternCandidates` | pares posição/dígito | Candidatos exatos que estabelecem o padrão; não indicam por si só uma ação de remoção. |
| `ScopeContext` | contexto tipado opcional | Informação específica necessária à explicação, como alvo de cage ou residual da Rule of 45. |

O efeito continua sendo carregado pelo `LogicalStep`: uma `LogicalPlacement` ou uma lista de `CandidateElimination`. `RelatedPositions` permanece útil como união ordenada das posições relacionadas; não substitui os papéis da evidência. A estratégia que já calcula a dedução fornece a evidência, evitando reconstruir regras em Application.

### Catálogo de explicações

Application mantém uma entrada em português por `LogicalTechniqueId` estável do catálogo v1: nome, explicação geral e instruções de formatação para as evidências. O texto não contém o valor nem as eliminações concretas do passo, para que o nível 1 não antecipe a ação.

## Resultado progressivo

### `HintLevel`

| Valor | Conteúdo permitido |
|---|---|
| `Explanation` (1) | ID/nome da técnica e explicação; sem células, alvo, dígito ou ação concreta. |
| `Highlights` (2) | Conteúdo do nível 1 e destaques tipados das posições e candidatos envolvidos; sem declarar valor a colocar nem candidatos a remover. |
| `Action` (3) | Conteúdo dos níveis anteriores e a ação lógica concreta. |

O destaque de candidatos no nível 2 identifica visualmente os candidatos envolvidos no padrão/efeito, sem afirmar que devem ser removidos. O resultado projeta `RelevantDigits`, `PatternCandidates` e `ScopeContext` quando compatíveis com o nível pedido. Em passos de eliminação, esses dados podem explicar o padrão no nível 2. Em passos de colocação, o nível 2 pode destacar a posição-alvo e o contexto, mas omite dígitos e pares posição/dígito que revelem o valor. No nível 3, `RemoveCandidates` identifica explicitamente quais devem ser removidos e a colocação passa a incluir o valor.

### `HintAction`

União discriminada que contém exatamente uma alternativa:

- `PlaceValue`: posição e dígito da colocação deduzida.
- `RemoveCandidates`: lista não vazia de pares posição/dígito a eliminar, ordenada por posição row-major e dígito crescente.

Não há colocação implícita em um passo que só elimina candidatos. Uma colocação só aparece no nível 3 quando a multiplicidade do puzzle original é `Unique`; caso contrário, o resultado usa `ValueNotConfirmed` e não inclui o dígito.

### `HintStatus` e `HintResult`

| Status | Significado e dados permitidos |
|---|---|
| `Available` | Um passo suportado existe; `HintResult` inclui conteúdo apenas até o nível pedido. |
| `PuzzleSolved` | O tabuleiro está completo e válido; mensagem indica que não há próximo passo lógico. |
| `InconsistentState` | Há conflito de regras ou nenhuma conclusão compatível com o snapshot; não inclui dica nem valor. |
| `NoSafeHint` | O estado é válido, mas nenhuma técnica do catálogo v1 encontra um passo; não inclui dica nem valor. |
| `ValueNotConfirmed` | O nível 3 pediu uma colocação, mas o puzzle original não tem unicidade confirmada; não inclui valor. Passos de eliminação continuam tipados como eliminação. |

Todo resultado ecoa `BoardRevision`. `HintResult` não carrega o `LogicalStep` completo nos níveis 1 e 2: inclui apenas a projeção permitida, com dígitos, pares posição/dígito e contexto de escopo condicionados ao nível para impedir vazamento acidental.

## Validação e estado

1. Rejeitar contexto ou snapshot cuja estrutura não corresponda ao puzzle original.
2. Se `SudokuBoardValidator` encontrar conflito, retornar `InconsistentState`.
3. Se o tabuleiro estiver completo e correto, retornar `PuzzleSolved` antes de procurar técnica.
4. Se o puzzle original for único, qualquer valor de jogador diferente de `UniqueSolution` significa que não há conclusão compatível; retornar `InconsistentState` sem dica. Se a origem for `Multiple`, limitar a busca ao snapshot com jogadas atuais: `NoSolution` retorna `InconsistentState`; uma solução ou uma solução única nesse snapshot não altera a multiplicidade da origem nem libera valor de colocação, que retorna `ValueNotConfirmed` no nível 3.
5. Em estado compatível, consultar `ILogicalStepAnalyzer` e seguir a prioridade e o desempate estáveis de 002. Se nenhum passo existir, retornar `NoSafeHint`.
6. O caso de uso ecoa `BoardRevision` recebido no pedido. A alteração da revisão invalida a progressão atual; a sessão de 004 descarta resposta obsoleta, cancela o pedido anterior quando possível e solicita novamente para o snapshot mais recente começando no nível 1, conforme FR-013/SC-005.

Cancelamento de operações CPU-bound propaga `OperationCanceledException`; não vira um status de dica. Falhas inesperadas também não viram `NoSafeHint`: sobem para tratamento seguro e logging na fronteira do host futuro.
