using Osmium.Core;

namespace Osmium.Engine
{
    public class Estimator
    {
        public static int GetEstimate(Position position) // absolute value, positive for white advantage, negative for black advantage
        {
            int result = 0;
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = position.GetPiece(rank, file);
                    if (piece is null)
                        continue;
                    Piece.Type type = (Piece.Type)piece?.type; // getting around the nulls
                    bool isWhite = (bool)piece?.isWhite;
                    int pieceValue = materialValue[(int)type] + pieceSquareTable[type][isWhite ? rank : (7 - rank), file];
                    result += pieceValue * (isWhite ? 1 : -1);
                }
            }
            return result;
        }

        static readonly int[] materialValue = [100, 330, 310, 500, 900, 0];        

        static readonly int[,] pawnSquareTable =
        {
            {  0,  0,  0,  0,  0,  0,  0,  0 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            {  5,  5, 10, 20, 20, 10,  5,  5 },
            {  0,  0,  0, 20, 20,  0,  0,  0 },
            {  5,  0,  0,  0,  0,  0,  0,  5 },
            {  5, 10, 10,-20,-20, 10, 10,  5 },
            {  0,  0,  0,  0,  0,  0,  0,  0 }
        };

        static readonly int[,] knightSquareTable =
        {
            { -35,-20,-10,-10,-10,-10,-20,-30 },
            { -20,-10,  0,  0,  0,  0,-10,-20 },
            { -10,  0,  5, 10, 10,  5,  0,-10 },
            { -10,  5, 10, 20, 20, 10,  5,-10 },
            { -10,  0, 10, 25, 25, 10,  0,-10 },
            { -10,  0, 10, 10, 10, 10,  0,-10 },
            { -20,-15,  0,  0,  0,  0,-15,-20 },
            { -30,-20,-15,-15,-15,-15,-20,-30 }
        };

        static readonly int[,] bishopSquareTable =
        {
            {-30,-30,-30,-30,-30,-30,-30,-30 },
            {-30,-10,-10,-10,-10,-10,-10,-30 },
            {-30,-10, 10, 10, 10, 10,-10,-30 },
            {-30,-10, 10, 30, 30, 10,-10,-30 },
            {-30,-10, 10, 30, 30, 10,-10,-30 },
            {-30,-10, 10, 10, 10, 10,-10,-30 },
            {-30,-10,-10,-10,-10,-10,-10,-30 },
            {-30,-30,-20,-30,-30,-20,-30,-30 }
        };

        static readonly int[,] rookSquareTable =
        {
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 },
            { 0,  0, 10, 10, 10, 10,  0,  0 }
        };

        static readonly int[,] queenSquareTable =
        {
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  5,  5,  5,  5,  0,  0 },
            { 0,  0,  5,  5,  5,  5,  0,  0 },
            { 0,  0,  5,  5,  5,  5,  0,  0 },
            { 0,  5,  5,  5,  5,  5,  0,  0 },
            { 0,  0,  5,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 }
        };

        static readonly int[,] kingSquareTable =
        {
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 }
        };

        static readonly Dictionary<Piece.Type, int[,]> pieceSquareTable = new()
        {
            { Piece.Type.Pawn, pawnSquareTable },
            { Piece.Type.Bishop, bishopSquareTable },
            { Piece.Type.Knight, knightSquareTable },
            { Piece.Type.Rook, rookSquareTable },
            { Piece.Type.Queen, queenSquareTable },
            { Piece.Type.King, kingSquareTable },
        };
    } 
}
