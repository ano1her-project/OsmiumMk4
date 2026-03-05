namespace Osmium.Core;

public class BitboardOperations
{
    public static ulong ShiftUp(ulong bitboard)
        => bitboard << 8;

    public static ulong ShiftUp(ulong bitboard, int shiftAmount)
        => bitboard << (8 * shiftAmount);

    public static ulong ShiftDown(ulong bitboard)
        => bitboard >> 8;

    public static ulong ShiftDown(ulong bitboard, int shiftAmount)
        => bitboard >> (8 * shiftAmount);
}

public enum PieceType
{
    WhitePawn,
    WhiteBishop,
    WhiteKnight,
    WhiteRook,
    WhiteQueen,
    WhiteKing,
    BlackPawn,
    BlackBishop,
    BlackKnight,
    BlackRook,
    BlackQueen,
}

public class Position
{
    ulong[] pieceBitboards = new ulong[12];

    public ulong GetPieceBitboard(PieceType pieceType)
        => pieceBitboards[(int)pieceType];
}