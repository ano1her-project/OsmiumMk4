using Osmium.Core;
using Osmium.Engine;
using System.Runtime.Intrinsics;

namespace Osmium.Tests
{
    public class CoreTests
    {
        [Fact]
        public void Vector2FromString_SampleSquare()
        {
            var u = Vector2.FromString("e4");
            var v = new Vector2(4, 3);
            Assert.True(u == v);
        }

        [Fact]
        public void Vector2FromString_AllSquares()
        {
            for (int rank = 1; rank <= 8; rank++)
            {
                for (char file = 'a'; file <= 'h'; file++)
                {
                    string squareName = file.ToString() + rank.ToString();
                    var u = Vector2.FromString(squareName);
                    var v = new Vector2(file - 'a', rank - 1);
                    Assert.True(u == v);
                }
            }
        }

        [Fact]
        public void Vector2ToString_AllSquares()
        {
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file <= 8; file++)
                {
                    string squareName = (char)('a' + file) + (rank + 1).ToString();
                    var v = new Vector2(file, rank);
                    Assert.Equal(squareName, v.ToString());
                }
            }
        }

        [Fact]
        public void PieceFromChar_AllPieces()
        {
            char[] chars = ['p', 'b', 'n', 'r', 'q', 'k'];
            Piece.Type[] pieceTypes = [Piece.Type.Pawn, Piece.Type.Bishop, Piece.Type.Knight, Piece.Type.Rook, Piece.Type.Queen, Piece.Type.King];
            // white pieces
            for (int i = 0; i < 6; i++)
            {
                var p = new Piece(pieceTypes[i], true);
                var q = Piece.FromChar(char.ToUpper(chars[i]));
                Assert.True(p == q);
            }
            // black pieces
            for (int i = 0; i < 6; i++)
            {
                var p = new Piece(pieceTypes[i], false);
                var q = Piece.FromChar(chars[i]);
                Assert.True(p == q);
            }
        }

        [Fact]
        public void CastlingRightsFromString_AllOptions()
        {
            string[] options = ["-", "K", "Q", "KQ", "k", "Kk", "Qk", "KQk", "q", "Kq", "Qq", "KQq", "kq", "Kkq", "Qkq", "KQkq"];
            for (int i = 0; i < options.Length; i++)
            {
                var c = Position.CastlingRightsFromString(options[i]);
                Assert.Equal(i, (int)c);
            }
        }

        [Fact]
        public void StartingPositionToFen()
            => Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", Position.startingPosition.ToFEN());

        [Fact]
        public void Raycast_RookSample()
        {
            var position = Position.FromFEN("k7/8/8/8/8/8/8/R1K5 w - - 0 1");
            Vector2 attacker = new(0, 0);
            Assert.Equal("R", position.GetPiece(0, 0).ToString());
            //
            Assert.Equal("k", position.Raycast(attacker, Vector2.up).ToString());
            Assert.True(position.Raycast(attacker, Vector2.up) == new Piece(Piece.Type.King, false));
            // de facto two ways of expressing the same thing
        }

        [Fact]
        public void Raycast_BishopSample()
        {
            var position = Position.FromFEN("7k/8/8/8/8/8/8/B1K5 w - - 0 1");
            Vector2 attacker = new(0, 0);
            Assert.Equal("B", position.GetPiece(0, 0).ToString());
            //
            Assert.Equal("k", position.Raycast(attacker, Vector2.one).ToString());
            Assert.True(position.Raycast(attacker, Vector2.one) == new Piece(Piece.Type.King, false));
            // de facto two ways of expressing the same thing
        }

        [Fact]
        public void Raycast_WallHit()
        {
            var position = Position.FromFEN("8/K6k/8/8/8/8/8/8 w - - 0 1");
            Vector2 origin = new(1, 1);
            foreach (var direction in Vector2.allDirections)
                Assert.True(position.Raycast(origin, direction) is null);
        }

        [Fact]
        public void IsKingInCheck_Samples()
        {
            var position = Position.FromFEN("r1bqkbnr/ppppp1pp/2n2p2/7Q/4P3/8/PPPP1PPP/RNB1KBNR w KQkq - 3 3");
            Assert.True(position.IsKingInCheck(false));
            //
            position = Position.FromFEN("1rbqkbnr/pppppppp/2n5/7Q/4P3/8/PPPP1PPP/RNB1KBNR w KQk - 3 3");
            Assert.False(position.IsKingInCheck(false));
        }

        [Fact]
        public void GetAllLegalMoves_StartingPositionMoveCount()
            => Assert.Equal(20, Position.startingPosition.GetAllLegalMoves().Count);

        [Fact]
        public void GetAllLegalMoves_AccountsForCheck()
        {
            var position = Position.FromFEN("r1bqkbnr/pppppppp/2n5/7Q/4P3/8/PPPP1PPP/RNB1KBNR b KQkq - 2 2");
            Assert.Equal(19, position.GetAllLegalMoves().Count);
        }

        [Fact]
        public void GetAllLegalMoves_Promotions()
        {
            var position = Position.FromFEN("8/1k5P/8/7K/8/8/8/8 w - - 0 1");
            Assert.Equal(9, position.GetAllLegalMoves().Count);
            //
            position = Position.FromFEN("8/8/8/8/8/k7/1p2K3/8 b - - 0 1");
            Assert.Equal(8, position.GetAllLegalMoves().Count);
        }

        [Fact]
        public void GetAllLegalMoves_Castling()
        {
            var position = Position.FromFEN("rnbqkbnr/pppppppp/8/8/4P3/2NPBN2/PPPQBPPP/R3K2R w KQkq - 13 9");
            Assert.Equal(40, position.GetAllLegalMoves().Count);
        }

        [Fact]
        public void MakeAndUnmake_StartingPosition()
        {
            var position = Position.startingPosition.DeepCopy();
            var moves = position.GetAllLegalMoves();
            Assert.Equal(20, moves.Count);
            foreach (var move in moves)
            {
                position.MakeMove(move, out var undoInfo);
                position.UnmakeMove(move, undoInfo);
                Assert.Equal("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", position.ToFEN());
            }
        }

        [Fact]
        public void MakeAndUnmake_Kiwipete()
        {
            var position = Position.FromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 99 99");
            var moves = position.GetAllLegalMoves();
            Assert.Equal(48, moves.Count);
            foreach (var move in moves)
            {
                position.MakeMove(move, out var undoInfo);
                position.UnmakeMove(move, undoInfo);
                Assert.Equal("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 99 99", position.ToFEN());
            }
        }
    }

    public class EstimateTests
    {
        [Fact]
        public void GetMaterialBalance_StartingPosition()
            => Assert.Equal(0, Estimator.GetEstimate(Position.startingPosition));
    }

    public class MinimaxTests
    {
        [Fact]
        public void FindBestMove_HangingPieces()
        {
            var position = Position.FromFEN("8/2k5/p1p5/8/8/5r2/5PPP/6K1 w - - 0 1");
            Move bestMove = new(new(6, 1), new(5, 2));
            Assert.Equal(bestMove, Minimax.FindBestMove(position, 1, -1, Minimax.DebugPrintMode.ProgressBar, out _));
            //
            position = Position.FromFEN("rnb1kbnr/ppp2ppp/8/3qp3/8/2N5/PPPP1PPP/R1BQKBNR w KQkq - 0 4");
            bestMove = new(new(2, 2), new(3, 4));
            Assert.Equal(bestMove, Minimax.FindBestMove(position, 1, -1, Minimax.DebugPrintMode.ProgressBar, out _));
        }

        [Fact]
        public void FindBestMove_BackRankMates()
        {
            var position = Position.FromFEN("1k6/ppp5/8/8/8/6P1/5P1P/4R1K1 w - - 0 1");
            Move bestMove = new(new(4, 0), new(4, 7));
            Assert.Equal(bestMove, Minimax.FindBestMove(position, 1, -1, Minimax.DebugPrintMode.ProgressBar, out _));
            //
            position = Position.FromFEN("1k5n/ppp5/8/8/8/1Q4P1/5P1P/6K1 w - - 0 1");
            bestMove = new(new(1, 2), new(6, 7));
            Assert.Equal(bestMove, Minimax.FindBestMove(position, 1, -1, Minimax.DebugPrintMode.ProgressBar, out _));
            //
            position = Position.FromFEN("1k1b1r2/ppp5/8/8/8/6P1/3R1P1P/3R2K1 w - - 0 1");
            bestMove = new(new(3, 1), new(3, 7));
            Assert.Equal(bestMove, Minimax.FindBestMove(position, 3, -1, Minimax.DebugPrintMode.ProgressBar, out _));
        }

        [Fact]
        public void LeafNodeCount_StartingPosition() // perft
        {
            Assert.Equal(20, Minimax.CountLeafNodesAtDepth(Position.startingPosition, 1));
            Assert.Equal(400, Minimax.CountLeafNodesAtDepth(Position.startingPosition, 2));
            Assert.Equal(8_902, Minimax.CountLeafNodesAtDepth(Position.startingPosition, 3));
            Assert.Equal(197_281, Minimax.CountLeafNodesAtDepth(Position.startingPosition, 4));
            Assert.Equal(4_865_609, Minimax.CountLeafNodesAtDepth(Position.startingPosition, 5));
        }
    }
}
