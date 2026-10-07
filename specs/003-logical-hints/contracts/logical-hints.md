# Contrato de integração: dicas lógicas

Este contrato é consumido pela sessão de jogo de 004. É uma fronteira de Application, sem dependência de MAUI, ViewModel, armazenamento ou tipo de apresentação.

## Operação

```csharp
Task<HintResult> GetHintUseCase.ExecuteAsync(
    HintRequest request,
    CancellationToken cancellationToken);
```

O caso de uso recebe um snapshot imutável e não mantém estado de progresso entre chamadas. O chamador solicita explicitamente um dos três níveis.

## Pedido

`HintRequest` contém:

- `HintPuzzleContext`: puzzle original validado, multiplicidade calculada para esse puzzle e `SolutionGrid` quando a multiplicidade for única;
- `CurrentBoard`: snapshot completo com givens e jogadas atuais, construído a partir da mesma estrutura do puzzle;
- `Level`: `Explanation` (1), `Highlights` (2) ou `Action` (3);
- `BoardRevision`: revisão monotônica da sessão que identifica o snapshot.

O Domain recalcula candidatos a partir do snapshot. Anotações manuais de candidatos da interface não alteram a dedução.

## Campos de `HintResult`

- `BoardRevision` e `Status` identificam o snapshot e o resultado explícito.
- `TechniqueId`, `TechniqueName` e `Explanation` identificam a primeira técnica aplicável e seu texto localizado.
- `Highlights` é um mapa imutável de `HintHighlightRole` para posições: `Pattern` identifica o padrão, `Scope` a região/cage de apoio, `Target` o alvo de uma colocação e `Affected` as células das eliminações.
- `InvolvedCandidates` contém pares posição/dígito relevantes para um passo de eliminação sem classificá-los como removíveis.
- `Action` fica nula antes do nível 3. No nível 3 é exatamente `HintAction.PlaceValue(Position, Value)` ou `HintAction.RemoveCandidates(Candidates)`; a lista de remoções é não vazia, distinta e ordenada row-major por posição e depois por dígito.

## Resultado

Todo `HintResult` ecoa `BoardRevision` e tem um `HintStatus` explícito:

- `Available`: contém uma dica projetada estritamente até o nível solicitado;
- `PuzzleSolved`: o tabuleiro está completo e correto;
- `InconsistentState`: há conflito local ou nenhuma conclusão compatível com as jogadas atuais;
- `NoSafeHint`: estado compatível, mas nenhuma técnica do catálogo v1 se aplica;
- `ValueNotConfirmed`: nível 3 pediu uma colocação, porém a unicidade do puzzle original não foi confirmada; não inclui valor.

Para `Available`, o conteúdo segue estas regras:

| Nível | Conteúdo |
|---|---|
| 1 | ID/nome da técnica e explicação em português. `Highlights`, `InvolvedCandidates` e `Action` ficam vazios/nulos. |
| 2 | Nível 1 mais `Highlights` e candidatos envolvidos. Não declara que um dígito deve ser colocado ou que candidatos devem ser removidos. |
| 3 | Níveis anteriores mais exatamente uma ação tipada: `PlaceValue` ou `RemoveCandidates`. |

Uma técnica de eliminação pode ser avançada até o nível 3 sem produzir colocação: sua ação é `RemoveCandidates`. Uma colocação só pode ser divulgada no nível 3 quando a multiplicidade do puzzle original for `Unique` e o valor coincidir com a solução confirmada. A unicidade de uma busca restrita às jogadas atuais não altera a multiplicidade original.

Se a origem tem multiplicidade `Multiple` e as jogadas atuais eliminam todas as soluções, o resultado é `InconsistentState`. Se as jogadas atuais deixam uma única solução, isso não confirma a unicidade do puzzle original: uma solicitação de colocação no nível 3 retorna `ValueNotConfirmed` sem valor. Uma ação de eliminação continua disponível porque não coloca um valor.

`PuzzleSolved`, `InconsistentState`, `NoSafeHint` e `ValueNotConfirmed` não carregam ação. O caso de uso não transforma cancelamento ou falhas inesperadas em estados de dica.

## Consumo pela sessão de 004

1. Manter a revisão monotônica do tabuleiro e cancelar a chamada anterior quando o estado mudar.
2. Ao receber o resultado, comparar `BoardRevision` com a revisão atual antes de exibir qualquer conteúdo.
3. Descartar uma resposta cuja revisão não corresponda ao tabuleiro atual; pedir uma nova dica para o snapshot atual no nível 1.
4. Reiniciar a progressão no nível 1 após qualquer jogada que altere o tabuleiro.
5. Exibir somente resultado da revisão atual, inclusive `NoSafeHint`, conforme `004-game-session` FR-013/SC-005.
6. Aplicar cores, animações e layout a partir dos papéis de destaque; a sessão não implementa nem reordena técnicas lógicas.

## Erros e cancelamento

Conflitos, ausência de conclusão compatível, puzzle resolvido e falta de passo conhecido são resultados tipados. `OperationCanceledException` propaga-se quando o token é cancelado. Falhas inesperadas propagam-se à fronteira do host para tratamento seguro e logging; o texto exibido ao jogador não contém detalhes internos.

## Registro de dependências

003 não cria uma composition root artificial para suas bibliotecas. A feature 004 registra `GetHintUseCase` e as dependências concretas necessárias no composition root MAUI real introduzido na fatia 3, conforme `specs/README.md` e a própria especificação de 004.
