# FEATURE SPEC: Dicas Lógicas Progressivas

- **Feature Branch**: A definir na implementação
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD RF-013 e RN-009, seções 11 e 19; visão do sistema de dicas em Ideia.md. Depende de 001-killer-sudoku-rules e usa 002-puzzle-engine para conferência de soluções.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** Uma dica que apenas preenche uma célula ajuda a terminar o puzzle, mas não ensina o jogador a raciocinar. Jogadores precisam entender por que um passo é válido antes de revelar uma resposta.
>
> **Definição de sucesso:** O jogo oferece pistas em etapas, explica a técnica lógica utilizada e destaca as células relacionadas; revelar diretamente um valor é o último nível.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; cada técnica deve ser adicionável sem concentrar todas as regras em um seletor monolítico.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender o próximo passo (Priority: P1)

O jogador pede ajuda e recebe uma explicação baseada em uma técnica lógica aplicável ao estado atual.

**Why this priority**: Ensinar o raciocínio é o principal diferencial do produto.

**Independent Test**: Preparar estados de puzzle conhecidos e confirmar técnica, explicação, alvo e células destacadas de cada dica.

**Acceptance Scenarios**:

1. **Given** um estado válido que contém um passo lógico reconhecido, **When** o jogador solicita uma dica, **Then** recebe a explicação da técnica e o destaque necessário para acompanhar o raciocínio.
2. **Given** mais de uma técnica aplicável, **When** a dica é escolhida, **Then** a técnica mais simples disponível é priorizada de forma consistente.
3. **Given** uma jogada válida altera os candidatos, **When** o jogador solicita outra dica, **Then** a explicação corresponde ao novo estado.

### User Story 2 - Revelar uma resposta somente se necessário (Priority: P2)

O jogador pode avançar de uma explicação para um destaque mais direto e, por último, para um valor sugerido.

**Why this priority**: O jogador controla o nível de ajuda sem perder o objetivo pedagógico.

**Independent Test**: Percorrer cada nível de ajuda no mesmo puzzle e conferir que a resposta direta só aparece no último nível.

**Acceptance Scenarios**:

1. **Given** uma dica disponível, **When** o jogador pede um nível mais explícito, **Then** recebe progressivamente mais orientação antes de qualquer resposta direta.
2. **Given** o nível de resposta direta foi solicitado, **When** a célula-alvo e o valor são apresentados, **Then** esse valor é consistente com a solução única validada.
3. **Given** um estado inconsistente ou sem técnica conhecida, **When** o jogador pede uma dica, **Then** o jogo informa que não há dica segura disponível para aquele estado.

### Edge Cases

- O tabuleiro atual já está completo ou apresenta conflitos.
- Nenhuma técnica implementada encontra um passo lógico.
- Mais de uma técnica da mesma prioridade encontra um passo.
- A posição-alvo foi preenchida entre o pedido e a exibição da dica.
- Um puzzle sem solução única não deve permitir sugestão de valor como se fosse certa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma explicação antes de revelar diretamente uma resposta.
- **FR-002**: O sistema MUST destacar as células e a posição-alvo relevantes para a técnica apresentada.
- **FR-003**: O sistema MUST escolher primeiro a técnica mais simples disponível segundo uma ordem estável.
- **FR-004**: O MVP MUST explicar todas as técnicas do catálogo v1 de dificuldade de 002: Naked Single, Hidden Single, Cage Single, Cage Combination, Cage/Region Intersection, Rule of 45, Naked Pair, Hidden Pair e Naked Triple.
- **FR-005**: O sistema MUST garantir que toda sugestão corresponda a um passo válido no estado atual.
- **FR-006**: O sistema MUST recusar sugestão de valor em estado inconsistente ou puzzle sem solução única confirmada.
- **FR-007**: O sistema MUST informar quando não existe dica segura entre as técnicas disponíveis.
- **FR-008**: O sistema MUST permitir avançar até revelar uma resposta direta somente como último nível de ajuda.
- **FR-009**: Cada técnica MUST apresentar nome, explicação em linguagem clara, células relacionadas e, quando aplicável, alvo e valor sugerido.

### Key Entities

- **Dica**: técnica, explicação, células destacadas, alvo e nível de revelação.
- **Técnica lógica**: regra de raciocínio aplicável a um estado de puzzle.
- **Nível de ajuda**: progressão da explicação para o destaque e, por último, a resposta.
- **Estado da partida**: tabuleiro e candidatos sobre os quais a dica foi calculada.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: As nove técnicas do catálogo v1 de 002 têm cenário de aceitação com explicação e posições relacionadas demonstráveis.
- **SC-002**: 100% das dicas dos cenários de referência identificam a técnica, explicam o raciocínio e destacam as células relevantes.
- **SC-003**: Nenhuma sugestão é apresentada como certa quando contradiz o estado válido ou a solução única do puzzle.
- **SC-004**: Em todos os cenários de progressão, a resposta direta aparece apenas após os níveis explicativos e de destaque.

## Assumptions

- A linguagem das explicações será localizada inicialmente em português, consistente com a documentação do produto.
- As deduções e IDs estáveis do catálogo v1 são compartilhados com Domain/002; o motor de dicas acrescenta explicações localizadas para cada uma das nove técnicas.
- Se nenhuma técnica aplicável existir, o jogo oferece uma mensagem clara em vez de inventar uma dica.
- Depende de 001-killer-sudoku-rules e de um puzzle válido e único fornecido por 002-puzzle-engine.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Application para buscar e priorizar dicas; Domain para regras reutilizáveis; MAUI para explicações e destaques; testes de aplicação e domínio.
- O motor de dicas permanece independente do solver computacional; pode usar seu resultado para validar uma sugestão, sem assumir a responsabilidade de resolver.
- As deduções e os IDs estáveis são consumidos de `LogicalStep`/`LogicalState` do catálogo v1 de 002; a feature 003 fornece explicações e destaques, sem duplicar as regras nem confiar nas anotações manuais do jogador como prova.
- Estratégias seguem o princípio extensível registrado na constituição do projeto. Uma interface é criada somente se representar essa fronteira útil.
- Explicações e erros exibidos ao jogador não incluem stack trace nem detalhes internos.
- Regras de C#, logging, segurança e gates compartilhados estão em specs/README.md.

## 3. FATIAS VERTICAIS DE IMPLEMENTAÇÃO (máximo 3)

### Slice 1: Explicar singles
- **Escopo**: Explicar Naked Single, Hidden Single e Cage Single, destacando alvo e contexto na partida.
- **Validação local**: Conferir cenários conhecidos, explicação e células destacadas; compilar os projetos afetados.

### Slice 2: Explicar técnicas Killer Sudoku
- **Escopo**: Explicar Cage Combination, Cage/Region Intersection e Rule of 45, mantendo prioridade determinística.
- **Validação local**: Comparar passos com estados de referência e verificar que cada explicação é consistente com candidatos.

### Slice 3: Explicar técnicas avançadas e progredir até a resposta
- **Escopo**: Explicar Naked Pair, Hidden Pair e Naked Triple; integrar níveis progressivos e revelar valor somente no último nível, quando o puzzle for único.
- **Validação local**: Exercitar dica inexistente, estado inconsistente, atualização entre pedidos e revelação direta final.

## 4. GATES DE VALIDAÇÃO (.NET Toolchain)

Na solution existente, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build --configuration Release após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Esses comandos se aplicam à `CageLogic.slnx` e aos projetos `net10.0` existentes. Builds dos targets Windows/Android dependem do host MAUI e dos workloads que serão introduzidos posteriormente.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir o contrato desta feature.
3. Registrar no DI qualquer contrato criado ou alterado na mesma fatia.
4. Não implementar as técnicas em ViewModels nem fundir dica explicável e solver computacional.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.
