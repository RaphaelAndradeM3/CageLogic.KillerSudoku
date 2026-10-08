# Plano de Implementação: Sessão de Jogo Killer Sudoku

**Branch**: 004-game-session | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**Input**: Especificação em specs/004-game-session/spec.md, derivada do PRD RF-001 a RF-013 e RF-016 a RF-020.

## Resumo

Introduzir o host .NET MAUI para Windows e Android e uma coordenação de sessão em Application. O host compõe os casos de uso existentes de geração, jogadas, candidatos, validação e dicas; apresenta a grade 9×9 e encaminha toque, mouse e teclado à mesma intenção. A sessão mantém em memória a partida, as notas, o histórico, a revisão de tabuleiro, a progressão de dicas e o tempo ativo. Regras e deduções continuam em Domain/Application. A persistência de sessão encerrada, preferências e estatísticas agregadas continua na feature 005.

## Contexto técnico

**Linguagem/versão**: C# / .NET 10, nullable habilitado conforme os projetos existentes.

**Dependências principais**: .NET MAUI para o host Windows/Android; GraphicsView e Microsoft.Maui.Graphics para renderizar a grade; CommunityToolkit.Mvvm para estado observável e comandos assíncronos; Microsoft.Extensions.DependencyInjection e Microsoft.Extensions.Logging; CageLogic.Application e CageLogic.Domain. Serilog e sinks de arquivo ficam no limite de Infrastructure/host, sem dependência Serilog no núcleo.

**Armazenamento**: estado da partida apenas em memória durante o processo. Persistência após fechamento, retomada entre execuções, preferências e estatísticas agregadas são escopo de 005. Logs locais ficam sob a pasta de dados da aplicação, não junto ao executável ou ao repositório.

**Testes**: NUnit 5 e NUnit.Analyzers existentes. Ampliar CageLogic.Application.Tests para coordenador, histórico, revisão, pausa e conclusão; acrescentar testes de Infrastructure para logging diário e tratamento seguro. Validar visualmente e por interação nos alvos Windows e Android depois de configurar workloads/emulador. Não introduzir framework de automação de UI sem necessidade demonstrada.

**Plataforma-alvo**: Windows 10 19041 ou posterior e Android, com targets net10.0-windows10.0.19041.0 e net10.0-android. Os workloads MAUI e os SDKs de plataforma são pré-requisitos dos builds de host.

**Tipo de projeto**: aplicação desktop/móvel .NET MAUI, com bibliotecas Domain, Application e Infrastructure.

**Metas de desempenho**: a spec exige responsividade mas não aprova meta numérica. Não bloquear a thread de UI com validação de jogadas, cálculo de candidatos, geração, análise de dicas ou gravação de logs. Coordenar cálculos por comandos assíncronos e publicar projeções/alterações observáveis na thread principal. Apresentar carregamento antes de aguardar geração ou dica e manter edição disponível enquanto a dica estiver pendente. Medir nos devices alvo antes de propor uma meta numérica de latência p95; até lá, as medições são uma linha de base. O escopo de cálculo é um tabuleiro de 81 células.

**Restrições**: funcionamento sem conexão; puzzle único confirmado gerado por 002; regras, candidatos, solver e geração fora de ViewModels/controles; jogada conflitante continua editável e visível; resposta que apenas diverge da solução não recebe indicação durante a partida; notas automáticas substituem todas as notas em células vazias e são uma única ação de histórico; somente mudanças nos valores do `SudokuBoard`, inclusive Undo/Redo que altere esses valores, incrementam a `BoardRevision` e invalidam dicas; mudanças somente nas notas de candidatos não alteram a revisão nem invalidam o cálculo; pausa automática ao sair do primeiro plano e retomada somente por ação explícita; sem persistência de sessão em disco nesta feature.

**Escala/escopo**: uma sessão ativa e um tabuleiro 9×9 por janela de jogo; quatro dificuldades existentes (Easy, Medium, Hard, Expert); no máximo três fatias verticais; sem login, sincronização, rede, editor de puzzles ou plataformas fora de Windows/Android.

## Verificação da constituição e dos guardrails

**Gate inicial (antes da pesquisa)**: .specify/memory/constitution.md contém somente placeholders de template, sem princípios ratificados ou regras de governança aplicáveis. O gate efetivo vem de specs/README.md e dos contratos das features 001–003.

**Guardrails aplicados**:
- Dependências seguem MAUI/Infrastructure → Application → Domain; Domain não referencia UI, MAUI, arquivos ou Serilog.
- ViewModels e controle gráfico encaminham intenções e exibem resultados; não implementam regras, candidatos, solver, geração ou prioridade de dicas.
- Usar serviços concretos existentes e DI por construtor; criar interfaces apenas para uma fronteira real ou teste determinístico de concorrência.
- Geração e dicas são assíncronas/canceláveis usando os casos de uso existentes. Validação de jogadas e cálculo completo de candidatos também não podem bloquear a thread de UI: os comandos os coordenam assincronamente, mantendo regras e cálculos nos casos de uso/Domain e publicando o estado observável na thread principal.
- Conflitos esperados são apresentados por resultado de validação; exceções não são usadas para controle normal nem ocultadas.
- Serilog é configurado no limite Infrastructure/host, com mensagens e erros em arquivos diários separados, timestamp com offset, correlation ID e dados seguros. O jogador recebe mensagem genérica para falhas inesperadas.
- Usar os gates Release com warnings como errors e executar testes depois de build bem-sucedido. Builds MAUI por alvo exigem workloads configurados.

**Gate após o desenho**: aprovado. O host MAUI e o adaptador Infrastructure são necessários para entregar a interface multiplataforma e o logging local obrigatório; não são camadas artificiais sem consumidor. As bibliotecas existentes são reutilizadas. Não há violação de princípio ratificado.

**Complexidade**: nenhuma exceção aos guardrails do projeto requer justificativa adicional.

## Estrutura do projeto

### Documentação desta feature

- specs/004-game-session/plan.md
- specs/004-game-session/research.md
- specs/004-game-session/data-model.md
- specs/004-game-session/contracts/game-session.md
- specs/004-game-session/quickstart.md
- specs/004-game-session/tasks.md contém a decomposição de implementação por história, criada pelo fluxo speckit-tasks.

### Código-fonte

- src/CageLogic.Domain: regras existentes; sem dependência MAUI.
- src/CageLogic.Application/GameSessions: estado/coordenador da sessão, projeção de estado, notas de candidatos, histórico, temporizador e integração de dicas.
- src/CageLogic.Application: reutilizar Generation, Moves, Candidates, Validation, Hints e Difficulty existentes; adicionar somente contratos/casos de uso necessários ao fluxo da sessão.
- src/CageLogic.Infrastructure/Logging: configuração Serilog, sinks diários, opções de retenção e fallback de emergência, usando a abstração Microsoft.Extensions.Logging.
- src/CageLogic.Maui: App, MauiProgram, navegação, páginas de início/partida/resumo, ViewModels, controle composto do tabuleiro, GraphicsView/IDrawable, recursos e adaptadores de ciclo de vida Windows/Android.
- tests/CageLogic.Application.Tests/GameSessions: testes NUnit de sessão e histórico.
- tests/CageLogic.Infrastructure.Tests/Logging: testes NUnit dos arquivos, filtros, retenção e fallback.
- CageLogic.slnx: incluir os projetos host e Infrastructure e os respectivos testes.

**Decisão de estrutura**: criar CageLogic.Maui porque ainda não há host executável. Criar CageLogic.Infrastructure para separar e testar a escrita de logs locais, que é uma fronteira de sistema de arquivos exigida pelos guardrails e será reutilizada pela persistência de 005. Application continua testável em net10.0; o host apresenta os estados da sessão e registra as dependências concretas na composition root real de MauiProgram. Não criar um projeto separado de ViewModels nem um composition root artificial.

## Abordagem de implementação

1. **Iniciar partida**: listar as dificuldades do DifficultyProfileCatalog. O comando inicia GeneratePuzzleUseCase.ExecuteAsync, mostra carregamento, aceita cancelamento explícito, permite uma geração em execução por vez e apresenta resultados explícitos de falha/cancelamento com retry sem travar a UI. Em sucesso, construir a sessão a partir de GeneratedPuzzle, que já contém puzzle validado e solução única.
2. **Coordenar estado editável**: manter SudokuBoard imutável e notas manuais separadas. ApplyMoveUseCase continua aceitando valores que conflitam e retorna BoardValidationResult para destaque. Encaminhar validação de jogadas e o cálculo completo de candidatos por comandos assíncronos, sem executar cálculo na thread de UI; atualizar a sessão e a projeção observável na thread principal. CandidateCalculator/GetCandidatesUseCase fornece candidatos automáticos para cada célula vazia; substituir as notas desse conjunto inteiro em uma ação.
3. **Histórico transacional**: guardar snapshots antes/depois do conteúdo editável — board e notas — em pilhas undo/redo. Inserção/remoção de valor, edição de nota e preenchimento automático são ações; seleção, modo, pausa e tempo não são. Uma operação sem mudança efetiva não cria entrada. Undo/redo restaura conteúdo mas atribui uma nova BoardRevision monotônica.
4. **Projetar e desenhar o tabuleiro**: produzir uma projeção somente leitura com givens, valores, notas, seleção, conflitos, cages/alvos e destaques de dica. Um controle MAUI encapsula GraphicsView e usa a mesma transformação geométrica para desenho e hit-test, recalculando-a em resize. Expor cada célula e controle como alvo semântico para TalkBack e Narrator; anunciar posição, valor, fixo/editável, notas, seleção, conflito e dica. A camada visual emite CellPosition/intenções, nunca decisões de regra.
5. **Entrada e dicas**: normalizar toque, mouse e teclado para comandos de seleção/valor/candidato/apagar/undo/redo. Setas percorrem células sem circular nas bordas; 1–9 edita conforme o modo; Backspace/Delete limpa conforme o modo; Ctrl+Z/Ctrl+Y desfaz/refaz. Inserir resposta limpa as notas da célula como parte da mesma transação, e Undo dessa inserção restaura as notas. Manter ações de edição disponíveis enquanto uma dica é calculada. Somente uma alteração efetiva nos valores do SudokuBoard incrementa BoardRevision; alteração isolada de candidatos não incrementa a revisão nem invalida o pedido. Ao mudar valores, cancelar o pedido anterior quando possível, limpar a progressão visível e solicitar de novo desde Explanation. Antes de apresentar qualquer resposta, comparar a revisão retornada com a atual; resultado obsoleto é descartado mesmo quando o cancelamento já foi pedido.
6. **Tempo e ciclo de vida**: acumular tempo ativo com TimeProvider/GetTimestamp e GetElapsedTime; um timer de UI só atualiza a apresentação. Window.Deactivated e Window.Stopped pausam de forma idempotente. Window.Resumed deixa a sessão pausada até Resume explícito. O estado em memória não promete recuperação após encerramento do processo; isso pertence a 005.
7. **Conclusão e tratamento de erros**: só concluir quando o tabuleiro estiver completo, sem conflitos e cada célula coincidir com SolutionGrid. Valores legalmente posicionados mas errados não são realçados até a tentativa de concluir. O resumo conta uma vez cada entrada de resposta que viola regra local; correção, Undo e Redo não alteram essa contagem histórica. Conta cada nível progressivo de dica realmente exibido; não conta valor válido divergente da solução, `NoSafeHint`, cancelamento nem resultado obsoleto. Falhas inesperadas são correlacionadas e registradas na fronteira; exibir texto genérico seguro.
8. **Testar ponta a ponta**: testes determinísticos NUnit para sessão, regras de histórico, timer via TimeProvider fake, conflitos, candidatos, revisão e resultados atrasados. Testes de Infrastructure isolam diretório/retention em pasta temporária. Builds separados e execução manual em Windows e Android verificam input, redimensionamento, pausa, acessibilidade, apresentação e responsividade durante validação, cálculo completo de candidatos, geração e dicas. Registrar p95 nos dispositivos alvo como linha de base; definir um limite de aceitação após medir esses dispositivos.

## Fatias verticais e gates

Preservar exatamente as três fatias da spec:

1. **Exibir e selecionar**: host e projetos, registro real de DI/logging, início por dificuldade, desenho de grid/cages, projeção semântica e seleção por toque/mouse/teclado.
2. **Jogar, anotar e corrigir**: entrada/apagamento, conflitos visíveis, notas manuais, auto-fill substitutivo, undo/redo e projeção atualizada; validar nos dois alvos.
3. **Pausar e concluir**: ciclo de vida/timer, dicas e proteção por revisão, conclusão/resumo, logging global seguro, ausência de resposta de solução antes da conclusão.

Executar na raiz, com .NET 10 e workloads Android/Windows instalados:

    dotnet build CageLogic.slnx --configuration Release --warnaserror
    dotnet test CageLogic.slnx --no-build --configuration Release
    dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-android --configuration Release --warnaserror
    dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-windows10.0.19041.0 --configuration Release --warnaserror

O comando de testes só roda após build bem-sucedido. Se workloads não estiverem disponíveis, registrar essa limitação e executar também os projetos net10.0 existentes sem tentar simular sucesso de target MAUI.

## Decisões complementares incorporadas

- Cada entrada de resposta que viola uma ou mais regras locais soma um erro; uma entrada conta uma vez, ainda que cause vários conflitos. Correção, Undo e Redo não alteram a contagem histórica, e valor válido apenas divergente da solução não conta.
- Cada nível progressivo de dica efetivamente exibido soma uma dica; `NoSafeHint`, pedidos cancelados e resultados obsoletos não contam.
- Setas movem seleção célula a célula sem circular; 1–9 edita conforme o modo; Backspace/Delete limpa conforme o modo; Ctrl+Z/Ctrl+Y desfaz/refaz. TalkBack e Narrator alcançam as 81 células e controles, com os estados semânticos especificados em FR-012.
- Inserir resposta limpa notas na mesma transação; desfazer essa inserção restaura as notas. Apagar a resposta depois em uma nova ação deixa a célula sem as notas anteriores.

A meta de latência numérica pode ser definida após medição nos devices; até lá, p95 é registrado como linha de base. O requisito qualitativo já é obrigatório: validação, cálculo de candidatos, geração e dicas não bloqueiam a UI, e edição permanece disponível durante o cálculo de uma dica.
