# Contrato interno: Game Session

**Fronteira**: MAUI ViewModels/controles → coordenação de Application. Não há API HTTP nem dependência de serviço remoto.

## Intenções de entrada

| Intenção | Entrada | Resultado observável |
|---|---|---|
| `StartGame` | `DifficultyLevel`, token de cancelamento | Estado de carregamento e cancelamento explícito pelo jogador, seguido de sessão criada ou falha/cancelamento explícito com retry. Só um pedido de geração fica ativo; repetições não são enfileiradas. |
| `CancelGeneration` | nenhum | Cancela o pedido de geração ativo por `CancellationToken`; sem geração ativa, não altera o estado. Após cancelamento, permite nova tentativa. |
| `SelectCell` | `CellPosition` | Atualiza seleção se a posição estiver dentro de 0..8; não altera board, notas ou histórico. |
| `SetInputMode` | `Answer` ou `Candidate` | Altera como os próximos dígitos serão interpretados; não altera histórico. |
| `EnterDigit` | `CellPosition` selecionada e dígito 1..9 | No modo resposta, usa `ApplyMoveUseCase`, limpa notas da célula na mesma ação e conta um erro se o valor inserido viola uma ou mais regras locais; no modo candidato, alterna uma nota em célula vazia/editável. Atualiza a projeção e histórico se o estado mudou. |
| `ClearSelected` | `CellPosition` selecionada | Apaga resposta de jogador ou limpa as notas da célula conforme modo; não altera givens. Limpar uma resposta deixa as notas vazias. Sem mudança, sem nova entrada no histórico. |
| `AutoFillCandidates` | sessão atual | Substitui notas de todas as células vazias/editáveis pelos candidatos calculados no board; uma transação Undo/Redo. |
| `Undo` / `Redo` | nenhum | Restaura board e notas do snapshot; seleção, modo, pausa, tempo e contagem histórica de erros não são restaurados. Incrementa revisão se valores do board mudarem. |
| `RequestNextHint` | nenhum | Solicita próximo nível para snapshot atual, somente se consistente; ignora/coalesce pedidos repetidos enquanto ocupado. Resultado é validado contra `BoardRevision` antes de ser publicado. |
| `Pause` / `Resume` | nenhum | Para/reinicia o acumulador monotônico. Retorno do lifecycle só pausa; não chama `Resume`. |
| `TryComplete` | nenhum | Conclui apenas com board preenchido, sem conflitos e igual à solução. Caso contrário, mantém a partida ativa e indica estado incompleto ou solução não satisfeita sem revelar comparações precoces durante a edição. |

Os nomes acima são nomes de intenção, não obrigam a criar uma interface pública para cada operação. Implementar a API proporcionalmente às convenções do Application e conectar os comandos na composition root MAUI real.

## Projeção de saída

`GameSessionViewState` (nome indicativo) deve ser imutável e expor apenas dados necessários à tela:

- dificuldade, status de geração/sessão e tempo ativo formatável;
- para cada célula: posição, valor exibido, se é fixa/editável, notas, seleção, conflito e destaque de dica por papel semântico;
- cages e seus alvos;
- modo de entrada, disponibilidade Undo/Redo, pausa e estado ocupado de geração/dica;
- dica atual apenas da revisão correspondente, incluindo técnica, explicação, células destacadas, dígitos/contexto disponíveis no nível atual e ação apenas quando o contrato de 003 a libera;
- resumo ao concluir com tempo, dificuldade, contagem de erros (um por entrada conflitante, sem decremento por correção/Undo/Redo) e dicas (um por nível progressivo exibido, sem contar `NoSafeHint`, cancelamentos ou resultados obsoletos), respeitando a regra de não sinalizar divergência de solução antes da tentativa de conclusão.

A projeção acessível deve expor semanticamente as 81 células e cada controle da partida para TalkBack e Narrator. Para cada célula, anunciar posição, valor ou vazio, fixa/editável, notas, seleção, conflito e destaque de dica. Não transmitir esses estados somente por cor.

## Contratos de estado e validação

- Posição fora da grade é ignorada na camada de hit-test e não chega como mutação da sessão.
- Dígitos fora de 1..9 são rejeitados na validação do comando/UI; exceções de argumento não devem ser usadas para fluxo normal do jogador.
- Célula fixa produz feedback de não editável; a grade continua com o valor fixo.
- Jogada conflitante é aplicada e sua validação é projetada para a correção do usuário. Se o board estiver inconsistente, `RequestNextHint` retorna/mostra estado de dica indisponível por inconsistência e não chama o analisador como se houvesse dica segura.
- Erros esperados (geração indisponível, board incompleto ou conflito) são estados/resultados, não exceções. Falhas inesperadas são registradas na fronteira e apresentadas por mensagem segura.

## Concorrência e revisão de dica

1. Criar snapshot imutável de puzzle, board, nível e revisão atual.
2. Iniciar `GetHintUseCase` de modo assíncrono com `CancellationToken`.
3. Mudança nos valores do board: incrementar revisão, reiniciar nível para `Explanation`, cancelar pedido anterior quando possível e solicitar novo snapshot.
4. Ao completar: publicar somente se a revisão retornada ainda for a atual e o pedido continuar sendo o pedido ativo; caso contrário, descartar resultado.
5. Aplicar a mesma checagem para `NoSafeHint`; nunca mostrar estado terminal obsoleto.

Editar candidatos manuais durante uma dica não muda a revisão analisada, pois esses candidatos não fazem parte do contrato do analisador lógico.

## Entrada por plataforma

Toque, mouse, teclado e controles de dígito acessíveis mapeiam para as mesmas intenções. Setas movem uma célula na direção indicada, sem circular nas bordas; 1–9 insere resposta ou alterna nota conforme o modo; Backspace/Delete limpa resposta ou notas conforme o modo; Ctrl+Z desfaz e Ctrl+Y refaz. As células fixas podem ser selecionadas e anunciadas, mas não editadas. O desenho e hit-test compartilham a mesma geometria, recalculada após resize.
