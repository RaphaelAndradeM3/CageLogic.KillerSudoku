# Quickstart: validar Regras e Candidatos

## Estado deste repositório

Este repositório contém documentação, mas ainda não possui uma solution .NET, projetos de domínio/aplicação ou testes. Os comandos abaixo passam a ser executáveis depois que a implementação da feature criar os projetos listados em plan.md. Nenhum build ou teste foi executado durante o planejamento.

## Pré-requisitos após a criação dos projetos

- .NET 10 SDK instalado.
- Solution na raiz do repositório, com Domain, Application e projetos de teste NUnit.
- Para validar um host MAUI em Windows ou Android, o host e os workloads/SDKs da plataforma precisam existir. O host MAUI está fora do escopo da feature 001, então esses builds ficam para a feature que o criar.

## Verificação completa

A partir da raiz do repositório, após a criação da solution:

1. Restaurar e compilar em Release, com warnings tratados como erros:

    dotnet build --configuration Release --warnaserror

2. Executar os testes depois de um build bem-sucedido:

    dotnet test --no-build

3. Executar um projeto de teste isoladamente quando investigando uma falha:

    dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj
    dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj

## Cenários determinísticos esperados

- Estrutura válida: as 81 posições são cobertas exatamente uma vez por cages não vazias, conectadas por lados, com alvo atingível por dígitos distintos.
- Estrutura inválida: detectar posições fora da grade, cobertura ausente ou sobreposta, células duplicadas na cage, cage vazia/desconectada e alvo estruturalmente inalcançável; devolver erro de estrutura separado dos conflitos de valores.
- Validação do tabuleiro: detectar repetição em linha, coluna, bloco e cage e soma de cage que não possa mais ser atingida; distinguir tabuleiro parcial válido/incompleto de tabuleiro completo/resolvido.
- Candidatos: comparar o conjunto da célula vazia com fixtures para linha, coluna, bloco, valores atuais da cage e combinações distintas que atinjam o alvo. Não exigir uma solução completa do tabuleiro.
- Atualização: após inserir ou limpar um valor, validar novamente o estado e obter candidatos compatíveis com as restrições atualizadas.
- Dados determinísticos: usar fixtures fixos. Não consultar relógio, rede, random ou estado global.

## Builds por plataforma

Depois que a solution contiver o host MAUI e workloads estiverem configurados, executar os builds Windows e Android definidos pelo projeto. Os Target Framework Monikers específicos serão documentados com o host; esta feature não inventa TFMs sem o projeto MAUI. As bibliotecas Domain e Application devem continuar compilando como net10.0 independente da plataforma.