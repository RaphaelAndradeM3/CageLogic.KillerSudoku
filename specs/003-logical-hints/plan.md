# Plano de Implementação: Dicas Lógicas Progressivas

**Branch**: `003-logical-hints` | **Date**: 2026-10-06 | **Spec**: [spec.md](spec.md)

**Input**: Especificação em `specs/003-logical-hints/spec.md`.

## Resumo

Adicionar à Application um caso de uso que apresenta, em três níveis, o próximo passo selecionado pelo catálogo determinístico de técnicas lógicas existente em 002. O Domain continuará responsável por deduzir o passo e fornecer evidências tipadas; Application fornecerá explicações em português e projetará somente o conteúdo permitido no nível solicitado. O resultado verificará conflitos e compatibilidade com o puzzle original, ecoará a revisão do snapshot analisado e nunca mostrará uma ação concreta antes do nível 3. A sessão de 004 descartará respostas obsoletas e solicitará uma nova análise desde o nível 1. Serão explicadas as nove técnicas v1, inclusive passos que apenas eliminam candidatos.

## Contexto técnico

**Linguagem/versão**: C# / .NET 10, nullable conforme os projetos atuais.

**Dependências principais**: `CageLogic.Domain` consumido por `CageLogic.Application`; NUnit 5 nos projetos de teste existentes. Nenhum pacote novo previsto.

**Armazenamento**: N/A. A dica e seu nível são transitórios; persistência pertence à feature 005.

**Testes**: `CageLogic.Domain.Tests` e `CageLogic.Application.Tests`; build e testes da solution `CageLogic.slnx`.

**Plataforma-alvo**: bibliotecas .NET 10 compartilhadas pelo futuro host Windows/Android. O host MAUI ainda não existe; a integração visual pertence à feature 004.

**Tipo de projeto**: bibliotecas Domain e Application em solução .NET.

**Metas de desempenho**: resposta assíncrona e cancelável, com análise CPU-bound fora da thread chamadora. Não há orçamento de latência numérico aprovado; medir em Release no host futuro antes de propor um.

**Restrições**: modo offline; tabuleiro 9×9; regras sem dependência de MAUI; nenhuma regra ou seleção de técnica em ViewModel; não confiar em candidatos anotados manualmente; respeitar a prioridade e os desempates determinísticos de 002; chamadas longas recebem e propagam `CancellationToken`.

**Escopo**: nove técnicas do catálogo v1; três níveis progressivos; estados para puzzle resolvido, estado inconsistente, ausência de passo seguro e valor não confirmado; compatibilidade com as jogadas atuais; eco da revisão na resposta. Descarte e recálculo de resposta obsoleta pertencem à fronteira da sessão em 004 (FR-013/SC-005).

## Verificação da constituição e dos guardrails

**Gate inicial (antes da pesquisa)**: `.specify/memory/constitution.md` ainda contém somente placeholders do template, sem princípios ratificados que possam servir de gate. Foram aplicados os guardrails ativos em `specs/README.md`: manter Domain independente de UI/infraestrutura, evitar camadas e padrões sem necessidade, usar resultados explícitos para estados esperados, propagar cancelamento e validar com build/test da solution.

**Gate após o desenho**: aprovado. O plano reutiliza Domain/Application existentes, não introduz projeto, pacote, banco, host, logging ou abstração sem consumidor atual. Application ecoa a revisão analisada; a sessão de 004 compara revisões e descarta respostas obsoletas. Falhas esperadas são estados tipados; cancelamento e falhas inesperadas propagam-se à fronteira apropriada.

**Complexidade**: nenhuma exceção aos guardrails do projeto requer justificativa.

## Estrutura do projeto

### Documentação desta feature

```text
specs/003-logical-hints/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── logical-hints.md
└── tasks.md                 # criado pelo fluxo speckit-tasks
```

### Código-fonte

```text
src/
├── CageLogic.Domain/
│   └── LogicalSteps/
│       ├── LogicalStepEvidence.cs       # novo modelo tipado de evidências
│       └── ...                          # LogicalStep e estratégias existentes estendidos
└── CageLogic.Application/
    └── Hints/
        ├── GetHintUseCase.cs
        ├── HintPuzzleContext.cs
        ├── HintRequest.cs
        ├── HintLevel.cs
        ├── HintResult.cs
        ├── HintStatus.cs
        ├── HintAction.cs
        └── LogicalHintExplanationCatalog.cs

tests/
├── CageLogic.Domain.Tests/
│   └── LogicalSteps/
│       └── LogicalTechniqueTests.cs     # ampliar cenários para evidências
└── CageLogic.Application.Tests/
    └── Hints/
        ├── GetHintUseCaseTests.cs
        └── LogicalHintExplanationCatalogTests.cs
```

**Decisão de estrutura**: estender os projetos existentes. O Domain retorna `LogicalStepEvidence` sem prosa localizada. Application consome o analyzer existente, valida o snapshot, consulta o catálogo português e produz uma projeção específica por nível. Não criar projeto de contratos externo: `contracts/logical-hints.md` documenta a fronteira que 004 consumirá. Como 003 não tem host nem composition root real, não cria registro artificial de DI; 004 registra `GetHintUseCase` e suas dependências necessárias no composition root MAUI real na fatia 3, quando a sessão começa a consumir o caso de uso, conforme `specs/README.md`.

## Abordagem de implementação

1. **Evidência e explicação por técnica**: estender `LogicalStep`/estratégias para que cada uma das nove técnicas forneça posições de padrão/apoio, escopo, efeito e dígitos/contexto necessários. Manter os dados sem idioma no Domain. Criar em Application o catálogo de nomes e explicações em português indexado por `LogicalTechniqueId`; não duplicar a lógica de dedução.
2. **Caso de uso e validação**: adicionar `GetHintUseCase.ExecuteAsync(HintRequest, CancellationToken)`. Validar a estrutura, conflitos locais, puzzle resolvido e existência de uma conclusão compatível com o snapshot. Para origem única, comparar entradas com a solução confirmada. Quando a origem não tem multiplicidade única, uma busca limitada pelo snapshot pode detectar ausência de conclusão, mas não libera valor de colocação.
3. **Projeção progressiva**: nível 1 retorna técnica e explicação; nível 2 acrescenta destaques tipados das posições e candidatos envolvidos, sem declarar a ação; nível 3 acrescenta `PlaceValue` ou `RemoveCandidates`. Para colocações, exigir unicidade confirmada do puzzle original. Eliminações permanecem eliminações e não são convertidas em colocações.
4. **Revisão, cancelamento e testes**: ecoar `BoardRevision` na resposta. A sessão de 004 cancela pedidos quando possível, descarta resultados cuja revisão ficou obsoleta e solicita novamente para o snapshot atual desde o nível 1, conforme FR-013/SC-005 de 004. Testar determinismo, projeções, nove cenários de referência LH-01 a LH-09, validação, estados terminais, compatibilidade e cancelamento nos projetos NUnit existentes.

## Sequenciamento em fatias verticais

O detalhamento em tarefas deve preservar as três fatias da especificação, entregando uma capacidade exercitável em cada uma:

1. **Singles explicáveis**: evidência e explicação para Naked Single, Hidden Single e Cage Single; validar passo, técnica, posições e destaques correspondentes.
2. **Técnicas Killer Sudoku**: acrescentar Cage Combination, Cage/Region Intersection e Rule of 45; confirmar que prioridade e desempates continuam vindo do catálogo de 002 e cobrir os novos contextos/evidências.
3. **Técnicas avançadas e progressão**: acrescentar Naked Pair, Hidden Pair e Naked Triple; fechar projeções de três níveis, compatibilidade/estados terminais, cancelamento e eco da revisão; testar colocações e eliminações sem conversão de uma em outra. Descarte e novo pedido por revisão são validados na sessão de 004 conforme FR-013/SC-005.

## Gates de validação planejados

Executar na raiz, depois que o código da feature for implementado:

```powershell
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
```

O segundo comando só deve ser executado após build bem-sucedido. Testes focados e cenários documentados estão em [quickstart.md](quickstart.md). Esta etapa cria documentação de planejamento; não executou build ou testes. Builds Windows/Android ficam para 004, quando host MAUI e workloads existirem.
