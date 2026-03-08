namespace Osmium.Core;

public readonly struct Move
{
    public readonly int from, to;
    public readonly PieceType piece;

    public Move(int p_from, int p_to, PieceType p_piece)
    {
        from = p_from;
        to = p_to;
        piece = p_piece;
    }

    public override string ToString()
        => $"{Squares.ToString(from)}{Squares.ToString(to)}";
}

// Information about what piece is moved depends on the position and a theoretically pure Move struct wouldn't need it.
// However, getting this information in MakeMove() is expensive while saving it when generating the move is free.