namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[6];
    ulong[] colorBitboards = new ulong[2];
    ulong emptySquareSet;

    public Position(ulong[] p_pieceBitboards, ulong[] p_colorBitboards)
    {
        pieceBitboards = p_pieceBitboards;
        colorBitboards = p_colorBitboards;
        emptySquareSet = ~(colorBitboards[0] | colorBitboards[1]);
    }

    public Position(ulong pawnBitboard, ulong bishopBitboard, ulong knightBitboard, ulong rookBitboard, ulong queenBitboard, ulong kingBitboard, ulong whiteBitboard, ulong blackBitboard) :
        this([pawnBitboard, bishopBitboard, knightBitboard, rookBitboard, queenBitboard, kingBitboard], [whiteBitboard, blackBitboard]) {}

    public static Position StartingPosition()
    => new([
        71776119061282560ul, // pawns
        (1ul << 2) | (1ul << 5) | (1ul << 58) | (1ul << 61), // bishops
        (1ul << 1) | (1ul << 6) | (1ul << 57) | (1ul << 62), // knights
        (1ul << 0) | (1ul << 7) | (1ul << 56) | (1ul << 63), // rooks
        (1ul << 3) | (1ul << 59), // queens
        (1ul << 4) | (1ul << 60)], // kings
        [65535ul, 18446462598732840960ul]); // white and black respectively

    public ulong GetPieceBitboard(PieceType pieceType)
        => pieceBitboards[(int)pieceType];

    public ulong GetColorBitboard(PieceColor pieceColor)
        => colorBitboards[(int)pieceColor];

    public ulong GetPieceOfColorBitboard(PieceType pieceType, PieceColor pieceColor)
        => pieceBitboards[(int)pieceType] & colorBitboards[(int)pieceColor];

    public ulong GetEmptySquareSet()
        => emptySquareSet;

    // get moves by piece:

    public List<Move> GetPawnPushes(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        bool forwardIsNorth = pawnColor == PieceColor.White;
        var singlePushTargets = (forwardIsNorth ? Bitboards.ShiftNorth(pawns) : Bitboards.ShiftSouth(pawns)) 
            & emptySquareSet;
        var doublePushTargets = (forwardIsNorth ? Bitboards.ShiftNorth(singlePushTargets) : Bitboards.ShiftSouth(singlePushTargets)) 
            & emptySquareSet & Bitboards.GetPawnDoublePushTargets(pawnColor);
        List<Move> result = [];
        while (singlePushTargets != 0)
        {
            singlePushTargets = Bitboards.PopLeastSignificantOne(singlePushTargets, out int target);
            result.Add(new(target - 8, target));
        }
        while (doublePushTargets != 0)
        {
            doublePushTargets = Bitboards.PopLeastSignificantOne(doublePushTargets, out int target);
            result.Add(new(target - 16, target));
        }
        return result;
    }

    public List<Move> GetPawnCaptures(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        var enemies = GetColorBitboard(pawnColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        List<Move> result = [];
        while (pawns != 0)
        {
            pawns = Bitboards.PopLeastSignificantOne(pawns, out int from);
            var targets = Bitboards.GetPawnCaptures(pawnColor, from) & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target));
            }
        }
        return result;
    }

    public List<Move> GetBishopMoves(PieceColor bishopColor)
    {
        var bishops = GetPieceOfColorBitboard(PieceType.Bishop, bishopColor);
        List<Move> result = [];
        while (bishops != 0)
        {
            bishops = Bitboards.PopLeastSignificantOne(bishops, out int from);
            for (Direction direction = Direction.Northeast; (int)direction < 8; direction += 2)
            {
                // Let's assume the bishop is at b1 and a blocking piece is at f5.
                // The ray bitboard will contain all squares in the line from c2 to h7.
                // The blocker will equal 37 (f5).
                // blockerBitboard - 1 will contain all squares with an index less than 37.
                // Thus, the targets bitboard will contain all squares in the line from c2 to e4 including e4.
                var ray = Bitboards.GetRayBitboard(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerBitboard = 1ul << blocker;
                    targets = (directionIsPositive ? (blockerBitboard - 1) : ~((blockerBitboard << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = bishopColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    if ((blockerBitboard & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker));
                }
                else // if there is no blocker
                    targets = ray; 
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target));
                }
            }
        }        
        return result;
    }

    public List<Move> GetKnightMoves(PieceColor knightColor)
    {
        var knights = GetPieceOfColorBitboard(PieceType.Knight, knightColor);
        var enemies = GetColorBitboard(knightColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        List<Move> result = [];
        while (knights != 0)
        {
            knights = Bitboards.PopLeastSignificantOne(knights, out int from);
            var targets = Bitboards.knightMoves[from] & (emptySquareSet | enemies);
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target));
            }
        }
        return result;
    }

    public List<Move> GetRookMoves(PieceColor rookColor)
    {
        var bishops = GetPieceOfColorBitboard(PieceType.Rook, rookColor);
        List<Move> result = [];
        while (bishops != 0)
        {
            bishops = Bitboards.PopLeastSignificantOne(bishops, out int from);
            for (Direction direction = Direction.North; (int)direction < 8; direction += 2)
            {
                var ray = Bitboards.GetRayBitboard(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    // see GetBishopMoves() comments
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerBitboard = 1ul << blocker;
                    targets = (directionIsPositive ? (blockerBitboard - 1) : ~((blockerBitboard << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = rookColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    if ((blockerBitboard & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target));
                }
            }
        }
        return result;
    }

    public List<Move> GetQueenMoves(PieceColor queenColor)
    {
        var bishops = GetPieceOfColorBitboard(PieceType.Queen, queenColor);
        List<Move> result = [];
        while (bishops != 0)
        {
            bishops = Bitboards.PopLeastSignificantOne(bishops, out int from);
            for (Direction direction = Direction.North; (int)direction < 8; direction++)
            {
                var ray = Bitboards.GetRayBitboard(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    // see GetBishopMoves() comments
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerBitboard = 1ul << blocker;
                    targets = (directionIsPositive ? (blockerBitboard - 1) : ~((blockerBitboard << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = queenColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    if ((blockerBitboard & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target));
                }
            }
        }
        return result;
    }

    public List<Move> GetKingMoves(PieceColor kingColor)
    {
        var kings = GetPieceOfColorBitboard(PieceType.King, kingColor);
        int king = Bitboards.LeastSignificantOne(kings);
        var enemies = GetColorBitboard(kingColor == PieceColor.White ? PieceColor.Black : PieceColor.White);
        var targets = Bitboards.kingMoves[king] & (emptySquareSet | enemies);
        List<Move> result = [];
        while (targets != 0)
        {
            targets = Bitboards.PopLeastSignificantOne(targets, out int target);
            result.Add(new(king, target));
        }
        return result;
    }

    //

    public List<Move> GetPseudoLegalMoves(PieceColor colorToMove)
    {
        return [
            ..GetPawnPushes(colorToMove), 
            ..GetPawnCaptures(colorToMove), 
            ..GetBishopMoves(colorToMove),
            ..GetKnightMoves(colorToMove),
            ..GetRookMoves(colorToMove),
            ..GetQueenMoves(colorToMove),
            ..GetKingMoves(colorToMove),
        ];
    }
}