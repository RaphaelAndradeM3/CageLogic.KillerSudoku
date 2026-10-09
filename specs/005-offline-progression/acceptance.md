# Aceitação manual — 005-offline-progression

Data da verificação automatizada: 2026-10-09.

## Gates automatizados

- `dotnet build CageLogic.slnx --configuration Release --warnaserror`: passou, 0 avisos e 0 erros.
- `dotnet test CageLogic.slnx --no-build --configuration Release`: passaram 181 testes (Domain 38, Application 123, Infrastructure 20), 0 falhas.
- Build MAUI Windows Release com `--warnaserror`: passou, 0 avisos e 0 erros.
- Build MAUI Android Release com `--warnaserror`: passou, 0 avisos e 0 erros.
- `git diff --check`: passou na revisão das alterações da Fase 5.

## Aceitação manual — pendente (T028)

| Alvo | Resultado | Evidência e bloqueio |
| --- | --- | --- |
| Windows | Não executada | A inventariação disponível (`cua.getState`) retornou `apps=[]` e `browsers=[]`; não há superfície de UI interativa para operar o app ou validar Narrator. |
| Android | Não executada | `adb` não está no PATH; `adb.exe` e `emulator.exe` não existem em `%LOCALAPPDATA%\Android\Sdk`. Sem ferramenta/superfície Android, não foi possível enumerar dispositivos conectados nem exercitar TalkBack. |

Os cenários de encerramento/retomada, conclusão, abandono, recuperação de falha, reinício com tema salvo e acessibilidade com Narrator/TalkBack continuam sem verificação manual.

## Medições de desempenho

O p95 não foi medido: não há alvos executáveis disponíveis para definir hardware e perfil do banco, aquecer o app ou coletar amostras. Portanto, não há amostras ou resultado a comparar ao limite de 250 ms.

Para concluir T028, disponibilizar uma sessão Windows interativa com Narrator e um dispositivo Android ou emulador acessível por `adb` com TalkBack; registrar o modelo, sistema, perfil do banco, aquecimento, número de amostras e p95 medido para cada alvo.
