using Osmium.Core;
using Osmium.Minimax;

namespace Osmium.Tests;

public class PerftTests
{
    [Theory]
    [InlineData(1, 20u)]
    [InlineData(2, 400u)]
    [InlineData(3, 8_902u)]
    [InlineData(4, 197_281u)]
    [InlineData(5, 4_865_609u)]
    [InlineData(6, 119_060_324u)]
    public void StartingPositionPerft(int depth, uint expected)
        => Assert.Equal(expected, Perft.CountLeafNodesAtDepth(Position.StartingPosition(), depth));
}
