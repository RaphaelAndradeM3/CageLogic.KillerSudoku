# Checklist de Qualidade dos Requisitos: Regras e Candidatos de Killer Sudoku

**Purpose**: Avaliar completude, clareza, consistência e cobertura dos requisitos de regras do tabuleiro, estrutura das cages e cálculo de candidatos.
**Created**: 2026-10-01
**Feature**: [spec.md](../spec.md) e [plan.md](../plan.md)

**Note**: Checklist customizada para revisão da qualidade dos requisitos desta feature; não avalia a implementação.
**Review Ownership**: Este artefato pertence ao revisor. Marque um item `[x]` somente quando concluir que o critério de qualidade dos requisitos foi satisfeito.
**Marker Semantics**: `[x]` significa que o requisito foi revisado e considerado satisfatório quanto à qualidade; não significa que a implementação foi concluída.

## Completude dos requisitos

- [x] CHK001 - Os requisitos definem as regras de valores distintos em linhas, colunas, blocos e cages tanto para estados parciais quanto para tabuleiros completos? [Cobertura, Spec §FR-003–FR-006]
- [x] CHK002 - Todos os casos de estrutura inválida das cages estão enumerados e separados dos conflitos dos valores jogados? [Completude, Spec §FR-009, §SC-004]
- [x] CHK003 - O tratamento de valores fora do intervalo de 1 a 9 está definido e classificado como erro de estrutura, entrada inválida ou conflito? [Gap, Ambiguidade, Spec §Edge Cases, §FR-009]
- [x] CHK004 - A distinção entre valores fixos do puzzle e valores inseridos pelo jogador, incluindo a impossibilidade de alterar um valor fixo, está formulada como requisito normativo? [Gap, Assumption, Spec §Assumptions]
- [x] CHK005 - A atualização após mudança de estado especifica se inclui inserir, substituir e limpar valores? [Clareza, Spec §FR-008; Data Model §State transitions]

## Clareza dos requisitos

- [x] CHK006 - A possibilidade de completar o alvo de uma cage está definida com precisão para valores parciais, incluindo somas acima do alvo e alvos inalcançáveis com os dígitos restantes? [Clareza, Spec §FR-005–FR-007]
- [x] CHK007 - A definição de candidato deixa explícitos os filtros locais de linha, coluna, bloco e cage, a conclusão possível da soma com dígitos distintos e a exclusão de busca por solução global? [Clareza, Spec §FR-007; Clarifications]
- [x] CHK008 - Os requisitos tornam inequívoco como coordenadas fora da grade são identificadas e reportadas sem serem confundidas com células válidas? [Clareza, Spec §FR-001, §FR-009; Data Model §Structural rules]
- [x] CHK009 - A atingibilidade estrutural do alvo está definida como uma combinação exata de dígitos distintos de 1 a 9 para a quantidade de células da cage? [Clareza, Spec §FR-009; Data Model §Structural rules]
- [x] CHK010 - Está definido se uma validação deve reportar vários conflitos simultâneos e quais informações identificam cada conflito e suas posições? [Gap, Completude, Spec §SC-003]

## Consistência entre requisitos

- [x] CHK011 - Validade, completude, estado resolvido, impossibilidade de atingir o alvo e estrutura inválida representam resultados distintos e consistentes entre si? [Consistência, Spec §FR-005–FR-006, §FR-009, §SC-003–SC-004]
- [x] CHK012 - A elegibilidade de candidatos para células preenchidas é consistente entre FR-007 e o cenário de aceitação correspondente? [Consistência, Spec §FR-007; User Story 2, cenário 3]
- [x] CHK013 - Os limites de escopo são consistentes entre a especificação e o plano quanto a operação offline, solver global, importação, editor de cages, interface e camadas afetadas? [Consistência, Spec §Assumptions, §2–§5; Plan §Constraints, §Project Structure]
- [x] CHK014 - Os metadados da branch da feature são consistentes entre o cabeçalho de `spec.md` e o de `plan.md`? [Conflito, Spec cabeçalho; Plan cabeçalho]

## Qualidade dos critérios de aceitação

- [x] CHK015 - Os critérios de sucesso apontam estados de referência e resultados esperados suficientes para avaliar regras e conjuntos de candidatos sem interpretação subjetiva? [Mensurabilidade, Spec §SC-001–SC-002]
- [x] CHK016 - O critério de identificação de conflitos especifica os dados observáveis necessários para distinguir a regra violada e as posições envolvidas? [Mensurabilidade, Spec §SC-003]

## Cobertura de cenários e casos de borda

- [x] CHK017 - Os cenários cobrem candidatos após inserir, substituir e limpar um valor, além de solicitações para células já preenchidas? [Cobertura, Gap, Spec §FR-008; User Story 2]
- [x] CHK018 - Cages vazias, posições repetidas, contato apenas diagonal, cobertura ausente ou sobreposta e alvo inalcançável têm resultados de estrutura inválida definidos? [Casos de borda, Spec §FR-002, §FR-009, §Edge Cases]
- [x] CHK019 - O resultado para uma célula sem candidatos em um estado previamente inconsistente está definido? [Caso de borda, Spec §Edge Cases; User Story 2]
- [x] CHK020 - Os cenários distinguem um estado parcial sem conflitos, uma cage cujo alvo já é inalcançável e um tabuleiro completo que ainda viola uma regra? [Cobertura, Spec §FR-005–FR-006; User Story 1]

## Requisitos não funcionais e pressupostos

- [x] CHK021 - A ausência de uma meta numérica de latência está explicitamente aceita para o escopo fixo de 81 células, ou existe um limite mensurável a definir? [Pressuposto, Plan §Technical Context — Performance Goals]
- [x] CHK022 - A operação offline está expressa com clareza suficiente para orientar requisitos de domínio e aplicação sem pressupor serviços externos? [Clareza, Spec §Assumptions; Plan §Constraints]

## Notes

- Marque itens `[x]` somente após revisar a qualidade do requisito correspondente.
- Deixe itens sem resolução desmarcados e registre comentários ou referências junto ao item.
- `$speckit-implement` pode ler o estado da checklist como gate, mas não deve alterar os marcadores.
- `checklists/requirements.md` mantém seu ciclo de vida separado, gerenciado por `$speckit-specify` e `$speckit-clarify`.
