# FEATURE SPEC: Dicas Lógicas Progressivas

- **Feature Branch**: 003-logical-hints
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD RF-013 e RN-009, seções 11 e 19; visão do sistema de dicas em Ideia.md. Depende de 001-killer-sudoku-rules e usa 002-puzzle-engine para conferência de soluções.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** Uma dica que apenas preenche uma célula ajuda a terminar o puzzle, mas não ensina o jogador a raciocinar. Jogadores precisam entender por que um passo é válido antes de revelar uma resposta.
>
> **Definição de sucesso:** O jogo oferece pistas em etapas, explica a técnica lógica utilizada e destaca as células relacionadas; revelar diretamente um valor é o último nível.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; cada técnica deve ser adicionável sem concentrar todas as regras em um seletor monolítico.

## Clarifications

### Session 2026-10-06

- Q: Quando a técnica lógica escolhida só elimina candidatos e não coloca um dígito, como a dica deve avançar? → A: Explicar a eliminação, destacar as células e os candidatos afetados e, no nível mais explícito, mostrar quais candidatos remover sem inserir um dígito nesse passo.
- Q: Se os números inseridos pelo jogador não causam conflito imediato, mas deixam o tabuleiro sem solução compatível com o puzzle original, como o motor de dicas deve reagir? → A: Tratar o estado como inconsistente, não oferecer dica segura nem valor e informar que as jogadas atuais impedem uma conclusão válida.
- Q: A especificação pede explicação e destaque, mas também descreve uma progressão entre eles. O que cada nível da dica deve mostrar? → A: Usar três níveis: (1) nome da técnica e explicação; (2) destaque das células afetadas e do alvo; (3) ação lógica explícita, colocando o valor deduzido ou indicando quais candidatos remover quando o passo só elimina candidatos.
- Q: Se a célula-alvo for preenchida depois do cálculo da dica, mas antes de ela ser exibida, o que o sistema deve fazer? → A: 003 associa a resposta à revisão do snapshot analisado; a sessão de 004 descarta respostas de revisões antigas e solicita uma nova dica para o estado atual desde o nível 1, ou informa que não há dica segura.
- Q: Se o jogador pedir uma dica com o tabuleiro completo e correto, o que o jogo deve responder? → A: Informar que o puzzle já está resolvido e que não há próximo passo lógico.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender o próximo passo (Priority: P1)

O jogador pede ajuda e recebe uma explicação baseada em uma técnica lógica aplicável ao estado atual.

**Why this priority**: Ensinar o raciocínio é o principal diferencial do produto.

**Independent Test**: Preparar estados de puzzle conhecidos e confirmar técnica, explicação, alvo e células destacadas de cada dica.

**Acceptance Scenarios**:

1. **Given** um estado válido que contém um passo lógico reconhecido, **When** o jogador solicita o primeiro nível da dica, **Then** recebe o nome da técnica e sua explicação, sem revelar ainda o destaque das células relacionadas.
2. **Given** mais de uma técnica aplicável, **When** a dica é escolhida, **Then** a técnica mais simples disponível é priorizada de forma consistente.
3. **Given** uma jogada válida altera os candidatos, **When** o jogador solicita outra dica, **Then** a explicação corresponde ao novo estado.
4. **Given** o próximo passo lógico elimina candidatos sem colocar um dígito, **When** o jogador avança até o nível mais explícito da dica, **Then** vê quais candidatos remover e as células afetadas, sem receber um dígito para colocar nesse passo.
5. **Given** o caso de uso analisa um snapshot do tabuleiro, **When** devolve qualquer resultado de dica, **Then** o resultado identifica a revisão desse snapshot para que a sessão consumidora possa reconhecer se ficou obsoleto.

### User Story 2 - Revelar uma resposta somente se necessário (Priority: P2)

O jogador percorre três níveis de ajuda: nome da técnica e explicação; destaque das células afetadas e do alvo; ação lógica explícita. No terceiro nível, um passo de colocação revela o valor deduzido, enquanto um passo de eliminação indica os candidatos a remover.

**Why this priority**: O jogador controla o nível de ajuda sem perder o objetivo pedagógico.

**Independent Test**: Percorrer cada nível de ajuda no mesmo puzzle e conferir que a resposta direta só aparece no último nível.

**Acceptance Scenarios**:

1. **Given** uma dica no primeiro nível, **When** o jogador avança ao segundo nível, **Then** vê as células afetadas e o alvo destacados, sem revelar ainda a ação lógica explícita.
2. **Given** uma dica no segundo nível, **When** o jogador avança ao terceiro nível e o passo é uma colocação, **Then** vê o valor deduzido, consistente com a solução única validada.
3. **Given** um estado inconsistente ou sem técnica conhecida, **When** o jogador pede uma dica, **Then** o jogo informa que não há dica segura disponível para aquele estado.
4. **Given** as jogadas atuais não causam conflito imediato, mas não admitem nenhuma conclusão compatível com o puzzle original, **When** o jogador pede uma dica, **Then** o jogo informa que as jogadas impedem uma conclusão válida e não oferece dica nem valor.
5. **Given** o tabuleiro está completo e correto, **When** o jogador pede uma dica, **Then** o jogo informa que o puzzle já está resolvido e que não há próximo passo lógico.
6. **Given** o puzzle original tem multiplicidade `Multiple` e as jogadas atuais deixam exatamente uma solução compatível, **When** o nível 3 pede uma colocação, **Then** o caso de uso retorna `ValueNotConfirmed` e não revela o valor, pois a unicidade do snapshot restrito não confirma a unicidade do puzzle original.

### Edge Cases

- O tabuleiro completo e correto deve ser informado como puzzle resolvido, sem próximo passo lógico.
- O tabuleiro apresenta conflitos com as regras de Killer Sudoku.
- O tabuleiro não apresenta conflito imediato, mas as jogadas do jogador impedem qualquer conclusão compatível com o puzzle original.
- Nenhuma técnica implementada encontra um passo lógico.
- Mais de uma técnica da mesma prioridade encontra um passo.
- A posição-alvo ou outra célula é alterada entre o cálculo e a exibição; a sessão de 004 descarta a dica da revisão anterior e solicita análise do snapshot atual.
- Um puzzle sem solução única não deve permitir sugestão de valor como se fosse certa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma explicação antes de revelar diretamente uma resposta.
- **FR-002**: No segundo nível da dica, o sistema MUST destacar as células afetadas e a posição-alvo relevantes para a técnica apresentada, antes de revelar a ação lógica explícita.
- **FR-003**: O sistema MUST escolher a primeira técnica aplicável na ordem estável do catálogo v1 de 002. Dentro da técnica, MUST aplicar o desempate definido por 002: posições relacionadas em ordem row-major, dígito crescente e colocação antes de eliminação.
- **FR-004**: O MVP MUST explicar todas as técnicas do catálogo v1 de dificuldade de 002: Naked Single, Hidden Single, Cage Single, Cage Combination, Cage/Region Intersection, Rule of 45, Naked Pair, Hidden Pair e Naked Triple.
- **FR-005**: Cada resultado de dica MUST ecoar a `BoardRevision` do snapshot imutável usado no cálculo. A sessão consumidora de 004 MUST comparar essa revisão com a revisão atual antes da exibição; descarte, recálculo e reinício da progressão pertencem ao requisito FR-013 de `004-game-session`.
- **FR-006**: O sistema MUST tratar como inconsistente um estado com conflitos de regras ou sem conclusão válida compatível com o puzzle original; nesse estado, MUST informar que as jogadas atuais impedem uma conclusão válida e não oferecer dicas nem valores. O sistema MUST recusar sugestão de valor quando o puzzle não tiver solução única confirmada.
- **FR-007**: O sistema MUST informar quando não existe dica segura entre as técnicas disponíveis. Se o tabuleiro estiver completo e correto, MUST informar que o puzzle está resolvido e que não há próximo passo lógico.
- **FR-008**: A ajuda MUST progredir em três níveis ordenados: nome da técnica e explicação; destaque das células afetadas e do alvo; ação lógica explícita. Um valor de colocação MUST aparecer somente no terceiro nível.
- **FR-009**: Cada técnica MUST apresentar nome, explicação em linguagem clara, células relacionadas, pares posição/dígito exatos do padrão e contexto tipado de escopo quando aplicáveis; o alvo pode ser destacado antes do nível 3, mas seu valor sugerido só pode ser apresentado nesse nível.
- **FR-010**: Quando o passo lógico escolhido somente eliminar candidatos, o sistema MUST explicar e destacar as células e os candidatos afetados e, no nível mais explícito, indicar quais candidatos remover sem sugerir um dígito para colocar nesse passo.

### Key Entities

- **Dica**: técnica, explicação, células destacadas, alvo e nível de revelação.
- **Técnica lógica**: regra de raciocínio aplicável a um estado de puzzle.
- **Nível de ajuda**: progressão da explicação para o destaque e, por último, a resposta.
- **Estado da partida**: tabuleiro e candidatos sobre os quais a dica foi calculada.

### Cenários de referência para SC-001 e SC-002

O denominador de SC-002 é fechado: são exatamente os cenários LH-01 a LH-09 abaixo. Cada fixture representa um snapshot válido no qual a técnica indicada é a primeira aplicável na ordem de 002 e registra o ID esperado, posições/papéis da evidência e efeito (colocação ou eliminações). Os casos de colocação usam puzzle original com solução única confirmada; os casos de eliminação não declaram colocação.

| ID | Técnica esperada | Evidência que a fixture declara para destaque | Efeito esperado |
|---|---|---|---|
| LH-01 | Naked Single | alvo e candidato restante | Colocação |
| LH-02 | Hidden Single | alvo, dígito e linha/coluna/bloco do escopo | Colocação |
| LH-03 | Cage Single | alvo, cage e valor residual | Colocação |
| LH-04 | Cage Combination | células da cage, combinações viáveis e candidatos eliminados | Eliminação de candidatos |
| LH-05 | Cage/Region Intersection | posições da interseção, região/cage de origem e candidatos eliminados | Eliminação de candidatos |
| LH-06 | Rule of 45 | unidade, cages/resíduo usados e candidatos eliminados | Eliminação de candidatos |
| LH-07 | Naked Pair | duas células do padrão, escopo, dígitos do par e candidatos eliminados | Eliminação de candidatos |
| LH-08 | Hidden Pair | duas células do padrão, escopo, dígitos do par e candidatos eliminados | Eliminação de candidatos |
| LH-09 | Naked Triple | três células do padrão, escopo, dígitos da tripla e candidatos eliminados | Eliminação de candidatos |

Cada ID é um vetor de teste determinístico com snapshot imutável completo e solução/multiplicidade de origem, técnica esperada, efeito esperado e mapa exato de posições/dígitos por papel de evidência. A fixture deve garantir que a técnica indicada seja a primeira aplicável na prioridade e desempate de 002. SC-002 compara o ID e os destaques ao vetor e compara nome/explicação ao conteúdo produzido pelo catálogo para esse ID e snapshot. A contagem do critério é exatamente nove vetores, sem inclusão dinâmica de casos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Existem fixtures de aceitação LH-01 a LH-09, uma por técnica v1, cada uma com snapshot e efeito esperado definidos.
- **SC-002**: Em 9/9 fixtures LH-01 a LH-09, o `LogicalTechniqueId`, o mapa exato de destaques, os dígitos e os pares posição/dígito do padrão correspondem ao vetor determinístico; nome e explicação em português correspondem à saída do catálogo para a técnica e as evidências esperadas, respeitando o nível solicitado.
- **SC-003**: Nenhuma sugestão é apresentada como certa quando contradiz o estado válido ou a solução única do puzzle.
- **SC-004**: Em todos os cenários de progressão, a ação lógica explícita aparece apenas no terceiro nível, após a explicação e o destaque; um valor de colocação nunca aparece antes desse nível.
- **SC-005**: Para cada cenário de referência cujo passo somente elimina candidatos, o nível mais explícito identifica os candidatos a remover e não apresenta um dígito para colocar nesse passo.
- **SC-006**: Em todos os cenários com jogadas atuais sem conclusão compatível com o puzzle original, o sistema informa essa condição e não apresenta dica nem valor.
- **SC-007**: Em 100% dos resultados de dica, `BoardRevision` corresponde à revisão do snapshot recebido, e a técnica, evidência e ação projetadas derivam desse mesmo snapshot imutável. A sessão não exibe resultado de revisão antiga, conforme `004-game-session` FR-013/SC-005.
- **SC-008**: Em todos os cenários com tabuleiro completo e correto, o sistema informa que o puzzle está resolvido e não apresenta uma dica como próximo passo.
- **SC-009**: Para puzzle original com multiplicidade `Multiple`, se o snapshot atual não tiver solução compatível, o resultado é `InconsistentState`; se tiver exatamente uma solução compatível, isso não libera `PlaceValue`, e o pedido de nível 3 retorna `ValueNotConfirmed` sem valor.

## Assumptions

- A linguagem das explicações será localizada inicialmente em português, consistente com a documentação do produto.
- As deduções e IDs estáveis do catálogo v1 são compartilhados com Domain/002; o motor de dicas acrescenta explicações localizadas para cada uma das nove técnicas.
- Se nenhuma técnica aplicável existir, o jogo oferece uma mensagem clara em vez de inventar uma dica.
- Depende de 001-killer-sudoku-rules e consome normalmente puzzles únicos publicados por 002-puzzle-engine; os estados de contexto `NoSolution` e `Multiple` também têm comportamento defensivo definido por FR-006 e SC-009.
- 003 devolve resultados vinculados à `BoardRevision`; a sessão de 004 é responsável por descartar resultados obsoletos e pedir novamente desde o nível 1, conforme FR-013/SC-005 de `004-game-session`.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Application para buscar e priorizar dicas; Domain para regras reutilizáveis; testes de aplicação e domínio. A apresentação MAUI e o ciclo de vida da sessão pertencem a 004.
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
3. Registrar dependências pela composition root existente na mesma fatia. Se a feature criar um contrato antes de existir host/composition root, não criar DI ou host artificial; documentar a dependência e registrá-la na primeira composition root real que consumir o contrato, conforme `specs/README.md`.
4. Não implementar as técnicas em ViewModels nem fundir dica explicável e solver computacional.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.
