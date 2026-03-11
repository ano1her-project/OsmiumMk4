using Osmium.Core;

namespace Osmium.Minimax;

public class Perft
{
    public static uint CountLeafNodesAtDepth(Position position, int depth)
    {
        if (depth == 0)
            return 1;
        var pseudoLegalMoves = position.GetPseudoLegalMoves();
        if (depth == 1)
            return (uint)position.FilterLegalMoves(pseudoLegalMoves).Count;
        var kingColor = position.colorToMove;
        uint leafCount = 0;
        foreach (var move in pseudoLegalMoves)
        {
            position.MakeMove(move, out var undoInfo);
            if (!position.IsKingInCheck(kingColor))
                leafCount += CountLeafNodesAtDepth(position, depth - 1);
            position.UnmakeMove(move, undoInfo);
        }
        return leafCount;
    }
}
