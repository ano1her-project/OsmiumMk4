using Osmium.Core;
using Osmium.Minimax;

namespace Osmium.Tests;

public class PuzzleTests
{
    static void FindBestMove(string fen, string expectedMove, int depth)
        => Assert.Equal(expectedMove, Minimax.Minimax.FindBestMove(Position.FromFEN(fen), depth, out _).ToString());

    // ultimately, all of these methods are the same
    // they're split off solely for aesthetics and legibility

    [Theory]
    [InlineData("1k3b2/pp4pp/3q4/8/8/1P4P1/P1P4P/1K1R2N1 w - - 0 1", "d1xd6", 3)]
    public void HangingPiece(string fen, string expectedMove, int depth)
        => FindBestMove(fen, expectedMove, depth);

    [Theory]
    [InlineData("6k1/r4ppp/8/8/8/1P6/P1P5/1K2R3 w - - 0 1", "e1e8", 3)]
    [InlineData("1r4k1/5ppp/1b6/p1p5/1p6/6PP/P3QP2/4R1K1 w - - 0 1", "e2e8", 3)]
    [InlineData("r1n2k2/5pp1/7p/8/2P4Q/P1N4P/5PP1/6K1 w - - 0 1", "h4d8", 3)]
    [InlineData("1r1r4/5k2/2b2p1p/6p1/8/6P1/RB3P1P/6K1 b - - 0 1", "d8d1", 3)]
    public void BackRankMate(string fen, string expectedMove, int depth)
        => FindBestMove(fen, expectedMove, depth);

    [Theory]
    [InlineData("3R4/8/8/7K/1k6/8/8/2R5 w - - 0 1", "d8b8", 3)] // ladder mate
    public void EndgameMate(string fen, string expectedMove, int depth)
        => FindBestMove(fen, expectedMove, depth);


    [Theory]
    [InlineData("2b5/6kp/p3p3/b1p1K2Q/P1r5/8/2P2PPP/R6R b - - 4 28", "a5c7", 3)]
    [InlineData("rnb2rk1/p1p2pp1/1p6/7p/3QP3/1P4P1/PBP2P1P/RN2R1K1 w - - 0 1", "d4xg7", 3)]
    public void MateInOne(string fen, string expectedMove, int depth)
        => FindBestMove(fen, expectedMove, depth);
}