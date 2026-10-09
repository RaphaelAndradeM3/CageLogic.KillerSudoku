# Checklist de Qualidade dos Requisitos: Persistência Offline e Progresso

**Purpose**: Revisar clareza, completude, consistência e mensurabilidade dos requisitos de persistência, recuperação e progresso.
**Created**: 2026-10-09
**Feature**: [spec.md](../spec.md)

**Note**: Esta checklist personalizada avalia a qualidade dos requisitos, não a implementação.
**Review Ownership**: Artefato de revisão pertencente ao reviewer. Marque `[x]` somente quando o critério de qualidade do requisito tiver sido revisado e considerado satisfeito.
**Marker Semantics**: `[x]` significa que o requisito foi revisado quanto à qualidade; não significa que a implementação foi concluída.

## Completude dos Requisitos

- [ ] CHK001 Todos os tipos de mudança da sessão que precisam ser restaurados estão enumerados, incluindo respostas, notas, undo/redo, contadores e tempo? [Completeness, Spec §FR-001–FR-002]
- [ ] CHK002 Está definido se erros e dicas da partida ainda ativa entram nos totais antes de conclusão ou abandono? [Gap, Spec §FR-005]
- [ ] CHK003 O fluxo e o momento que caracterizam abandono explícito estão descritos? [Completeness, Spec §FR-005]
- [ ] CHK004 Os requisitos de acessibilidade para estatísticas, tema, salvamento pendente e mensagens de recuperação estão especificados? [Gap, Spec §FR-007–FR-008]

## Clareza dos Requisitos

- [ ] CHK005 “Jogada confirmada” e “salvamento pendente” têm significado preciso para respostas, notas, undo e redo? [Clarity, Spec §FR-001]
- [ ] CHK006 “Condições normais de armazenamento” define uma condição objetiva para avaliar a meta de latência? [Ambiguity, Spec §FR-001, SC-006]
- [ ] CHK007 O tempo da partida define quando o cronômetro conta, pausa e retoma durante segundo plano, encerramento e restauração? [Clarity, Spec §FR-002, FR-005]
- [ ] CHK008 O cálculo e a apresentação de média, melhor tempo e totais especificam unidade, arredondamento e período considerado? [Clarity, Spec §FR-005–FR-006]

## Consistência dos Requisitos

- [ ] CHK009 FR-001 e FR-008 descrevem de forma consistente o estado de mudanças em memória após falha e enquanto novas mudanças aguardam salvamento? [Consistency, Spec §FR-001, FR-008]
- [ ] CHK010 O estado vazio de estatísticas define valores ausentes para média e melhor tempo de modo consistente entre requisito e experiência de usuário? [Consistency, Spec §US2, FR-006]
- [ ] CHK011 A retenção de registros é compatível com estatísticas acumuladas e com a premissa de dados locais sem sincronização? [Consistency, Spec §FR-005, Assumptions]

## Qualidade dos Critérios de Aceitação

- [ ] CHK012 SC-001 distingue interrupção antes, durante e depois da confirmação de uma jogada, de modo que o resultado esperado seja objetivo? [Measurability, Spec §SC-001]
- [ ] CHK013 SC-005 identifica os tipos de falha e o critério de aprovação para preservar o último salvamento válido? [Measurability, Spec §SC-005]
- [ ] CHK014 Os cenários de referência para SC-002 especificam dados e resultados esperados para contagens, totais, média e melhor tempo? [Gap, Spec §SC-002]
- [ ] CHK015 SC-006 define amostra, plataforma e condições de medição suficientes para decidir se o p95 de 250 ms foi atingido? [Measurability, Spec §SC-006]

## Cobertura de Cenários

- [ ] CHK016 Os requisitos distinguem primeira inicialização, retomada ativa, reabertura após conclusão e início após abandono? [Coverage, Spec §US1, FR-002, FR-004–FR-005]
- [ ] CHK017 O escopo offline identifica explicitamente se geração de jogo, retomada, estatísticas e preferências funcionam sem conexão? [Clarity, Spec §FR-003, FR-007, SC-004]
- [ ] CHK018 Os requisitos de atualização do formato definem os resultados esperados para versões anteriores e migrações que falham? [Coverage, Spec §Edge Cases, FR-010]

## Cobertura de Casos Limite e Recuperação

- [ ] CHK019 A resposta a armazenamento cheio ou indisponível especifica tentativas posteriores, continuidade da sessão em memória e estado de confirmação? [Coverage, Edge Case, Spec §FR-008]
- [ ] CHK020 Dados corrompidos, incompatíveis e parcialmente recuperáveis têm resultados distintos ou uma regra de recuperação inequívoca? [Clarity, Spec §FR-010]
- [ ] CHK021 Está definido o que acontece se o app for fechado ou uma operação for cancelada enquanto o salvamento estiver pendente? [Coverage, Gap, Spec §FR-001, Edge Cases]

## Requisitos Não Funcionais

- [ ] CHK022 A meta p95 e a responsividade durante salvamento são verificáveis separadamente em Windows e Android? [Coverage, Spec §FR-001, SC-003, SC-006]
- [ ] CHK023 A regra de logging esclarece se tabuleiro, solução, notas, histórico e valores digitados são dados que não devem ser registrados? [Clarity, Spec §FR-009, §2]
- [ ] CHK024 “Fluxos principais” em FR-003 delimita quais jornadas precisam funcionar offline? [Ambiguity, Spec §FR-003]

## Dependências e Premissas

- [ ] CHK025 Os significados de erro e dica usados nas estatísticas correspondem aos eventos e contadores definidos em 004-game-session? [Dependency, Spec §FR-005, Assumptions]
- [ ] CHK026 A política de retenção de partidas e a possibilidade de limpar dados locais estão definidas ou explicitamente adiadas? [Assumption, Gap, Spec §Key Entities, Assumptions]
- [ ] CHK027 O tema aplicado na primeira execução, antes de existir preferência salva, está definido? [Gap, Spec §FR-007]

## Ambiguidades e Conflitos

- [ ] CHK028 “Preservar o último estado salvo validamente” esclarece o resultado se nenhum snapshot íntegro puder ser lido? [Ambiguity, Spec §FR-008, FR-010]
- [ ] CHK029 Está claro se ações adicionais podem ser feitas enquanto existe uma jogada pendente, e como cada uma será confirmada sem perda de ordem? [Ambiguity, Spec §FR-001, FR-008]

## Notes

- Todos os itens começam desmarcados; o estado das caixas pertence ao reviewer.
- Marcar `[x]` avalia somente a qualidade dos requisitos, não a conclusão de código ou testes.
- `$speckit-implement` pode ler o estado da checklist, mas não deve alterar seus marcadores.
- `requirements.md` é a checklist de qualidade da spec com ciclo de vida separado, mantida por `$speckit-specify` e `$speckit-clarify`.
