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
            int pieceTypeTimesEight = (int)pieceType * 8;
            var whitePieces = position.GetPieceOfColorBitboard(pieceType, PieceColor.White);            
            int whitePieceCount = 0;
            while (whitePieces != 0)
            {
                whitePieces = Bitboards.PopLeastSignificantOne(whitePieces, out int piece);
                whitePieceCount++;
                result += pieceSquareTable[pieceTypeTimesEight + Squares.Mirror(piece)];
            }
            var blackPieces = position.GetPieceOfColorBitboard(pieceType, PieceColor.Black);
            int blackPieceCount = 0;
            while (blackPieces != 0)
            {
                blackPieces = Bitboards.PopLeastSignificantOne(blackPieces, out int piece);
                blackPieceCount++;
                result -= pieceSquareTable[pieceTypeTimesEight + piece];
            }
            int difference = whitePieceCount - blackPieceCount;
            result += difference * materialValue[(int)pieceType];
        }
        return result;
    }

    static readonly int[] materialValue = [100, 330, 310, 500, 900, 0];

    // what's pictured below are actually the piece square tables for black (because i didnt realize how stuff's ordered in an array ig)
    // but visually they resemble the board from white's perspective

    static readonly int[] pieceSquareTable =
    [
        // pawn
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 20, 20, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5,  0,  0,  0,  0,  0,  0,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0,
        // bishop
        -30,-30,-30,-30,-30,-30,-30,-30,
        -30,-10,-10,-10,-10,-10,-10,-30,
        -30,-10, 10, 10, 10, 10,-10,-30,
        -30,-10, 10, 30, 30, 10,-10,-30,
        -30,-10, 10, 30, 30, 10,-10,-30,
        -30,-10, 10, 10, 10, 10,-10,-30,
        -30,-10,-10,-10,-10,-10,-10,-30,
        -30,-30,-20,-30,-30,-20,-30,-30,
        // knight
        -35,-20,-10,-10,-10,-10,-20,-30,
        -20,-10,  0,  0,  0,  0,-10,-20,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5, 10, 20, 20, 10,  5,-10,
        -10,  0, 10, 25, 25, 10,  0,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -20,-15,  0,  0,  0,  0,-15,-20,
        -30,-20,-15,-15,-15,-15,-20,-30,
        // rook
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        // queen
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  5,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        // king
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0
    ];
}