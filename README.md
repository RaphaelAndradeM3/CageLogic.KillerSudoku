# CageLogic Killer Sudoku

Aplicativo offline-first de **Killer Sudoku** para Windows e Android, desenvolvido com .NET 10 e .NET MAUI. O jogador resolve um tabuleiro 9×9 respeitando linhas, colunas, blocos e cages conectadas com metas de soma.

> **Estado:** as features 001–005 estão implementadas: regras e candidatos, geração de puzzles com solução única, dicas lógicas, sessão de jogo e progresso offline. Os gates automatizados mais recentes passaram com 196 testes. A aceitação manual completa de T028 ainda está pendente para conclusão de partida, falha de gravação na interface, fala com Narrator/TalkBack e medição p95 no Android; veja [aceitação da feature 005](specs/005-offline-progression/acceptance.md).

## Funcionalidades

- Gera puzzles por dificuldade, com cages Killer Sudoku e valores-alvo exibidos no tabuleiro. Puzzles Easy começam com pistas fixas.
- Permite inserir respostas e candidatos, desfazer/refazer, pausar o tempo e pedir dicas lógicas progressivas.
- Salva partidas localmente em SQLite e permite retomá-las offline, incluindo notas e histórico.
- Registra estatísticas locais e preferência de tema do sistema, claro ou escuro.
- Executa como aplicativo MAUI para Windows e Android.

## Regras do jogo

1. O tabuleiro tem 9 linhas, 9 colunas e 9 blocos de 3×3.
2. Cada linha, coluna e bloco deve conter os números de 1 a 9 sem repetição.
3. Cada célula pertence a exatamente uma cage conectada.
4. Os valores de uma cage devem somar sua meta, sem repetir números dentro dela.
5. Cada puzzle gerado deve ter exatamente uma solução.

## Arquitetura

As regras permanecem independentes da interface e da persistência. As dependências apontam para o domínio:

```text
MAUI / Infrastructure
          ↓
     Application
          ↓
       Domain
```

- **Domain:** tabuleiro, células, cages, candidatos, validação e técnicas lógicas.
- **Application:** casos de uso de dicas, sessão, solver, geração, dificuldade e progresso.
- **Infrastructure:** persistência SQLite local e preferências.
- **MAUI:** telas, ViewModels, navegação e desenho/interação com o tabuleiro.

## Requisitos

- .NET SDK 10.
- Workloads MAUI e SDKs de plataforma para compilar Windows e Android.
- Para aceitação manual, ambiente Windows e emulador ou dispositivo Android.

## Compilar e testar

Execute na raiz do repositório. A solution contém bibliotecas, o host MAUI e três projetos de teste NUnit.

```powershell
dotnet restore CageLogic.slnx
dotnet build CageLogic.slnx --configuration Release --warnaserror
dotnet test CageLogic.slnx --no-build --configuration Release
```

Para validar os targets MAUI separadamente:

```powershell
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-windows10.0.19041.0 --configuration Release --warnaserror
dotnet build src/CageLogic.Maui/CageLogic.Maui.csproj --framework net10.0-android --configuration Release --warnaserror
```

Os resultados detalhados, comandos efetivamente usados e limitações de aceitação estão em [acceptance.md](specs/005-offline-progression/acceptance.md) e [quickstart.md](specs/005-offline-progression/quickstart.md).

## Publicar

O script [`scripts/publish-windows-android.bat`](scripts/publish-windows-android.bat) publica o app Windows e um APK Android Release assinado em `artifacts/publish/`, sem pedir credenciais. Na primeira execução, cria uma chave e uma senha aleatórias em `%LOCALAPPDATA%\CageLogic\KillerSudoku\signing` e reutiliza essa identidade nas próximas publicações; os arquivos ficam fora do repositório. Faça backup dessa pasta para preservar a assinatura; se perder a chave, o Android não aceitará o APK seguinte como atualização da instalação existente. Para instalar uma atualização, incremente também `ApplicationVersion` em `src/CageLogic.Maui/CageLogic.Maui.csproj`. Esta chave local destina-se a instalação própria e testes; configure uma chave de lançamento gerenciada e guardada com segurança antes de distribuir pela Play Store.

## Revisões automatizadas

Na análise do PR #5, o Qodo foi o revisor mais forte para bugs de confiabilidade; o CodeRabbit complementou com acessibilidade, testes, documentação e publicação; o Copilot ajudou na priorização e confirmou riscos de alto impacto. Os revisores repetiram achados sobre recuperação de dados inválidos, migração SQLite, desenho com tema do sistema, contenção no SQLite, estado de salvamento e contagem de testes. A comparação e a lista detalhada estão em [revisão dos bots do PR #5](specs/005-offline-progression/review-findings.md).

## Documentação

- [Ideia do projeto](Ideia.md)
- [Requisitos do produto](PRD.md)
- [Constituição de engenharia](constitution.md)
- [Mapa das cinco especificações](specs/README.md)
- [Especificação de progresso offline](specs/005-offline-progression/spec.md)
- [Aceitação Windows e Android](specs/005-offline-progression/acceptance.md)
- [Comparação das revisões do PR #5](specs/005-offline-progression/review-findings.md)

## Licença

Este projeto está sob a licença [GNU General Public License v3.0](LICENSE).
