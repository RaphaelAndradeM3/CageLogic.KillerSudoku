# Aceitação manual — 005-offline-progression

Data da verificação: 2026-10-09.

## Gates automatizados

- Build MAUI Windows Release após corrigir o layout, com `--warnaserror --no-restore`: passou, 0 avisos e 0 erros.
- Build MAUI Android Release após a correção, com `--warnaserror --no-restore -p:AndroidLinkMode=None -p:RunAOTCompilation=false`: passou, 0 avisos e 0 erros. A compilação completa padrão tentou escrever artefato de trimming no cache NuGet global e foi bloqueada pela permissão de escrita fora do workspace.
- `dotnet test CageLogic.slnx --no-build --configuration Release --no-restore`: passaram 181 testes (Domain 38, Application 123, Infrastructure 20), 0 falhas.
- `git diff --check`: passou após a correção de layout e atualização desta evidência.

## Perfis usados

| Alvo | Ambiente | Persistência |
| --- | --- | --- |
| Windows | Windows 10 Pro 22H2, build 19045, x64; PCWARE IPMH510G, Intel Core i5-10400F @ 2.90 GHz, 47,9 GiB RAM; disco do sistema Kingston SA400S37960G | Banco local `progression.db` no diretório privado MAUI; SQLite usa WAL e `synchronous=FULL` nas conexões de escrita. O tipo/formatação do volume de dados não foi medido. |
| Android | Emulador Pixel 7, Android 16/API 36, AVD `sdk_gphone64_x86_64`, 1080×2400, 420 dpi, 1 GiB de RAM configurado no Device Manager | Banco no diretório privado do aplicativo; SQLite usa WAL e `synchronous=FULL`. Armazenamento virtual do AVD, sem medição de latência do dispositivo virtual. Wi-Fi e dados móveis foram desligados durante os cenários offline e religados ao final. |

## Cenários exercitados

| Alvo | Resultado | Evidência |
| --- | --- | --- |
| Windows — quadro, tema, encerramento e retomada | Passou parcialmente | A aceitação encontrou o tabuleiro visualmente colapsado (células UIA de 1×1 px). `HorizontalOptions` do controle mudou de `Center` para `Fill`; após recompilar, o tabuleiro 9×9 ficou visível e cada célula UIA mediu 62×62 px. Uma partida Easy com notas 1 e 2 foi encerrada e reaberta; notas foram restauradas e o cronômetro ficou pausado após aguardar 8 s. O tema escuro persistiu no reinício; ao final, a preferência foi restaurada para “Sistema”. Uma nova nota 4 foi salva após a correção do layout. |
| Windows — abandono e estatísticas | Passou | O diálogo informou que a partida seria registrada como abandonada e não poderia ser retomada. Após confirmar os dois jogos de aceitação, a tela inicial informou que não havia partida pronta; estatísticas registraram 2 iniciadas, 0 concluídas, tempos vazios e 0 jogadas conflitantes/dicas exibidas. |
| Android — offline, tema, partida e retomada | Passou parcialmente | Wi-Fi e dados móveis ficaram desligados durante o primeiro teste. Tema escuro e partida Easy foram salvos localmente; notas 1 e 2 sobreviveram a undo/redo, encerramento forçado e reabertura. O cronômetro reapareceu pausado e não avançou por 12 s. Abandonar removeu a retomada. Após instalar o APK recompilado, a tela mostrou o tabuleiro 9×9 e cada célula UIA mediu 112×112 px. Os jogos de aceitação foram abandonados; a tela inicial voltou a informar que não havia partida pronta. Estatísticas ficaram com 2 iniciadas, 0 concluídas e tempos vazios. A preferência foi restaurada para “Sistema” e a conectividade para os valores originais (Wi-Fi/dados ligados). |
| Nomes e estados acessíveis | Inspecionados, sem leitor de tela | Windows UI Automation expôs as 81 células com linha, coluna, estado, notas e seleção, além de botões e estado “Salvo.”. A árvore Android expôs rótulos e descrições dos controles/estatísticas. Isso não substitui validação de fala com Narrator e TalkBack. |
| Conclusão de partida | Não executada | Não foi concluído um puzzle manualmente nos alvos; contagens de conclusão, resumo final e ausência de retomada após concluir permanecem sem aceitação manual. |
| Recuperação após falha de gravação | Não executada manualmente | Rollback, preservação do último estado íntegro e estados de falha estão cobertos por testes automatizados; não foi induzida falha de armazenamento na interface. |

## Desempenho de salvamento

| Alvo | Amostras | p95 | Método e resultado |
| --- | ---: | ---: | --- |
| Windows | 30, após 3 aquecimentos | 57,4 ms | Medido da invocação de uma entrada candidata via UI Automation até a árvore UIA observar as notas esperadas e o estado `Salvo.`. Mínimo 24,6 ms, mediana 30,6 ms, máximo 65,8 ms. A métrica inclui a observação UIA local e ficou abaixo do limite de 250 ms. |
| Android | Não medido | — | Os fluxos de persistência foram exercitados no emulador, mas não foi coletada uma série cronometrada reproduzível para comparar ao limite de 250 ms. |

## Pendências para T028

T028 continua desmarcada. Para concluí-la, ainda é necessário validar uma partida concluída, falha de gravação na interface, fala/uso efetivo com Narrator e TalkBack e p95 de salvamento no Android. O serviço TalkBack do emulador estava desativado; não foi ativado durante esta rodada.
