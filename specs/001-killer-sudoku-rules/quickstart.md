# Quickstart: validar Regras e Candidatos

## Projetos implementados

- `CageLogic.slnx`: solution .NET 10 na raiz.
- `src/CageLogic.Domain/`: modelos do puzzle, cages, tabuleiro, movimentos, validação estrutural, conflitos e cálculo de candidatos. O projeto não depende de MAUI ou infraestrutura.
- `src/CageLogic.Application/`: `ApplyMoveUseCase` aplica/limpa entradas e retorna o tabuleiro resultante e sua validação; `ValidateBoardUseCase` valida um `SudokuBoard` existente sem aplicar movimento ou alterá-lo; `GetCandidatesUseCase` calcula candidatos a partir do tabuleiro fornecido.
- `tests/CageLogic.Domain.Tests/`: testes NUnit para estrutura, cobertura, conectividade, soma, conflitos e candidatos.
- `tests/CageLogic.Application.Tests/`: testes NUnit para movimentos, validação independente do tabuleiro e atualização de candidatos depois de inserir ou limpar um valor.

## Pré-requisitos

- .NET 10 SDK.
- Acesso aos pacotes públicos do NuGet para a primeira restauração.

## Build e testes

A partir da raiz do repositório:

1. Restaurar e compilar em Release, tratando warnings como erros:

    dotnet build --configuration Release --warnaserror

2. Executar os testes depois do build bem-sucedido:

    dotnet test --configuration Release --no-build

3. Executar um projeto de teste isoladamente ao investigar uma falha:

    dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj
    dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj

## Cenários determinísticos cobertos

- Estrutura: coordenadas fora da grade, cages vazias ou com posições duplicadas, cobertura ausente ou sobreposta, desconexão por contato apenas diagonal, alvo estruturalmente inalcançável e valores fixos fora de 1 a 9.
- Validação: repetição em linha, coluna, bloco e cage; soma de cage que não pode mais ser completada; estado parcial válido/incompleto e estado completo resolvido.
- Validação independente na Application: `ValidateBoardUseCase` recebe o estado atual e retorna `BoardValidationResult` sem aplicar movimento nem modificar o tabuleiro; os testes cobrem estado parcial válido e conflito de linha.
- Movimentos: inserir, substituir e limpar valores do jogador; recusar edição de valor fixo com resultado explícito; retornar um novo tabuleiro e sua validação.
- Candidatos: restrições de linha, coluna, bloco e cage; combinações distintas que completam o alvo; célula preenchida omitida; conjunto vazio em estado local inconsistente; consulta recalculada após inserir/limpar. Não é feita busca por solução global do tabuleiro.
- Fixtures fixos: os testes não dependem de rede, relógio, aleatoriedade ou estado global.

## Limites desta feature

Domain e Application permanecem em `net10.0`, sem host MAUI, persistência, importação de puzzles ou solver global. Builds Windows/Android e workloads MAUI pertencem à feature que introduzir o host.
