# Mapa de especificações — CageLogic Killer Sudoku

## Meta imutável do projeto

O CageLogic Killer Sudoku é um jogo offline-first de Killer Sudoku para Windows e Android. O jogador recebe puzzles com solução única, pode jogar e validar as regras e aprende técnicas por meio de dicas progressivas explicáveis. Preservar essa meta em cada alteração; não adicionar camadas, abstrações ou padrões sem justificativa na arquitetura existente.

## Conjunto enxuto de specs

O roadmap do MVP está descrito em cinco specs, abaixo do limite de sete. Cada uma entrega uma capacidade testável de ponta a ponta e tem no máximo três fatias:

1. [001 — Regras e candidatos](001-killer-sudoku-rules/spec.md): base de tabuleiro, cages, validação e candidatos.
2. [002 — Motor de puzzles](002-puzzle-engine/spec.md): solução, unicidade, geração e dificuldade; depende de 001.
3. [003 — Dicas lógicas](003-logical-hints/spec.md): explicações e ajuda progressiva; depende de 001 e consulta a unicidade de 002.
4. [004 — Sessão de jogo](004-game-session/spec.md): interface e fluxo de partida em Windows/Android; depende de 001, 002 e 003.
5. [005 — Progresso offline](005-offline-progression/spec.md): salvar/retomar, estatísticas e preferências; depende de 004.

Multiplayer, login, nuvem, anúncios, monetização, iOS e publicação em lojas são explicitamente pós-MVP ou fora do escopo descrito no PRD. Desafios diários e editor/importação de puzzles também não estão incluídos nas cinco specs.

## Limites de arquitetura e desenvolvimento

- Ler esta documentação e a constituição do projeto antes de implementar. Preservar a versão .NET 10 planejada, nullable e convenções presentes quando os projetos forem criados.
- Manter as regras do jogo independentes de MAUI, SQLite, Android, Windows e detalhes de infraestrutura. Dependências apontam para o domínio; ViewModels coordenam interface e não implementam regras, candidatos, solver ou geração.
- Criar interfaces apenas para fronteiras, substituição em testes ou variabilidade real. Usar injeção por construtor. Quando houver composition root real, registrar nela as dependências criadas ou alteradas na fatia que primeiro as integra ao host. Se uma biblioteca for implementada antes de existir host/composition root, não criar um host ou composition root artificial; documentar a dependência pendente e registrá-la na primeira fatia consumidora que possa usar a composition root real.
- Usar async de ponta a ponta para I/O e operações longas; aceitar CancellationToken onde operações demoradas possam ser canceladas. Não usar .Result ou .Wait().
- Tratar jogadas inválidas como resultado explícito, não como exceção. Não capturar silenciosamente; tratar falhas inesperadas na fronteira apropriada e preservar contexto/stack trace.
- Usar logging estruturado sem interpolar valores. Configurar Serilog somente na composition root ou adaptador de Infrastructure, por meio de Microsoft.Extensions.Logging ou porta equivalente; não expor tipos Serilog no Domain.
- O logging operacional deve incluir timestamp, nível, mensagem, exceção quando existir, correlation ID e contexto de negócio disponível. Usar arquivos diários separados para mensagens gerais e erros, caminho configurável sob logs por padrão, retenção e timezone explícitos. Nunca registrar segredos, payloads sensíveis ou PII sem necessidade e proteção definidas.
- Tratar exceções não observadas na fronteira do host com mensagem genérica segura ao jogador e fallback mínimo se o próprio logger falhar. Não duplicar logs da mesma exceção em várias camadas.
- Aplicar SOLID, Strategy, Factory Method, Builder ou Singleton somente quando a constituição e a necessidade real justificarem. Não criar repositório genérico, interface mecânica ou estado global mutável por padrão.

## Política de fatias e validação

- Cada fatia precisa atravessar as partes necessárias para demonstrar o fluxo completo; agrupar contrato, regra, integração e teste do mesmo resultado. Não particionar tarefas por DTO/interface/repositório isolados.
- Limitar cada feature a até três fatias descritas no seu spec. Manter referência à meta imutável e aos critérios de aceitação em cada etapa de implementação.
- Quando existir solution, executar dotnet build --configuration Release --warnaserror e, após build bem-sucedido, dotnet test --no-build --configuration Release.
- Executar builds dos targets Windows e Android após workloads serem configurados. Rodar testes focados e a suíte existente conforme projetos e convenções reais.
- Usar filtro Category=Integration somente se a categoria for configurada no projeto. WebApplicationFactory não é pressuposto para um aplicativo MAUI offline.
- `CageLogic.slnx`, as bibliotecas `CageLogic.Domain` e `CageLogic.Application` e os projetos NUnit de teste já existem em .NET 10. Eles cobrem a base implementada em 001; o host MAUI e os targets Windows/Android ainda não existem.
- Os gates `dotnet build CageLogic.slnx --configuration Release --warnaserror` e, após sucesso, `dotnet test CageLogic.slnx --no-build --configuration Release` aplicam-se à solution atual. A atualização destes documentos não executou esses comandos.
