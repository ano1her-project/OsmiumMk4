namespace Osmium.Core;

public class BitboardOperations
{
    static readonly ulong aFile = 1ul | (1ul << 8) | (1ul << 16) | (1ul << 24) | (1ul << 32) | (1ul << 40) | (1ul << 48) | (1ul << 56);
    static readonly ulong notAFile = ~aFile;
    static readonly ulong hFile = (1ul << 7) | (1ul << 15) | (1ul << 23) | (1ul << 31) | (1ul << 39) | (1ul << 47) | (1ul << 55) | (1ul << 63);
    static readonly ulong notHFile = ~hFile;

    public static ulong ShiftUp(ulong bitboard)
        => bitboard << 8;

    public static ulong ShiftDown(ulong bitboard)
        => bitboard >> 8;

    public static ulong ShiftRight(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notHFile) >> 1;

    public static ulong ShiftLeft(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notAFile) << 1;
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