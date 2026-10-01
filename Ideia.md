# Ideia.md

## Nome do projeto

**CageLogic Killer Sudoku**

## Nome sugerido do repositório Git

**CageLogic.KillerSudoku**

Nome alternativo mais direto:

- `KillerSudoku.Maui`
- `KillerSudokuMaui`
- `CageLogic`
- `CageLogic.Maui`

A recomendação principal é **CageLogic.KillerSudoku**, porque diferencia o projeto de repositórios genéricos chamados apenas "Sudoku" ou "KillerSudoku" e ainda comunica a proposta central: raciocínio lógico com cages.

---

## Descrição curta — Português

Aplicativo multiplataforma de Killer Sudoku desenvolvido em .NET 10 e .NET MAUI para Windows e Android, com geração de puzzles, validação em tempo real, candidatos, histórico de jogadas, solução única e sistema de dicas progressivas baseadas em técnicas humanas de resolução.

## Short description — English

Cross-platform Killer Sudoku app built with .NET 10 and .NET MAUI for Windows and Android, featuring puzzle generation, real-time validation, candidates, move history, unique-solution verification, and progressive human-style solving hints.

---

## Visão do produto

O **CageLogic Killer Sudoku** será um jogo de Killer Sudoku moderno, multiplataforma e offline-first, projetado para Windows e Android a partir de uma base de código compartilhada em C#.

O diferencial principal será o sistema de dicas. Em vez de apenas revelar um número correto, o jogo deverá explicar o raciocínio necessário para que o jogador avance.

Exemplos:

- destacar linha, coluna, bloco ou cage relevante;
- mostrar candidatos possíveis;
- explicar uma combinação de soma;
- aplicar a regra dos 45;
- identificar candidato único;
- identificar single oculto;
- explicar interseções entre cage e bloco;
- revelar a resposta apenas como último nível de ajuda.

---

## Problema que o projeto resolve

Muitos jogos de Sudoku oferecem uma dica que simplesmente preenche uma célula.

Isso resolve momentaneamente o puzzle, mas não ensina o jogador.

A proposta do CageLogic é transformar a dica em uma pequena explicação lógica, permitindo que o usuário aprenda técnicas de Killer Sudoku enquanto joga.

---

## Público-alvo

- jogadores iniciantes em Killer Sudoku;
- jogadores intermediários que querem aprender novas técnicas;
- jogadores avançados que desejam puzzles difíceis;
- usuários Windows que preferem jogar no desktop;
- usuários Android que querem jogar offline;
- estudantes e desenvolvedores interessados em algoritmos de resolução de Sudoku.

---

## Plataformas iniciais

- Windows
- Android

Possíveis extensões futuras:

- iOS
- macOS
- WebAssembly, caso a arquitetura permita portar o domínio para outra UI.

---

## Tecnologias propostas

- .NET 10
- C#
- .NET MAUI
- XAML
- MVVM
- CommunityToolkit.Mvvm
- GraphicsView / IDrawable para o tabuleiro
- SQLite para persistência local
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- xUnit ou NUnit
- FluentAssertions opcional
- GitHub Actions para CI

---

## Arquitetura

A solução seguirá princípios de Clean Architecture e separará regras de negócio da interface.

Estrutura inicial:

```text
CageLogic.KillerSudoku.sln

src/
  CageLogic.KillerSudoku.Domain/
  CageLogic.KillerSudoku.Application/
  CageLogic.KillerSudoku.Infrastructure/
  CageLogic.KillerSudoku.Maui/

tests/
  CageLogic.KillerSudoku.Domain.Tests/
  CageLogic.KillerSudoku.Application.Tests/
```

### Domain

Responsável por:

- Board
- Cell
- Cage
- Position
- CandidateSet
- regras do Sudoku
- regras do Killer Sudoku
- validações matemáticas

### Application

Responsável por:

- iniciar partida;
- registrar jogada;
- desfazer/refazer;
- validar tabuleiro;
- solicitar dica;
- resolver puzzle;
- classificar dificuldade;
- gerar novo puzzle.

### Infrastructure

Responsável por:

- SQLite;
- preferências;
- arquivos;
- logs;
- importação/exportação futura.

### MAUI

Responsável por:

- Views;
- ViewModels;
- navegação;
- GraphicsView;
- interação touch;
- teclado;
- temas;
- acessibilidade.

---

## Regras essenciais do Killer Sudoku

O jogo deve respeitar:

1. Grade 9x9.
2. Cada linha contém números de 1 a 9 sem repetição.
3. Cada coluna contém números de 1 a 9 sem repetição.
4. Cada bloco 3x3 contém números de 1 a 9 sem repetição.
5. Cada cage possui uma soma-alvo.
6. A soma dos valores das células de uma cage deve ser igual à soma-alvo.
7. Números não podem se repetir dentro da mesma cage.
8. O puzzle publicado deve possuir solução única.

---

## Diferenciais planejados

- dicas progressivas;
- explicação da técnica usada;
- destaque visual das células envolvidas;
- gerador próprio;
- verificação de solução única;
- classificação de dificuldade baseada nas técnicas necessárias;
- modo escuro;
- estatísticas;
- desafios diários;
- funcionamento offline;
- arquitetura testável;
- código aberto, se essa for a estratégia escolhida.

---

## Sistema de dicas

Cada estratégia será uma implementação independente.

Exemplo:

```csharp
public interface IHintStrategy
{
    int Priority { get; }
    HintResult? FindHint(SudokuBoard board);
}
```

Possíveis estratégias:

1. Naked Single
2. Hidden Single
3. Cage Single
4. Cage Combination
5. Rule of 45
6. Cage/Box Intersection
7. Naked Pair
8. Hidden Pair
9. Naked Triple
10. técnicas avançadas futuras

A prioridade deve fazer o motor procurar primeiro a técnica mais simples.

---

## Roadmap inicial

### Fase 1 — Fundação

- solution;
- projetos;
- entidades;
- value objects;
- testes.

### Fase 2 — Regras

- validação de linha;
- coluna;
- bloco;
- cage;
- candidatos.

### Fase 3 — Solver

- backtracking;
- constraint propagation;
- verificação de solução única.

### Fase 4 — Hint Engine

- estratégias básicas;
- estratégias Killer;
- explicações.

### Fase 5 — Generator

- solução completa;
- criação de cages;
- verificação;
- classificação.

### Fase 6 — MAUI

- tabuleiro;
- numpad;
- candidatos;
- interação;
- undo/redo.

### Fase 7 — Produto

- persistência;
- estatísticas;
- desafios;
- temas;
- publicação.

---

## Referências funcionais

O projeto pode se inspirar em produtos existentes para compreender a experiência de jogo, sem copiar código, identidade visual, textos, puzzles proprietários, assets ou implementação específica.

Referências:

- Sudoku.com Killer Sudoku
- Killer Sudoku mobile apps
- projetos open source de Sudoku/Killer Sudoku

A implementação do CageLogic deve ser original.
