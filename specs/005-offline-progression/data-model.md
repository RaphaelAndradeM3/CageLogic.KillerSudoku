# Modelo de Dados: Persistência Offline e Progresso

## Limites e direção das dependências

- Domain continua responsável por regras do tabuleiro e invariantes do puzzle, sem conhecer armazenamento, MAUI, SQLite ou JSON.
- Application define contratos de carregamento/gravação, modelos de persistência e estatísticas; captura/restaura GameSession sem serializar o objeto vivo.
- Infrastructure implementa o contrato de persistência usando Microsoft.Data.Sqlite.
- O host MAUI injeta o caminho de AppDataDirectory, registra as dependências e implementa a porta de preferência com Preferences.
- Os contratos são internos ao aplicativo; não há endpoints nem contrato externo.

## Entidades da aplicação

### SavedGameSession

Representação versionada, imutável e validável para persistência. É construída a partir de GameSession por um mapeador da Application; na restauração, a Application valida e recria o objeto vivo.

Campos persistidos:

- SessionId: GUID estável para idempotência.
- Status: Active, Completed ou Abandoned.
- StartedAtUtc e, quando aplicável, CompletedAtUtc ou AbandonedAtUtc.
- Difficulty: nível que originou a partida.
- ActiveElapsedTicks: duração acumulada em foreground; restaura pausada.
- ErrorCount e DisplayedHintLevelCount: métricas definidas em 004-game-session.
- SnapshotVersion: versão do formato de snapshot.
- Snapshot: dados do jogo ativo; nulo para registros concluídos/abandonados.

Snapshot do jogo ativo:

- definição imutável do puzzle: valores iniciais e cages com posição e soma alvo;
- solução de 81 dígitos, necessária para TryComplete offline; nunca mostrada na interface e nunca registrada em logs;
- valores atuais do tabuleiro;
- candidatos/notas por célula;
- histórico de undo/redo, incluindo snapshots before/after de tabuleiro e notas e a posição atual das pilhas;
- versão explícita do formato.

Não persistir ViewState, seleção da célula, modo de entrada, dica em andamento, análise pendente, eventos de UI ou BoardRevision. Seleção e modo podem voltar aos valores padrão. BoardRevision e estado de validação são derivados. O estado de dica apresentado é transitório; a contagem acumulada de níveis exibidos é persistida.

### GameRecord

Uma linha é criada quando o caso de uso cria uma partida. Assim, Active já conta como iniciada. A transição Active → Completed ou Active → Abandoned é atômica e terminal. O mesmo SessionId nunca cria uma segunda partida; uma conclusão repetida não duplica estatísticas.

Uma partida abandonada continua contando como iniciada, não como concluída; seu tempo não entra em média/melhor tempo, mas erros e dicas entram nos totais. Tempo só acumula enquanto a sessão está em foreground, conforme o timer de 004.

### ProgressionStatistics

Projeção calculada a partir dos registros, sem manter cópia mutável:

- StartedCount: todos os registros criados (Active, Completed ou Abandoned).
- CompletedCount: registros Completed.
- AverageCompletedTime e BestCompletedTime: somente Completed.
- TotalErrors e TotalHints: registros de qualquer estado iniciado, incluindo Abandoned.
- Sem partidas concluídas: tempos são valores ausentes/empty; a UI mostra estado vazio, sem divisão por zero.

A tela pode atualizar agregados após commit. Estatísticas não são fonte de verdade nem precisam de uma transação de cache separada.

### ThemePreference

Enumeração pequena Light/Dark armazenada na chave local da porta de tema. Sem valor salvo, seguir tema do sistema até o usuário escolher. O estado visual aplicado não é autoridade sobre o valor armazenado.

## Esquema SQLite proposto

Uma tabela GameSessions mantém o registro compacto e o snapshot apenas enquanto ativo:

| Coluna | Tipo lógico | Regra |
|---|---|---|
| SessionId | TEXT/GUID | chave primária |
| Status | TEXT/enum | Active, Completed ou Abandoned |
| StartedAtUtc | TEXT UTC | obrigatório |
| CompletedAtUtc | TEXT UTC nullable | preenchido apenas em Completed |
| AbandonedAtUtc | TEXT UTC nullable | preenchido apenas em Abandoned |
| Difficulty | INTEGER/enum | obrigatório |
| ActiveElapsedTicks | INTEGER | não negativo |
| ErrorCount | INTEGER | não negativo |
| DisplayedHintLevelCount | INTEGER | não negativo |
| SnapshotVersion | INTEGER nullable | obrigatório se Active |
| SnapshotJson | TEXT nullable | obrigatório se Active; nulo em estados terminais |

Um índice único parcial garante no máximo uma linha Active. Chaves/checks e transições por UPDATE condicionadas a Status=Active protegem o estado. Estatísticas consultam a própria tabela.

A atualização de uma jogada, nota, undo/redo, contador ou tempo grava SnapshotJson e seus escalares na mesma transação. Criar jogo insere a linha Active e snapshot numa única transação. Concluir ou abandonar altera o estado terminal, grava timestamps/métricas finais e limpa o snapshot numa transação. Cada operação de commit é idempotente pelo SessionId e estado anterior.

O snapshot é JSON de DTOs de persistência versionados, não serialização de GameSession, Domain graph ou ViewState. A escolha mantém formato evolutivo e mapeamento explícito; SQLite fornece atomicidade da substituição.

## Captura, restauração e invariantes

- Ao iniciar uma partida, valida-se a geração e cria-se um SavedGameSession antes de navegar para o tabuleiro.
- Ao reabrir, somente uma linha Active com snapshot de versão conhecida e invariantes válidas pode ser restaurada.
- Restauração recria puzzle e solução validados, board, notas, histórico, contadores e duração acumulada; inicia pausada para que tempo em background/encerrado não conte.
- Valores iniciais não podem divergir do puzzle; solução e cages devem satisfazer as regras já existentes; valores/notas e histórico devem estar dentro dos domínios válidos.
- Um estado Completed abre a tela inicial e permanece nos agregados; não é reaberto como partida ativa nem concluído outra vez.
- Status desconhecido, versão futura ou snapshot inválido retorna recuperação necessária. O app preserva o arquivo e pede confirmação antes de substituir/iniciar outro estado.
- Falha numa gravação mantém a última transação válida no banco e a sessão corrente em memória. A ação fica pendente/não confirmada até commit; erro visível indica que mudanças recentes podem não estar salvas.
- Mudanças serializadas numa fila única preservam a ordem. Em timeout/cancelamento da tela, a operação de gravação em andamento termina ou informa resultado sem confirmação ambígua.

## Contratos de Application previstos

Os nomes finais seguem as convenções do código existente, mas a responsabilidade deve estar coberta por portas estreitas:

- IGameProgressStore: criar sessão, carregar sessão ativa, salvar snapshot, marcar conclusão e abandono, listar os dados necessários para estatísticas.
- IThemePreferenceStore: obter valor opcional e salvar Light/Dark.
- Use cases da Application orquestram captura/validação e transições; não recebem SqliteConnection nem tipos Microsoft.Maui.

Não é necessário adicionar repositório genérico, cache de estatísticas persistido ou evento externo. Acesso SQLite continua dentro do adaptador.

## Migrações

PRAGMA user_version identifica a versão do esquema. Migrações numeradas, ordenadas e transacionais são executadas na inicialização antes da consulta ao snapshot. SnapshotVersion evolui separadamente para permitir migração/validação do payload. Uma versão desconhecida nunca é convertida silenciosamente para novo jogo.
