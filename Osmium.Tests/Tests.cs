using Osmium.Core;
using Osmium.Minimax;
using Osmium.Heuristics;

namespace Osmium.Tests;

public class CoreTests
{
    [Fact]
    public void SquareToString()
        => Assert.Equal("e4", Squares.ToString(28));

    [Fact]
    public void SquareFromString()
        => Assert.Equal(26, Squares.FromString("c4"));

    [Fact]
    public void ShiftEastSoutheast()
    {
        var bitboard = (1ul << 24) | (1ul << 25) | (1ul << 26) | (1ul << 27) | (1ul << 28) | (1ul << 29) | (1ul << 30) | (1ul << 31);
        var expectedShift = (1ul << 18) | (1ul << 19) | (1ul << 20) | (1ul << 21) | (1ul << 22) | (1ul << 23);
        Assert.Equal(expectedShift, Bitboards.ShiftEastSoutheast(bitboard));
    }

    [Fact]
    public void PrecalculatedRayNorth()
    {
        var expected = (1ul << 35) | (1ul << 43) | (1ul << 51) | (1ul << 59);
        Assert.Equal(expected, Bitboards.GetRayBitboard(Direction.North, 27));
    }

    [Fact]
    public void PrecalculatedRayNortheast()
    {
        var expected = (1ul << 36) | (1ul << 45) | (1ul << 54) | (1ul << 63);
        Assert.Equal(expected, Bitboards.GetRayBitboard(Direction.Northeast, 27));
    }

    [Fact]
    public void GetPawnPushes_Simple()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 18) | (1ul << 28) | (1ul << 21) | (1ul << 14) | (1ul << 15);
        Position position = new([pawns, 0, 0, 0, 0, 0], [pawns, 0]);
        List<Move> expectedMoves = [new(8, 16), new(9, 17), new(14, 22), new(15, 23), new(18, 26), new(21, 29), new(28, 36)];
        Assert.Equal(expectedMoves, position.GetPawnPushes(PieceColor.White));
    }

    [Fact]
    public void GetPawnPushes_WithEnemies()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var enemyPawns = (1ul << 20) | (1ul << 21) | (1ul << 22);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, 0], [pawns, enemyPawns]);
        List<Move> expectedMoves = [new(8, 16), new(9, 17), new(10, 18), new(11, 19), new(15, 23)];
        Assert.Equal(expectedMoves, position.GetPawnPushes(PieceColor.White));
    }

    [Fact]
    public void GetPawnCaptures()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var enemyPawns = (1ul << 20) | (1ul << 21) | (1ul << 22);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, 0], [pawns, enemyPawns]);
        List<Move> expectedMoves = [new(11, 20), new(12, 21), new(13, 20), new(13, 22), new(14, 21), new(15, 22)];
        Assert.Equal(expectedMoves, position.GetPawnCaptures(PieceColor.White));
    }

    [Fact]
    public void GetKnightMoves()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var knights = (1ul << 21);
        var enemyPawns = (1ul << 36) | (1ul << 38) | (1ul << 39);
        Position position = new([pawns | enemyPawns, 0, knights, 0, 0, 0], [pawns | knights, enemyPawns]);
        List<Move> expectedMoves = [new(21, 4), new(21, 6), new(21, 27), new(21, 31), new(21, 36), new(21, 38)];
        Assert.Equal(expectedMoves, position.GetKnightMoves(PieceColor.White));
    }

    [Fact]
    public void GetKingMoves()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 28) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var kings = (1ul << 12);
        var enemyPawns = (1ul << 19) | (1ul << 21);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, kings], [pawns | kings, enemyPawns]);
        List<Move> expectedMoves = [new(12, 3), new(12, 4), new(12, 5), new(12, 19), new(12, 20), new(12, 21)];
        Assert.Equal(expectedMoves, position.GetKingMoves(PieceColor.White));
    }
}