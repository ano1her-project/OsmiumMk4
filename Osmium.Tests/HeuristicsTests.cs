using Osmium.Core;
using Osmium.Heuristics;

namespace Osmium.Tests;

public class HeuristicsTests
{
    [Fact]
    public void Evaluate_StartingPosition()
        => Assert.Equal(0, Heuristics.Heuristics.Evaluate(Position.StartingPosition()));
}