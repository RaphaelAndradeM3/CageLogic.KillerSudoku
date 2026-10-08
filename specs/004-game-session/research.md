# Pesquisa técnica: Sessão de Jogo

**Feature**: `004-game-session`
**Data**: 2026-10-08

## Estado atual do repositório

- A solution já contém `CageLogic.Domain`, `CageLogic.Application` e projetos NUnit para ambos; os projetos de biblioteca usam .NET 10.
- Ainda não há host MAUI nem projeto Infrastructure. O README prevê Windows e Android, `GraphicsView`, CommunityToolkit.Mvvm e DI.
- `SudokuBoard` é imutável e separa `GivenValue` de `PlayerValue`. `ApplyMoveUseCase` mantém uma jogada que conflita com as regras e retorna o resultado da validação; valores fixos são rejeitados como edição.
- `GeneratedPuzzle` entrega puzzle validado, solução, dificuldade classificada e metadados de geração. `GeneratePuzzleUseCase.ExecuteAsync` já executa geração CPU-bound fora da thread chamadora e recebe cancelamento.
- `GetCandidatesUseCase` e o calculador do Domain fornecem candidatos pelas regras. `GetHintUseCase` recebe snapshot, nível e revisão e retorna `HintResult.BoardRevision`.
- A configuração de logging ainda precisa de um limite de host/Infrastructure para atender `specs/README.md` sem adicionar Serilog a Domain ou Application.

## Decisões técnicas

### Host e desenho

Usar um host MAUI Windows/Android. O tabuleiro será um controle composto que desenha a grade por `GraphicsView`/`IDrawable`; a mesma transformação de coordenadas será usada para desenho e hit-test. Redimensionamento recalcula a geometria e um toque fora das 81 células não gera comando. O controle emite posição/intenções, e a sessão ouve comandos tipados; desenho não valida jogadas nem calcula candidatos.

Um canvas não deve ser presumido como substituto de 81 alvos semânticos. A solução expõe cada célula e controle à árvore de acessibilidade, mantém foco visível e não depende só de cor para comunicar seleção/conflito/dica. A implementação escolherá entre controles semânticos por célula e peers nativos conforme o host, com verificação manual por TalkBack e Narrator. O contrato de produto define setas para navegação sem circular nas bordas, 1–9 para editar conforme o modo, Backspace/Delete para limpar e Ctrl+Z/Ctrl+Y para Undo/Redo.

### Coordenação e histórico

Manter a sessão em Application, consumindo os casos de uso existentes e preservando `SudokuBoard` como snapshot imutável. Notas manuais ficam em mapa separado de `CellPosition` para dígitos; assim notas não se confundem com respostas nem entram no analisador de dicas. Inserir uma resposta remove as notas dessa célula na mesma transação; Undo restaura o estado anterior. Apagar a resposta posteriormente não restaura essas notas. Undo/redo guarda o antes/depois conjunto do board e das notas. Preenchimento automático substitui as notas de todas as células vazias e editáveis numa única transação. Seleção, modo, pausa e tempo ficam fora do histórico. Operação sem alteração efetiva não cria entrada.

`BoardRevision` identifica conteúdo do board analisado pela dica: cresce monotonicamente quando valores do board mudam, inclusive undo/redo que altera valores; não volta ao número de uma revisão restaurada. Edição isolada de notas não muda a revisão porque o analisador recebe somente o board. Cada dica usa um snapshot e sua revisão; cancelamento é cooperativo e a comparação da revisão continua obrigatória antes da exibição.

### Assíncrono e ciclo de vida

Geração e dicas são operações assíncronas canceláveis. A tela de início oferece cancelamento explícito da geração. Manter apenas um pedido de geração e um de dica em execução; comandos repetidos enquanto o mesmo comando está ocupado são ignorados/desabilitados em vez de enfileirados. Validação de jogadas e cálculo de candidatos também não podem bloquear a thread de UI: coordenar esses cálculos por comandos assíncronos e publicar a projeção observável na thread principal, sem mover regras ou cálculos para ViewModels. Edição pode continuar durante uma dica; mudança nos valores do board cancela o pedido anterior, reinicia a progressão desde `Explanation` e solicita nova análise. Alteração só de candidatos não muda `BoardRevision` nem invalida o snapshot. Um resultado tardio sempre é descartado se sua revisão não corresponder à atual.

Usar eventos MAUI `Window.Deactivated` e `Window.Stopped` como sinais idempotentes para pausar automaticamente. `Window.Resumed` não retoma a sessão por si só. O tempo ativo deve usar relógio monotônico via `TimeProvider.GetTimestamp`/`GetElapsedTime`; timer de UI só atualiza texto/visão. Encerramento do processo pode perder a sessão e está fora desta feature; persistência é de 005.

### Logging e testes

Configurar Serilog na composition root MAUI ou no adaptador Infrastructure, acessível pela abstração `Microsoft.Extensions.Logging`. Preservar arquivos diários separados para mensagens gerais e erros, timestamp com offset, correlation ID, retenção configurada, ausência de dados sensíveis e fallback seguro se o logger falhar. Testar a política de arquivos com diretório temporário e sink controlado; Domain/Application não dependem de Serilog.

Ampliar testes NUnit de Application para transações de sessão, conflito, notas, histórico, timer com `TimeProvider` controlável, revisão de dica e concorrência. Testes de UI automatizados não são pré-requisito; validar manualmente interação, redimensionamento e acessibilidade em Windows e Android. Não existe workload MAUI confirmado por esta etapa de planejamento.

## Alternativas consideradas

| Decisão | Alternativa | Motivo da escolha |
|---|---|---|
| Estado de sessão em Application | Colocar regras/estado dentro do ViewModel | Evita acoplar regras e histórico ao MAUI e permite testes NUnit sem host. |
| `GraphicsView` para o desenho | 81 controles visuais independentes como grade principal | Canvas simplifica desenho proporcional; acessibilidade semântica ainda será exposta explicitamente. |
| Notas fora de `SudokuBoard` | Tratar notas como valores do board | Mantém semântica do domínio e impede que o analisador de dicas confunda anotação com estado lógico. |
| Relógio monotônico `TimeProvider` | Contar ticks de timer da interface | O tempo não acumula atraso de renderização e pode ser testado deterministicamente. |
| Projeto Infrastructure para logging | Escrever arquivos diretamente em ViewModel ou Application | Isola I/O e a configuração Serilog exigida pelos guardrails; poderá atender a persistência de 005. |

## Decisões de produto incorporadas

- Erro: uma entrada de resposta que viola uma ou mais regras locais conta uma vez; correção, Undo ou Redo não alteram a contagem histórica. Um valor localmente válido apenas divergente da solução não conta.
- Dica: cada nível progressivo realmente exibido conta uma vez; `NoSafeHint`, cancelamento e resultado obsoleto não contam.
- Acessibilidade: as 81 células e os controles são alcançáveis por TalkBack no Android e Narrator no Windows, com posição, valor, fixo/editável, notas e estados de seleção/conflito/dica anunciados. Teclas: setas, 1–9, Backspace/Delete, Ctrl+Z/Ctrl+Y.
- Notas: inserir resposta limpa as notas como parte da mesma ação; Undo dessa ação as restaura. Uma remoção de resposta posterior deixa notas vazias.

A spec não fixa meta numérica de latência. Manter a UI responsiva durante validação, auto-candidates, geração e dicas; medir p95 nos dispositivos-alvo como linha de base antes de propor um limite de aceitação. Isso não impede decompor as tarefas funcionais.

## Referências primárias

- [Ciclo de vida de aplicativos MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle) — eventos da janela e relação com estados das plataformas.
- [GraphicsView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/graphicsview?view=net-maui-10.0) e [gráficos MAUI](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/?view=net-maui-10.0) — desenho por `IDrawable`/`ICanvas`.
- [Acessibilidade MAUI](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/accessibility?view=net-maui-10.0) — semântica e acessibilidade dos controles.
- [Injeção de dependência em MAUI](https://learn.microsoft.com/en-us/dotnet/architecture/maui/dependency-injection) — composition root e registros.
- [TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) e [GetElapsedTime](https://learn.microsoft.com/dotnet/api/system.timeprovider.getelapsedtime?view=net-10.0) — medição monotônica e relógio substituível em testes.
- [RelayCommand assíncrono](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/relaycommand) — comandos MVVM e prevenção de concorrência simultânea.
- `specs/README.md` — arquitetura, logging e gates de validação do repositório.
