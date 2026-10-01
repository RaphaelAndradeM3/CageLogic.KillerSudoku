# PRD.md

# Product Requirements Document

## Produto

**CageLogic Killer Sudoku**

## Repositório

`CageLogic.KillerSudoku`

## Versão

0.1 — Initial Product Definition

---

# 1. Objetivo

Construir um aplicativo de Killer Sudoku multiplataforma em .NET 10 e .NET MAUI, inicialmente para Windows e Android.

O produto deverá permitir jogar puzzles Killer Sudoku completos, validar as regras em tempo real, oferecer candidatos e histórico de jogadas, salvar partidas automaticamente e fornecer dicas explicativas baseadas em estratégias humanas.

---

# 2. Objetivos do produto

## 2.1 Objetivos principais

- oferecer uma experiência completa de Killer Sudoku;
- compartilhar a maior parte do código entre Windows e Android;
- funcionar offline;
- gerar puzzles válidos com solução única;
- explicar dicas sem revelar imediatamente a resposta;
- manter o domínio desacoplado da interface;
- garantir alta cobertura de testes sobre regras, solver, generator e hint engine.

## 2.2 Não objetivos do MVP

Não fazem parte da primeira versão:

- multiplayer;
- ranking online;
- login obrigatório;
- sincronização em nuvem;
- monetização;
- anúncios;
- iOS;
- publicação automática em lojas;
- IA generativa como requisito do motor de dicas.

---

# 3. Personas

## P1 — Jogador iniciante

Quer aprender Killer Sudoku e precisa de explicações.

## P2 — Jogador intermediário

Conhece Sudoku tradicional, mas precisa entender cages e combinações.

## P3 — Jogador avançado

Busca puzzles difíceis e técnicas mais sofisticadas.

## P4 — Usuário desktop

Prefere jogar com mouse e teclado no Windows.

## P5 — Usuário mobile

Joga em sessões rápidas no Android e espera boa experiência touch.

---

# 4. Requisitos funcionais

## RF-001 — Criar nova partida

O usuário deve poder iniciar um novo puzzle.

Critérios:

- selecionar dificuldade;
- criar tabuleiro 9x9;
- criar cages válidas;
- garantir solução única.

## RF-002 — Selecionar célula

O usuário deve poder selecionar qualquer célula editável.

## RF-003 — Inserir número

O usuário poderá inserir números de 1 a 9.

## RF-004 — Apagar número

O usuário poderá limpar uma célula não fixa.

## RF-005 — Candidatos

O usuário poderá alternar entre modo resposta e modo candidato.

## RF-006 — Auto candidatos

Opcionalmente, o sistema poderá calcular candidatos compatíveis com as regras atuais.

## RF-007 — Validar linha

Não permitir estado válido final com número repetido na mesma linha.

## RF-008 — Validar coluna

Não permitir estado válido final com número repetido na mesma coluna.

## RF-009 — Validar bloco

Não permitir estado válido final com número repetido no mesmo bloco 3x3.

## RF-010 — Validar cage

O sistema deverá verificar:

- soma parcial;
- soma final;
- repetição de valores;
- possibilidade matemática restante.

## RF-011 — Undo

O usuário poderá desfazer jogadas.

## RF-012 — Redo

O usuário poderá refazer jogadas desfeitas.

## RF-013 — Hint

O usuário poderá solicitar uma dica.

A dica deverá preferir:

1. explicação;
2. destaque;
3. redução de candidatos;
4. resposta direta somente no último nível.

## RF-014 — Salvar automaticamente

O estado da partida deverá ser persistido localmente.

## RF-015 — Continuar partida

Ao reabrir o aplicativo, o usuário poderá continuar a partida anterior.

## RF-016 — Pausar

O usuário poderá pausar a partida.

## RF-017 — Cronômetro

A partida deverá possuir temporizador.

## RF-018 — Estatísticas

Registrar:

- partidas iniciadas;
- partidas concluídas;
- tempo médio;
- melhor tempo;
- quantidade de dicas;
- erros.

## RF-019 — Tema

Suportar tema claro e escuro.

## RF-020 — Vitória

Ao preencher corretamente o tabuleiro, o sistema deverá encerrar a partida e exibir resumo.

---

# 5. Regras de negócio

## RN-001

O tabuleiro possui 81 células.

## RN-002

Cada linha deve conter os números 1 a 9 sem repetição.

## RN-003

Cada coluna deve conter os números 1 a 9 sem repetição.

## RN-004

Cada bloco 3x3 deve conter os números 1 a 9 sem repetição.

## RN-005

Cada célula pertence exatamente a uma cage.

## RN-006

A soma de todas as células de uma cage deve ser igual ao target da cage.

## RN-007

Um número não pode aparecer mais de uma vez na mesma cage.

## RN-008

Um puzzle distribuído ao jogador deve possuir exatamente uma solução.

## RN-009

Uma dica não deve sugerir um movimento inconsistente com o estado atual válido.

## RN-010

O gerador deverá rejeitar puzzles que não possam ser classificados ou que possuam múltiplas soluções.

---

# 6. Requisitos não funcionais

## RNF-001 — Plataforma

- .NET 10
- .NET MAUI
- Windows
- Android

## RNF-002 — Performance

A interface deve permanecer responsiva durante:

- validação;
- cálculo de candidatos;
- solicitação de dica.

Tarefas pesadas de geração podem executar de forma assíncrona.

## RNF-003 — Offline

As funcionalidades principais devem funcionar sem internet.

## RNF-004 — Testabilidade

O domínio não poderá depender do MAUI.

## RNF-005 — Logging

Usar `Microsoft.Extensions.Logging`.

Categorias mínimas:

- Information
- Warning
- Error

## RNF-006 — Persistência

Persistência local usando SQLite ou mecanismo equivalente.

## RNF-007 — Acessibilidade

Prever:

- contraste;
- tamanho de toque adequado;
- labels acessíveis;
- suporte a teclado no Windows.

## RNF-008 — Segurança

Não armazenar dados sensíveis sem necessidade.

---

# 7. Arquitetura proposta

```text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
MAUI
```

As dependências deverão apontar em direção ao domínio.

O projeto MAUI não deverá conter regras centrais do jogo.

---

# 8. Componentes de domínio

## SudokuBoard

Responsável por representar o estado do tabuleiro.

## SudokuCell

Representa uma célula.

## CellPosition

Value Object contendo linha e coluna.

## Cage

Representa um conjunto de células e seu target.

## CandidateSet

Representa candidatos possíveis.

## Move

Representa alteração feita pelo jogador.

## Puzzle

Representa puzzle, cages, solução e metadados.

---

# 9. Serviços principais

Interfaces sugeridas:

```csharp
public interface ISudokuValidator;
public interface ICandidateCalculator;
public interface ISudokuSolver;
public interface IPuzzleGenerator;
public interface IDifficultyAnalyzer;
public interface IHintEngine;
public interface IHintStrategy;
public interface IGameRepository;
```

---

# 10. Solver

O Solver será responsável por:

- encontrar solução;
- detectar ausência de solução;
- contar soluções;
- validar unicidade.

Implementação inicial:

- constraint propagation;
- backtracking com heurísticas.

O solver computacional não deve ser confundido com o motor de dicas humanas.

---

# 11. Hint Engine

Cada estratégia deverá retornar um objeto explicável.

Exemplo:

```csharp
public sealed record HintResult(
    HintType Type,
    string Title,
    string Explanation,
    IReadOnlyCollection<CellPosition> HighlightedCells,
    CellPosition? TargetCell,
    int? SuggestedValue);
```

O Hint Engine deverá percorrer estratégias em ordem de dificuldade.

---

# 12. Geração

Pipeline sugerido:

```text
GenerateSolvedBoard
        ↓
GenerateCages
        ↓
ValidateCages
        ↓
CheckUniqueSolution
        ↓
AnalyzeDifficulty
        ↓
Accept / Reject
```

---

# 13. Dificuldade

A dificuldade não deve depender somente da quantidade de cages.

Ela deverá considerar as técnicas necessárias para resolver o puzzle.

Primeira classificação:

- Easy
- Medium
- Hard
- Expert

---

# 14. Interface

## Tela inicial

- Continue
- New Game
- Daily Challenge
- Statistics
- Settings

## Tela de jogo

Elementos:

- board;
- timer;
- difficulty;
- mistakes;
- undo;
- redo;
- erase;
- notes;
- hint;
- keypad 1-9.

---

# 15. Desenho do tabuleiro

Preferência por `GraphicsView` + `IDrawable`.

Motivos:

- melhor controle de cages;
- melhor performance;
- linhas customizadas;
- highlights;
- candidatos;
- desenho consistente entre plataformas.

---

# 16. Persistência

Entidades locais:

- SavedGame
- GameMove
- UserStatistics
- Settings
- DailyChallengeState

---

# 17. Testes

Cobertura obrigatória para:

- regras de linha;
- regras de coluna;
- regras de bloco;
- cages;
- candidatos;
- solver;
- contador de soluções;
- hint strategies;
- generator.

Casos críticos:

- puzzle sem solução;
- puzzle com duas soluções;
- cage inválida;
- cage com soma impossível;
- repetição em cage;
- candidato incorreto;
- undo/redo.

---

# 18. CI

GitHub Actions deverá executar:

```text
restore
build
test
```

Futuro:

- Android build;
- Windows build;
- release artifacts.

---

# 19. Métricas de sucesso do MVP

O MVP será considerado funcional quando:

- executar no Windows;
- executar no Android;
- criar/carregar puzzle Killer;
- aceitar jogadas;
- validar regras;
- permitir candidatos;
- oferecer undo/redo;
- salvar e restaurar partida;
- resolver puzzle internamente;
- verificar solução única;
- entregar pelo menos quatro tipos de dica lógica;
- possuir testes automatizados do domínio.

---

# 20. Backlog pós-MVP

- desafios diários;
- achievements;
- estatísticas avançadas;
- compartilhamento;
- importação de puzzles;
- editor de Killer Sudoku;
- ranking opcional;
- sincronização em nuvem;
- solver visual passo a passo;
- tutorial interativo;
- iOS/macOS.
