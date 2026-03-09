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

public class PieceColors
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

public class Directions
{
    public static Direction Opposite(Direction direction)
        => (Direction)((int)(direction + 4) % 8);
}
