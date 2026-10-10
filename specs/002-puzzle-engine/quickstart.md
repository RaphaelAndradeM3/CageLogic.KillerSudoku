# Quickstart de validação — Motor de puzzles

Este guia valida o motor de puzzles e os projetos de teste da feature 002. A solution atual também inclui o host MAUI criado na feature 004, em `src/CageLogic.Maui/CageLogic.Maui.csproj`; os builds de plataforma dependem dos workloads Windows/Android instalados.

## Pré-requisitos

- SDK .NET 10 instalado.
- Para reproduzir a implementação original, use a branch/commit da feature 002; os comandos também podem ser executados em branches posteriores.
- Pacotes NuGet dos projetos de teste restauráveis no ambiente.
- Para builds do host MAUI atual: workloads MAUI e SDKs correspondentes; não são necessários para validar os projetos de biblioteca e seus testes.

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
| Pistas iniciais de Easy | Inspecionar um resultado `Success` antes de iniciar a partida | 27 células mostram valores fixos válidos, três por linha, coluna e bloco 3×3; as outras 54 permanecem vazias e a `SolutionGrid` fica separada. |
| Zero soluções | Puzzle estruturalmente válido com givens que conflitam | `SolutionSearchResult.NoSolution`, sem solução parcial. |
| Uma solução | Fixture conhecida e válida | `Unique`, primeira solução completa passa validação de linhas, colunas, blocos e cages. |
| Múltiplas soluções | Fixture ambígua | `Multiple` após a segunda solução; não afirmar contagem exata acima de 2. |
| Perfis lógicos | Fixture resolvível em cada faixa e fixtures nos limites entre perfis | Menor perfil cumulativo que conclui a trilha é reportado; catálogo/ordem repetidos dão mesmo resultado; técnica não implementada produz `Unclassifiable`. |
| Estado lógico | Eliminar candidatos por par/tripla, aplicar uma colocação e analisar o passo seguinte | As eliminações anteriores continuam ausentes; candidatos são recalculados sem recuperar opções eliminadas. |
| Geração por dificuldade | Solicitar cada perfil com seed e orçamento determinísticos | Sucesso contém puzzle validado, uma solução e análise igual à dificuldade solicitada. Repetir seed/opções reproduz o mesmo resultado no runtime suportado. |
| Tentativa rejeitada | Forçar candidato inválido, ambíguo ou não classificável no fixture da pipeline | Candidato é descartado e nunca aparece como `Success`. |
| Orçamento esgotado | Limite de tentativas baixo ou limite de duração atingido antes do aceite | `Unavailable` com motivo, tentativas iniciadas e tempo decorrido; sem solução ou puzzle; a dificuldade pedida não muda. |
| Cancelamento | Token cancelado antes e durante solver, análise e geração | Operação termina com `OperationCanceledException`; nenhum resultado parcial é publicado. |

### Cages e alvos no tabuleiro

Gerar um puzzle Easy com seed fixa e inspecionar `GeneratedPuzzle.Puzzle.Cages`: a partição deve conter regiões conectadas com mais de uma célula, cada alvo deve somar os dígitos correspondentes da solução e pelo menos um alvo deve ser maior que 9. Ao iniciar o puzzle, o tabuleiro MAUI desenha as bordas da cage e mostra o alvo no canto superior esquerdo da região.

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

Para Expert, a seed escolhe uma rotação ou reflexão do tabuleiro que desloca as 29 cages singleton por diferentes posições sem quebrar linhas, colunas, blocos ou conectividade das cages; repetir a seed reproduz o particionamento.

Execução focada:

```powershell
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~SudokuSolver
dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj --configuration Release --filter FullyQualifiedName~LogicalTechniqueTests
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~DifficultyAnalyzerTests
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~PuzzleGeneratorTests
```

O benchmark opt-in pode ser executado em Release assim:

```powershell
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~PuzzleGeneratorPerformanceTests --logger "console;verbosity=detailed" -- NUnit.ExplicitMode=Relaxed
```

## Resultado de benchmark disponível

Em 2026-10-05, o fixture Release foi executado no ambiente Windows 10 x64 (`10.0.19045`), SDK .NET `10.0.401` e runtime `10.0.12`, usando dez seeds fixas de dificuldade Easy (`20261001` a `20261010`). A latência total teve p50 de `160,55 ms`, p95 de `687,57 ms` e máximo de `687,57 ms`. Nas etapas, p50/p95/máximo foram: grade `0,03/4,02/4,02 ms`; cages `0,25/9,26/9,26 ms`; estrutura `0,51/16,45/16,45 ms`; unicidade `63,87/501,60/501,60 ms`; análise lógica `87,37/179,30/179,30 ms`. O cenário controlado de rejeição registrou três estruturas inválidas em três tentativas. A resposta observada ao token já cancelado foi `11,51 ms` neste processo; esse número não é um limite garantido.

Os valores acima medem este ambiente compartilhado e puzzles Easy com cages singleton, antes da atualização T045 para cages agrupadas. Eles não medem a estratégia atual nem definem orçamento de produção. Na medição de 2026-10-05, o host MAUI e um Android mínimo ainda não estavam configurados; a feature 004 adicionou o host posteriormente, e os builds atuais estão registrados abaixo. A API continua exigindo `GenerationBudget` explícito. Não configurar defaults até repetir o corpus em Windows de produto e no Android mínimo escolhido.

Em 2026-10-06, o mesmo benchmark foi executado em Release no Windows 10 x64 (`10.0.19045.0`), SDK .NET `10.0.401` e runtime `10.0.12`. No corpus Easy, a latência total foi p50 `218,09 ms`, p95 `613,02 ms` e máxima `613,02 ms`; a etapa de unicidade teve p50 `13,21 ms`, p95 `95,93 ms` e máxima `95,93 ms`. O fixture Hard (`408863218`) levou `1.854,39 ms`, com unicidade em `12,72 ms` e análise de dificuldade em `1.836,43 ms`. O fixture Expert (`1597463005`) levou `146,87 ms`, com unicidade em `2,56 ms` e análise de dificuldade em `135,61 ms`. Hard e Expert são uma execução fixa cada, não uma distribuição p50/p95. Os tempos confirmam que a busca de unicidade não é o gargalo desses fixtures; a análise de dificuldade dominou o caso Hard. São medições deste host e não definem orçamento Android ou de produção.

## Builds de plataforma

`CageLogic.slnx` contém as bibliotecas e os testes `net10.0`. O host MAUI da feature 004 está em `src/CageLogic.Maui/CageLogic.Maui.csproj`; executar nele:

```powershell
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --configuration Release -f net10.0-windows10.0.19041.0
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --configuration Release -f net10.0-android
```

## Validação executada

Em 2026-10-06, `dotnet build CageLogic.slnx --configuration Release --warnaserror --no-restore` concluiu com 0 warnings e 0 erros; `dotnet test CageLogic.slnx --no-build --no-restore --configuration Release` aprovou 78 testes (34 Domain e 44 Application). O benchmark opt-in não foi executado nesta validação. O restore de dependências de teste exigiu `dotnet restore CageLogic.slnx --source https://api.nuget.org/v3/index.json` neste ambiente porque o feed privado configurado rejeitou a credencial local.

Em 2026-10-09, após a alteração T045, `dotnet build CageLogic.slnx --configuration Release --warnaserror --no-restore` e a suíte de teste passaram. O build MAUI Release para Windows passou com 0 avisos e erros usando uma saída temporária, pois o app estava aberto. O build MAUI Release Android passou com 0 avisos e erros usando `-p:AndroidLinkMode=None -p:RunAOTCompilation=false`. A aceitação visual manual e as medições de geração com cages agrupadas continuam pendentes. A contagem desse registro foi substituída pela revalidação abaixo.

Em 2026-10-10, durante a revisão dos apontamentos do PR #5, os projetos de teste foram executados separadamente em Release e passaram 196 testes (38 Domain, 132 Application e 26 Infrastructure). O build MAUI Windows Release passou com zero avisos e erros. O build MAUI Android Release também passou com zero avisos e erros usando `-p:AndroidLinkMode=None -p:RunAOTCompilation=false` e um cache NuGet temporário gravável. O cache global padrão não permitiu ao linker criar um arquivo no pacote MAUI instalado; a restauração local e o build com cache temporário concluíram normalmente.
