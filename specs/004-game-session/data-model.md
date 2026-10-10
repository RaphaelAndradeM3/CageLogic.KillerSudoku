# Modelo de dados: Sessão de Jogo

## Entidades

### GameSession

Coordena uma única partida ativa em memória. Mantém a dificuldade escolhida, o resultado de geração, o board atual, as notas, a seleção, o modo de edição, o histórico, a revisão do board, o estado do cronômetro, o estado da dica e as contagens históricas de erros/dicas. As contagens pertencem à sessão e não são restauradas nem alteradas por Undo/Redo. Não persiste entre processos nesta feature.

### PuzzleContext

Dados imutáveis da partida derivados de `GeneratedPuzzle`: puzzle/cages/givens validados, `SolutionGrid` e dificuldade classificada. A solução é usada para a tentativa de conclusão e não deve ser projetada como feedback durante a partida.

### BoardState

Snapshot imutável de `SudokuBoard`, composto por 81 `SudokuCell` e cages. Cada célula distingue valor fixo (`GivenValue`) de valor do jogador (`PlayerValue`). A seleção e as notas não são armazenadas no board.

### CandidateNotes

Mapa imutável ou cópia defensiva de `CellPosition -> conjunto de dígitos` para notas manuais. As notas pertencem apenas a células editáveis vazias na projeção. Dígitos válidos são 1–9. Inserir uma resposta limpa as notas da célula na mesma transação de histórico; Undo da inserção restaura o snapshot com as notas. Apagar a resposta posteriormente como uma nova ação não restaura as notas antigas.

`AutoFillCandidates` recalcula candidatos pelas regras do board atual e substitui as notas de todas as células vazias e editáveis. A operação produz um único estado de notas antes/depois no histórico. O cálculo não lê nem preserva notas manuais anteriores.

### SessionHistory

Duas pilhas de snapshots transacionais antes/depois contendo `SudokuBoard` e `CandidateNotes`. Inserção/apagamento de resposta, alteração manual de nota e preenchimento automático são transações. Uma transação sem diferença não é registrada. Uma nova ação após Undo limpa a pilha Redo.

Undo/Redo restauram apenas board e notas. Seleção, modo, pausa/retomada, tempo e apresentação de dica permanecem independentes. Se o board mudar durante Undo/Redo, atribuir uma nova `BoardRevision`; nunca restaurar um identificador histórico.

### BoardRevision

Contador `long` monotônico da versão dos valores do `SudokuBoard` usados em uma análise de dica. Incrementa quando valores do board realmente mudam, inclusive por Undo/Redo. Uma alteração só de CandidateNotes não o incrementa, pois notas não fazem parte do snapshot lógico recebido pelo `GetHintUseCase`. Seleção, modo, timer e estado visual também não o incrementam.

### HintProgress

Armazena o nível progressivo atual e, enquanto houver cálculo, o token/identificador do pedido associado ao snapshot e `BoardRevision`. Ao mudar o board, reinicia em `Explanation`, cancela o pedido anterior quando possível e calcula para o snapshot atual. Resultado com revisão diferente da sessão é descartado. `NoSafeHint` também só é apresentado se vier da revisão atual. Notas manuais não são entrada do analisador.

### ActiveTimer

Acumula `TimeSpan` ativo com timestamps monotônicos de `TimeProvider`. Mantém um início de trecho ativo apenas enquanto a sessão estiver em andamento e não pausada. A pausa encerra o trecho atual; Retomar inicia outro. O timer da interface atualiza a apresentação, sem ser a fonte de tempo.

### SessionSummary

Projeção criada somente depois que o board estiver completo, consistente e igual à `SolutionGrid`. Inclui dificuldade, tempo ativo e contagens. Um erro é cada ação que insere um valor que viola ao menos uma regra local; a mesma ação soma uma vez mesmo que produza vários conflitos, e correção posterior não decrementa. Undo/Redo não altera essa contagem histórica. Valor válido apenas divergente da solução não conta como erro. Uma dica é cada nível progressivo efetivamente exibido; `NoSafeHint`, cancelamentos e resultados obsoletos não contam.

### CageCompletionProjection

`GameSessionCageViewState` inclui `IsSatisfied`, calculado sobre os valores atuais da cage. O estado só é verdadeiro quando todas as posições estão preenchidas, os dígitos são distintos e a soma coincide com `TargetSum`. A interface usa esse estado para destacar visualmente todas as células da cage. A projeção nunca compara valores com `SolutionGrid`.

## Relações e ciclo de vida

```text
GeneratedPuzzle
  ├─ PuzzleContext (puzzle, solução, dificuldade)
  └─ GameSession
       ├─ BoardState (SudokuBoard imutável)
       ├─ CandidateNotes
       ├─ SessionHistory (board + notas)
       ├─ BoardRevision ── HintProgress/GetHintUseCase
       ├─ ActiveTimer
       └─ SessionSummary (somente ao concluir)
```

## Regras de consistência

- Células fixas não aceitam entrada/remoção de resposta.
- Conflitos permanecem no board, são projetados para correção e bloqueiam dicas seguras enquanto a validação estiver inconsistente.
- Valor localmente válido que diverge da solução não é sinalizado durante a sessão; igualdade com a solução só é consultada ao tentar concluir.
- Dica só pode ser projetada se `HintResult.BoardRevision == GameSession.BoardRevision`.
- Tempo fora do primeiro plano não conta; retorno do aplicativo mantém a sessão pausada até ação explícita de Retomar.
- A sessão não salva/restaura estado após encerramento do processo. Essa responsabilidade pertence à feature 005.
