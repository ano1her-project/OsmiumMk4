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

public enum Direction // all cardinals are even, all diagonals are odd
{
    North,
    Northeast,
    East,
    Southeast,
    South,
    Southwest,
    West,
    Northwest
}
