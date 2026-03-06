namespace Osmium.Core;

public class BitboardOperations
{
    static readonly ulong aFile = 1ul | (1ul << 8) | (1ul << 16) | (1ul << 24) | (1ul << 32) | (1ul << 40) | (1ul << 48) | (1ul << 56);
    static readonly ulong notAFile = ~aFile;
    static readonly ulong hFile = (1ul << 7) | (1ul << 15) | (1ul << 23) | (1ul << 31) | (1ul << 39) | (1ul << 47) | (1ul << 55) | (1ul << 63);
    static readonly ulong notHFile = ~hFile;

    // i use west, east etc. instead of left and right in order to distinguish it from a bitwise shift to the left or right
    // in fact, shifting west (~ left) requires a bitwise shift right
    // in fact, shifting east (~ right) requires a bitwise shift left
    // ...
    // ...
    // 2 8 9 10 ...
    // 1 0 1 2 3 4 5 6 7 
    //   a b c d e f g h
    // BUT, within the binary number:
    // ...{9}{8}{7}{6}{5}{4}{3}{2}{1}{0}

    public static ulong ShiftNorth(ulong bitboard)
        => bitboard << 8;

    public static ulong ShiftSouth(ulong bitboard)
        => bitboard >> 8;

    public static ulong ShiftEast(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notHFile) << 1;

    public static ulong ShiftWest(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notAFile) >> 1;
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