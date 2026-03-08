using System.Diagnostics;
using System.Threading.Tasks;

namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[6];
    ulong[] colorBitboards = new ulong[2];
    ulong emptySquareSet;
    PieceColor colorToMove;

    public Position(ulong[] p_pieceBitboards, ulong[] p_colorBitboards, PieceColor p_colorToMove)
    {
        pieceBitboards = p_pieceBitboards;
        colorBitboards = p_colorBitboards;
        emptySquareSet = ~(colorBitboards[0] | colorBitboards[1]);
        colorToMove = p_colorToMove;
    }

    public Position(ulong pawnBitboard, ulong bishopBitboard, ulong knightBitboard, ulong rookBitboard, ulong queenBitboard, ulong kingBitboard, ulong whiteBitboard, ulong blackBitboard, PieceColor colorToMove) :
        this([pawnBitboard, bishopBitboard, knightBitboard, rookBitboard, queenBitboard, kingBitboard], [whiteBitboard, blackBitboard], colorToMove) {}

    public static Position StartingPosition()
    => new([
        71776119061282560ul, // pawns
        (1ul << 2) | (1ul << 5) | (1ul << 58) | (1ul << 61), // bishops
        (1ul << 1) | (1ul << 6) | (1ul << 57) | (1ul << 62), // knights
        (1ul << 0) | (1ul << 7) | (1ul << 56) | (1ul << 63), // rooks
        (1ul << 3) | (1ul << 59), // queens
        (1ul << 4) | (1ul << 60)], // kings
        [65535ul, 18446462598732840960ul], // white and black respectively
        PieceColor.White); 

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
            result.Add(new(target - 8, target, PieceType.Pawn, false));
        }
        while (doublePushTargets != 0)
        {
            doublePushTargets = Bitboards.PopLeastSignificantOne(doublePushTargets, out int target);
            result.Add(new(target - 16, target, PieceType.Pawn, false));
        }
        return result;
    }

    public List<Move> GetPawnCaptures(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        var enemies = GetColorBitboard(PieceColors.Opposite(pawnColor));
        List<Move> result = [];
        while (pawns != 0)
        {
            pawns = Bitboards.PopLeastSignificantOne(pawns, out int from);
            var targets = Bitboards.GetPawnCaptures(pawnColor, from) & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target, PieceType.Pawn, true));
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
                var ray = Bitboards.GetRayMask(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerMask = Bitboards.squareToMask[blocker];
                    targets = (directionIsPositive ? (blockerMask - 1) : ~((blockerMask << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = PieceColors.Opposite(bishopColor);
                    if ((blockerMask & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker, PieceType.Bishop, true));
                }
                else // if there is no blocker
                    targets = ray; 
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target, PieceType.Bishop, false));
                }
            }
        }        
        return result;
    }

    public List<Move> GetKnightMoves(PieceColor knightColor)
    {
        var knights = GetPieceOfColorBitboard(PieceType.Knight, knightColor);
        var enemies = GetColorBitboard(PieceColors.Opposite(knightColor));
        List<Move> result = [];
        while (knights != 0)
        {
            knights = Bitboards.PopLeastSignificantOne(knights, out int from);
            var targets = Bitboards.knightMoves[from] & emptySquareSet;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target, PieceType.Knight, false));
            }
            targets = Bitboards.knightMoves[from] & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target, PieceType.Knight, true));
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
                var ray = Bitboards.GetRayMask(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    // see GetBishopMoves() comments
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerMask = Bitboards.squareToMask[blocker];
                    targets = (directionIsPositive ? (blockerMask - 1) : ~((blockerMask << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = PieceColors.Opposite(rookColor);
                    if ((blockerMask & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker, PieceType.Rook, true));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target, PieceType.Rook, false));
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
                var ray = Bitboards.GetRayMask(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    // see GetBishopMoves() comments
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b
                    var blockerMask = Bitboards.squareToMask[blocker];
                    targets = (directionIsPositive ? (blockerMask - 1) : ~((blockerMask << 1) - 1)) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = PieceColors.Opposite(queenColor);
                    if ((blockerMask & GetColorBitboard(enemyColor)) != 0) // the blocker bitboard "survives" the enemy color mask = the blocker is an enemy
                        result.Add(new(from, blocker, PieceType.Queen, true));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target, PieceType.Queen, false));
                }
            }
        }
        return result;
    }

    public List<Move> GetKingMoves(PieceColor kingColor)
    {
        var kings = GetPieceOfColorBitboard(PieceType.King, kingColor);
        int king = Bitboards.LeastSignificantOne(kings);        
        var targets = Bitboards.kingMoves[king] & emptySquareSet;
        List<Move> result = [];
        while (targets != 0)
        {
            targets = Bitboards.PopLeastSignificantOne(targets, out int target);
            result.Add(new(king, target, PieceType.King, false));
        }
        var enemies = GetColorBitboard(PieceColors.Opposite(kingColor));
        targets = Bitboards.kingMoves[king] & enemies;
        while (targets != 0)
        {
            targets = Bitboards.PopLeastSignificantOne(targets, out int target);
            result.Add(new(king, target, PieceType.King, true));
        }
        return result;
    }

    //

    public List<Move> GetPseudoLegalMoves()
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

    //

    public void MakeMove(Move move, out UndoInfo undoInfo)
    {
        var capturedPiece = PieceType.King;
        // handle the capture first (if it is one)
        var toMask = Bitboards.squareToMask[move.to];
        if (move.isCapture)
        {
            for (PieceType pieceType = 0; pieceType < PieceType.King; pieceType++)
            {
                if ((pieceBitboards[(int)pieceType] & toMask) == 0)
                    continue;
                capturedPiece = pieceType;
                pieceBitboards[(int)pieceType] &= ~toMask;
                colorBitboards[(int)PieceColors.Opposite(colorToMove)] &= ~toMask;
                break;
            }
        }
        undoInfo = new(capturedPiece);
        //            
        var change = Bitboards.squareToMask[move.from] | toMask;
        colorBitboards[(int)colorToMove] ^= change;
        pieceBitboards[(int)move.piece] ^= change;
        colorToMove = PieceColors.Opposite(colorToMove);
    }

    public void UnmakeMove(Move move, UndoInfo undoInfo)
    {
        colorToMove = PieceColors.Opposite(colorToMove);
        var change = Bitboards.squareToMask[move.from] | Bitboards.squareToMask[move.to];
        colorBitboards[(int)colorToMove] ^= change;
        pieceBitboards[(int)move.piece] ^= change;
        // handle capture
        if (move.isCapture)
        {
            if (undoInfo.capturedPiece == PieceType.King)
                throw new UnreachableException();
            pieceBitboards[(int)undoInfo.capturedPiece] |= Bitboards.squareToMask[move.to];
        }
    }
}

public readonly struct UndoInfo
{
    public readonly PieceType capturedPiece;

    public UndoInfo(PieceType p_capturedPiece)
    {
        capturedPiece = p_capturedPiece;
    }
}