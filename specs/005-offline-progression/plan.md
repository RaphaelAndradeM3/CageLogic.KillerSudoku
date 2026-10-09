# Plano de Implementação: Persistência Offline e Progresso

**Branch**: 005-offline-progression
**Data**: 2026-10-09
**Spec**: [spec.md](spec.md)

**Entrada**: requisitos de [spec.md](spec.md), RF-014 a RF-019 do PRD e a experiência de sessão implementada em 004.

## Resumo

Persistir a sessão ativa, o histórico necessário para undo/redo e os registros usados pelas estatísticas em SQLite local. Cada alteração confirmável será salva numa transação antes de ser anunciada como confirmada. A gravação ocorrerá numa fila serial de trabalho em segundo plano; a interface mostrará o estado pendente e continuará utilizável. A recuperação reidratará um modelo de sessão validado e sempre pausado. Estatísticas serão derivadas dos registros de partidas, com conclusão idempotente. A preferência de tema usará a API Preferences do MAUI por meio de uma porta da Application.

## Contexto Técnico

**Linguagem/versão**: C# com .NET 10; nullable e implicit usings habilitados nos projetos existentes.
**Dependências principais**: .NET MAUI existente; Microsoft.Data.Sqlite a adicionar em Infrastructure; Microsoft.Extensions.Logging e Serilog já usados pelo host/adaptador.
**Armazenamento**: SQLite sob FileSystem.AppDataDirectory, com caminho resolvido no host MAUI e entregue ao adaptador de Infrastructure; Preferences do MAUI para tema.
**Testes**: NUnit 5 nos projetos CageLogic.Application.Tests, CageLogic.Domain.Tests e CageLogic.Infrastructure.Tests.
**Plataformas-alvo**: Windows 10.0.19041+ e Android API 21+ via CageLogic.Maui.
**Tipo de projeto**: aplicativo desktop e móvel .NET MAUI offline-first, com Domain, Application, Infrastructure e host MAUI existentes.
**Meta de desempenho**: pelo menos 95% das confirmações locais em até 250 ms em condições normais; interface responsiva e indicação pendente até o commit.
**Restrições**: funcionamento principal offline; nenhuma jogada confirmada pode ser perdida; uma transação por mudança confirmada; não logar conteúdo serializado do puzzle, solução ou sessão; migrações compatíveis; não expor SQLite ou APIs MAUI ao domínio.
**Escala/escopo**: um dispositivo por instalação, um jogo ativo por vez, tabuleiro 9×9, histórico compacto de jogos para agregados de progresso; sem sincronização, conta, API remota ou tela de histórico individual.

## Verificação da Constituição

A constituição em .specify/memory/constitution.md ainda contém placeholders e não define princípios ratificados que possam ser aplicados como gates. O gate usa as regras concretas de specs/README.md e as fronteiras já existentes na solution.

| Gate | Resultado | Evidência |
|---|---|---|
| Lógica central independente de MAUI e SQLite | PASS | Contratos de persistência e tema ficam na Application; adaptadores ficam em Infrastructure/host. |
| I/O assíncrono sem bloquear a UI | PASS | Uma fila serial em segundo plano executará o acesso síncrono subjacente ao SQLite; nenhum .Result ou .Wait(). |
| Persistência local e offline | PASS | Caminho de dados privado do app; sem serviços online. |
| Confirmação somente após persistência | PASS | O comando só recebe confirmação depois do commit da transação. |
| Simplicidade proporcional | PASS | Uma base SQLite e uma pequena porta de preferência; sem camada genérica de repositório ou serviço remoto. |
| Logging seguro e tratamento na fronteira | PASS | Log estruturado de operação/ID/correlação e exceção, nunca payloads da sessão ou solução; mensagem segura na UI. |

**Rechecagem após desenho**: PASS. O desenho preserva as regras concretas do README e não depende de princípios ausentes na constituição. A política FULL e a fila de gravações são necessárias para a garantia de durabilidade e responsividade; a medição de p95 permanece gate de aceitação.

## Estrutura do Projeto

### Documentação desta feature

~~~text
specs/005-offline-progression/
├── plan.md
├── research.md
├── data-model.md
└── quickstart.md
~~~

Não será criado contracts/: esta feature é interna ao aplicativo e não publica API HTTP, serviço ou protocolo externo. As portas Application e o esquema persistido estão descritos em data-model.md.

### Código existente a estender

~~~text
src/
├── CageLogic.Domain/                 # Regras de puzzle existentes; sem dependência de persistência
├── CageLogic.Application/
│   ├── GameSessions/                 # captura/restauração, timer e histórico serializáveis por DTO
│   └── Progression/                  # portas, casos de uso, estatísticas e modelos persistíveis
├── CageLogic.Infrastructure/
│   └── Progression/                  # SQLite, esquema e migrações
└── CageLogic.Maui/
    ├── Lifecycle/                    # carregar/retomar antes de apresentar a navegação
    ├── ViewModels/                   # estado pendente, falha e confirmação
    ├── Views/                        # estatísticas e preferências
    └── MauiProgram.cs                # caminho do banco e DI; Preferences adapter

tests/
├── CageLogic.Application.Tests/
│   ├── GameSessions/                 # round-trip e regras de agregação
│   └── Progression/
└── CageLogic.Infrastructure.Tests/
    └── Progression/                  # transações, recuperação e migrações
~~~

**Decisão de estrutura**: estender a solution e seus projetos atuais; não criar projeto, host, composition root ou fronteira externa novos.

## Entregáveis do Planejamento

- Fase 0: [research.md](research.md), com decisões técnicas e fontes primárias.
- Fase 1: [data-model.md](data-model.md) e [quickstart.md](quickstart.md).
- A criação de tasks.md pertence à fase seguinte, via speckit-tasks; este comando encerra o planejamento após a Fase 1.
