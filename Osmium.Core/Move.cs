namespace Osmium.Core;

public readonly struct Move
{
    public readonly int from, to;
    public readonly PieceType piece;
    public readonly bool isCapture;
    public readonly Flag flag;

    public Move(int p_from, int p_to, PieceType p_piece, bool p_isCapture, Flag p_flag)
    {
        from = p_from;
        to = p_to;
        piece = p_piece;
        isCapture = p_isCapture;
        flag = p_flag;
    }

    public enum Flag
    {
        None,
        PawnDoublePush,
        CastlingKingside,
        CastlingQueenside,
        EnPassant,
        PromotionToQueen,
        PromotionToRook,
        PromotionToKnight,
        PromotionToBishop
    }

    public override string ToString()
        => $"{Squares.ToString(from)}{(isCapture ? "x" : "")}{Squares.ToString(to)}";

    public static Move[] WithOrWithoutPromotions(int from, int to, bool isCapture, PieceColor pawnColor)
    {
        if (Squares.IsPromotionSquare(to, pawnColor))
        {
            var result = new Move[4];
            for (int i = 0; i < 4; i++)
                result[i] = new(from, to, PieceType.Pawn, isCapture, i + Move.Flag.PromotionToQueen);
            return result;
        }
        else
            return [new(from, to, PieceType.Pawn, isCapture, Move.Flag.None)];
    }
}

// Information about what piece is moved depends on the position and a theoretically pure Move struct wouldn't need it.
// However, getting this information in MakeMove() is expensive while saving it when generating the move is free.

// Information about what piece is captured, unfortunately, seems to be as hard to get in MakeMove() as it is when generating the move.
// Information about WHETHER a piece is captured, however, can be saved at move generation for free. So there's that.