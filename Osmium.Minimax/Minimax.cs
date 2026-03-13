using Osmium.Core;
using Osmium.Heuristics;

namespace Osmium.Minimax;

public static class Minimax
{
    public static Move FindBestMove(Position position, int depth, int highestEvalWhiteCanForce, int lowestEvalBlackCanForce, out int bestEval)
    {
        var colorToMove = position.colorToMove;
        var pseudoLegalMoves = position.GetPseudoLegalMoves();
        bestEval = colorToMove == PieceColor.White ? int.MinValue : int.MaxValue;
        Move bestMove = pseudoLegalMoves[0];
        foreach (var move in pseudoLegalMoves)
        {
            position.MakeMove(move, out var undoInfo);
            if (position.IsKingInCheck(colorToMove))
            {
                position.UnmakeMove(move, undoInfo);
                continue;
            }
            int eval = Evaluate(position, depth - 1, highestEvalWhiteCanForce, lowestEvalBlackCanForce);
            position.UnmakeMove(move, undoInfo);
            if ((colorToMove == PieceColor.White) ? // if move is not better than any previous
                (eval <= bestEval) :
                (eval >= bestEval))
                continue;
            bestEval = eval;
            bestMove = move;
            if (colorToMove == PieceColor.White)
            {
                if (bestEval >= lowestEvalBlackCanForce) // ..then black will never make the move that would lead to this node.
                    break;
                if (bestEval > highestEvalWhiteCanForce)
                    highestEvalWhiteCanForce = bestEval;
            }
            else // color to move == black
            {
                if (bestEval <= highestEvalWhiteCanForce) // ..then white will never make the move that would lead to this node.
                    break;
                if (bestEval < lowestEvalBlackCanForce)
                    lowestEvalBlackCanForce = bestEval;
            }
        }
        return bestMove;
    }

    public static Move FindBestMove(Position position, int depth, out int bestEval)
        => FindBestMove(position, depth, int.MinValue, int.MaxValue, out bestEval);

    public static int Evaluate(Position position, int depth, int highestEvalWhiteCanForce, int lowestEvalBlackCanForce)
    {
        var colorToMove = position.colorToMove;
        var legalMoves = position.GetLegalMoves();
        if (legalMoves.Count == 0)
            return position.IsKingInCheck(colorToMove) ?
                (colorToMove == PieceColor.White ? -Evals.checkmate : Evals.checkmate) :
                Evals.stalemate;
        if (depth == 0)
            return Heuristics.Heuristics.Evaluate(position);
        //
        int bestEval = colorToMove == PieceColor.White ? int.MinValue : int.MaxValue;
        foreach (var move in legalMoves)
        {
            position.MakeMove(move, out var undoInfo);
            int eval = Evaluate(position, depth - 1, highestEvalWhiteCanForce, lowestEvalBlackCanForce);
            position.UnmakeMove(move, undoInfo);
            if ((colorToMove == PieceColor.White) ? // if move is not better than any previous
                (eval <= bestEval) :
                (eval >= bestEval))
                continue;
            bestEval = eval;
            if (colorToMove == PieceColor.White)
            {
                if (bestEval >= lowestEvalBlackCanForce) // ..then black will never make the move that would lead to this node.
                    break;
                if (bestEval > highestEvalWhiteCanForce)
                    highestEvalWhiteCanForce = bestEval;
            }
            else // color to move == black
            {
                if (bestEval <= highestEvalWhiteCanForce) // ..then white will never make the move that would lead to this node.
                    break;
                if (bestEval < lowestEvalBlackCanForce)
                    lowestEvalBlackCanForce = bestEval;
            }
        }
        return bestEval;
    }
}

public static class Evals
{
    public readonly static int checkmate = 50_000;
    public readonly static int stalemate = 0;
}