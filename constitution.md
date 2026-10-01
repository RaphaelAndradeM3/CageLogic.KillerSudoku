# constitution.md

# CageLogic Killer Sudoku — Engineering Constitution

Este documento define princípios técnicos e regras de engenharia que devem orientar o desenvolvimento do projeto.

---

## 1. Regra de domínio independente

O domínio do Killer Sudoku deve ser completamente independente de:

- .NET MAUI;
- Android;
- Windows;
- SQLite;
- APIs externas;
- UI.

Nenhuma entidade de domínio poderá referenciar tipos específicos de interface ou infraestrutura.

---

## 2. Dependency Rule

As dependências deverão apontar para dentro:

```text
UI / Infrastructure
        ↓
Application
        ↓
Domain
```

O domínio não conhece as camadas externas.

---

## 3. SOLID obrigatório

Toda implementação deve buscar aderência a:

- Single Responsibility Principle;
- Open/Closed Principle;
- Liskov Substitution Principle;
- Interface Segregation Principle;
- Dependency Inversion Principle.

SOLID não deve ser usado para criar abstrações artificiais. Interfaces deverão existir quando houver fronteira, contrato, substituição ou ganho real de testabilidade.

---

## 4. Regras de negócio não ficam na ViewModel

ViewModels coordenam interação e estado de apresentação.

Elas não devem implementar:

- regras de Sudoku;
- regras de cage;
- cálculo de candidatos;
- solver;
- classificação;
- geração de puzzles.

---

## 5. Código orientado à intenção

Preferir nomes que revelem intenção.

Evitar:

```text
Manager
Helper
Util
Common
ProcessData
DoWork
HandleStuff
```

Preferir:

```text
SudokuSolver
CandidateCalculator
CageValidator
HintEngine
PuzzleGenerator
GameRepository
```

---

## 6. Funções pequenas e coesas

Métodos devem possuir uma responsabilidade clara.

Regras complexas deverão ser divididas em componentes nomeados.

---

## 7. Estratégias extensíveis

O mecanismo de hints deverá seguir Strategy.

Nova técnica não deve exigir alterar um grande `switch` central.

Exemplo:

```csharp
public interface IHintStrategy
{
    int Priority { get; }
    HintResult? FindHint(SudokuBoard board);
}
```

---

## 8. Preferir Value Objects

Conceitos do domínio devem ser representados explicitamente.

Exemplos:

- CellPosition
- CageSum
- CandidateSet
- Difficulty

Evitar passar pares de `int` sem significado quando um tipo de domínio pode tornar o código mais seguro.

---

## 9. Imutabilidade quando possível

Objetos de valor deverão preferencialmente ser imutáveis.

Alterações importantes de estado devem ser explícitas.

---

## 10. Solver separado do Hint Engine

`ISudokuSolver` responde perguntas computacionais.

Exemplos:

- existe solução?
- qual é a solução?
- quantas soluções existem?

`IHintEngine` responde perguntas pedagógicas.

Exemplo:

- qual é o próximo passo lógico explicável para um jogador humano?

Não misturar essas responsabilidades.

---

## 11. Generator separado do Solver

O gerador poderá usar o solver, mas solver não conhece generator.

---

## 12. Testes como requisito de mudança

Nova regra de domínio deve possuir testes.

Correção de bug deve incluir teste que reproduza a falha sempre que tecnicamente possível.

---

## 13. Testes determinísticos

Testes não poderão depender de:

- horário real;
- internet;
- random não controlado;
- estado global;
- ordem de execução.

Geradores aleatórios deverão aceitar seed ou abstração de random.

---

## 14. Puzzle sempre validado antes da publicação

Um puzzle gerado deve passar por:

1. validação estrutural;
2. validação de cages;
3. solver;
4. contagem de soluções;
5. análise de dificuldade.

Somente puzzles com solução única poderão chegar ao jogador.

---

## 15. Sem cópia de implementação proprietária

Sites e aplicativos comerciais podem ser utilizados como referência de produto e UX.

Não copiar:

- código;
- textos proprietários;
- assets;
- puzzles privados;
- identidade visual;
- banco de dados;
- algoritmos obtidos por engenharia reversa.

As regras matemáticas do jogo são implementadas de forma independente.

---

## 16. Logs estruturados

Utilizar `Microsoft.Extensions.Logging`.

Evitar concatenar strings quando dados estruturados forem apropriados.

Exemplo:

```csharp
_logger.LogInformation(
    "Puzzle generated. Difficulty: {Difficulty}, Attempts: {Attempts}",
    difficulty,
    attempts);
```

Nunca registrar dados sensíveis desnecessários.

---

## 17. Exceções

Exceções representam situações excepcionais.

Não usar exceção como fluxo normal de validação de jogada.

Para regras esperadas, preferir resultados explícitos.

Exemplo:

```csharp
MoveResult TrySetValue(...);
```

---

## 18. CancellationToken

Operações potencialmente demoradas devem aceitar `CancellationToken`.

Exemplos:

- geração de puzzle;
- análise avançada;
- importação;
- futuras sincronizações.

---

## 19. Async somente quando necessário

Não criar métodos assíncronos artificiais.

Usar `async` para:

- I/O;
- persistência;
- operações demoradas;
- APIs externas futuras.

---

## 20. UI responsiva

Nenhum cálculo pesado poderá bloquear a UI thread.

---

## 21. GraphicsView isolado

O componente visual do tabuleiro deve apenas:

- desenhar;
- receber interação;
- converter coordenadas;
- emitir eventos/comandos.

Ele não decide regras de Sudoku.

---

## 22. MVVM

A UI MAUI deverá seguir MVVM.

Preferência por:

- `ObservableObject`;
- `RelayCommand`;
- `AsyncRelayCommand`.

---

## 23. Persistência atrás de interfaces

Exemplo:

```csharp
public interface IGameRepository
{
    Task SaveAsync(GameState state, CancellationToken cancellationToken);
    Task<GameState?> LoadCurrentAsync(CancellationToken cancellationToken);
}
```

Application não deve depender diretamente de SQLite.

---

## 24. Versionamento

Usar SemVer:

```text
MAJOR.MINOR.PATCH
```

---

## 25. Branches

Sugestão:

```text
main
develop
feature/*
fix/*
refactor/*
test/*
docs/*
```

Para times pequenos, trunk-based também é aceitável desde que PRs e CI sejam obrigatórios.

---

## 26. Commits

Preferência por Conventional Commits.

Exemplos:

```text
feat: add cage validation
fix: prevent duplicate cage candidates
test: add unique solution scenarios
refactor: extract candidate calculator
docs: update hint engine design
```

---

## 27. Pull Requests

Todo PR relevante deve responder:

- qual problema resolve?
- qual decisão de design foi tomada?
- quais testes foram adicionados?
- existe mudança de comportamento?
- existe impacto de performance?

---

## 28. Definition of Done

Uma funcionalidade está concluída quando:

- código compila;
- testes passam;
- novas regras possuem testes;
- logging adequado foi considerado;
- não existem warnings relevantes;
- documentação técnica foi atualizada quando necessário;
- funciona nas plataformas afetadas.

---

## 29. Performance mensurável

Otimização deve ser baseada em medição.

Não sacrificar clareza por micro-otimizações sem evidência.

---

## 30. Refatoração contínua

Refatorações pequenas são preferíveis a reescritas grandes.

Quando código começar a apresentar:

- duplicação;
- classe grande;
- método longo;
- responsabilidade difusa;
- dependência circular;

deve-se planejar refatoração acompanhada por testes.

---

## 31. Simplicidade antes de abstração

Implementar a solução mais simples que preserve corretamente o modelo.

Não criar abstrações para requisitos hipotéticos sem evidência.

---

## 32. Documentação viva

Arquivos mínimos no repositório:

```text
README.md
PRD.md
Ideia.md
constitution.md
CONTRIBUTING.md
LICENSE
```

ADRs podem ser adicionados para decisões arquiteturais relevantes.

---

## 33. Decisões arquiteturais importantes

Usar ADR para decisões como:

- GraphicsView vs Grid de controles;
- SQLite;
- algoritmo de solver;
- estratégia do generator;
- política de difficulty;
- sincronização futura.

---

## 34. Segurança de dependências

Pacotes NuGet devem:

- possuir origem confiável;
- estar ativos;
- ter licença compatível;
- ser mantidos atualizados;
- não ser adicionados quando a BCL resolver adequadamente o problema.

---

## 35. Qualidade acima de quantidade

O objetivo não é produzir o maior número de classes.

O objetivo é produzir código:

- correto;
- testável;
- legível;
- extensível;
- sustentável.
