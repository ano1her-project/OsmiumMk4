using Osmium.Core;

namespace Osmium.Engine
{
    public class Minimax
    {
        static readonly int checkmateEval = 50_000;
        static readonly int stalemateEval = 0;

        public static Move FindBestMove(Position position, int depth, out int bestEval) // very similar to Evaluate() but also returns the move
        {
            var moves = position.GetAllLegalMoves();
            Console.WriteLine($"Found {moves.Count} move(s)..");
            bestEval = position.whiteToMove ? int.MinValue : int.MaxValue;
            Move bestMove = moves[0];
            Console.WriteLine(new string(' ', moves.Count) + moves.Count.ToString()); // print progress bar
            foreach (var move in moves)
            {
                int eval = Evaluate(position.AfterMove(move), depth - 1);
                bool isBetterThanPrevious = position.whiteToMove ? (eval > bestEval) : (eval < bestEval);
                Console.Write("▒"); // print progress bar
                if (!isBetterThanPrevious)
                    continue;
                bestEval = eval;
                bestMove = move;
            }
            Console.WriteLine();
            return bestMove;
        }

        public static int Evaluate(Position position, int depth)
        {
            // first check for checkmate or stalemate
            var moves = position.GetAllLegalMoves();
            if (moves.Count == 0)
                return position.IsKingInCheck(position.whiteToMove) ? (position.whiteToMove ? -checkmateEval : checkmateEval) : stalemateEval;
            // if reached the end of depth
            if (depth == 0)
                return Estimator.GetEstimate(position);
            // otehrwise, just recurse deeper
            int bestEval = position.whiteToMove ? int.MinValue : int.MaxValue;
            foreach (var move in moves)
            {
                int eval = Evaluate(position.AfterMove(move), depth - 1);
                bool isBetterThanPrevious = position.whiteToMove ? (eval > bestEval) : (eval < bestEval);
                if (isBetterThanPrevious)
                    bestEval = eval;
            }
            return bestEval;
        }
    }
}
