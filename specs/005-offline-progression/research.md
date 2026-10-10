# Pesquisa Técnica: Persistência Offline e Progresso

**Feature**: 005-offline-progression
**Data**: 2026-10-09

## Decisões

### Persistência da partida e progresso: SQLite local

**Decisão**: adicionar Microsoft.Data.Sqlite no projeto Infrastructure e manter a base no diretório privado retornado por FileSystem.AppDataDirectory. O host MAUI resolve o caminho em runtime e injeta a configuração no adaptador; nenhum caminho de plataforma entra na Application ou Domain.

SQLite permite agrupar a alteração do estado atual, contadores, histórico e transição de status numa transação. Isso atende à regra de só confirmar uma jogada depois do commit e evita gravações parciais entre estado e estatísticas. Usaremos WAL e PRAGMA synchronous=FULL, configurado em cada conexão gravadora, com um único gravador serial. A documentação de SQLite descreve que FULL sincroniza o WAL em cada commit; WAL permite leitores simultâneos, mas continua havendo um escritor por vez.

**Alternativas consideradas**:

- Snapshot JSON em arquivo: fácil de inspecionar, mas serializeAsync não oferece por si só transação, substituição crash-safe, backup ou recuperação. Seria necessário projetar e manter um protocolo de arquivo temporário, cópias e validação.
- sqlite-net-pcl: opção simples e usada no exemplo MAUI, mas as consultas SQL e transações explícitas do fluxo desta feature tornam Microsoft.Data.Sqlite uma escolha direta adequada para Infrastructure.
- Preferências MAUI para a partida: inadequadas para estado estruturado e histórico; a documentação recomenda Preferences para poucos valores pequenos, não grandes payloads.

Fontes: [guia SQLite do .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/database-sqlite?view=net-maui-10.0), [File System Helpers do MAUI](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/file-system-helpers?view=net-maui-10.0), [limites assíncronos de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async), [transações do Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions), [WAL do SQLite](https://sqlite.org/wal.html), [PRAGMA synchronous](https://sqlite.org/pragma.html#pragma_synchronous), [atomic commit](https://www.sqlite.org/atomiccommit.html).

### Operação assíncrona e confirmação

Microsoft.Data.Sqlite não implementa I/O de arquivo realmente assíncrono: suas APIs com sufixo Async executam de modo síncrono. Portanto, um worker/fila única no background executará a transação síncrona fora da thread de UI; a fronteira Application continua oferecendo operação assíncrona e CancellationToken onde seguro. Depois que o commit começou, cancelar a espera da UI não poderá transformar uma gravação concluída em falha lógica. A fila mantém ordem e associa a confirmação ao snapshot committed.

A UI apresenta “salvando” enquanto aguarda. Só depois do commit a ação correspondente é marcada como confirmada. Se a escrita falhar, a última transação válida permanece, a sessão em memória continua disponível e a UI informa que alterações recentes não foram salvas; a fila poderá tentar novamente sem afirmar sucesso prematuro.

A meta p95 de 250 ms é um critério empírico, não uma garantia da documentação. Medir com FULL em Windows e Android reais; se não atingir, otimizar o formato/transação e a integração antes de alterar a durabilidade.

Fonte: [limitações assíncronas de Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async).

### Migração e evolução do formato

**Decisão**: usar PRAGMA user_version para identificar a versão do esquema; aplicar migrações ordenadas dentro de transação na abertura. Cada snapshot tem também versão do payload. A aplicação valida puzzle, solução, notas e histórico antes de hidratar GameSession. Versão futura, dado inválido ou corrupção produz resultado explícito de recuperação; não se sobrescreve a base inválida nem se apresenta um jogo incorreto como íntegro.

Fontes: [PRAGMA user_version](https://sqlite.org/pragma.html#pragma_user_version), [SQLite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).

### Tema visual

**Decisão**: a Application expõe uma pequena porta de preferência; o host MAUI implementa com Preferences para a escolha claro/escuro. Na primeira execução sem escolha salva, o tema segue o sistema. Preferences corresponde a uma configuração pequena chave/valor e não mistura o tema com a transação de uma jogada.

Acesso à preferência continua local; testes de aplicação usam adaptador falso. Android já configura android:allowBackup="false" no manifesto, reduzindo exportação automática desses dados de app.

Fonte: [Preferences no .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/storage/preferences).

## Restrições e riscos

- FULL pode aumentar latência em dispositivos lentos. O único escritor serializado e o indicador pendente preservam responsividade; p95 precisa ser medido em hardware Windows e Android com dados de referência.
- WAL cria arquivos auxiliares -wal e -shm enquanto aberto. O banco não será copiado manualmente como arquivo isolado durante operação; SQLite controla checkpoints e consistência.
- AppDataDirectory tem localização e comportamento de persistência específicos por plataforma. Resolver o caminho em runtime e não construir caminho absoluto fixo.
- Registros individuais são necessários para calcular contagens e somas sem manter totais duplicados que possam divergir. Cada linha é pequena; política de retenção/limpeza não está definida pelo produto e fica fora deste MVP.
- Nenhum payload de snapshot, solução ou histórico deve ser incluído em logs. O log pode ter ID da sessão, tipo da operação, duração e exceção sanitizada.
- Reutilizar a configuração Serilog já existente: arquivos diários separados e retenção configurável, com padrão atual de 14 dias em LoggingOptions; não criar uma política paralela para esta feature.
- SQLite protege commits contra interrupção conforme configuração e filesystem; não promete sobrevivência a falha física do armazenamento. A especificação cobre falhas do app e estado local, e os testes manuais deverão incluir encerramento forçado do processo.

## Questões resolvidas

As escolhas de banco, confirmação, retenção de formato, preferências e p95 estão definidas para permitir a geração das tarefas. A implementação deverá escolher e fixar uma versão compatível do pacote Microsoft.Data.Sqlite ao adicionar a dependência; a versão exata é detalhe de implementação e não muda o desenho.
