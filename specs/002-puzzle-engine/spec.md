# FEATURE SPEC: Motor de Resolução, Geração e Dificuldade

- **Feature Branch**: 002-puzzle-engine
- **Created**: 2026-10-01
- **Status**: Ready for implementation
**Input**: PRD, seções 10 a 13 e 19; roadmap inicial do README. Depende de 001-killer-sudoku-rules.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** O jogador precisa receber puzzles corretos, adequados à dificuldade escolhida e com uma única resposta. Puzzles ambíguos, impossíveis ou mal classificados comprometem a confiança no jogo.
>
> **Definição de sucesso:** O jogo consegue resolver puzzles, contar soluções, gerar puzzles Killer válidos com solução única e oferecê-los na dificuldade solicitada.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; mantenha solver, gerador e classificador com responsabilidades separadas, sem abstrações sem necessidade demonstrável.

## Clarifications

### Session 2026-10-05

- Q: Se o gerador esgotar o limite configurado sem encontrar um puzzle único e classificado na dificuldade solicitada, o que deve acontecer? → A: Retornar uma falha explícita sem oferecer puzzle de outro nível; o limite concreto de tentativas/tempo será definido no plano após medições.
- Q: Como o motor deve atribuir Easy, Medium, Hard e Expert conforme as técnicas lógicas necessárias para resolver o puzzle? → A: Cada nível corresponde a um conjunto documentado de técnicas necessárias.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verificar se um puzzle pode ser resolvido (Priority: P1)

O jogador recebe um puzzle cuja solução pode ser encontrada e cuja unicidade foi verificada.

**Why this priority**: A unicidade é uma regra do produto e uma condição para liberar qualquer puzzle.

**Independent Test**: Validar puzzles de referência com nenhuma, uma e múltiplas soluções e comparar a contagem limitada ao resultado necessário para distinguir esses casos.

**Acceptance Scenarios**:

1. **Given** uma definição estruturalmente válida cujos valores fixos se contradizem, **When** o motor tenta resolvê-la, **Then** informa `NoSolution`; uma definição estruturalmente inválida é rejeitada antes da busca.
2. **Given** um puzzle com solução única, **When** soluções são contadas, **Then** o motor confirma exatamente uma.
3. **Given** um puzzle com mais de uma solução, **When** soluções são contadas, **Then** o motor identifica que não é único e o puzzle não é oferecido.

### User Story 2 - Iniciar puzzle na dificuldade escolhida (Priority: P1)

O jogador escolhe uma dificuldade e recebe um puzzle Killer Sudoku que respeita as regras e o nível pedido.

**Why this priority**: Criar uma nova partida é um fluxo principal do MVP.

**Independent Test**: Solicitar puzzles por nível, validar sua estrutura, contar soluções e verificar que a análise de dificuldade corresponde ao nível anunciado segundo os conjuntos documentados de técnicas.

**Acceptance Scenarios**:

1. **Given** uma dificuldade disponível, **When** o jogador solicita um novo puzzle, **Then** recebe um puzzle validado, com solução única e classificado no nível solicitado segundo as técnicas documentadas.
2. **Given** uma tentativa de geração inválida ou não classificável, **When** o motor avalia o resultado, **Then** esse puzzle é descartado e não chega ao jogador.
3. **Given** a geração em andamento, **When** o jogador cancela a solicitação, **Then** a operação termina sem bloquear a navegação nem publicar resultado parcial.
4. **Given** que o limite configurado de geração seja esgotado sem encontrar um puzzle único classificado no nível solicitado, **When** o pedido termina, **Then** o motor retorna indisponibilidade explícita e não oferece um puzzle de outro nível.
5. **Given** um puzzle gerado é iniciado, **When** o tabuleiro aparece ao jogador, **Then** as 81 células começam vazias e as cages com seus alvos são as pistas; a solução não é exposta como valor fixo.

### Edge Cases

- Nenhuma solução, múltiplas soluções ou dados de cage estruturalmente inválidos.
- Definição estruturalmente inválida deve ser diferenciada de uma definição válida sem solução; só `ValidatedPuzzle` entra no solver.
- Esgotamento do limite configurado de geração sem candidatos válidos no nível solicitado; retornar indisponibilidade sem fallback de dificuldade.
- Cancelamento durante geração ou análise.
- Puzzle cuja dificuldade não pode ser determinada pelas técnicas reconhecidas.
- Repetir a geração com a mesma semente de teste deve produzir resultado reproduzível.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST encontrar uma solução quando ela existir e informar ausência de solução quando não existir.
- **FR-002**: O sistema MUST distinguir zero, exatamente uma e duas ou mais soluções; a busca MUST parar na segunda solução distinta e não afirmar uma contagem exata acima de 2.
- **FR-003**: O sistema MUST validar todas as regras de Sudoku e Killer Sudoku antes de aceitar um puzzle gerado.
- **FR-004**: O sistema MUST oferecer somente puzzles com exatamente uma solução.
- **FR-005**: O sistema MUST permitir solicitar um puzzle por uma das dificuldades Easy, Medium, Hard ou Expert.
- **FR-006**: O sistema MUST classificar a dificuldade pela correspondência documentada entre as técnicas lógicas necessárias e cada nível Easy, Medium, Hard ou Expert, não somente pela quantidade ou formato das cages.
- **FR-007**: O sistema MUST rejeitar puzzles que não possam ser classificados no intervalo suportado.
- **FR-008**: O sistema MUST permitir cancelar operações demoradas sem publicar puzzle parcial. Solver/geração observam o token durante a busca; a Application executa o trabalho CPU-bound fora da thread de UI. Cancelamento do chamador encerra como `OperationCanceledException`, nunca como `Unavailable`.
- **FR-009**: O sistema MUST permitir controlar a aleatoriedade em testes para que casos automatizados sejam reproduzíveis. O replay é garantido para a mesma seed, pedido, orçamento, versão do algoritmo/catálogo e runtime suportado.
- **FR-010**: O sistema MUST retornar indisponibilidade explícita com motivo, tentativas e tempo decorrido, sem puzzle parcial e sem substituir a dificuldade solicitada, quando o orçamento configurado for esgotado sem encontrar um puzzle elegível.
- **FR-011**: Cada pedido MUST receber limites explícitos e positivos de tentativas e duração. A API da biblioteca não define valores padrão de produção antes das medições documentadas em Windows e no Android mínimo suportado.
- **FR-012**: Todo puzzle gerado para o MVP MUST iniciar sem dígitos fixos; seus 81 valores permanecem vazios para o jogador, e a grade-solução é mantida separada das pistas de cage.

### Rubrica de dificuldade — catálogo v1

Os conjuntos abaixo são cumulativos e normativos para `DifficultyProfileCatalog` versão 1:

| Perfil | Técnicas permitidas |
|---|---|
| Easy | Naked Single, Hidden Single, Cage Single |
| Medium | Todas de Easy + Cage Combination |
| Hard | Todas de Medium + Cage/Region Intersection + Rule of 45 |
| Expert | Todas de Hard + Naked Pair + Hidden Pair + Naked Triple |

Todos os passos consomem o estado lógico atual: tabuleiro mais candidatos ainda possíveis. Esse estado começa com os candidatos legais calculados para o puzzle; eliminações deduzidas permanecem removidas entre passos. Uma colocação atualiza o tabuleiro e recalcula candidatos, sem reintroduzir candidatos eliminados anteriormente. As técnicas da tabela têm estes efeitos:

| Técnica | Dedução v1 |
|---|---|
| Naked Single | Coloca o único candidato restante de uma célula vazia. |
| Hidden Single | Coloca um dígito quando ele só pode ocupar uma célula vazia em uma linha, coluna ou bloco 3×3. |
| Cage Single | Enumera atribuições completas da cage compatíveis com candidatos, soma e não repetição; coloca um valor que é igual em todas as atribuições para uma célula. |
| Cage Combination | Elimina de uma célula da cage os candidatos que não aparecem naquela posição em nenhuma atribuição completa válida da cage. |
| Cage/Region Intersection | Se todas as atribuições da cage colocam um dígito apenas em células de uma mesma linha, coluna ou bloco, elimina esse dígito das demais células vazias dessa região fora da cage. |
| Rule of 45 | Para uma linha, coluna ou bloco `H`, a soma das partes de cages que cruzam `H` é `45 - soma dos alvos das cages inteiramente contidas em H`. Elimina candidatos das partes cruzadas sem suporte em uma combinação de atribuições de cage que alcance esse residual. |
| Naked Pair | Em uma região, duas células cuja união de candidatos contém exatamente dois dígitos eliminam esses dígitos das outras células da região. |
| Hidden Pair | Em uma região, dois dígitos que aparecem como candidatos exatamente nas mesmas duas células restringem essas células a esse par. |
| Naked Triple | Em uma região, três células com união de candidatos de exatamente três dígitos eliminam esses dígitos das outras células da região. |

A classificação mede o perfil menos avançado que completa uma trilha lógica determinística iniciada do puzzle original. Para cada perfil, o classificador reinicia o estado lógico e aplica somente técnicas permitidas, respeitando a ordem da tabela. Se mais de um passo da mesma técnica estiver disponível, escolhe primeiro a lista ordenada de posições relacionadas em row-major, depois o dígito crescente e depois colocação antes de eliminação; os efeitos de um passo também são ordenados. A classificação só ocorre quando todas as células estão preenchidas e o validador confirma a solução. Se nenhum perfil concluir, o resultado é `Unclassifiable`. O solver com backtracking não participa da trilha lógica.

### Key Entities

- **Puzzle**: cages, alvos e estado inicial sem dígitos fixos publicado ao jogador; a solução completa é mantida separada.
- **Solução**: preenchimento completo que respeita as regras.
- **Dificuldade**: uma das categorias Easy, Medium, Hard ou Expert.
- **Análise de dificuldade**: técnicas necessárias e categoria atribuída.
- **Pedido de geração**: dificuldade solicitada e estado de cancelamento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos puzzles liberados ao jogador passam pela validação estrutural e têm exatamente uma solução nos cenários de aceitação.
- **SC-002**: Todos os fixtures sem solução, únicos e ambíguos são distinguidos corretamente pelo motor, com a contagem limitada a 0, 1 ou 2 (2 significa “duas ou mais”).
- **SC-003**: Todo puzzle publicado recebe uma categoria suportada que corresponde ao conjunto documentado de técnicas necessárias para resolvê-lo.
- **SC-004**: Cancelar uma geração encerra o pedido sem exibir um puzzle incompleto como partida disponível.
- **SC-005**: Ao esgotar o limite configurado sem encontrar um puzzle único classificado na dificuldade solicitada, o pedido retorna indisponibilidade sem publicar puzzle de outra dificuldade.
- **SC-006**: Todo pedido de geração usa limites explícitos de tentativas e duração; nenhum padrão de produção é configurado antes de medir e documentar o pipeline nos dispositivos-alvo.
- **SC-007**: 100% dos puzzles gerados são entregues com as 81 células sem valores fixos e com a solução completa separada do estado inicial apresentado.

## Assumptions

- A geração será local e não dependerá de serviço externo.
- As quatro dificuldades documentadas no PRD são suficientes para o MVP.
- Um puzzle Killer gerado começa sem valores fixos; cages e alvos são as pistas visíveis.
- O catálogo de técnicas e os desempates da rubrica v1 são contratos de produto versionados; alteração futura requer nova versão do catálogo.
- O motor de solver não será usado como motor de explicações; essa responsabilidade pertence à feature 003-logical-hints.
- A estratégia algorítmica está definida no plano; valores padrão de orçamento só serão escolhidos após medições nos dispositivos-alvo. A ausência de padrão de produção não impede executar pedidos com orçamento explícito.
- Depende das regras e dos candidatos de 001-killer-sudoku-rules.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas afetadas**: Domain para regras compartilhadas; Application para resolução, geração e análise; testes de domínio e aplicação. `CageLogic.slnx`, os projetos Domain/Application e os dois projetos NUnit de teste já existem em `net10.0`; nenhum projeto novo ou host MAUI é necessário nesta feature.
- Solver, generator e análise de dificuldade têm responsabilidades distintas. O gerador pode consultar o solver; o solver não conhece o gerador.
- Operações demoradas aceitam `CancellationToken`; casos de uso executam trabalho CPU-bound fora da thread de UI.
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

Na solution existente, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build --configuration Release após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Esses gates se aplicam à `CageLogic.slnx` e aos projetos `net10.0` existentes. Builds dos targets Windows/Android dependem do host MAUI e dos workloads que serão introduzidos pela feature 004.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir o contrato desta feature.
3. Registrar no DI qualquer contrato criado ou alterado na mesma fatia.
4. Não misturar solver computacional com dicas explicativas nem fazer o solver depender do gerador.
5. Aplicar os guardrails C# e logging compartilhados em specs/README.md.
