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
        Assert.Equal(expected, Bitboards.GetRayMask(Direction.North, 27));
    }

    [Fact]
    public void PrecalculatedRayNortheast()
    {
        var expected = (1ul << 36) | (1ul << 45) | (1ul << 54) | (1ul << 63);
        Assert.Equal(expected, Bitboards.GetRayMask(Direction.Northeast, 27));
    }

    [Fact]
    public void StartingPositionFromFEN()
    {
        var startingPositionFromCtor = Position.StartingPosition();
        var startingPositionFromFEN = Position.FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        for (PieceType pieceType = 0; (int)pieceType < 6; pieceType++)
            Assert.Equal(
                startingPositionFromCtor.GetPieceBitboard(pieceType),
                startingPositionFromFEN.GetPieceBitboard(pieceType));
        for (PieceColor pieceColor = 0; (int)pieceColor < 2; pieceColor++)
            Assert.Equal(
                startingPositionFromCtor.GetColorBitboard(pieceColor),
                startingPositionFromFEN.GetColorBitboard(pieceColor));
        Assert.Equal(
            startingPositionFromCtor.GetEmptySquareSet(),
            startingPositionFromFEN.GetEmptySquareSet());
        Assert.Equal(
            startingPositionFromCtor.colorToMove,
            startingPositionFromFEN.colorToMove);
    }

    [Fact]
    public void GetPawnPushes_WithoutBlockers()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 18) | (1ul << 28) | (1ul << 21) | (1ul << 14) | (1ul << 15);
        Position position = new([pawns, 0, 0, 0, 0, 0], [pawns, 0], PieceColor.White);
        Assert.Equal(11, position.GetPawnPushes(PieceColor.White).Count);
    }

    [Fact]
    public void GetPawnPushes_WithEnemies()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var enemyPawns = (1ul << 20) | (1ul << 21) | (1ul << 22);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, 0], [pawns, enemyPawns], PieceColor.White);
        Assert.Equal(10, position.GetPawnPushes(PieceColor.White).Count);
    }

    [Fact]
    public void GetPawnCaptures()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var enemyPawns = (1ul << 20) | (1ul << 21) | (1ul << 22);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, 0], [pawns, enemyPawns], PieceColor.White);
        List<Move> expectedMoves = [new(11, 20, PieceType.Pawn, true), new(12, 21, PieceType.Pawn, true), new(13, 20, PieceType.Pawn, true), new(13, 22, PieceType.Pawn, true), new(14, 21, PieceType.Pawn, true), new(15, 22, PieceType.Pawn, true)];
        Assert.Equal(expectedMoves, position.GetPawnCaptures(PieceColor.White));
    }

    [Fact]
    public void GetBishopMoves_WithNoBlockers()
    {
        var bishops = 1ul << 45;
        Position position = new([0, bishops, 0, 0, 0, 0], [bishops, 0], PieceColor.White);
        Assert.Equal(11, position.GetBishopMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetBishopMoves_WithBlockers()
    {
        var bishops = 1ul << 45;
        var whitePawns = (1ul << 18) | (1ul << 38);
        Position position = new([whitePawns, bishops, 0, 0, 0, 0], [bishops | whitePawns, 0], PieceColor.White);
        Assert.Equal(6, position.GetBishopMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetBishopMoves_WithCapture()
    {
        var bishops = 1ul << 45;
        var whitePawns = 1ul << 18;
        var blackPawns = 1ul << 38;
        Position position = new([whitePawns | blackPawns, bishops, 0, 0, 0, 0], [bishops | whitePawns, blackPawns], PieceColor.White);
        Assert.Equal(7, position.GetBishopMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetKnightMoves()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 12) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var knights = 1ul << 21;
        var enemyPawns = (1ul << 36) | (1ul << 38) | (1ul << 39);
        Position position = new([pawns | enemyPawns, 0, knights, 0, 0, 0], [pawns | knights, enemyPawns], PieceColor.White);
        List<Move> expectedMoves = [new(21, 4, PieceType.Knight, false), new(21, 6, PieceType.Knight, false), new(21, 27, PieceType.Knight, false), new(21, 31, PieceType.Knight, false), new(21, 36, PieceType.Knight, true), new(21, 38, PieceType.Knight, true)];
        Assert.Equal(expectedMoves, position.GetKnightMoves(PieceColor.White));
    }

    [Fact]
    public void GetRookMoves_WithNoBlockers()
    {
        var rooks = 1ul << 45;
        Position position = new([0, 0, 0, rooks, 0, 0], [rooks, 0], PieceColor.White);
        Assert.Equal(14, position.GetRookMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetRookMoves_WithBlockers()
    {
        var rooks = 1ul << 45;
        var whitePawns = 1ul << 21;
        var blackPawns = 1ul << 42;
        Position position = new([whitePawns | blackPawns, 0, 0, rooks, 0, 0], [rooks | whitePawns, blackPawns], PieceColor.White);
        Assert.Equal(9, position.GetRookMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetQueenMoves_WithNoBlockers()
    {
        var queens = 1ul << 45;
        Position position = new([0, 0, 0, 0, queens, 0], [queens, 0], PieceColor.White);
        Assert.Equal(25, position.GetQueenMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetQueenMoves_WithBlockers()
    {
        var queens = 1ul << 45;
        var whitePawns = (1ul << 13) | (1ul << 27);
        var blackPawns = (1ul << 42) | (1ul << 52);
        Position position = new([whitePawns | blackPawns, 0, 0, 0, queens, 0], [queens | whitePawns, blackPawns], PieceColor.White);
        Assert.Equal(16, position.GetQueenMoves(PieceColor.White).Count);
    }

    [Fact]
    public void GetKingMoves()
    {
        var pawns = (1ul << 8) | (1ul << 9) | (1ul << 10) | (1ul << 11) | (1ul << 28) | (1ul << 13) | (1ul << 14) | (1ul << 15);
        var kings = (1ul << 12);
        var enemyPawns = (1ul << 19) | (1ul << 21);
        Position position = new([pawns | enemyPawns, 0, 0, 0, 0, kings], [pawns | kings, enemyPawns], PieceColor.White);
        List<Move> expectedMoves = [new(12, 3, PieceType.King, false), new(12, 4, PieceType.King, false), new(12, 5, PieceType.King, false), new(12, 20, PieceType.King, false), new(12, 19, PieceType.King, true), new(12, 21, PieceType.King, true)];
        Assert.Equal(expectedMoves, position.GetKingMoves(PieceColor.White));
    }

    [Fact]
    public void StartingPosition_MoveCount()
        => Assert.Equal(20, Position.StartingPosition().GetPseudoLegalMoves().Count);
}

public class MinimaxTests
{
    [Fact]
    public void Perft1()
        => Assert.Equal(20, Perft.CountLeafNodesAtDepth(Position.StartingPosition(), 1));

    [Fact]
    public void Perft2()
        => Assert.Equal(400, Perft.CountLeafNodesAtDepth(Position.StartingPosition(), 2));

    [Fact]
    public void Perft3()
        => Assert.Equal(8_902, Perft.CountLeafNodesAtDepth(Position.StartingPosition(), 3));

    [Fact]
    public void Perft4()
        => Assert.Equal(197_281, Perft.CountLeafNodesAtDepth(Position.StartingPosition(), 4));
}