using System.Numerics;
using Osmium.Core;

namespace Osmium.Heuristics;

public static class Heuristics
{
    public static int Evaluate(Position position) // absolute value in centipawns, positive for white advantage, negative for black advantage
    {
        int result = 0;
        for (PieceType pieceType = 0; pieceType < PieceType.King; pieceType++)
        {
            var whitePieces = position.GetPieceOfColorBitboard(pieceType, PieceColor.White);
            var blackPieces = position.GetPieceOfColorBitboard(pieceType, PieceColor.Black);
            int diff = BitOperations.PopCount(whitePieces) - BitOperations.PopCount(blackPieces);
            result += diff * materialValue[(int)pieceType];
        }
        return result;
    }

    static readonly int[] materialValue = [100, 330, 310, 500, 900, 0];        

    /*static readonly int[,] pawnSquareTable =
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
    };*/
} 
