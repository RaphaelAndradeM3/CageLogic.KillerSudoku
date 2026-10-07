# Hint Requirements Checklist: Dicas Lógicas Progressivas

**Purpose**: Revisar completude, clareza, consistência e cobertura dos requisitos de dicas lógicas progressivas.
**Created**: 2026-10-07
**Feature**: [spec.md](../spec.md)

**Note**: Este checklist customizado foi gerado por `$speckit-checklist` com base no contexto e nos requisitos da feature.
**Review Ownership**: Este checklist é um artefato de revisão da qualidade dos requisitos, pertencente ao reviewer. Marque um item `[x]` somente quando o reviewer considerar satisfeito o critério de qualidade.
**Marker Semantics**: `[x]` significa que o critério foi revisado e satisfeito quanto à qualidade dos requisitos. Não significa que a implementação foi concluída.

## Requirement Completeness

- [ ] CHK001 Cada uma das nove técnicas tem requisitos para nome, explicação e informação necessária para relacionar o raciocínio às células e candidatos? [Completeness, Spec §FR-004, §FR-009, §SC-001]
- [ ] CHK002 Os três níveis definem todo o conteúdo permitido para passos de colocação e de eliminação, inclusive o que não pode ser antecipado? [Completeness, Spec §FR-008, §FR-010, Clarifications]
- [ ] CHK003 Os requisitos distinguem os resultados de puzzle resolvido, conflito local, ausência de conclusão compatível, ausência de técnica e valor não confirmado? [Completeness, Spec §FR-006, §FR-007, User Story 2]
- [ ] CHK004 Está definido o que pode ser explicado ou destacado para um puzzle original com múltiplas soluções, além da proibição de revelar um valor como certo? [Gap, Spec §FR-006, Edge Cases, Assumptions]
- [ ] CHK005 A exigência de descartar e recalcular uma dica obsoleta cobre as mudanças de estado relevantes e o resultado quando o novo estado não tem dica segura? [Completeness, Spec §FR-005, User Story 1 §5, §SC-007]

## Requirement Clarity

- [ ] CHK006 “Técnica mais simples” está definida sem ambiguidade pela ordem estável do catálogo e pelos critérios de desempate, inclusive quando várias técnicas se aplicam? [Clarity, Ambiguity, Spec §FR-003, User Story 1 §2]
- [ ] CHK007 “Células afetadas”, “alvo” e “células relacionadas” têm significados suficientemente distintos para os nove tipos de técnica? [Clarity, Spec §FR-002, §FR-009, §SC-001]
- [ ] CHK008 Os requisitos diferenciam claramente candidatos destacados no nível 2 de candidatos explicitamente indicados para remoção no nível 3? [Clarity, Ambiguity, Spec §FR-008, §FR-010, Clarifications]
- [ ] CHK009 “Conclusão válida compatível com o puzzle original” deixa claro como distinguir uma jogada sem conflito local de um estado sem solução compatível? [Clarity, Spec §FR-006, User Story 2 §4, §SC-006]
- [ ] CHK010 “Dica segura” distingue com clareza um estado inconsistente de um estado válido para o qual nenhuma técnica conhecida se aplica? [Clarity, Spec §FR-006, §FR-007, User Story 2 §3]

## Requirement Consistency

- [ ] CHK011 A ausência de destaques no nível 1 e sua presença no nível 2 são consistentes entre requisitos funcionais, histórias e cenários de aceitação? [Consistency, Spec §FR-001, §FR-002, §FR-008, User Story 1 §1, User Story 2 §1]
- [ ] CHK012 A proibição de dicas em estados inconsistentes é compatível com a recusa apenas do valor quando a unicidade do puzzle original não foi confirmada? [Consistency, Spec §FR-006, §FR-007, Edge Cases]
- [ ] CHK013 A regra de que o valor aparece somente no nível 3 está alinhada com “valor sugerido quando aplicável” e com todos os cenários de colocação e eliminação? [Consistency, Spec §FR-008, §FR-009, §FR-010, §SC-004, §SC-005]
- [ ] CHK014 O comportamento definido para destacar candidatos afetados no nível 2 é compatível com a exigência de não revelar a ação lógica explícita antes do nível 3? [Conflict, Spec §FR-002, §FR-008, §FR-010, Clarifications]

## Acceptance Criteria Quality

- [ ] CHK015 O conjunto fechado LH-01 a LH-09 usado para medir “100%” define técnica, efeito e papéis de evidência por fixture, e SC-002 compara ID/destaques ao vetor e nome/explicação ao catálogo? [Measurability, Spec §Cenários de referência, §SC-001, §SC-002]
- [ ] CHK016 “Não contradiz o estado válido ou a solução única” define um critério observável para considerar uma sugestão correta? [Measurability, Spec §SC-003, §FR-006]
- [ ] CHK017 A fronteira entre 003 ecoar a revisão e 004 descartar/recalcular resultados obsoletos está explícita e tem critério verificável em 004 FR-013/SC-005? [Acceptance Criteria, Clarity, Spec §FR-005, §SC-007]

## Scenario Coverage

- [ ] CHK018 Os cenários cobrem cada técnica do catálogo v1, inclusive passos que apenas eliminam candidatos? [Coverage, Spec §FR-004, §FR-010, §SC-001, §SC-005]
- [ ] CHK019 A especificação cobre mais de uma técnica aplicável e define a escolha estável sem depender de interpretação subjetiva? [Coverage, Spec §FR-003, User Story 1 §2, Edge Cases]
- [ ] CHK020 A sessão de 004 descarta/recalcula se qualquer jogada mudar o tabuleiro entre cálculo e apresentação, e reinicia no nível 1 inclusive quando o novo resultado é `NoSafeHint`? [Coverage, Spec §FR-005, §SC-007, 004 FR-013/SC-005]

## Edge Case Coverage

- [ ] CHK021 Conflito local e ausência de qualquer conclusão compatível são tratados como situações distintas, embora ambas impeçam uma dica? [Edge Case, Spec §FR-006, User Story 2 §3–4, Edge Cases]
- [ ] CHK022 O caso de tabuleiro completo e correto está claramente separado de “nenhuma técnica conhecida” e de tabuleiro completo inválido? [Edge Case, Spec §FR-007, User Story 2 §5, §SC-008]
- [ ] CHK023 Para um passo com várias eliminações, está claro que o nível 3 identifica cada candidato a remover e não sugere uma colocação? [Edge Case, Clarity, Spec §FR-010, §SC-005, User Story 1 §4]
- [ ] CHK024 A especificação esclarece o comportamento diante de puzzle de origem sem solução ou com múltiplas soluções, em vez de depender apenas da premissa de puzzles únicos? [Edge Case, Assumption, Spec §FR-006, Edge Cases, Assumptions]

## Non-Functional Requirements

- [ ] CHK025 Os requisitos definem ou explicitamente adiam uma meta mensurável de tempo de resposta para análise lógica e busca de compatibilidade no tabuleiro 9×9? [Gap, Non-Functional, Spec §FR-005, §FR-006, §2]
- [ ] CHK026 Os requisitos deixam claro se cancelamento de uma análise longa é necessário e qual resultado observável o jogador recebe quando ela é cancelada? [Gap, Non-Functional, Spec §FR-005, §2]

## Dependencies & Assumptions

- [ ] CHK027 A dependência de puzzle validado e da multiplicidade/solução fornecidas por 002 define o comportamento quando esse contexto está ausente ou não é confiável? [Dependency, Gap, Spec §FR-006, Assumptions, §2]
- [ ] CHK028 A fronteira entre deduções lógicas e conferência computacional de compatibilidade está descrita sem sugerir que o motor de dicas deve resolver o puzzle? [Dependency, Clarity, Spec §FR-003, §FR-006, §2]
- [ ] CHK029 Está explícito se candidatos anotados manualmente pelo jogador podem influenciar a explicação ou apenas servem como informação visual? [Assumption, Clarity, Spec §2]

## Ambiguities & Conflicts

- [ ] CHK030 “Explicação em linguagem clara” tem critérios suficientes para revisar consistência e compreensão nas nove técnicas? [Ambiguity, Gap, Spec §FR-004, §FR-009, Assumptions]
- [ ] CHK031 As mensagens para estado inconsistente, puzzle resolvido e ausência de técnica são suficientemente distintas para não atribuir um erro do jogador a uma limitação do catálogo? [Ambiguity, Consistency, Spec §FR-006, §FR-007, User Story 2 §3–5]
- [ ] CHK032 Está explícito que uma origem `Multiple` permanece sem unicidade confirmada mesmo quando as jogadas atuais reduzem o conjunto a uma única solução, enquanto ausência de solução compatível retorna `InconsistentState`? [Consistency, Edge Case, Spec §FR-006, §SC-009, Data Model §Validação]
- [ ] CHK033 A regra de DI explica que bibliotecas sem composition root não criam registro/host artificial e que 004 registra o caso de uso e dependências no composition root MAUI real? [Dependency, Consistency, `specs/README.md`, 004 Slice 3]

## Notes

- Marque itens `[x]` somente após revisar e considerar satisfeito o critério de qualidade do requisito.
- Deixe itens sem marca quando ainda precisarem de esclarecimento, correção ou avaliação do reviewer.
- `$speckit-implement` lê o estado dos checkboxes como gate e não deve alterar seus marcadores.
- `requirements.md` é o checklist integrado mantido por `$speckit-specify` e `$speckit-clarify`; este arquivo customizado tem ciclo de revisão separado.
- `tasks.md` foi gerado após este checklist. O checklist continua pertencendo ao reviewer; a existência de tarefas não marca seus critérios como revisados.
