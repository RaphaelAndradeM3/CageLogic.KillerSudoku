using CageLogic.Application.Hints;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Tests.Hints;

public sealed class LogicalHintExplanationCatalogTests
{
    [TestCase(LogicalTechniqueId.NakedSingle, "Único candidato")]
    [TestCase(LogicalTechniqueId.HiddenSingle, "Posição única")]
    [TestCase(LogicalTechniqueId.CageSingle, "Valor único na cage")]
    [TestCase(LogicalTechniqueId.CageCombination, "Combinações da cage")]
    [TestCase(LogicalTechniqueId.CageRegionIntersection, "Interseção de cage e região")]
    [TestCase(LogicalTechniqueId.RuleOf45, "Regra do 45")]
    [TestCase(LogicalTechniqueId.NakedPair, "Par nu")]
    [TestCase(LogicalTechniqueId.HiddenPair, "Par oculto")]
    [TestCase(LogicalTechniqueId.NakedTriple, "Tripla nua")]
    public void SupportedTechniques_HavePortugueseExplanationsWithoutConcreteAnswers(
        LogicalTechniqueId techniqueId,
        string expectedName)
    {
        var text = new LogicalHintExplanationCatalog().Get(techniqueId);

        Assert.Multiple(() =>
        {
            Assert.That(text.Name, Is.EqualTo(expectedName));
            Assert.That(text.Explanation, Is.Not.Empty);
            Assert.That(text.Explanation, Does.Not.Contain("5"));
            Assert.That(text.Explanation, Does.Not.Contain("(0,").IgnoreCase);
        });
    }
}
