# Modelo de dados — Motor de puzzles

Este desenho estende `CageLogic.Domain` e `CageLogic.Application` existentes. Os nomes abaixo descrevem os contratos e invariantes pretendidos; não afirmam que as classes já foram implementadas. `PuzzleDefinition`, `ValidatedPuzzle`, `SudokuBoard`, `CandidateSet`, `CageDefinition` e os validadores de 001 continuam sendo as representações de entrada/estado estrutural existentes.

## Tipos

### SolutionGrid

Representa a solução completa, separada do estado de jogo `SudokuBoard`.

| Campo | Tipo | Regra |
|---|---|---|
| Values | coleção imutável de 81 dígitos | Cada valor é de 1 a 9; índices usam `CellPosition`/ordem estável de tabuleiro. |

Invariantes: cada linha, coluna e bloco 3×3 contém 1–9 uma vez; cada cage respeita soma e não repetição; a grade completa passa no validador do domínio para o puzzle correspondente. Valores não podem ser alterados depois da construção.

### SolutionSearchResult

Resultado da busca/count do solver sobre um `ValidatedPuzzle`.

| Campo | Tipo | Regra |
|---|---|---|
| Multiplicity | `NoSolution`, `Unique`, `Multiple` | `Multiple` quer dizer duas ou mais; a contagem termina na segunda grade completa válida. |
| FirstSolution | `SolutionGrid?` | Nulo se `NoSolution`; presente para `Unique` e `Multiple`. |
| SolutionsFoundUpToLimit | inteiro 0–2 | Contagem limitada, nunca apresentada como total exato acima de 2. |

`ValidatedPuzzle` é pré-condição: erro estrutural deve ser retornado pelo fluxo de `PuzzleStructureValidator`, não confundido com puzzle estruturalmente válido que possui zero soluções. Contradição entre givens pode resultar em `NoSolution`. Cancelamento é `OperationCanceledException`, não um valor de multiplicidade.

### LogicalTechniqueId, LogicalStep e DifficultyProfile

IDs estáveis identificam a regra aplicada e não incluem nome localizado ou texto para o jogador.

| Tipo | Campos | Regras |
|---|---|---|
| `LogicalTechniqueId` | código estável, nome de referência | ID não muda ao traduzir a interface; novas técnicas têm definição/fixture próprios. |
| `LogicalStep` | ID de técnica; alterações lógicas (atribuição e/ou eliminação); posições relacionadas | Passo válido para o estado informado; não carrega texto de hint nem presentation model. |
| `LogicalState` | `SudokuBoard`; candidatos restantes por célula vazia | Imutável; começa com candidatos do `CandidateCalculator`; persiste eliminações entre passos e recalcula após colocação sem recuperar candidatos eliminados. |
| `DifficultyProfile` | nível, versão de catálogo, IDs de técnicas permitidas, prioridade determinística | Perfis são cumulativos. Catálogo v1 ordena as técnicas como Naked Single, Hidden Single, Cage Single, Cage Combination, Cage/Region Intersection, Rule of 45, Naked Pair, Hidden Pair, Naked Triple. Dentro da técnica, ordenar posições relacionadas em row-major, dígito crescente e colocação antes de eliminação; a versão fixa esse desempate. |
| `DifficultyAnalysisResult` | estado `Classified`/`Unclassifiable`; nível se classificado; versão; trilha de IDs | `Classified` só quando a trilha lógica completa o puzzle. Tentativa e erro não pode concluir a trilha. |

`LogicalState` é um estado imutável da análise lógica: `SudokuBoard` atual mais o conjunto restante de candidatos por célula vazia. Ele é inicializado com `CandidateCalculator`; eliminações de um `LogicalStep` persistem nas iterações seguintes. Ao aplicar uma colocação, atualiza-se o tabuleiro e recalculam-se candidatos básicos, intersectando-os com as eliminações preservadas. Candidatos eliminados não reaparecem durante a trilha. As estratégias consomem `LogicalState` e produzem no máximo um próximo `LogicalStep` determinístico.

Uma atribuição válida de cage completa todas as células vazias com candidatos atuais, respeita o alvo exato da cage e não repete dígitos dentro dela; valores já preenchidos são fixos. `Cage Single`, `Cage Combination`, `Cage/Region Intersection` e `Rule of 45` usam essas atribuições. Para Rule of 45, o conjunto das partes em cage que cruzam uma região deve atingir `45` menos a soma dos alvos das cages inteiramente contidas; uma atribuição sem esse suporte não pode justificar candidato.

Catálogo v1 normativo:

| Nível | Técnicas disponíveis cumulativamente |
|---|---|
| Easy | Naked Single, Hidden Single, Cage Single |
| Medium | Easy + Cage Combination |
| Hard | Medium + Cage/Region Intersection, Rule of 45 |
| Expert | Hard + Naked Pair, Hidden Pair, Naked Triple |

Cada nível é testado do início do puzzle e o classificador escolhe o perfil menos avançado que resolve por completo e passa pelo validador. Aplicam-se os passos na ordem determinística descrita acima; as posições e os efeitos de cada passo também são ordenados de forma estável. Se nenhuma sequência permitida concluir o estado, retornar `Unclassifiable`; não usar a solução do `SudokuSolver` como passo lógico. Os IDs são independentes de texto/localização e podem ser consumidos pela feature 003.

### PuzzleGenerationRequest e GenerationBudget

| Tipo | Campos | Regras |
|---|---|---|
| `PuzzleGenerationRequest` | `Difficulty`, seed opcional, `GenerationBudget` | Dificuldade e orçamento são obrigatórios; seed explícita é usada pelos fixtures/replay. |
| `GenerationBudget` | `MaxAttempts` inteiro positivo; `TimeLimit` `TimeSpan` positivo | O tempo é contado monotonicamente desde o início do pedido; tentativa conta quando uma nova grade/candidato começa a ser produzido. O primeiro limite atingido encerra a busca. A API não fornece padrão de produção até as medições nos dispositivos-alvo; todo chamador precisa passar um orçamento. Fixtures fixam tentativas e não dependem de timeout de relógio. |

Seed igual, algoritmo/versão, pedido, catálogo e orçamento iguais devem percorrer as mesmas escolhas e produzir o mesmo resultado no runtime de suporte. Sem seed, o gerador pode variar entre pedidos. Se estabilidade byte-a-byte entre futuras versões de runtime for requisito, versionar um PRNG próprio antes de prometer esse comportamento.

### GeneratedPuzzle e PuzzleGenerationResult

| Tipo | Campos | Regras |
|---|---|---|
| `GeneratedPuzzle` | `ValidatedPuzzle`; `SolutionGrid`; `DifficultyAnalysisResult`; seed usada quando disponível; `Attempts` inteiro; `Elapsed` `TimeSpan` | Só existe depois de estrutura, unicidade e perfil pedido aprovados. Para o MVP, o `ValidatedPuzzle` gerado contém zero givens: todas as células estão vazias para o jogador e a solução permanece separada das pistas de cage. |
| `PuzzleGenerationResult` | variante `Success(GeneratedPuzzle)` ou `Unavailable(reason, attempts, elapsed)` | `Unavailable` não inclui puzzle nem solução parcial e não troca a dificuldade pedida. Só representa esgotamento do orçamento. Cancelamento do chamador propaga `OperationCanceledException`, não é variante do resultado. Se cancelamento do chamador e deadline ocorrerem juntos, cancelamento do chamador prevalece. |
| `UnavailableReason` | `AttemptBudgetExhausted` ou `TimeBudgetExhausted` | Motivo explícito do primeiro limite observado. O deadline também é observado durante as etapas longas por cancelamento interno; ao vencê-lo sem cancelamento do chamador, descarta o candidato em curso e retorna `Unavailable`. |

## Relações e fluxo de estado

```text
PuzzleGenerationRequest
        │
        ▼
grade completa ──► cages conectadas/alvos ──► PuzzleDefinition
                                               │
                                               ▼
                                      ValidatedPuzzle
                                               │
                            ┌──────────────────┴─────────────────┐
                            ▼                                    ▼
                 SolutionSearchResult                    DifficultyAnalysisResult
                    Unique?                                perfil solicitado?
                            └──────────────────┬─────────────────┘
                                               ▼
                                    GeneratedPuzzle (Success)
```

- Candidate inválido, `NoSolution`, `Multiple` ou `Unclassifiable` é descartado e inicia nova tentativa enquanto o orçamento permitir.
- `Unique` e perfil correspondente são ambas condições para publicar; validar a solução encontrada contra o tabuleiro/cages continua obrigatório.
- Ao atingir qualquer orçamento antes de aceitar, retornar `Unavailable` com motivo, número de tentativas iniciadas e tempo decorrido, sem fallback. O chamador pode solicitar explicitamente uma nova geração/dificuldade.
- Cancelamento em qualquer etapa encerra a operação sem transição para `Success` e sem expor estado parcial.

## Invariantes de publicação

1. As cages cobrem cada célula exigida pela definição uma vez, são conectadas por lados e não repetem valores dentro da cage.
2. Cada alvo é alcançado por dígitos distintos permitidos e coincide com a solução completa mantida internamente.
3. A estrutura passou por `PuzzleStructureValidator`; a solução passa regras de Sudoku/Killer.
4. `SolutionSearchResult.Multiplicity == Unique`.
5. A análise lógica terminou pela primeira vez no nível pedido, segundo a versão/tie-break documentados.
6. O `GeneratedPuzzle` não pode ser construído a partir de candidato incompleto, não único, não classificável ou cancelado.
7. A definição entregue pelo gerador não contém valores fixos; a solução validada não é copiada para as células iniciais.
