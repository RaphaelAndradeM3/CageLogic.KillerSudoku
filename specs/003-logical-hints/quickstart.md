# Quickstart: dicas lógicas progressivas

## Pré-requisitos

- .NET SDK 10 instalado.
- Executar os comandos a partir da raiz do repositório.
- Solution `CageLogic.slnx` e projetos NUnit atuais. Não há host MAUI nesta feature; não são necessários workloads Windows/Android para os gates de 003.

## Implementar e validar o fluxo

1. Adicionar `LogicalStepEvidence` no Domain e preencher a evidência nas nove estratégias existentes. Manter texto localizado fora do Domain.
2. Criar o catálogo de explicações e `GetHintUseCase` na Application. Aplicar validação de estrutura, conflitos, estado resolvido e compatibilidade das jogadas com o puzzle original antes de projetar uma dica.
3. Testar projeções: nível 1 só apresenta técnica/explicação; nível 2 preenche `Highlights` (`Pattern`, `Scope`, `Target`, `Affected`) e `InvolvedCandidates` sem declarar a ação; nível 3 preenche `Action` como `PlaceValue` confirmada ou `RemoveCandidates` ordenada.
4. Implementar a sessão consumidora conforme `004-game-session` FR-013/SC-005: enviar snapshot/revisão, descartar resultados obsoletos, solicitar novamente o snapshot atual desde o nível 1 e exibir apenas resultado da revisão atual ou `NoSafeHint`. Registrar `GetHintUseCase` e dependências no composition root MAUI real da fatia 3 de 004; não criar composition root para as bibliotecas de 003.

## Validação focada

Ampliar os cenários existentes do Domain para garantir evidência estruturada e estável:

```powershell
dotnet test tests/CageLogic.Domain.Tests/CageLogic.Domain.Tests.csproj --configuration Release --filter FullyQualifiedName~LogicalTechniqueTests
```

Adicionar testes do caso de uso e do catálogo na Application:

```powershell
dotnet test tests/CageLogic.Application.Tests/CageLogic.Application.Tests.csproj --configuration Release --filter FullyQualifiedName~Hints
```

Os testes devem cobrir os vetores LH-01 a LH-09 definidos em `spec.md`; prioridade/desempate; projeções sem vazamento; destaque de candidatos em passos de eliminação; colocação somente com solução única confirmada; origem `Multiple` com estado restrito sem solução (`InconsistentState`) e com solução única (`ValueNotConfirmed` para colocação); conflito local; tabuleiro resolvido; `NoSafeHint`; revisão ecoada; e propagação de cancelamento.

## Gates da solution

Após os testes focados, executar na raiz:

```powershell
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
```

Executar o segundo comando apenas se o build concluir com sucesso. Os comandos acima são os gates planejados; este quickstart não afirma que tenham sido executados nesta etapa de documentação.

## Resultados esperados de referência

- Pedido de nível 1 não inclui posições, dígitos ou a ação concreta.
- Pedido de nível 2 inclui destaques necessários; para eliminação, pode destacar os candidatos envolvidos sem classificá-los como removíveis.
- Nível 3 de passo de eliminação retorna somente os pares posição/dígito a remover, sem sugerir colocação.
- Nível 3 de passo de colocação retorna o valor somente quando o puzzle original tem solução única confirmada e o valor corresponde a ela.
- Estado com conflito ou sem solução compatível retorna `InconsistentState`, sem dica/ação; estado completo e válido retorna `PuzzleSolved`.
- Resposta com revisão diferente da revisão atual não é exibida pela sessão; ela pede nova análise do snapshot atual desde o nível 1.
- Estado válido sem técnica do catálogo retorna `NoSafeHint`.
- Para origem `Multiple`, nenhuma solução compatível com o snapshot retorna `InconsistentState`; uma única solução compatível no snapshot restrito continua sem autorizar `PlaceValue`.

O conjunto mensurável do SC-002 é fechado nos nove vetores LH-01 a LH-09. Cada teste confere o ID da técnica e os destaques com seu vetor; nome e explicação correspondem à saída do catálogo para a técnica e o snapshot esperados.

Build visual dos targets Windows/Android e teste de sessão na interface ficam para 004, depois que o host MAUI e workloads estiverem configurados.
