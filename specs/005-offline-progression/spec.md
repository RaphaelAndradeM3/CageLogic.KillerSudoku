# FEATURE SPEC: Persistência Offline e Progresso

- **Feature Branch**: A definir na implementação
- **Created**: 2026-10-01
- **Status**: Draft
**Input**: PRD RF-014 a RF-019; seções de persistência, estatísticas e métricas em PRD. Depende de 004-game-session.

## 1. META IMUTÁVEL (Global Goal)

> **Problema de negócio:** Partidas Killer Sudoku podem durar mais que uma sessão de uso. Se o jogador precisa recomeçar após fechar o app ou não consegue acompanhar sua evolução, perde progresso e motivação.
>
> **Definição de sucesso:** O jogo salva a partida localmente, restaura-a ao reabrir e apresenta estatísticas e preferências sem exigir login ou conexão.
>
> **Regra de ouro:** Preserve a arquitetura e as convenções do repositório; mantenha dados locais atrás de fronteiras de aplicação e não adicione serviços online ao MVP.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Continuar uma partida (Priority: P1)

O jogador fecha ou interrompe o aplicativo e retorna à mesma partida com seu estado restaurado.

**Why this priority**: Continuidade protege o principal investimento do jogador: o tempo dedicado ao puzzle.

**Independent Test**: Alterar um puzzle, sair e reabrir o app; comparar tabuleiro, candidatos, tempo e estado da sessão.

**Acceptance Scenarios**:

1. **Given** uma partida em andamento, **When** o estado muda, **Then** a partida é salva localmente sem exigir uma ação manual explícita.
2. **Given** uma partida salva, **When** o jogador reabre o aplicativo, **Then** pode continuar do estado e tempo preservados.
3. **Given** não há partida anterior, **When** o aplicativo é aberto, **Then** oferece iniciar uma nova partida sem apresentar dados inexistentes como salvos.

### User Story 2 - Acompanhar progresso e preferências (Priority: P2)

O jogador consulta resultados de partidas e escolhe preferências visuais que continuam aplicadas depois de reabrir o aplicativo.

**Why this priority**: Estatísticas e preferências dão continuidade de produto sem serem pré-requisitos para concluir o primeiro puzzle.

**Independent Test**: Concluir partidas com tempos, erros e dicas conhecidos; reiniciar o app e conferir estatísticas e tema.

**Acceptance Scenarios**:

1. **Given** partidas concluídas, **When** o jogador abre estatísticas, **Then** vê iniciadas, concluídas, tempo médio, melhor tempo, erros e dicas.
2. **Given** o jogador muda o tema, **When** o aplicativo é reaberto, **Then** a preferência continua aplicada.
3. **Given** nenhuma partida foi concluída, **When** estatísticas são abertas, **Then** valores vazios são exibidos sem erro nem divisão por zero.

### Edge Cases

- Encerramento inesperado durante salvamento ou leitura.
- Armazenamento local indisponível, cheio ou com dados antigos/inválidos.
- Primeira inicialização e atualização de versão dos dados persistidos.
- Reabertura de partida encerrada, limpa ou já concluída.
- Estatísticas sem histórico, com partidas pausadas ou com sessão abandonada.
- Falha de persistência não pode corromper silenciosamente a única cópia de uma partida.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST salvar automaticamente o estado da partida localmente durante o jogo.
- **FR-002**: O sistema MUST restaurar a partida atual após reiniciar o aplicativo, incluindo puzzle, valores, candidatos, histórico necessário e tempo.
- **FR-003**: O sistema MUST operar os fluxos principais de partida, salvamento e retomada sem internet.
- **FR-004**: O sistema MUST informar quando não existe partida anterior disponível.
- **FR-005**: O sistema MUST registrar partidas iniciadas e concluídas, duração, melhor tempo, erros e dicas usadas.
- **FR-006**: O sistema MUST calcular tempo médio e melhor tempo sem erro quando não houver partidas concluídas.
- **FR-007**: O sistema MUST permitir escolher tema claro e escuro e preservar a preferência localmente.
- **FR-008**: O sistema MUST apresentar falhas inesperadas por mensagem segura e permitir ao jogador tentar recuperar ou reiniciar sem expor detalhes internos.
- **FR-009**: O sistema MUST evitar registrar senha, token, dados pessoais ou conteúdo sensível nos logs.
- **FR-010**: O sistema MUST tratar formatos persistidos incompatíveis ou inválidos sem iniciar uma partida incorreta como se fosse íntegra.

### Key Entities

- **Partida salva**: puzzle, estado editável, candidatos, tempo e progresso.
- **Histórico de jogadas**: mudanças necessárias para retomar o estado e o undo/redo suportado.
- **Registro de partida**: resultado, tempo, erros e dicas.
- **Estatísticas**: agregados de partidas iniciadas e concluídas.
- **Preferências**: tema visual e escolhas locais do jogador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cenários de encerramento e reabertura restauram o último estado salvo válido da partida.
- **SC-002**: As estatísticas exibidas correspondem exatamente às partidas concluídas nos cenários de referência.
- **SC-003**: As preferências permanecem aplicadas após reiniciar o aplicativo nos dois sistemas-alvo.
- **SC-004**: Os fluxos de retomada, estatísticas e tema funcionam sem conexão e não exigem conta de usuário.

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
