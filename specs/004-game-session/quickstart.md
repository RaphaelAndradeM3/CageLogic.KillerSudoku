# Quickstart: Sessão de Jogo

## Pré-requisitos

- SDK .NET 10.
- Workloads MAUI e SDKs necessários para Android; para Windows, workload/SDK Windows suportado pela máquina.
- Emulador ou dispositivo Android e ambiente Windows para validação manual dos dois alvos.
- Restaurar dependências NuGet antes do primeiro build.

Os workloads do host MAUI ainda não fazem parte da solution na etapa de planejamento. Os comandos abaixo são gates para depois da implementação; não foram executados ao criar este plano.

## Build e testes automatizados

Na raiz do repositório:

```powershell
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-android --configuration Release --warnaserror
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-windows10.0.19041.0 --configuration Release --warnaserror
```

Rodar `dotnet test` somente após o build da solution concluir com sucesso. Se um workload não estiver instalado, registrar essa limitação; não interpretar falha de pré-requisito como sucesso de compilação do alvo.

## Verificação manual de aceitação

Executar cada fluxo em Windows e Android, online e offline:

1. Iniciar partidas em Easy, Medium, Hard e Expert; confirmar carregamento visível, cancelamento explícito, resultado explícito e retry em caso de falha/cancelamento, ausência de fila para pedidos repetidos, cage targets e interface não bloqueada.
2. Selecionar células usando toque/mouse e as setas (sem circular nas bordas); inserir 1–9 conforme modo, apagar com Backspace/Delete e desfazer/refazer com Ctrl+Z/Ctrl+Y. Redimensionar/rotacionar e confirmar que hit-test acompanha o desenho.
3. Inserir e apagar respostas; confirmar que givens não mudam, conflitos continuam visíveis/corrigíveis e valores localmente válidos não são revelados como diferentes da solução.
4. Alternar modo resposta/candidato, editar notas e preencher candidatos automáticos; validar que o auto-fill substitui todas as notas nas células vazias e desfaz/refaz como uma ação. Inserir resposta com notas deve limpá-las, Undo deve restaurá-las e uma remoção posterior não deve trazê-las de volta.
5. Desfazer/refazer sequências mistas e confirmar que seleção, modo, pausa e tempo ficam fora do histórico.
6. Pedir dica e alterar os valores do board antes do retorno; confirmar descarte da revisão antiga, reinício no nível 1 e publicação somente da revisão atual, incluindo `NoSafeHint`. Repetir alterando somente notas manuais e confirmar que isso não invalida a revisão analisada.
7. Pausar/retomar, alternar o app para segundo plano e voltar; confirmar que retorna pausado e o intervalo não aumenta o tempo ativo.
8. Tentar concluir com células vazias, conflito e resposta válida porém divergente da solução; depois completar corretamente e verificar tempo, dificuldade e contagens de erro/dica conforme FR-010.
9. Inspecionar logs sob a pasta de dados da aplicação: rotação diária, arquivos separados, contexto/correlation ID e ausência de valores sensíveis; simular falha do logger e verificar fallback seguro.
10. Usar TalkBack no Android e Narrator no Windows para percorrer todas as 81 células e controles, ouvir coordenada/valor/estado/notas/conflito/dica e operar sem depender de cor.
11. Durante validação de jogadas e cálculo completo de candidatos, confirmar que a interface continua processando navegação e entrada sem executar esses cálculos na thread de UI. Durante geração e dica, confirmar que o estado de carregamento aparece sem bloquear a interface e que seleção/edição continuam processáveis enquanto a dica está pendente. Registrar modelo/configuração do dispositivo, versão do sistema e p95 de entrada/realce de célula, validação/auto-candidates e resposta da interface durante geração/dica como linha de base; propor um limite numérico somente após medir os alvos reais.

## Registro de execução (parcial, 2026-10-08)

Este registro contém evidências parciais; não substitui a aceitação manual completa de Windows e Android exigida por T032.

| Verificação | Resultado |
|---|---|
| Build Release | Passou com 0 avisos e 0 erros nos projetos da solution, incluindo os targets Android e Windows. A restauração foi feita pelo cache NuGet local após falha de autenticação nos feeds. |
| Testes NUnit | 138 aprovados, 0 falhas, 0 ignorados. |
| Android AVD | APK instalado e app aberto no perfil `pixel_7_-_api_36_0` (`sdk_gphone64_x86_64`, Android 16/API 36). A geração Easy abriu a página da partida. |
| Células semânticas | A árvore UIAutomator apresentou 81 IDs `BoardCell_*` com posição e estado de valor/notas/seleção/conflito/dica. TalkBack falado não foi exercitado. |
| Candidatos e histórico | Entrada de nota, Undo/Redo, auto-fill e Undo do auto-fill foram exercitados por ADB; a nota foi removida/restaurada como esperado. |
| Pausa explícita | O botão alternou para “Retomar tempo”; o tempo exibido permaneceu igual durante a pausa e voltou a avançar após Retomar. |
| Retorno do background | Inconclusivo: após Home e retorno, o botão ainda indicava “Pausar tempo”. O AVD apresentou “System UI isn't responding” e a ponte de acessibilidade falhou; repetir em ambiente estável antes de aprovar o ciclo de vida. |
| Offline, TalkBack, Narrator e p95 | Não verificados. A tentativa de modo avião foi limitada pela permissão de broadcast do Android shell e pela instabilidade do AVD; não houve interação manual Windows nem medição confiável de p95. |

T032 permanece desmarcada até executar toda a matriz em Windows e Android e resolver/retestar o comportamento de background observado.
