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
            int whitePieceCount = 0;
            int[] whitePieceSquareTable = GetPieceSquareTable(pieceType, PieceColor.White);
            while (whitePieces != 0)
            {
                whitePieces = Bitboards.PopLeastSignificantOne(whitePieces, out int piece);
                whitePieceCount++;
                result += whitePieceSquareTable[piece];
            }
            var blackPieces = position.GetPieceOfColorBitboard(pieceType, PieceColor.Black);
            int blackPieceCount = 0;
            int[] blackPieceSquareTable = GetPieceSquareTable(pieceType, PieceColor.Black);
            while (blackPieces != 0)
            {
                blackPieces = Bitboards.PopLeastSignificantOne(blackPieces, out int piece);
                blackPieceCount++;
                result -= blackPieceSquareTable[piece];
            }
            int difference = whitePieceCount - blackPieceCount;
            result += difference * materialValue[(int)pieceType];
        }
        return result;
    }

    static readonly int[] materialValue = [100, 330, 310, 500, 900, 0];

    // what's pictured below are actually the piece square tables for black (because i didnt realize how stuff's ordered in an array ig)
    // but visually they resemble the board from white's perspective

    static readonly int[] pawnSquareTable =
        [
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 20, 20, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5,  0,  0,  0,  0,  0,  0,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
        ];

    static readonly int[] bishopSquareTable =
    [
        -30,-30,-30,-30,-30,-30,-30,-30,
        -30,-10,-10,-10,-10,-10,-10,-30,
        -30,-10, 10, 10, 10, 10,-10,-30,
        -30,-10, 10, 30, 30, 10,-10,-30,
        -30,-10, 10, 30, 30, 10,-10,-30,
        -30,-10, 10, 10, 10, 10,-10,-30,
        -30,-10,-10,-10,-10,-10,-10,-30,
        -30,-30,-20,-30,-30,-20,-30,-30
    ];

    static readonly int[] knightSquareTable =
    [
        -35,-20,-10,-10,-10,-10,-20,-30,
        -20,-10,  0,  0,  0,  0,-10,-20,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5, 10, 20, 20, 10,  5,-10,
        -10,  0, 10, 25, 25, 10,  0,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -20,-15,  0,  0,  0,  0,-15,-20,
        -30,-20,-15,-15,-15,-15,-20,-30
    ];

    static readonly int[] rookSquareTable =
    [
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0,
        0,  0, 10, 10, 10, 10,  0,  0
    ];

    static readonly int[] queenSquareTable =
    [
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  5,  5,  5,  0,  0,
        0,  5,  5,  5,  5,  5,  0,  0,
        0,  0,  5,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0
    ];

    static readonly int[] kingSquareTable =
    [
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0,
        0,  0,  0,  0,  0,  0,  0,  0
    ];

    static int[] MirrorPieceSquareTable(int[] pst) // probably should be used solely as a precalculation method
        => [
            pst[56], pst[57], pst[58], pst[59], pst[60], pst[61], pst[62], pst[63],
            pst[48], pst[49], pst[50], pst[51], pst[52], pst[53], pst[54], pst[55],
            pst[40], pst[41], pst[42], pst[43], pst[44], pst[45], pst[46], pst[47],
            pst[32], pst[33], pst[34], pst[35], pst[36], pst[37], pst[38], pst[39],
            pst[24], pst[25], pst[26], pst[27], pst[28], pst[29], pst[30], pst[31],
            pst[16], pst[17], pst[18], pst[19], pst[20], pst[21], pst[22], pst[23],
            pst[8],  pst[9],  pst[10], pst[11], pst[12], pst[13], pst[14], pst[15],
            pst[0],  pst[1],  pst[2],  pst[3],  pst[4],  pst[5],  pst[6],  pst[7]
        ];

    public static readonly int[][][] pieceSquareTables = [
        [MirrorPieceSquareTable(pawnSquareTable), MirrorPieceSquareTable(bishopSquareTable), MirrorPieceSquareTable(knightSquareTable), MirrorPieceSquareTable(rookSquareTable), MirrorPieceSquareTable(queenSquareTable), MirrorPieceSquareTable(kingSquareTable)],
        [pawnSquareTable, bishopSquareTable, knightSquareTable, rookSquareTable, queenSquareTable, kingSquareTable]
    ];

    static int[] GetPieceSquareTable(PieceType pieceType, PieceColor pieceColor)
        => pieceSquareTables[(int)pieceColor][(int)pieceType];
}