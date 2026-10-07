# Pesquisa — Dicas Lógicas Progressivas

## Baseline local

- `CageLogic.slnx` contém bibliotecas `CageLogic.Domain` e `CageLogic.Application` em `net10.0` e projetos NUnit 5 para ambas. O projeto Application referencia Domain; não há host MAUI, Infrastructure, composition root ou registro de DI no código atual.
- A constituição em `.specify/memory/constitution.md` ainda contém somente placeholders do template. Os guardrails aplicáveis estão em `specs/README.md`.
- 002 já fornece `LogicalTechniqueId`, `LogicalStep`, `LogicalState`, `ILogicalStepAnalyzer`, `LogicalStepAnalyzer`, `CandidateCalculator`, `SudokuBoardValidator`, `SudokuSolver`, `FindSolutionsUseCase`, `SolutionGrid` e `GeneratedPuzzle`.
- `LogicalStep` contém exatamente uma colocação ou uma ou mais eliminações e posições relacionadas ordenadas. `LogicalState.Create` calcula candidatos a partir do tabuleiro; não aceita anotações manuais como prova. `LogicalStepAnalyzer` aplica a prioridade do catálogo v1 e os desempates determinísticos definidos em 002.
- Naked Single, Hidden Single e Cage Single produzem colocações nos fixtures existentes. Cage Combination, Cage/Region Intersection, Rule of 45, Naked Pair, Hidden Pair e Naked Triple produzem eliminações.
- `RelatedPositions` não distingue posições de apoio, escopo e efeito nem registra todos os dígitos/contextos usados pela técnica. Só copiar essa lista não basta para explicar de forma verificável alguns padrões, como Hidden Pair.
- `SudokuBoardValidator` detecta conflitos locais, não a ausência de uma conclusão global. `CandidateCalculator` também não prova solvabilidade. `GeneratedPuzzle` já contém puzzle validado e solução completa, e o gerador só o publica depois de confirmar unicidade.

## Decisões

### 1. Reutilizar o catálogo lógico de 002 e anexar evidências estruturadas

**Decision**: `GetHintUseCase` chama `ILogicalStepAnalyzer` e consome o `LogicalStep` escolhido por 002. Não reimplementar nem priorizar técnicas em Application. Estender o passo com evidência estruturada e sem texto localizado: posições de padrão/apoio, posições de efeito, dígitos relevantes e contexto de escopo necessário (por exemplo região, cage/alvo ou residual da Rule of 45). A evidência é produzida pela própria estratégia que já reconhece a dedução.

**Rationale**: mantém uma fonte única para regras e desempates. As evidências permitem que Application descreva por que aquele passo se aplica e que 004 destaque as células corretas; o consumidor não precisa deduzir o padrão a partir de uma lista indistinta de posições.

**Alternatives considered**:

- Recalcular o padrão em Application duplicaria regras e poderia discordar do `LogicalStep` selecionado.
- Usar apenas prosa genérica por técnica e `RelatedPositions` não identifica com precisão as células de apoio e os dígitos de todos os padrões.
- Armazenar explicações localizadas no Domain misturaria linguagem/apresentação com deduções reutilizáveis.

### 2. Manter textos em Application e indexá-los por ID estável

**Decision**: manter um catálogo pequeno de nome e explicação em português em `CageLogic.Application`, indexado por `LogicalTechniqueId`. O catálogo combina o texto da técnica com as evidências tipadas do passo. Não adicionar pacote de localização nem interface de provider para uma única língua inicial.

**Rationale**: os IDs permanecem estáveis e independentes do texto; Domain continua neutro quanto a idioma e UI. O catálogo é simples de testar e poderá ser migrado para recursos localizados quando existir uma segunda língua ou infraestrutura de localização em MAUI.

**Alternatives considered**: textos no Domain; novos serviços/interfaces de localização sem consumidor atual; tradução ou pacote externo antes do requisito.

### 3. Projetar uma saída por nível, sem vazar ações futuras

**Decision**: definir `HintLevel` com três níveis e `HintResult` como projeção apropriada ao nível pedido. Nível 1 retorna ID/nome da técnica e explicação. Nível 2 acrescenta papéis de destaque para posições e candidatos envolvidos, sem declarar a ação lógica. Nível 3 retorna uma ação discriminada: `PlaceValue` para passo de colocação ou pares posição/dígito a remover para passo de eliminação. A resposta não expõe o `LogicalStep` completo nos níveis 1 e 2. Estados terminais distintos representam `PuzzleSolved`, `InconsistentState`, `NoSafeHint` e `ValueNotConfirmed`.

**Rationale**: satisfaz os três níveis aprovados e impede que uma tela mostre acidentalmente a ação antes da última etapa. No caso de eliminação, o segundo nível pode destacar visualmente os candidatos envolvidos, como pedido na clarificação, enquanto o terceiro nível os identifica explicitamente como candidatos a remover. Eliminação continua sendo um passo lógico completo, sem fingir que coloca uma resposta.

**Alternatives considered**: retornar o passo completo em todos os níveis e confiar que cada ViewModel o oculte; retornar somente texto sem ação tipada; descartar as técnicas que só eliminam candidatos.

### 4. Validar conflito, conclusão, compatibilidade e unicidade separadamente

**Decision**: validar primeiro o snapshot atual com `SudokuBoardValidator`; se completo e correto, retornar `PuzzleSolved`; se houver conflito local, retornar `InconsistentState`. Para puzzle gerado e único, comparar valores inseridos pelo jogador com `GeneratedPuzzle.Solution`; divergência significa nenhuma conclusão compatível e bloqueia todas as dicas. Para entradas sem prova de unicidade, não expor valor de colocação; se a origem puder ser múltipla, usar uma busca limitada pelo estado atual para separar estado sem conclusão de estado ainda compatível. A busca não substitui a prioridade lógica nem completa uma explicação.

**Rationale**: validação local não detecta todos os erros. O caminho normal não precisa executar uma busca de unicidade em cada nível: o gerador já fornece a solução única confirmada. A busca permanece disponível para entradas fora desse contrato. Multiplicidade do puzzle original e compatibilidade do tabuleiro em jogo são informações diferentes; unicidade criada apenas pelas jogadas não autoriza revelar um valor como resposta do puzzle original.

**Alternatives considered**:

- Aceitar qualquer `LogicalState` após validação local deixaria dicas disponíveis em um tabuleiro sem solução global.
- Rodar busca completa para todo nível de toda dica repetiria trabalho já concluído pelo gerador.
- Usar o primeiro resultado de um puzzle múltiplo como se fosse solução única produziria uma resposta enganosa.

### 5. Deixar revisão do snapshot e recálculo de dica obsoleta com a sessão

**Decision**: cada pedido trabalha sobre `SudokuBoard` imutável e carrega uma revisão monotônica da partida. O resultado ecoa essa revisão. A sessão/ViewModel de 004 descarta respostas cuja revisão deixou de ser atual, cancela o pedido antigo quando possível e solicita uma nova dica para o snapshot atual começando no nível 1, conforme FR-013/SC-005 de `004-game-session`. O nível de progressão é transitório; uma alteração do tabuleiro invalida a sequência anterior.

**Rationale**: Application não possui a sessão mutável para observar uma jogada ocorrida enquanto a chamada está em execução. O dono do estado consegue comparar revisão e reenviar sem estado global ou callback de UI dentro do caso de uso.

**Alternatives considered**: manter sessão ou estado de dica mutável dentro do Domain/Application; publicar um resultado antigo com aviso; criar antecipadamente um provider de sessão que só existirá em 004.

### 6. Expor caso de uso da Application e deixar UI/DI para 004

**Decision**: criar contrato de aplicação presentation-neutral para 004, sem criar ViewModel, projeto MAUI, composition root ou registro falso de DI em 003. A tela futura decide desenho, cores e animação a partir de posições e papéis tipados. Domain não recebe dependência de MAUI. Resultados esperados são estados explícitos; cancelamento propaga; falhas inesperadas sobem até a fronteira do host. A feature 004 registra `GetHintUseCase` e suas dependências necessárias no composition root MAUI real introduzido na fatia 3, conforme `specs/README.md`.

**Rationale**: `specs/README.md` e 004 atribuem regras à Application/Domain e interação a MVVM/MAUI. A árvore atual não contém o host nem lugar real de registro. O contrato e o guia de validação deixam 004 livre para integrar sem adiantar sua infraestrutura.

**Alternatives considered**: criar um app MAUI incompleto nesta feature; colocar regras em ViewModel; adicionar `IServiceCollection`, Serilog ou ILogger ao Domain/Application sem composition root; criar interfaces sem uma fronteira existente.

### 7. Executar análise de CPU fora da UI e propagar cancelamento

**Decision**: expor a busca de dica como `ExecuteAsync(..., CancellationToken)` e executar a análise síncrona de CPU no thread pool, seguindo `FindSolutionsUseCase`/`GeneratePuzzleUseCase`. `ILogicalStepAnalyzer` continua síncrono e recebe o token cooperativo. Não envolver algoritmos síncronos em `async` artificial nem bloquear com `.Wait()`/`.Result`.

**Rationale**: reutiliza o padrão do repositório e mantém o futuro host responsivo. A orientação oficial de C# recomenda `Task.Run` para cálculo CPU-bound e `await` para não bloquear a thread chamadora; cancelamento em .NET é cooperativo e deve ser propagado aos passos longos ([async scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios), [cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)).

**Alternatives considered**: executar análise potencialmente longa na thread da UI; tornar Domain assíncrono sem I/O; capturar cancelamento como falha ou `NoSafeHint`.

### 8. Não inventar orçamento de latência nem gate MAUI

**Decision**: não estabelecer p95 numérico sem medição e sem aparelho Android mínimo definido. Fixar o escopo em tabuleiro 9×9, testes determinísticos e cancelamento. Aferir tempo de compatibilidade/busca em Release no host futuro antes de propor um orçamento de produção. 003 valida bibliotecas .NET atuais; build visual Windows/Android fica para 004, quando houver MAUI e workloads.

**Rationale**: o benchmark existente de 002 mede geração/classificação em amostras pequenas e não é uma linha de base de dica por pedido. Não extrapolar número de outra etapa ou máquina.

**Alternatives considered**: p95 arbitrário; exigir workload MAUI antes de o host existir; usar tempo de relógio como assert determinística em NUnit.

## Fontes consultadas

### Repositório

- `specs/README.md` e `.specify/memory/constitution.md`.
- `specs/003-logical-hints/spec.md`, `specs/004-game-session/spec.md` e `specs/005-offline-progression/spec.md`.
- `specs/002-puzzle-engine/{spec.md,data-model.md,research.md,quickstart.md}`.
- `src/CageLogic.Domain/LogicalSteps/`, `src/CageLogic.Domain/Validation/`, `src/CageLogic.Application/{Difficulty,Generation,Solving,Validation}/`.
- `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`, `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalStateTests.cs`, e projetos NUnit de Application.

### Fonte primária externa

- Microsoft Learn, [Asynchronous programming scenarios](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios): separa cálculo CPU-bound de I/O e recomenda mover cálculo para `Task.Run` quando a responsividade do chamador é necessária.
- Microsoft Learn, [Cancellation in managed threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads): define cancelamento cooperativo, propagação do token e tratamento de `OperationCanceledException`.
