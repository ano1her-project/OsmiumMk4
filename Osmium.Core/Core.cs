namespace Osmium.Core;

public class BitboardOperations
{
    static readonly ulong aFile = 1ul | (1ul << 8) | (1ul << 16) | (1ul << 24) | (1ul << 32) | (1ul << 40) | (1ul << 48) | (1ul << 56);
    static readonly ulong bFile = (1ul << 1) | (1ul << 9) | (1ul << 17) | (1ul << 25) | (1ul << 33) | (1ul << 41) | (1ul << 49) | (1ul << 57);
    static readonly ulong gFile = (1ul << 6) | (1ul << 14) | (1ul << 22) | (1ul << 30) | (1ul << 38) | (1ul << 46) | (1ul << 54) | (1ul << 62);
    static readonly ulong hFile = (1ul << 7) | (1ul << 15) | (1ul << 23) | (1ul << 31) | (1ul << 39) | (1ul << 47) | (1ul << 55) | (1ul << 63);
    static readonly ulong notAFile = ~aFile;
    static readonly ulong notHFile = ~hFile;
    static readonly ulong notABFiles = ~(aFile & bFile);
    static readonly ulong notGHFiles = ~(gFile & hFile);

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

    // cardinal directions:

    public static ulong ShiftNorth(ulong bitboard)
        => bitboard << 8;

    public static ulong ShiftEast(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notHFile) << 1;

    public static ulong ShiftSouth(ulong bitboard)
        => bitboard >> 8;

    public static ulong ShiftWest(ulong bitboard) // prevents pieces at the edges from wrapping around
        => (bitboard & notAFile) >> 1;

    // diagonal directions:

    public static ulong ShiftNortheast(ulong bitboard)
        => (bitboard & notHFile) << 9;

    public static ulong ShiftSoutheast(ulong bitboard)
        => (bitboard & notHFile) >> 7;

    public static ulong ShiftSouthwest(ulong bitboard)
        => (bitboard & notAFile) >> 9;

    public static ulong ShiftNorthwest(ulong bitboard)
        => (bitboard & notAFile) << 7;

    // hippogonal directions:

    public static ulong ShiftNorthNortheast(ulong bitboard)
        => (bitboard & notHFile) << 17;

    public static ulong ShiftEastNortheast(ulong bitboard)
        => (bitboard & notGHFiles) << 10;

    public static ulong ShiftEastSoutheast(ulong bitboard)
        => (bitboard & notGHFiles) >> 6;

    public static ulong ShiftSouthSoutheast(ulong bitboard)
        => (bitboard & notHFile) >> 15;

    public static ulong ShiftSouthSouthwest(ulong bitboard)
        => (bitboard & notAFile) >> 17;

    public static ulong ShiftWestSouthwest(ulong bitboard)
        => (bitboard & notABFiles) >> 10;

    public static ulong ShiftWestNorthwest(ulong bitboard)
        => (bitboard & notABFiles) << 6;

    public static ulong ShiftNorthNorthwest(ulong bitboard)
        => (bitboard & notAFile) << 15;
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