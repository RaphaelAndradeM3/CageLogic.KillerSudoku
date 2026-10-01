# FEATURE SPEC: Regras e Candidatos de Killer Sudoku

- **Feature Branch**: A definir na implementação
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD e visão do CageLogic Killer Sudoku; requisitos RN-001 a RN-009 e regras essenciais do README.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** Jogadores precisam inserir valores e entender quando o estado do tabuleiro respeita as regras de Sudoku e das cages. Sem validação coerente, não conseguem confiar nos candidatos nem avançar com segurança.
>
> **Definição de sucesso:** O jogo representa um tabuleiro Killer Sudoku, identifica conflitos de linha, coluna, bloco e cage, e apresenta somente candidatos que ainda podem fazer parte de uma solução válida.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; não crie abstrações ou padrões sem necessidade demonstrável.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validar valores do tabuleiro (Priority: P1)

O jogador insere ou altera um valor e consegue identificar se ele conflita com as regras de Sudoku ou Killer Sudoku.

**Why this priority**: Regras corretas são a base de toda partida, do solver e das dicas.

**Independent Test**: Aplicar estados de tabuleiro válidos e inválidos e comparar os conflitos apresentados com cada regra do jogo.

**Acceptance Scenarios**:

1. **Given** um tabuleiro parcial sem conflitos, **When** o jogador insere um valor permitido, **Then** o estado continua válido.
2. **Given** um valor repetido em uma linha, coluna, bloco ou cage, **When** o estado é validado, **Then** o conflito correspondente é informado.
3. **Given** uma cage parcialmente preenchida, **When** a soma parcial já excede o alvo ou não pode mais alcançá-lo, **Then** o estado é indicado como impossível.

### User Story 2 - Consultar candidatos possíveis (Priority: P2)

O jogador consulta os valores que ainda podem ocupar uma célula vazia no estado atual.

**Why this priority**: Candidatos apoiam o raciocínio e alimentam as dicas explicativas.

**Independent Test**: Comparar os candidatos calculados para posições conhecidas após alterar valores em linhas, colunas, blocos e cages.

**Acceptance Scenarios**:

1. **Given** uma célula vazia em um tabuleiro parcial válido, **When** os candidatos são solicitados, **Then** somente valores compatíveis com todas as regras são retornados.
2. **Given** uma jogada que altera as restrições da célula, **When** os candidatos são atualizados, **Then** valores incompatíveis deixam de aparecer.
3. **Given** uma célula preenchida, **When** seus candidatos são solicitados, **Then** ela não é apresentada como uma posição vazia elegível.

### Edge Cases

- Cages vazias, com uma célula, com alvo impossível ou com células repetidas.
- Uma célula ausente em todas as cages, ou pertencente a mais de uma.
- Tabuleiro com posições fora da grade, valores fora de 1 a 9 ou estado parcialmente preenchido.
- Soma final correta, mas com repetição dentro da cage.
- Uma célula sem candidato devido a um estado prévio inconsistente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST representar a grade com 9 linhas, 9 colunas e 81 posições identificáveis.
- **FR-002**: O sistema MUST exigir que cada célula pertença a exatamente uma cage.
- **FR-003**: O sistema MUST verificar valores distintos de 1 a 9 em cada linha, coluna e bloco 3×3.
- **FR-004**: O sistema MUST verificar que os valores de cada cage não se repitam e que sua soma final corresponda ao alvo.
- **FR-005**: O sistema MUST identificar quando os valores restantes de uma cage já não podem atingir o alvo.
- **FR-006**: O sistema MUST distinguir estados válidos, conflitos e dados incompletos sem tratar uma jogada normal como falha excepcional.
- **FR-007**: O sistema MUST calcular candidatos de uma célula vazia considerando simultaneamente linha, coluna, bloco e cage.
- **FR-008**: O sistema MUST atualizar a validade e os candidatos após uma mudança de estado.

### Key Entities

- **Tabuleiro**: 81 posições, valores atuais e relação com as cages.
- **Posição**: linha e coluna de uma célula.
- **Cage**: conjunto de posições e soma-alvo.
- **Candidatos**: valores possíveis para uma posição vazia no estado atual.
- **Conflito**: regra violada e posições relacionadas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Todos os cenários de linhas, colunas, blocos, cages, cobertura de células e soma parcial têm resultado verificável e consistente com as regras documentadas.
- **SC-002**: Em todos os estados de referência, o conjunto apresentado para cada célula vazia coincide com os valores que não violam nenhuma regra.
- **SC-003**: Todo conflito identificado informa a regra e a posição envolvidas; um estado sem conflito não recebe falso positivo nos cenários de aceitação.

## Assumptions

- As regras descritas no README e no PRD são a fonte de verdade do jogo.
- Valores fixos do puzzle e valores digitados pelo jogador poderão ser distinguidos pelo estado da partida.
- Importação de puzzles e editor de cages não fazem parte desta especificação.
- A aplicação completa funcionará sem conexão à internet.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Domain e Application; a tela de jogo consumirá os resultados. Os projetos concretos ainda serão definidos ao criar a solution.
- O domínio não dependerá de MAUI, SQLite ou logging de infraestrutura.
- Validações esperadas retornarão resultados explícitos; exceções ficam para falhas excepcionais.
- Injeção de dependências seguirá a composition root existente quando criada. Interfaces serão usadas em fronteiras reais ou quando trouxerem substituição/testabilidade.
- Regras detalhadas de C#, logging, segurança e gates compartilhados estão em specs/README.md.

## 3. FATIAS VERTICAIS DE IMPLEMENTAÇÃO (máximo 3)

### Slice 1: Representar tabuleiro e cages
- **Escopo**: Representar as 81 células e validar a cobertura e a estrutura das cages.
- **Validação local**: Compilar os projetos afetados e verificar tabuleiros/cages válidos e inválidos com testes determinísticos.

### Slice 2: Validar jogadas Killer Sudoku
- **Escopo**: Receber uma mudança de valor, avaliar Sudoku e cage, e disponibilizar conflitos para a experiência de jogo.
- **Validação local**: Exercitar inserção, repetição, soma parcial impossível e preenchimento final correto.

### Slice 3: Calcular candidatos
- **Escopo**: Atualizar os candidatos após mudanças do tabuleiro e expor o resultado para a tela e para o motor de dicas.
- **Validação local**: Comparar conjuntos de candidatos em cenários de fronteira e no tabuleiro completo.

## 4. GATES DE VALIDAÇÃO (.NET Toolchain)

Quando a solution existir, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Os comandos ainda não podem ser executados: o repositório contém apenas documentação e não possui solution ou projetos.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir os contratos desta feature.
3. Se um contrato registrado no DI mudar, atualizar o registro no mesmo slice.
4. Preservar a separação entre domínio, aplicação, infraestrutura e MAUI; não colocar regras em ViewModels.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.