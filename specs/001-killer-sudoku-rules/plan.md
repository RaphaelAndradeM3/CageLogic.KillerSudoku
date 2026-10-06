# Implementation Plan: Regras e Candidatos de Killer Sudoku

**Branch**: 001-killer-sudoku-rules | **Date**: 2026-10-01 | **Spec**: [spec.md](spec.md)

**Input**: PRD, regras do README e decisões registradas na especificação 001.

## Summary

Criar a base de domínio e aplicação para representar um tabuleiro Killer Sudoku de 9×9, validar a estrutura das cages e os valores jogados, e calcular candidatos locais. O domínio será independente do MAUI e da infraestrutura. A validação devolverá resultados explícitos, distinguindo estrutura inválida do puzzle, conflitos de valores, estado parcial válido e tabuleiro resolvido. O cálculo de candidatos aplicará restrições de linha, coluna, bloco e cage, sem procurar uma solução completa do tabuleiro.

## Technical Context

**Language/Version**: C# com .NET 10; nullable habilitado conforme as convenções do repositório.

**Primary Dependencies**: BCL nos projetos Domain e Application. NUnit 5, Microsoft.NET.Test.Sdk e NUnit3TestAdapter nos projetos de teste; usar os valores correntes do template oficial, sem fixar versões neste plano. MAUI e CommunityToolkit.Mvvm pertencem ao host futuro e não são dependências desta feature.

**Storage**: N/A. Os modelos desta feature são valores em memória; persistência pertence à feature 005.

**Testing**: NUnit 5 com VSTest e dotnet test. Testes determinísticos cobrem estrutura, validação e candidatos; nenhum teste depende de internet, relógio, estado global ou aleatoriedade.

**Target Platform**: Bibliotecas Domain e Application em net10.0, sem APIs de plataforma. A aplicação completa terá host MAUI para Windows e Android em uma feature posterior.

**Project Type**: Bibliotecas C# Domain e Application, com projetos de teste correspondentes. Não criar tela, camada de persistência ou host MAUI nesta feature.

**Performance Goals**: A grade tem 81 posições e candidatos usam somente restrições locais e combinações da cage. Não foi definido limite numérico de latência. Medir a implementação após existir um host executável; não otimizar antes de haver evidência.

**Constraints**: Operação offline; nenhuma dependência do Domain em MAUI, SQLite, Android, Windows ou logging. Jogadas e validações esperadas produzem resultados explícitos, não exceções. Máximo de três fatias verticais. Importação, editor de cages, solver global e interface ficam fora do escopo.

**Scale/Scope**: Um tabuleiro 9×9 por estado analisado, 81 células identificáveis, cada célula em exatamente uma cage conectada por lados. Apenas Domain, Application e testes são afetados.

## Constitution Check

**Gate: PASS.** A constituição de engenharia em constitution.md e os limites em specs/README.md sustentam o desenho:

- As dependências apontam para Domain; nenhum tipo MAUI ou infraestrutura entra no domínio.
- CellPosition, CandidateSet e alvos de cage são conceitos explícitos; objetos de valor são imutáveis quando possível.
- Regras de negócio ficam fora de ViewModels. Esta feature não cria ViewModels nem tela.
- Regras inválidas são retornadas por resultados explícitos. Exceções ficam restritas a falhas excepcionais.
- Testes de regras são determinísticos e fazem parte das fatias.
- Não adicionar interfaces, padrões, logging, banco ou serviços externos sem uma fronteira concreta. Se logging operacional for necessário em um host futuro, usar Microsoft.Extensions.Logging na composition root/adaptador, sem tipos de logging no Domain.
- O contexto de .specify/memory/constitution.md contém apenas placeholders; esta verificação usa a constituição de engenharia publicada em constitution.md e os guardrails de specs/README.md.
- A meta de latência permanece sem número porque a grade tem tamanho fixo e a feature não executa solver global. Esse ponto não bloqueia o desenho; medir depois da primeira implementação.

**Gate após o desenho: PASS.** Os artefatos mantêm as três fatias da especificação, sem violação justificada ou novo componente externo. A solution e os projetos de biblioteca/teste agora existem, então os gates Release se aplicam às bibliotecas. Builds Windows/Android só serão aplicáveis quando a feature 004 introduzir o host MAUI e os workloads correspondentes; esta feature produz bibliotecas net10.0 sem alvo de plataforma.

## Project Structure

### Documentation (this feature)

- specs/001-killer-sudoku-rules/plan.md
- specs/001-killer-sudoku-rules/research.md
- specs/001-killer-sudoku-rules/data-model.md
- specs/001-killer-sudoku-rules/quickstart.md
- specs/001-killer-sudoku-rules/tasks.md será produzido por speckit-tasks, não por este comando.

Não criar contracts/: esta feature não expõe API externa, protocolo de integração ou formato público; as chamadas entre Domain e Application são APIs internas de código.

### Source Code (repository root)

- src/CageLogic.Domain/: posições, tabuleiro, cages, validação e candidatos.
- src/CageLogic.Application/: casos de uso para aplicar jogadas, consultar validação e solicitar candidatos.
- tests/CageLogic.Domain.Tests/: testes determinísticos das regras e modelos.
- tests/CageLogic.Application.Tests/: testes de coordenação dos casos de uso.

MAUI, Infrastructure, persistência, solver global e tela do jogo ficam para as features que os introduzem. Esta feature criou a solution, as bibliotecas Domain/Application e os projetos de teste usados pelas fatias de regras e candidatos.

**Structure Decision**: Manter Domain e Application como bibliotecas net10.0 independentes do host. Criar testes junto às regras e casos de uso. A camada MAUI consumirá os resultados em uma feature posterior. As decisões seguem a dependência voltada para dentro e evitam adicionar projetos sem função nesta feature.
