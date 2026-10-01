# FEATURE SPEC: Motor de Resolução, Geração e Dificuldade

- **Feature Branch**: A definir na implementação
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD, seções 10 a 13 e 19; roadmap inicial do README. Depende de 001-killer-sudoku-rules.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** O jogador precisa receber puzzles corretos, adequados à dificuldade escolhida e com uma única resposta. Puzzles ambíguos, impossíveis ou mal classificados comprometem a confiança no jogo.
>
> **Definição de sucesso:** O jogo consegue resolver puzzles, contar soluções, gerar puzzles Killer válidos com solução única e oferecê-los na dificuldade solicitada.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; mantenha solver, gerador e classificador com responsabilidades separadas, sem abstrações sem necessidade demonstrável.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verificar se um puzzle pode ser resolvido (Priority: P1)

O jogador recebe um puzzle cuja solução pode ser encontrada e cuja unicidade foi verificada.

**Why this priority**: A unicidade é uma regra do produto e uma condição para liberar qualquer puzzle.

**Independent Test**: Validar puzzles de referência com nenhuma, uma e múltiplas soluções e comparar a contagem limitada ao resultado necessário para distinguir esses casos.

**Acceptance Scenarios**:

1. **Given** um puzzle inconsistente, **When** o motor tenta resolvê-lo, **Then** informa que não há solução.
2. **Given** um puzzle com solução única, **When** soluções são contadas, **Then** o motor confirma exatamente uma.
3. **Given** um puzzle com mais de uma solução, **When** soluções são contadas, **Then** o motor identifica que não é único e o puzzle não é oferecido.

### User Story 2 - Iniciar puzzle na dificuldade escolhida (Priority: P1)

O jogador escolhe uma dificuldade e recebe um puzzle Killer Sudoku que respeita as regras e o nível pedido.

**Why this priority**: Criar uma nova partida é um fluxo principal do MVP.

**Independent Test**: Solicitar puzzles por nível, validar sua estrutura, contar soluções e verificar que a análise de dificuldade corresponde ao nível anunciado.

**Acceptance Scenarios**:

1. **Given** uma dificuldade disponível, **When** o jogador solicita um novo puzzle, **Then** recebe um puzzle validado com uma única solução.
2. **Given** uma tentativa de geração inválida ou não classificável, **When** o motor avalia o resultado, **Then** esse puzzle é descartado e não chega ao jogador.
3. **Given** a geração em andamento, **When** o jogador cancela a solicitação, **Then** a operação termina sem bloquear a navegação nem publicar resultado parcial.

### Edge Cases

- Nenhuma solução, múltiplas soluções ou dados de cage estruturalmente inválidos.
- Geração sem candidatos válidos para atingir o nível solicitado após novas tentativas.
- Cancelamento durante geração ou análise.
- Puzzle cuja dificuldade não pode ser determinada pelas técnicas reconhecidas.
- Repetir a geração com a mesma semente de teste deve produzir resultado reproduzível.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST encontrar uma solução quando ela existir e informar ausência de solução quando não existir.
- **FR-002**: O sistema MUST contar soluções o suficiente para distinguir zero, uma e mais de uma.
- **FR-003**: O sistema MUST validar todas as regras de Sudoku e Killer Sudoku antes de aceitar um puzzle gerado.
- **FR-004**: O sistema MUST oferecer somente puzzles com exatamente uma solução.
- **FR-005**: O sistema MUST permitir solicitar um puzzle por uma das dificuldades Easy, Medium, Hard ou Expert.
- **FR-006**: O sistema MUST classificar a dificuldade segundo as técnicas lógicas necessárias, não somente pela quantidade ou formato das cages.
- **FR-007**: O sistema MUST rejeitar puzzles que não possam ser classificados no intervalo suportado.
- **FR-008**: O sistema MUST permitir cancelar operações demoradas sem publicar um puzzle parcial.
- **FR-009**: O sistema MUST permitir controlar a aleatoriedade em testes para que casos automatizados sejam reproduzíveis.

### Key Entities

- **Puzzle**: givens, cages e estado inicial publicado ao jogador.
- **Solução**: preenchimento completo que respeita as regras.
- **Dificuldade**: uma das categorias Easy, Medium, Hard ou Expert.
- **Análise de dificuldade**: técnicas necessárias e categoria atribuída.
- **Pedido de geração**: dificuldade solicitada e estado de cancelamento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos puzzles liberados ao jogador passam pela validação estrutural e têm exatamente uma solução nos cenários de aceitação.
- **SC-002**: Todos os fixtures sem solução, únicos e ambíguos são distinguidos corretamente pelo motor.
- **SC-003**: Todo puzzle publicado recebe uma categoria suportada que corresponde às técnicas necessárias para resolvê-lo.
- **SC-004**: Cancelar uma geração encerra o pedido sem exibir um puzzle incompleto como partida disponível.

## Assumptions

- A geração será local e não dependerá de serviço externo.
- As quatro dificuldades documentadas no PRD são suficientes para o MVP.
- O motor de solver não será usado como motor de explicações; essa responsabilidade pertence à feature 003-logical-hints.
- A estratégia algorítmica e os limites de tempo serão definidos no plano de implementação a partir de medições nos dispositivos-alvo.
- Depende das regras e dos candidatos de 001-killer-sudoku-rules.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Domain para regras compartilhadas; Application para resolução, geração e análise; testes de domínio e aplicação. Os projetos ainda não existem.
- Solver, generator e análise de dificuldade têm responsabilidades distintas. O gerador pode consultar o solver; o solver não conhece o gerador.
- Operações demoradas aceitam cancelamento quando a API do projeto for definida e não bloqueiam a thread de UI.
- DI segue a composition root existente quando criada; abstrações somente para fronteiras reais.
- Regras de C#, logging, segurança e gates compartilhados estão em specs/README.md.

## 3. FATIAS VERTICAIS DE IMPLEMENTAÇÃO (máximo 3)

### Slice 1: Resolver e distinguir unicidade
- **Escopo**: Receber um puzzle, respeitar as regras e informar zero, uma ou múltiplas soluções.
- **Validação local**: Testar puzzles de referência para cada resultado e compilar os projetos afetados.

### Slice 2: Gerar puzzles Killer válidos
- **Escopo**: Produzir um puzzle para dificuldade escolhida, descartar estruturas ou somas inválidas e aceitar somente solução única.
- **Validação local**: Testar geração reproduzível com aleatoriedade controlada, cancelamento e rejeição de puzzle ambíguo.

### Slice 3: Classificar e liberar dificuldade
- **Escopo**: Analisar as técnicas necessárias, classificar Easy, Medium, Hard ou Expert e entregar somente resultado compatível ao início de partida.
- **Validação local**: Conferir puzzles de referência por nível e o fluxo completo de solicitação, validação e publicação.

## 4. GATES DE VALIDAÇÃO (.NET Toolchain)

Quando a solution existir, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Os comandos ainda não podem ser executados: o repositório contém apenas documentação e não possui solution ou projetos.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir o contrato desta feature.
3. Registrar no DI qualquer contrato criado ou alterado na mesma fatia.
4. Não misturar solver computacional com dicas explicativas nem fazer o solver depender do gerador.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.