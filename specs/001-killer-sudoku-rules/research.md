# Research: Regras e Candidatos de Killer Sudoku

**Date**: 2026-10-01
**Feature**: 001-killer-sudoku-rules

## Decisions

### Runtime and platform boundary

**Decision**: Use .NET 10 for Domain and Application class libraries, targeting net10.0. Keep the rule engine platform-neutral. The MAUI host will target Windows and Android later and reference these libraries.

**Rationale**: The PRD and specs/README.md select .NET 10 and .NET MAUI for Windows and Android. The MAUI single-project model can target Android and Windows from one shared host project, while Sudoku rules have no platform API needs. Microsoft documents Android API 21+ and Windows 10 version 1809+ as supported MAUI platforms; exact target frameworks and minimum SDK settings belong to the later host project. Project-specific inference: because only the modern .NET host is in scope, target net10.0 directly instead of netstandard2.0; this avoids maintaining an older compatibility surface that has no consumer here.

**Support maintenance**: Checked 2026-10-01, .NET 10 is in active LTS support through 2028-11-14. .NET MAUI 10 has a separate support policy through 2027-05-11. Keep MAUI workload and SDK servicing aligned; do not assume MAUI has the same support window as .NET.

**Alternatives considered**: netstandard2.0 would be relevant if the rule library needed older .NET Framework consumers. No such consumer exists in this app, so net10.0 avoids multi-targeting and additional compatibility constraints. Adding a MAUI host in feature 001 would expand the feature beyond its Domain/Application scope; the host belongs to the later session/UI feature.

Sources:

- [Supported platforms for .NET MAUI apps](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-10.0)
- [Target multiple platforms from a .NET MAUI single project](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/single-project?view=net-maui-10.0)
- [Cross-platform targeting in .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [.NET MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui)

### Test framework and runner

**Decision**: Use NUnit 5 in the new test projects and VSTest through the .NET CLI.

**Rationale**: The repository has no existing test convention; its README leaves NUnit or xUnit open. NUnit's official .NET project guide provides a net10.0 example and the dotnet new nunit template, which reduces setup ambiguity for a new solution. Microsoft documents both NUnit and xUnit as supported .NET test frameworks and both work with dotnet test. Keep the default VSTest path; Microsoft.Testing.Platform is optional and not required by this feature.

**Alternatives considered**: xUnit remains viable and is supported by Microsoft for .NET testing. No Killer Sudoku test scenario requires one framework's unique feature. If the repository adopts an xUnit convention before scaffolding, follow that established convention instead of adding a second framework.

Sources:

- [Testing in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [NUnit on .NET Core and .NET Standard](https://docs.nunit.org/articles/nunit/getting-started/dotnet-core-and-dotnet-standard.html)
- [xUnit.net v3 getting started](https://xunit.net/docs/getting-started/v3/getting-started)
- [dotnet test command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

### Candidate scope

**Decision**: Compute local candidates using current row, column, block, and cage restrictions plus whether the cage can still be completed to its target using distinct values. Do not call the global solver or require a full-board solution.

**Rationale**: This is the accepted clarification in spec.md and aligns with the separate solver responsibility in feature 002. The board and digit set are fixed and small. Candidate calculation is deterministic and can be checked against reference fixtures.

**Alternatives considered**: Removing candidates that cannot appear in any full-board solution would couple this feature to solver search and duplicate feature 002. That behavior is explicitly outside the accepted candidate definition.

### Storage and external contracts

**Decision**: Add no storage, network integration, or external contract artifact. Keep this feature's board state in memory. Persistence belongs to feature 005; puzzle generation/import is outside scope.

**Rationale**: The product is offline-first, the spec excludes puzzle import and cage editing, and the current layers affected are Domain and Application. The feature's code contracts are internal project APIs, not public HTTP, file, or integration protocols.

## Deferred, non-blocking item

No numeric latency target was specified. The operation is bounded by an 81-cell board and local constraints, and the candidate path does not invoke the global solver. Implement the straightforward deterministic design first, then measure if the user interface needs an explicit latency budget. Do not add a benchmark project or cache as part of this plan without evidence.