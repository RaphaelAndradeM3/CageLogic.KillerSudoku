# Quickstart de validação — Motor de puzzles

Este guia valida a biblioteca e os projetos de teste da solução atual. A solution e os projetos Domain/Application já existem. Não há host MAUI nesta solution; builds de plataforma dependem da criação do host e da configuração dos workloads Windows/Android.

## Pré-requisitos

- SDK .NET 10 instalado.
- Repositório na branch `002-puzzle-engine`.
- Pacotes NuGet dos projetos de teste restauráveis no ambiente.
- Para execução do futuro host móvel: workloads MAUI e SDKs correspondentes; não são necessários para validar as bibliotecas net10.0.

## Build e suíte

Na raiz do repositório:

```powershell
dotnet restore CageLogic.slnx
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
```

Resultado esperado: restore/build/test terminam com exit code zero; warnings impedem o build por `--warnaserror`. Caso o build falhe, corrigir antes de rodar testes.

## Cenários do motor

Implementar os cenários como fixtures NUnit nos projetos já existentes e conferir:

| Cenário | Execução | Resultado esperado |
|---|---|---|
| Puzzle estruturalmente inválido | `PuzzleStructureValidator.Validate` com cage desconectada, cobertura incorreta ou alvo inviável | `PuzzleStructureResult` inválido; o solver não recebe `ValidatedPuzzle` inexistente. |
| Puzzle gerado sem dígitos iniciais | Inspecionar um resultado `Success` antes de iniciar a partida | As 81 células iniciais estão vazias; os alvos/cages são as pistas visíveis e a `SolutionGrid` fica separada. |
| Zero soluções | Puzzle estruturalmente válido com givens que conflitam | `SolutionSearchResult.NoSolution`, sem solução parcial. |
| Uma solução | Fixture conhecida e válida | `Unique`, primeira solução completa passa validação de linhas, colunas, blocos e cages. |
| Múltiplas soluções | Fixture ambígua | `Multiple` após a segunda solução; não afirmar contagem exata acima de 2. |
| Perfis lógicos | Fixture resolvível em cada faixa e fixtures nos limites entre perfis | Menor perfil cumulativo que conclui a trilha é reportado; catálogo/ordem repetidos dão mesmo resultado; técnica não implementada produz `Unclassifiable`. |
| Estado lógico | Eliminar candidatos por par/tripla, aplicar uma colocação e analisar o passo seguinte | As eliminações anteriores continuam ausentes; candidatos são recalculados sem recuperar opções eliminadas. |
| Geração por dificuldade | Solicitar cada perfil com seed e orçamento determinísticos | Sucesso contém puzzle validado, uma solução e análise igual à dificuldade solicitada. Repetir seed/opções reproduz o mesmo resultado no runtime suportado. |
| Tentativa rejeitada | Forçar candidato inválido, ambíguo ou não classificável no fixture da pipeline | Candidato é descartado e nunca aparece como `Success`. |
| Orçamento esgotado | Limite de tentativas baixo ou limite de duração atingido antes do aceite | `Unavailable` com motivo, tentativas iniciadas e tempo decorrido; sem solução ou puzzle; a dificuldade pedida não muda. |
| Cancelamento | Token cancelado antes e durante solver, análise e geração | Operação termina com `OperationCanceledException`; nenhum resultado parcial é publicado. |

## Execução focada

Depois de definidos os nomes reais dos fixtures, executar cada projeto separadamente para encurtar o ciclo. Exemplo de comandos no estado atual da solution:

```powershell
dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj --configuration Release
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release
```

Usar `--filter FullyQualifiedName~NomeDoCaso` quando for útil executar um cenário individual; o nome do filtro depende dos fixtures que serão criados na implementação. Depois dos testes focados, executar a suíte completa da solution.

## Medição necessária para limites de geração

Antes de configurar limite padrão de produção, selecionar e registrar o Android mínimo/dispositivo representativo e rodar Release nele e num Windows, usando uma lista fixa de seeds. Registrar p50/p95/máxima do pedido total e de geração de grade, cages, validação estrutural, contagem de unicidade e dificuldade; também tentativas, motivos de rejeição e latência de cancelamento. Usar os números e a meta de responsividade escolhida para fixar limites de tempo e tentativas. Testes automatizados continuam usando orçamento determinístico por tentativas e não dependem desse tempo de parede. A biblioteca permanece implementável e testável com orçamentos explícitos antes dessa decisão de release.

O plano original não executou comandos; a validação da implementação está registrada abaixo.

## Fixtures disponíveis na implementação

Os fixtures NUnit criados para o motor são:

- `SudokuSolverNoSolutionTests`, `SudokuSolverMultiplicityTests` e `SudokuSolverCancellationTests` em `tests/CageLogic.Application.Tests/Solving/`.
- `LogicalTechniqueTests` e `LogicalStateTests` em `tests/CageLogic.Domain.Tests/LogicalSteps/`.
- `DifficultyAnalyzerTests` em `tests/CageLogic.Application.Tests/Difficulty/`.
- `PuzzleGeneratorTests` em `tests/CageLogic.Application.Tests/Generation/`.
- `PuzzleGeneratorPerformanceTests` é opt-in e está fora da suíte comum.

Os fixtures ponta a ponta com solver e classificador reais cobrem Easy (`20261005`), Medium (`4100`) e Hard (`408863218`), todos com zero givens e solução única. O teste Expert atual injeta um classificador fixo para verificar o contrato da pipeline; não há fixture real Expert. A busca exploratória de 500 tentativas com seed `4100` não produziu Expert dentro de 60 s; a pipeline corretamente retornou `Unavailable`. A geração real Expert continua pendente em T040.

Execução focada:

```powershell
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~SudokuSolver
dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj --configuration Release --filter FullyQualifiedName~LogicalTechniqueTests
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~DifficultyAnalyzerTests
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~PuzzleGeneratorTests
```

O benchmark opt-in pode ser executado em Release assim:

```powershell
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~PuzzleGeneratorPerformanceTests --logger "console;verbosity=detailed"
```

## Resultado de benchmark disponível

Em 2026-10-05, o fixture Release foi executado no ambiente Windows 10 x64 (`10.0.19045`), SDK .NET `10.0.401` e runtime `10.0.12`, usando dez seeds fixas de dificuldade Easy (`20261001` a `20261010`). A latência total teve p50 de `160,55 ms`, p95 de `687,57 ms` e máximo de `687,57 ms`. Nas etapas, p50/p95/máximo foram: grade `0,03/4,02/4,02 ms`; cages `0,25/9,26/9,26 ms`; estrutura `0,51/16,45/16,45 ms`; unicidade `63,87/501,60/501,60 ms`; análise lógica `87,37/179,30/179,30 ms`. O cenário controlado de rejeição registrou três estruturas inválidas em três tentativas. A resposta observada ao token já cancelado foi `11,51 ms` neste processo; esse número não é um limite garantido.

Os valores acima medem este ambiente compartilhado e puzzles Easy com cages singleton. Não definem orçamento de produção. O repositório ainda não contém host MAUI nem configuração de aparelho Android mínimo; workloads Android e MAUI Windows estão instalados, mas não há dispositivo Android selecionado/conectado. A API continua exigindo `GenerationBudget` explícito. Não configurar defaults até repetir o corpus em Windows de produto e no Android mínimo escolhido.

## Builds de plataforma

Hoje, `CageLogic.slnx` contém somente bibliotecas e testes `net10.0`; não há projeto host para targets de plataforma. Quando a feature 004 adicionar o host MAUI, executar no projeto host real:

```powershell
dotnet build <caminho-do-host.csproj> --configuration Release -f net10.0-windows10.0.19041.0
dotnet build <caminho-do-host.csproj> --configuration Release -f net10.0-android
```

## Validação executada

Em 2026-10-05, `dotnet build CageLogic.slnx --configuration Release --warnaserror --no-restore` concluiu sem warnings; `dotnet test CageLogic.slnx --no-build --no-restore --configuration Release` aprovou 77 testes e ignorou o benchmark opt-in. O fixture Hard real também passou na suíte. O benchmark opt-in passou no Windows conforme os números acima. O restore de dependências de teste exigiu `dotnet restore CageLogic.slnx --source https://api.nuget.org/v3/index.json` neste ambiente porque o feed privado configurado rejeitou a credencial local.
