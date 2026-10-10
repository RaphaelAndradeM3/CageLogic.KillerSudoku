# FEATURE SPEC: Persistência Offline e Progresso

- **Feature Branch**: 005-offline-progression
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD RF-014 a RF-019; seções de persistência, estatísticas e métricas em PRD. Depende de 004-game-session.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** Partidas Killer Sudoku podem durar mais que uma sessão de uso. Se o jogador precisa recomeçar após fechar o app ou não consegue acompanhar sua evolução, perde progresso e motivação.
>
> **Definição de sucesso:** O jogo salva a partida localmente, restaura-a ao reabrir e apresenta estatísticas e preferências sem exigir login ou conexão.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; mantenha dados locais atrás de fronteiras de aplicação e não adicione serviços online ao MVP.

## Clarifications

### Session 2026-10-09

- Q: Se o salvamento falhar ou os dados da partida não puderem ser lidos, como o app deve agir? → A: Preservar o último salvamento válido; avisar sobre a falha e manter a partida atual em memória; se não houver estado recuperável, pedir confirmação antes de iniciar outra partida.
- Q: Se o aplicativo for encerrado inesperadamente logo após uma jogada, quanto progresso confirmado pode ser perdido? → A: Nenhuma jogada confirmada pode ser perdida; apenas uma jogada interrompida antes da confirmação pode precisar ser repetida.
- Q: O que o app deve mostrar ao reabrir depois que a partida já foi concluída? → A: Abrir a tela inicial; manter a conclusão nas estatísticas e não restaurar a partida como ativa.
- Q: Quando o jogador abandona uma partida antes de concluí-la, quais informações dela devem entrar nas estatísticas? → A: Contar como iniciada, não como concluída; excluir o tempo da média e do melhor tempo; incluir erros e dicas já usados nos totais.
- Q: Quanto deve demorar, no máximo, para confirmar uma jogada depois de salvá-la localmente em condições normais? → A: Confirmar 95% das jogadas em até 250 ms; enquanto aguarda, manter a interface responsiva e indicar que a jogada ainda está sendo salva.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuar uma partida (Priority: P1)

O jogador fecha ou interrompe o aplicativo e retorna à mesma partida com seu estado restaurado.

**Why this priority**: Continuidade protege o principal investimento do jogador: o tempo dedicado ao puzzle.

**Independent Test**: Alterar um puzzle, sair e reabrir o app; comparar tabuleiro, candidatos, tempo e estado da sessão.

**Acceptance Scenarios**:

1. **Given** uma partida em andamento, **When** o estado muda, **Then** a partida é salva localmente sem exigir uma ação manual explícita.
2. **Given** uma partida salva, **When** o jogador reabre o aplicativo, **Then** pode continuar do estado e tempo preservados.
3. **Given** não há partida anterior, **When** o aplicativo é aberto, **Then** oferece iniciar uma nova partida sem apresentar dados inexistentes como salvos.
4. **Given** uma partida em andamento com um salvamento válido, **When** uma gravação falha, **Then** o último salvamento válido é preservado, a partida continua em memória e o jogador é avisado de que mudanças recentes podem não estar salvas.
5. **Given** nenhum estado válido pode ser recuperado, **When** o aplicativo é aberto, **Then** pede confirmação antes de iniciar outra partida.
6. **Given** uma jogada foi confirmada ao jogador, **When** o aplicativo é encerrado inesperadamente e reaberto, **Then** todas as jogadas confirmadas são restauradas; apenas uma jogada interrompida antes da confirmação pode precisar ser repetida.
7. **Given** a partida já foi concluída e registrada, **When** o jogador reabre o aplicativo, **Then** o app abre a tela inicial, mantém a conclusão nas estatísticas e não restaura a partida como ativa nem conta a conclusão novamente.
8. **Given** uma jogada aguarda persistência local, **When** o salvamento está em andamento, **Then** a interface permanece responsiva, indica que a jogada está pendente e só a confirma após persistência; em condições normais, 95% dos salvamentos terminam em até 250 ms.

### User Story 2 - Acompanhar progresso e preferências (Priority: P2)

O jogador consulta resultados de partidas e escolhe preferências visuais que continuam aplicadas depois de reabrir o aplicativo.

**Why this priority**: Estatísticas e preferências dão continuidade de produto sem serem pré-requisitos para concluir o primeiro puzzle.

**Independent Test**: Concluir partidas com tempos, erros e dicas conhecidos; reiniciar o app e conferir estatísticas e tema.

**Acceptance Scenarios**:

1. **Given** partidas concluídas, **When** o jogador abre estatísticas, **Then** vê iniciadas, concluídas, tempo médio, melhor tempo, erros e dicas.
2. **Given** o jogador muda o tema, **When** o aplicativo é reaberto, **Then** a preferência continua aplicada.
3. **Given** nenhuma partida foi concluída, **When** estatísticas são abertas, **Then** valores vazios são exibidos sem erro nem divisão por zero.
4. **Given** uma partida iniciada é explicitamente abandonada antes da conclusão, **When** as estatísticas são consultadas, **Then** ela conta como iniciada, mas não concluída; seu tempo não entra na média nem no melhor tempo, enquanto os erros e as dicas já usados entram nos totais.

### Edge Cases

- Encerramento inesperado durante salvamento ou leitura: todas as jogadas confirmadas devem ser restauradas; uma jogada interrompida antes da confirmação pode precisar ser repetida, sem substituir o último salvamento válido.
- Armazenamento local indisponível ou cheio: a falha de escrita preserva o último salvamento válido; o jogador pode continuar na sessão em memória e recebe aviso de que mudanças recentes podem não estar salvas. Dados antigos, incompatíveis ou inválidos não são tratados como íntegros.
- Primeira inicialização e atualização de versão dos dados persistidos.
- Ao reabrir o aplicativo após uma conclusão, abrir a tela inicial, preservar a conclusão nas estatísticas e não contá-la novamente.
- Estatísticas sem histórico, com partidas pausadas ou após abandono: a partida abandonada conta como iniciada, mas não concluída; seu tempo não entra na média nem no melhor tempo, e erros e dicas usados permanecem nos totais.
- Se nenhum estado válido puder ser recuperado, o aplicativo pede confirmação antes de iniciar outra partida.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST salvar automaticamente as mudanças da partida localmente. Uma jogada só é confirmada ao jogador após persistência local bem-sucedida. Em condições normais de armazenamento, 95% dos salvamentos MUST ser confirmados em até 250 ms. Enquanto um salvamento estiver pendente, a interface MUST continuar responsiva e indicar esse estado; uma jogada MUST NOT ser apresentada como confirmada antes de ser salva. Toda jogada confirmada MUST poder ser restaurada após encerramento inesperado; somente uma jogada interrompida antes da confirmação pode precisar ser repetida.
- **FR-002**: O sistema MUST restaurar a partida atual após reiniciar o aplicativo, incluindo puzzle, valores, candidatos, histórico necessário e tempo. Se a partida já estiver concluída, MUST abrir a tela inicial sem restaurá-la como ativa e manter sua conclusão registrada uma única vez nas estatísticas.
- **FR-003**: O sistema MUST operar os fluxos principais de partida, salvamento e retomada sem internet.
- **FR-004**: O sistema MUST informar quando não existe partida anterior disponível.
- **FR-005**: O sistema MUST contar uma partida como iniciada quando ela é criada e como concluída somente após a solução correta. Uma partida explicitamente abandonada MUST continuar contada como iniciada, mas não como concluída; sua duração MUST ser excluída da média e do melhor tempo, e seus erros e dicas já usados MUST permanecer nos totais.
- **FR-006**: O sistema MUST calcular tempo médio e melhor tempo sem erro quando não houver partidas concluídas.
- **FR-007**: O sistema MUST permitir escolher tema claro e escuro e preservar a preferência localmente.
- **FR-008**: O sistema MUST apresentar falhas inesperadas com mensagem segura. Quando uma gravação falhar, MUST preservar o último estado de partida salvo validamente, manter a sessão atual em memória e avisar que alterações recentes não estão salvas; essas alterações MUST NOT ser apresentadas como confirmadas até que uma gravação seja bem-sucedida.
- **FR-009**: O sistema MUST evitar registrar senha, token, dados pessoais ou conteúdo sensível nos logs.
- **FR-010**: O sistema MUST tratar formatos persistidos incompatíveis ou inválidos sem iniciar uma partida incorreta como se estivesse íntegra. Se nenhum estado válido puder ser recuperado, MUST pedir confirmação antes de iniciar outra partida.

### Key Entities

- **Partida salva**: puzzle, estado editável, candidatos, tempo e progresso.
- **Histórico de jogadas**: mudanças necessárias para retomar o estado e o undo/redo suportado.
- **Registro de partida**: estado concluído ou abandonado, tempo, erros e dicas; erros e dicas permanecem nos totais mesmo se a partida for abandonada.
- **Estatísticas**: agregados de partidas iniciadas e concluídas.
- **Preferências**: tema visual e escolhas locais do jogador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos testes de encerramento inesperado, todas as jogadas confirmadas ao jogador são restauradas; somente uma jogada interrompida antes da confirmação pode precisar ser repetida.
- **SC-002**: As estatísticas correspondem exatamente às partidas concluídas nos cenários de referência; partidas abandonadas contam como iniciadas, não como concluídas, não afetam média/melhor tempo e mantêm erros e dicas nos totais; reabrir após uma conclusão não duplica a partida registrada.
- **SC-003**: As preferências permanecem aplicadas após reiniciar o aplicativo nos dois sistemas-alvo.
- **SC-004**: Os fluxos de retomada, estatísticas e tema funcionam sem conexão e não exigem conta de usuário.
- **SC-005**: Em 100% dos testes de falha de gravação, o último salvamento válido é preservado, a sessão atual continua disponível em memória e o jogador recebe aviso; mudanças sem persistência bem-sucedida não são apresentadas como confirmadas; sem estado recuperável, outra partida não começa sem confirmação.
- **SC-006**: Em condições normais de armazenamento local, pelo menos 95% dos salvamentos terminam e confirmam a jogada em até 250 ms; enquanto aguardam, a interface permanece responsiva e sinaliza que a jogada está pendente.

## Assumptions

- Os dados pertencem ao dispositivo e não são sincronizados entre Windows e Android no MVP.
- A partida atual, histórico e preferências são armazenados localmente.
- A política de retenção de logs e evolução do formato persistido será definida no plano técnico antes da implementação.
- O fluxo de partida e os eventos de início, conclusão, erro e dica vêm de 004-game-session.

## 2. CONTRATOS & LIMITES DA ARQUITETURA

- **Camadas previstas afetadas**: Application, Infrastructure e MAUI; testes de integração para persistência local e de aplicação para agregados.
- Persistência é acessada por contratos de aplicação; a lógica central não referencia SQLite ou APIs de plataforma.
- Serilog fica na composition root/adaptador de Infrastructure, sem tipos Serilog no Domain. Aplicar logging estruturado, arquivos diários separados para mensagens e erros, timestamp com offset e correlation ID quando aplicável.
- Falhas inesperadas são registradas na fronteira do host; a mensagem ao jogador é genérica e segura, com fallback mínimo se o logging falhar.
- Regras de C#, logging, segurança e gates compartilhados estão em specs/README.md.

## 3. FATIAS VERTICAIS DE IMPLEMENTAÇÃO (máximo 3)

### Slice 1: Salvar e retomar partida
- **Escopo**: Persistir mudanças da sessão localmente e restaurar tabuleiro, candidatos, tempo e continuidade após reinício.
- **Validação local**: Encerrar/reabrir em cenários de jogo válidos, sem partida e com dado inválido ou armazenamento indisponível.

### Slice 2: Registrar e consultar estatísticas
- **Escopo**: Registrar eventos de início/conclusão e apresentar tempos, erros e dicas agregados.
- **Validação local**: Comparar a tela de estatísticas a um conjunto conhecido de partidas, inclusive histórico vazio.

### Slice 3: Preservar preferências e falhas seguras
- **Escopo**: Salvar tema, manter uso offline e apresentar falhas de forma segura com logging operacional configurado.
- **Validação local**: Reabrir nos dois alvos, simular falha de leitura/escrita e verificar correlação, retenção e ausência de dados sensíveis em logs.

## 4. GATES DE VALIDAÇÃO (.NET Toolchain)

Na solution existente, executar na raiz:

- dotnet build --configuration Release --warnaserror
- dotnet test --no-build --configuration Release após build bem-sucedido
- Compilar os targets Windows e Android quando workloads e SDKs estiverem configurados.

Esses comandos se aplicam à `CageLogic.slnx` e aos projetos `net10.0` existentes. Builds dos targets Windows/Android dependem do host MAUI e dos workloads que serão introduzidos em 004. Testes de integração devem usar a infraestrutura realmente adotada; WebApplicationFactory e filtro Category=Integration só serão usados se forem adequados e já estiverem configurados para o aplicativo.

## 5. INSTRUÇÕES DE EXECUÇÃO PARA O AGENTE (Agent Guardrails)

1. Criar arquivos somente dentro do projeto correspondente e respeitar namespaces e convenções existentes.
2. Alterar somente o necessário para cumprir o contrato desta feature.
3. Registrar no DI qualquer contrato criado ou alterado na mesma fatia.
4. Não capturar exceções silenciosamente; preservar stack trace ao relançar e não expor detalhes internos ao jogador.
5. Não registrar segredo ou dado pessoal sem necessidade operacional e proteção definida.
6. Aplicar os guardrails C# e logging compartilhados em specs/README.md.
