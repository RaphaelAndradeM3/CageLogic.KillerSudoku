# CageLogic Killer Sudoku

Aplicativo multiplataforma de **Killer Sudoku**, planejado em .NET 10 e .NET MAUI para Windows e Android. A proposta combina puzzles com solução única, validação durante a partida e dicas que explicam o raciocínio usado para avançar.

> **Status:** as bibliotecas `CageLogic.Domain` e `CageLogic.Application` já incluem regras, candidatos, solver, geração de puzzles e análise de dificuldade, com testes NUnit em `net10.0`. As dicas, a sessão de jogo, a progressão e o host MAUI continuam em desenvolvimento; ainda não há aplicativo executável.

## Visão do projeto

O CageLogic pretende oferecer uma experiência offline de Killer Sudoku para quem quer jogar e aprender técnicas de resolução. As dicas deverão priorizar explicações e destaques visuais, deixando a revelação direta de uma resposta como último recurso.

## Recursos disponíveis nas bibliotecas

- Gerar puzzles por dificuldade com seed, orçamento configurável e verificação de solução única.
- Resolver puzzles e classificar dificuldade por uma trilha lógica reproduzível.

## Funcionalidades planejadas

- Inserir e apagar valores, usar candidatos e validar as regras do tabuleiro e das cages.
- Desfazer e refazer jogadas e salvar a partida localmente para continuar depois.
- Solicitar dicas progressivas com explicações de técnicas lógicas.
- Registrar tempo, erros, dicas e estatísticas da partida.
- Jogar offline em Windows e Android.

Dicas, sessão de jogo, progressão e aplicativo MAUI continuam planejados. Regras, validação, candidatos, resolução, geração e classificação de dificuldade já estão implementados nas bibliotecas .NET.

## Regras do jogo

O puzzle segue as regras tradicionais de Sudoku e acrescenta as cages do Killer Sudoku:

1. O tabuleiro tem 9 linhas, 9 colunas e 9 blocos de 3×3.
2. Cada linha, coluna e bloco deve conter os números de 1 a 9 sem repetição.
3. Cada célula pertence a exatamente uma cage.
4. Os valores de uma cage devem somar seu alvo, sem repetir números dentro dela.
5. Todo puzzle disponibilizado ao jogador deve ter exatamente uma solução.

## Arquitetura pretendida

As regras do jogo devem permanecer independentes da interface e da infraestrutura. As dependências apontam para o domínio:

```text
MAUI / Infrastructure
          ↓
     Application
          ↓
       Domain
```

- **Domain:** tabuleiro, células, cages, candidatos e regras do jogo.
- **Application:** casos de uso de partida, jogadas, dicas, solver, geração e classificação.
- **Infrastructure:** persistência local, preferências e logging.
- **MAUI:** telas, ViewModels, navegação e desenho/interação com o tabuleiro.

O solver computacional deverá ser separado do motor de dicas: o primeiro encontra e conta soluções; o segundo procura passos lógicos que possam ser explicados ao jogador. As estratégias de dicas deverão poder ser adicionadas de forma independente.

## Tecnologias propostas

- .NET 10, C# e .NET MAUI com XAML e MVVM.
- CommunityToolkit.Mvvm e `GraphicsView` para a interface e o tabuleiro.
- SQLite para persistência local.
- `Microsoft.Extensions.DependencyInjection` e `Microsoft.Extensions.Logging`.
- xUnit ou NUnit para testes automatizados.
- GitHub Actions para integração contínua.

As tecnologias acima são propostas na documentação e podem ser detalhadas durante a implementação.

## Roadmap inicial

1. Criar a solução, os projetos e os testes de domínio. **Concluído.**
2. Implementar validações, cages e cálculo de candidatos. **Concluído na base da feature 001.**
3. Desenvolver solver e verificação de solução única. **Concluído nas bibliotecas Application.**
4. Criar estratégias de dicas e explicações.
5. Desenvolver o gerador e a classificação de dificuldade. **Concluído nas bibliotecas Application.**
6. Construir a interface MAUI, persistência e experiência de jogo.

## Documentação

- [Ideia do projeto](Ideia.md): visão, público, proposta, arquitetura e roadmap.
- [Requisitos do produto](PRD.md): objetivos, requisitos funcionais e não funcionais.
- [Constituição de engenharia](constitution.md): princípios e regras técnicas do projeto.
- [Especificações do MVP](specs/README.md): mapa das cinco specs, dependências e guardrails de implementação.

## Executar

As bibliotecas e seus testes podem ser compilados na raiz com `dotnet build CageLogic.slnx --configuration Release --warnaserror`; após um build bem-sucedido, execute `dotnet test CageLogic.slnx --no-build --configuration Release`. O aplicativo ainda não é executável porque o host MAUI não foi criado.

## Licença

Este projeto está sob a licença [GNU General Public License v3.0](LICENSE).
