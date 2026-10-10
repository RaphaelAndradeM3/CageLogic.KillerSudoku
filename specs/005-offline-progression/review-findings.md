# Revisão automatizada do PR #5

Este documento compara os comentários de Qodo, Copilot e CodeRabbit no PR **feat: add offline progression and session persistence**. A comparação foi feita em 2026-10-10 a partir do relatório da revisão do PR.

## Resultado por revisor

| Revisor | Achados publicados | Contribuição mais útil nesta revisão |
| --- | ---: | --- |
| Qodo | 9 bugs | Análise mais profunda dos riscos de persistência, recuperação e ciclo de vida da partida. Encontrou o caso em que substituir uma partida antes de gerar e salvar a próxima podia deixar o jogador sem nenhuma partida. |
| Copilot | 6 achados | Priorização explícita por severidade e confirmação independente dos riscos de migração, recuperação e gravação já confirmada. Também destacou o contraste dos erros no tema escuro. |
| CodeRabbit | 9 comentários iniciais e 1 comentário posterior | Cobertura mais ampla de acessibilidade, teste de idempotência, documentação e script de publicação, além de confirmar problemas funcionais vistos por outros revisores. |

As contagens incluem comentários repetidos e recomendações de documentação/teste; não representam bugs únicos nem uma pontuação diretamente comparável.

## Achados repetidos

| Área | Revisores | Como os comentários se relacionam |
| --- | --- | --- |
| Snapshot/registro inválido impede recuperação ou novas partidas | Qodo, Copilot e CodeRabbit | Qodo detalhou JSON vazio, posição de cage nula, datas/identificadores inválidos e ações de recuperação. Copilot resumiu o bypass do contrato de recuperação e a falta de ação de retry. CodeRabbit apontou posições nulas e campos inválidos no registro. |
| Migração ou abertura do banco pode impedir o app de iniciar | Qodo e Copilot | Ambos observaram que executar migração durante a criação do host deixa a falha sem uma tela de recuperação. |
| Tabuleiro não redesenha ao mudar o tema do sistema | Qodo e CodeRabbit | Ambos pediram observar a troca de tema e invalidar o desenho com a paleta atual. |
| Contenção entre leitura e gravação SQLite | Qodo e CodeRabbit | Ambos associaram falhas concorrentes ao cache compartilhado com WAL e recomendaram remover o cache compartilhado. |
| Indicador de persistência fica preso em “Salvando…” | Qodo e CodeRabbit | Ambos encontraram saídas sem alteração ou com falha que não restauravam o último estado de salvamento. |
| Contagem de testes divergente na documentação | Copilot e CodeRabbit | Ambos encontraram totais diferentes nos documentos e no resumo do PR. |

## Achados complementares

- **Qodo:** a substituição da partida atual deve ocorrer somente depois que o novo puzzle estiver gerado e puder ser salvo. Esse caso é diferente do cancelamento depois de um novo registro já ter sido confirmado.
- **Copilot:** chamou atenção para o caso em que uma nova sessão já confirmada no SQLite podia não ser publicada no estado em memória após cancelamento. Esse é distinto da perda da partida antiga descrita pelo Qodo. Também apontou o baixo contraste de mensagens de erro no tema escuro.
- **CodeRabbit:** identificou que descrições semânticas fixas ocultavam o texto dinâmico de status para Narrator/TalkBack; pediu um teste de conclusão repetida; corrigiu instruções obsoletas sobre o host MAUI; e apontou risco de selecionar um APK antigo deixado na pasta de publicação.

## Avaliação

Para bugs críticos de lógica e confiabilidade deste PR, **Qodo foi o revisor mais forte**. **CodeRabbit foi o complemento mais amplo** em acessibilidade, cobertura de teste, documentação e publicação. **Copilot agregou priorização por severidade e confirmação independente** de riscos de maior impacto. A combinação dos três foi mais útil que usar apenas a contagem de comentários para escolher um vencedor.

## Tratamento e limites

As correções de código e documentação decorrentes da revisão foram incluídas no commit `f35ce30` (`fix: address offline progression review findings`). Os gates automatizados registrados para 2026-10-10 passaram com 196 testes e builds MAUI Windows/Android sem avisos ou erros; veja [acceptance.md](acceptance.md) para os comandos e detalhes.

A revisão automatizada não substitui aceitação manual. A tarefa T028 segue desmarcada enquanto não forem validados conclusão de partida, falha de gravação pela interface, fala efetiva com Narrator e TalkBack e p95 de salvamento no Android.
