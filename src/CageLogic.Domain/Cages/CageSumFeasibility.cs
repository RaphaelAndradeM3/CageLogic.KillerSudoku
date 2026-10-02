namespace CageLogic.Domain.Cages;

/// <summary>Checks whether distinct Sudoku digits can complete a cage target exactly.</summary>
public static class CageSumFeasibility
{
    public static bool CanReachTarget(int targetSum, int remainingCellCount, IEnumerable<int> usedDigits)
    {
        ArgumentNullException.ThrowIfNull(usedDigits);

        if (remainingCellCount is < 0 or > 9)
        {
            return false;
        }

        var used = new bool[10];
        var usedSum = 0;
        foreach (var digit in usedDigits)
        {
            if (digit is < 1 or > 9 || used[digit])
            {
                return false;
            }

            used[digit] = true;
            usedSum += digit;
        }

        return CanChoose(1, remainingCellCount, targetSum - usedSum, used);
    }

    private static bool CanChoose(int firstDigit, int count, int remainingSum, IReadOnlyList<bool> used)
    {
        if (count == 0)
        {
            return remainingSum == 0;
        }

        for (var digit = firstDigit; digit <= 9; digit++)
        {
            if (used[digit])
            {
                continue;
            }

            if (CanChoose(digit + 1, count - 1, remainingSum - digit, used))
            {
                return true;
            }
        }

        return false;
    }
}
