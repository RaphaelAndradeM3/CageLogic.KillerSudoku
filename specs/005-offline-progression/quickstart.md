# Quickstart de Implementação: 005 Offline Progression

## Pré-condições

- Branch atual: 005-offline-progression.
- Solution: CageLogic.slnx; SDK .NET 10 e workloads MAUI Windows/Android configurados para os builds de plataforma.
- Feature anterior: 004-game-session, que fornece geração, sessão, histórico, timer, contagem de erros/dicas e conclusão.
- Persistência continua sem internet, conta ou serviço remoto.

## Sequência proposta

1. Criar DTOs versionados e operações de captura/restauração de sessão na Application. Preservar puzzle, solução offline, board, notas, undo/redo, contadores e tempo; restaurar pausada.
2. Criar portas de progresso e preferência, use cases de iniciar/carregar/salvar/concluir/abandonar e projeção de estatísticas.
3. Implementar o adaptador SQLite em Infrastructure, migração user_version, WAL + synchronous=FULL e transações por alteração confirmável.
4. Integrar uma fila de gravação serial em segundo plano no host MAUI; carregar estado antes da navegação, apresentar retomada, indicador de salvamento, falha/retry e confirmação explícita antes de descartar recuperação inválida.
5. Acrescentar estatísticas e preferência de tema claro/escuro; padrão inicial acompanha sistema.
6. Registrar dependências no MauiProgram e verificar fluxos em Windows e Android sem rede.

## Verificação automatizada

Na raiz do repositório:

~~~powershell
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj -f net10.0-windows10.0.19041.0 --configuration Release --warnaserror
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj -f net10.0-android --configuration Release --warnaserror
~~~

Rodar testes NUnit focados antes da suíte:

- Application: round-trip de snapshot, validação/rejeição, pausa/tempo, undo/redo após restauração, stats vazias, conclusão/abandono e idempotência.
- Infrastructure: commit/rollback, falha sem sobrescrever commit anterior, migrações, índice de jogo ativo, dados incompatíveis e concorrência serializada.

## Aceitação manual em Windows e Android

1. Desconectar rede; iniciar jogo, fazer entradas/notas, undo e redo; encerrar o processo e reabrir. Todas as ações confirmadas devem voltar, inclusive notas e histórico.
2. Conferir tempo ativo: fechar ou deixar em segundo plano não acumula; partida reabre pausada.
3. Concluir, encerrar e reabrir: tela inicial abre; estatísticas continuam com uma única conclusão.
4. Abandonar explicitamente: iniciadas +1, concluídas sem alteração; média/melhor sem essa partida; erros e dicas permanecem nos totais.
5. Forçar falha de gravação e abrir snapshot inválido/incompatível: último commit preservado, mensagem segura, mudanças não persistidas visíveis como pendentes/não confirmadas; não iniciar outra partida sem confirmação.
6. Alterar claro/escuro, fechar e reabrir; validar em Windows e Android.
7. Medir tempo do comando até confirmação e responsividade com armazenamento local normal. p95 deve ser ≤250 ms em cada alvo; registrar dispositivo, sistema, número de amostras, configuração FULL e resultado.

## Observabilidade segura

Logs podem registrar operação, SessionId, duração, resultado, correlation ID e exceção na fronteira. Não registrar SnapshotJson, solução, board, notas ou valores digitados. A mensagem ao jogador não expõe caminho de arquivo, SQL ou stack trace.
