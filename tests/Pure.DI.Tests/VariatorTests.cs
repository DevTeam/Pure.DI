namespace Pure.DI.Tests;

using Core;

public class VariatorTests
{
    [Fact]
    public void ShouldProvideVariants()
    {
        // Given
        var variator = new Variator<string>();

        // When
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a", "1_b"),
            new("2_a", "2_b", "2_c"),
            new("3_a"),
        };

        // Then - sequential variation generation
        variator.TryGetNext(sets, out var variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_a"); // initial state

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_a"); // move forward in 1

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_b, 3_a"); // move forward in 2

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_b, 3_a"); // move forward in 1

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_c, 3_a"); // move forward in 2

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_c, 3_a"); // move forward in 1

        variator.TryGetNext(sets, out variants).ShouldBeFalse();
    }

    [Fact]
    public void ShouldReturnFalseForEmptyVariations()
    {
        // Given
        var variator = new Variator<string>();
        var sets = Enumerable.Empty<SetOfOptions<string>>();

        // When & Then
        variator.TryGetNext(sets, out var variants).ShouldBeFalse();
        variants.ShouldBeNull();
    }

    [Fact]
    public void ShouldHandleSingleVariationWithSingleElement()
    {
        // Given
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a")
        };

        // When & Then
        variator.TryGetNext(sets, out var variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a");

        variator.TryGetNext(sets, out variants).ShouldBeFalse();
    }

    [Fact]
    public void ShouldHandleSingleVariationWithMultipleElements()
    {
        // Given
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a", "1_b", "1_c")
        };

        // When & Then
        variator.TryGetNext(sets, out var variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_c");

        variator.TryGetNext(sets, out variants).ShouldBeFalse();
    }

    [Fact]
    public void ShouldGenerateAllCombinationsForTwoVariations()
    {
        // Given
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a", "1_b"),
            new("2_a", "2_b")
        };

        // When & Then - 2 * 2 = 4 combinations
        variator.TryGetNext(sets, out var variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_a");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_a");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_b");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_b");

        variator.TryGetNext(sets, out variants).ShouldBeFalse();
    }

    [Fact]
    public void ShouldHandleFourVariations()
    {
        // Given
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a", "1_b"),
            new("2_a", "2_b"),
            new("3_a", "3_b"),
            new("4_a")
        };

        // When & Then - first variation changes most frequently
        variator.TryGetNext(sets, out var variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_a, 4_a");

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_a, 4_a"); // set #1 changes

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_b, 3_a, 4_a"); // set #2 changes, set #1 resets

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_b, 3_a, 4_a"); // set #1 changes

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_b, 4_a"); // set #3 changes, 1 and 2 reset

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_b, 4_a"); // set #1 changes

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_a, 2_b, 3_b, 4_a"); // set #2 changes, set #1 resets

        variator.TryGetNext(sets, out variants).ShouldBeTrue();
        string.Join(", ", variants).ShouldBe("1_b, 2_b, 3_b, 4_a"); // set #1 changes

        variator.TryGetNext(sets, out variants).ShouldBeFalse(); // All exhausted
    }

    [Fact]
    public void ShouldReturnFalseWhenVariationHasNoElements()
    {
        // Given
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a"),
            new() // Empty variation
        };

        // When & Then
        variator.TryGetNext(sets, out _).ShouldBeFalse();
    }

    [Fact]
    public void ShouldHandleLargeNumberOfVariations()
    {
        // Given - 5 variations with multiple elements
        // Total combinations: 3 * 4 * 2 * 5 * 3 = 360
        var variator = new Variator<string>();
        var sets = new List<SetOfOptions<string>>
        {
            new("1_a", "1_b", "1_c"),           // 3 elements
            new("2_a", "2_b", "2_c", "2_d"),    // 4 elements
            new("3_a", "3_b"),                  // 2 elements
            new("4_a", "4_b", "4_c", "4_d", "4_e"), // 5 elements
            new("5_a", "5_b", "5_c")            // 3 elements
        };

        // When & Then - Verify initial state and several transitions
        var iterationCount = 0;
        var maxIterations = 360;

        while (variator.TryGetNext(sets, out var variants))
        {
            iterationCount++;
            if (iterationCount > maxIterations)
            {
                // Prevent infinite loop in case of bug
                throw new InvalidOperationException("Too many iterations");
            }

            // Verify we get the correct number of variants
            variants.Count.ShouldBe(5);

            // Verify the pattern at various iterations
            switch (iterationCount)
            {
                // Initial state
                case 1:
                    string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_a, 4_a, 5_a");
                    break;
                case 2:
                    string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_a, 4_a, 5_a");
                    break;
                case 3:
                    string.Join(", ", variants).ShouldBe("1_c, 2_a, 3_a, 4_a, 5_a");
                    break;
                // After first variation resets, second variation advances
                case 4:
                    string.Join(", ", variants).ShouldBe("1_a, 2_b, 3_a, 4_a, 5_a");
                    break;
                case 5:
                    string.Join(", ", variants).ShouldBe("1_b, 2_b, 3_a, 4_a, 5_a");
                    break;
                case 6:
                    string.Join(", ", variants).ShouldBe("1_c, 2_b, 3_a, 4_a, 5_a");
                    break;
                // Second variation advances again
                case 7:
                    string.Join(", ", variants).ShouldBe("1_a, 2_c, 3_a, 4_a, 5_a");
                    break;
                case 8:
                    string.Join(", ", variants).ShouldBe("1_b, 2_c, 3_a, 4_a, 5_a");
                    break;
                case 9:
                    string.Join(", ", variants).ShouldBe("1_c, 2_c, 3_a, 4_a, 5_a");
                    break;
                case 10:
                    string.Join(", ", variants).ShouldBe("1_a, 2_d, 3_a, 4_a, 5_a");
                    break;
                // Third variation advances, first and second reset
                case 13:
                    string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_b, 4_a, 5_a");
                    break;
                case 14:
                    string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_b, 4_a, 5_a");
                    break;
                case 19:
                    string.Join(", ", variants).ShouldBe("1_a, 2_c, 3_b, 4_a, 5_a");
                    break;
                case 20:
                    string.Join(", ", variants).ShouldBe("1_b, 2_c, 3_b, 4_a, 5_a");
                    break;
                case 21:
                    string.Join(", ", variants).ShouldBe("1_c, 2_c, 3_b, 4_a, 5_a");
                    break;
                case 22:
                    string.Join(", ", variants).ShouldBe("1_a, 2_d, 3_b, 4_a, 5_a");
                    break;
                case 23:
                    string.Join(", ", variants).ShouldBe("1_b, 2_d, 3_b, 4_a, 5_a");
                    break;
                case 24:
                    string.Join(", ", variants).ShouldBe("1_c, 2_d, 3_b, 4_a, 5_a");
                    break;
                // Fourth variation advances, first three reset
                case 25:
                    string.Join(", ", variants).ShouldBe("1_a, 2_a, 3_a, 4_b, 5_a");
                    break;
                case 26:
                    string.Join(", ", variants).ShouldBe("1_b, 2_a, 3_a, 4_b, 5_a");
                    break;
                // Note: Iterations beyond 26 are harder to calculate precisely.
                // We rely on the final count check to ensure all 360 combinations are generated
            }
        }

        // Verify we got all expected combinations
        iterationCount.ShouldBe(maxIterations);
    }

    [Fact]
    public void ShouldHandleSixVariationsWithManyElements()
    {
        // Given - 6 variations
        // Total combinations: 2 * 2 * 2 * 2 * 2 * 2 = 64
        var variator = new Variator<int>();
        var sets = new List<SetOfOptions<int>>
        {
            new(10, 20),
            new(100, 200),
            new(1000, 2000),
            new(10000, 20000),
            new(100000, 200000),
            new(1000000, 2000000)
        };

        // When & Then
        var iterationCount = 0;
        var maxIterations = 64;
        var sumCounts = new Dictionary<int, int>();

        while (variator.TryGetNext(sets, out var variants))
        {
            iterationCount++;
            if (iterationCount > maxIterations)
            {
                throw new InvalidOperationException("Too many iterations");
            }

            variants.Count.ShouldBe(6);

            // Track sums to verify all combinations are generated
            var sum = variants.Sum();
            sumCounts[sum] = sumCounts.GetValueOrDefault(sum, 0) + 1;

            // Verify the first few combinations
            switch (iterationCount)
            {
                case 1:
                    variants.ShouldBe(new List<int> { 10, 100, 1000, 10000, 100000, 1000000 });
                    break;
                case 2:
                    variants.ShouldBe(new List<int> { 20, 100, 1000, 10000, 100000, 1000000 });
                    break;
                case 3:
                    variants.ShouldBe(new List<int> { 10, 200, 1000, 10000, 100000, 1000000 });
                    break;
                case 4:
                    variants.ShouldBe(new List<int> { 20, 200, 1000, 10000, 100000, 1000000 });
                    break;
            }
        }

        // Verify we got all expected combinations
        iterationCount.ShouldBe(maxIterations);
        sumCounts.Count.ShouldBe(maxIterations); // Each combination should have a unique sum
    }
}