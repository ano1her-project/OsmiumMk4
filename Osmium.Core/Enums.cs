namespace Osmium.Core;

public enum PieceType
{
    Pawn,
    Bishop,
    Knight,
    Rook,
    Queen,
    King
}

public enum PieceColor
{
    White,
    Black
}

public static class PieceColors
{
    public static PieceColor Opposite(PieceColor pieceColor)
        => pieceColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
}

public enum Direction // all cardinals are even, all diagonals are odd
{   
    North,
    Northeast,
    East,
    Southeast,
    South,
    Southwest,
    West,
    Northwest,
}

public static class Directions
{
    public static Direction Opposite(Direction direction)
        => (Direction)((int)(direction + 4) % 8);
}

[Flags] public enum CastlingRights : byte
{
    None = 0,
    WhiteKingside = 1,
    WhiteQueenside = 2,
    BlackKingside = 4,
    BlackQueenside = 8
}
