namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[6];
    ulong[] colorBitboards = new ulong[2];
    ulong emptySquareSet;

    public ulong GetPieceBitboard(PieceType pieceType)
        => pieceBitboards[(int)pieceType];

    public ulong GetColorBitboard(PieceColor pieceColor)
        => colorBitboards[(int)pieceColor];

    public ulong GetPieceOfColorBitboard(PieceType pieceType, PieceColor pieceColor)
        => pieceBitboards[(int)pieceType] & colorBitboards[(int)pieceColor];

    public ulong GetPawnPushTargets(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        return (pawnColor == PieceColor.White ? Bitboards.ShiftNorth(pawns) : Bitboards.ShiftSouth(pawns)) & emptySquareSet;
    }
}