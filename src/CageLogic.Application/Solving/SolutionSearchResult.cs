namespace CageLogic.Application.Solving;

/// <summary>A bounded solution search result; counts above two are represented as two.</summary>
public sealed class SolutionSearchResult
{
    public SolutionSearchResult(
        SolutionMultiplicity multiplicity,
        SolutionGrid? firstSolution,
        int solutionsFoundUpToLimit)
    {
        var expectedCount = multiplicity switch
        {
            SolutionMultiplicity.NoSolution => 0,
            SolutionMultiplicity.Unique => 1,
            SolutionMultiplicity.Multiple => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(multiplicity))
        };

        if (solutionsFoundUpToLimit != expectedCount)
        {
            throw new ArgumentException("The solution count must match the reported multiplicity.", nameof(solutionsFoundUpToLimit));
        }

        if ((solutionsFoundUpToLimit == 0) != (firstSolution is null))
        {
            throw new ArgumentException("A first solution is present exactly when at least one solution was found.", nameof(firstSolution));
        }

        Multiplicity = multiplicity;
        FirstSolution = firstSolution;
        SolutionsFoundUpToLimit = solutionsFoundUpToLimit;
    }

    public SolutionMultiplicity Multiplicity { get; }

    public SolutionGrid? FirstSolution { get; }

    public int SolutionsFoundUpToLimit { get; }
}
