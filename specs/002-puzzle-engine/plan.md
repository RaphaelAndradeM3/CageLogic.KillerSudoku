# Implementation Plan: Motor de puzzles Killer Sudoku

**Branch**: `002-puzzle-engine` | **Date**: 2026-10-05 | **Spec**: [spec.md](spec.md)

**Input**: [Especificação da feature](spec.md), PRD §§10–13 e 19, [constituição](../../constitution.md), [mapa de especificações](../README.md) e código atual das features 001.

## Summary

Entregar resolução computacional, contagem de soluções limitada a duas, geração local de puzzles Killer e classificação por técnicas lógicas documentadas. O solver será um CSP com busca em profundidade, seleção da célula com menos candidatos (MRV) e propagação pelos validadores/candidatos existentes. O gerador usará uma solução completa, construirá cages conectadas e aceitará somente puzzles estruturalmente válidos, com solução única e resolvidos pelo perfil lógico pedido. O catálogo cumulativo v1 é Easy (Naked Single, Hidden Single, Cage Single), Medium (Easy + Cage Combination), Hard (Medium + Cage/Region Intersection + Rule of 45) e Expert (Hard + Naked Pair, Hidden Pair, Naked Triple). Classificador, solver e gerador terão contratos separados; nenhum dependerá da feature 003 para produzir explicações.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`), nullable e implicit usings conforme os projetos existentes.

**Primary Dependencies**: `CageLogic.Domain` e `CageLogic.Application` existentes; BCL. Sem pacote de solver externo. Testes seguem NUnit 5 e as convenções dos dois projetos de teste existentes.

**Storage**: N/A. Geração e busca são locais e em memória.

**Testing**: `CageLogic.Domain.Tests` e `CageLogic.Application.Tests`; fixtures determinísticos com NUnit. Validar `dotnet build --configuration Release --warnaserror` e, após build bem-sucedido, `dotnet test --no-build`.

**Target Platform**: bibliotecas `net10.0`; uso posterior pelo app offline Windows/Android. O host MAUI e seus targets ainda não estão presentes nesta solution.

**Project Type**: bibliotecas Domain/Application numa aplicação móvel futura; sem API externa, serviço ou persistência.

**Performance Goals**: Não há baseline nem dispositivo Android mínimo definido. Resolução e geração devem observar cancelamento durante busca, manter a UI responsiva e registrar métricas agregadas. Definir valores padrão para tentativas e tempo somente depois de medir o fluxo implementado em Release num Windows e num Android representativos; isso é um gate anterior à liberação do gerador, não uma promessa numérica sem dados.

**Constraints**: Tabuleiro fixo 9×9. Contagem para em 2 soluções (`Multiple` significa pelo menos duas). Só publicar puzzle validado, único e classificado exatamente na dificuldade solicitada. A classificação reinicia cada perfil desde o puzzle inicial, escolhe a primeira dedução pela ordem de técnicas do catálogo e pelo desempate estável de posição/dígito/efeito, e exige resolução lógica completa sem backtracking. Cada pedido recebe limites positivos explícitos de tentativas e duração; esgotamento retorna indisponibilidade com motivo/métricas, sem puzzle parcial ou fallback. Cancelamento do chamador propaga `OperationCanceledException` e não é indisponibilidade. Nenhum padrão de produção será definido antes de medições documentadas em Windows e no Android mínimo suportado. Sementes de teste e ordenação determinística tornam a geração reproduzível; fixtures não dependem de timeout de relógio.

**Scale/Scope**: Uma solicitação de geração por operação; 81 células e cages que particionam o tabuleiro. Sem serviço externo, dataset de puzzles ou projeto novo.

## Constituição e gates

Usar `constitution.md` na raiz como constituição vigente e `specs/README.md` para os guardrails compartilhados. `.specify/memory/constitution.md` contém apenas o template placeholder e não define princípios do projeto.

| Gate | Resultado | Aplicação ao plano |
|---|---|---|
| Domínio independente e dependências para dentro | PASS | Tipos lógicos compartilháveis ficam em Domain; solver, gerador e orquestração ficam em Application. Nenhuma referência a MAUI, SQLite ou plataforma. |
| Separação solver / dicas / gerador | PASS | Solver só responde busca e contagem; passos lógicos não carregam texto de dica; gerador pode chamar solver/analisador. 002 não referencia tipos ou serviços de 003. |
| Interfaces somente em fronteiras reais | PASS | Reutilizar projetos e serviços concretos existentes; não adicionar interface por técnica nem projeto. Registrar DI quando uma composition root existir. |
| Imutabilidade e resultados explícitos | PASS | Solução e resultados publicados são imutáveis; `NoSolution`, `Multiple`, `Unclassifiable` e `Unavailable` são resultados normais, não exceções. |
| Determinismo e cancelamento | PASS | Seed explícita em fixtures; ordem estável; `CancellationToken` cooperativo em loops/recursão; trabalho CPU-bound sai da UI thread. |
| Validação antes da publicação e medição | PASS | Estrutura, cages, unicidade e dificuldade são verificadas antes do sucesso. Os limites de geração só serão fixados após benchmark documentado. |

**Gate pré-pesquisa**: PASS. Não há violação constitucional que exija exceção ou projeto adicional.

## Project Structure

### Documentação desta feature

```text
specs/002-puzzle-engine/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── spec.md
```

`contracts/` não será criado: a feature acrescenta serviços internos às bibliotecas .NET e não expõe API, CLI, protocolo ou UI pública externa. `tasks.md` pertence à etapa `$speckit-tasks`.

### Código afetado

```text
src/CageLogic.Domain/
├── LogicalSteps/                  # IDs estáveis e deduções sem texto de apresentação
├── Candidates/                    # CandidateCalculator existente, reutilizado
├── Puzzles/                       # PuzzleDefinition/ValidatedPuzzle existentes
└── Validation/                    # validadores existentes, reutilizados

src/CageLogic.Application/
├── Solving/                       # SudokuSolver e modelos de resultado/solução
├── Difficulty/                    # perfis, catálogo e DifficultyAnalyzer
└── Generation/                    # pedido, orçamento, gerador e resultado

tests/CageLogic.Domain.Tests/
└── LogicalSteps/                  # fixtures unitários de cada dedução

tests/CageLogic.Application.Tests/
├── Solving/                       # zero, uma, múltiplas e cancelamento
├── Difficulty/                    # técnicas, limites e não classificável
└── Generation/                    # semente, validação, orçamento e cancelamento
```

**Structure Decision**: Expandir os projetos Domain/Application e seus testes já existentes, seguindo pastas/namespaces por responsabilidade. A inspeção do repositório encontrou `CageLogic.slnx`, os dois projetos e os testes da feature 001. A seção da spec 002 que diz que ainda não existem solution/projetos e que os gates não podem ser executados está desatualizada; este plano usa a árvore real e não altera a feature 001.

## Ordem de implementação e validação

1. **Solver e unicidade**: solver puro de CPU sobre `ValidatedPuzzle`, recálculo de candidatos no estado de cada nó, MRV e ramificação por cópia imutável do tabuleiro. Guardar a primeira solução e parar ao encontrar a segunda. Validar folha completa com `SudokuBoardValidator`; `CandidateCalculator` isolado não prova que exista solução global.
2. **Técnicas e classificação**: adicionar as nove deduções do catálogo v1 como passos lógicos presentation-neutral reutilizáveis por 003. A análise usa `LogicalState` imutável (tabuleiro e candidatos); eliminações permanecem entre passos e colocações recalculam candidatos sem recuperar eliminações anteriores. Executar cada perfil cumulativo (Easy, Medium, Hard, Expert) a partir do estado inicial. A ordem de técnica é a sequência do catálogo; dentro da técnica, ordenar por posições relacionadas em row-major, dígito crescente e colocação antes de eliminação. Classificar no perfil menos avançado que preencha o tabuleiro e passe o validador. Tentativa e erro do solver não conta como técnica lógica. Travar sem solução lógica resulta em `Unclassifiable`.
3. **Geração e aceite**: produzir grade resolvida com busca e ordem controlada pela seed; particionar células em cages ortogonalmente conectadas, sem repetir dígitos da solução dentro da cage; somar os valores da grade para obter alvos e aplicar a política de dígitos iniciais definida na spec. Passar pelo `PuzzleStructureValidator`, confirmar solução única e analisar dificuldade. Descartar candidato reprovado e tentar novamente dentro do orçamento explícito. O gerador mede tentativas e duração desde o início do pedido; o primeiro limite esgotado retorna `Unavailable` com razão e métricas.
4. **Entrega ao chamador**: embrulhar o cálculo demorado no caso de uso/Application com `Task.Run` e o mesmo token, sem tornar artificialmente assíncronos os algoritmos puros. Publicar `GeneratedPuzzle` somente no sucesso. Esgotamento retorna `Unavailable`; cancelamento continua distinto.
5. **Medição**: em Release e corpus fixo de seeds, medir etapas da geração, tentativas e motivos de rejeição, latência p50/p95/máxima e resposta ao cancelamento. Repetir em Windows e no Android mínimo escolhido pelo produto; decidir e documentar então os limites padrão configuráveis de tempo/tentativas. Esse é um gate para configurar e liberar a geração no host, não um bloqueio para implementar a biblioteca com orçamento explícito. Até existir host MAUI, validar a biblioteca e deixar build de target Windows/Android para quando os workloads/targets estiverem configurados.

### Complexity Tracking

Sem violações da constituição. Nenhum projeto, pacote ou contrato de infraestrutura novo está previsto.
