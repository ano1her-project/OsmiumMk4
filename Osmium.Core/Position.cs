using System.Diagnostics;
using System.Threading.Tasks;

namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[6];
    ulong[] colorBitboards = new ulong[2];
    ulong emptySquareSet;
    public PieceColor colorToMove;

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

    public static Position FromFEN(string fen)
    {
        var fields = fen.Split(' ');
        // 0th (1st) field = piece placement data
        var pieceBitboards = new ulong[6];
        var colorBitboards = new ulong[2];
        var ranks = fields[0].Split('/');
        for (int rank = 7; rank >= 0; rank--)
        {
            int file = 0;
            foreach (var ch in ranks[7 - rank].ToCharArray())
            {
                if (char.IsDigit(ch))
                    file += ch - '0';
                else
                {
                    int square = 8 * rank + file;
                    PieceType piece = char.ToLower(ch) switch { 
                        'p' => PieceType.Pawn,
                        'b' => PieceType.Bishop,
                        'n' => PieceType.Knight,
                        'r' => PieceType.Rook,
                        'q' => PieceType.Queen,
                        'k' => PieceType.King
                    };
                    pieceBitboards[(int)piece] |= Bitboards.squareToMask[square];
                    colorBitboards[char.IsUpper(ch) ? 0 : 1] |= Bitboards.squareToMask[square];
                    file++;
                }
            }
        }
        // 1st (2nd) field = active color
        PieceColor colorToMove = fields[1] == "w" ? PieceColor.White : PieceColor.Black;
        // 2nd (3rd) field = castling rights

        // 3rd (4th) field = en passant target square

        // 4th (5th) field = halfmove clock used for the fifty move rule
        int halfmoveClock = int.Parse(fields[4]);
        // 5th (6th) field = fullmove number
        int fullmoves = int.Parse(fields[5]);
        //
        return new(pieceBitboards, colorBitboards, colorToMove);
    }

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
        int offset = pawnColor == PieceColor.White ? -8 : 8;
        List<Move> result = [];
        while (singlePushTargets != 0)
        {
            singlePushTargets = Bitboards.PopLeastSignificantOne(singlePushTargets, out int target);
            result.Add(new(target + offset, target, PieceType.Pawn, false));
        }
        offset = pawnColor == PieceColor.White ? -16 : 16;
        while (doublePushTargets != 0)
        {
            doublePushTargets = Bitboards.PopLeastSignificantOne(doublePushTargets, out int target);
            result.Add(new(target + offset, target, PieceType.Pawn, false));
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

    List<Move> GetSliderMoves(PieceColor sliderColor, PieceType pieceType, Direction startAtDirection, int directionIncrement)
    {
        var sliders = GetPieceOfColorBitboard(pieceType, sliderColor);
        List<Move> result = [];
        while (sliders != 0)
        {
            sliders = Bitboards.PopLeastSignificantOne(sliders, out int from);
            for (Direction direction = startAtDirection; (int)direction < 8; direction += directionIncrement)
            {
                var ray = Bitboards.GetRayMask(direction, from);
                var piecesOnRay = ray & ~emptySquareSet;
                ulong targets;
                if (piecesOnRay != 0)
                {
                    // Say the slider is a bishop at b1 and a blocking piece is at f5.
                    // The ray bitboard will contain all squares in the line from c2 to h7.
                    // The blocker will equal 37 (f5).
                    // The opposite ray from the blocker will contain all squares in the line from e4 to b1.
                    // Thus, the targets bitboard will contain all squares in the line segment from c2 to e4.
                    bool directionIsPositive = Bitboards.IsPositive(direction);
                    int blocker = directionIsPositive ?               // from Bitboards.cs:
                        Bitboards.LeastSignificantOne(piecesOnRay) : // "positive" directions correspond to left shifts and the first hit is the ls1b
                        Bitboards.MostSignificantOne(piecesOnRay);  // "negative" directions correspond to right shifts and the first hit is the ms1b                    
                    targets = Bitboards.GetRayMask(Directions.Opposite(direction), blocker) & ray;
                    // "bonus" check on top - if the blocker is an enemy, add a capture move
                    var enemyColor = PieceColors.Opposite(sliderColor);
                    if ((Bitboards.squareToMask[blocker] & GetColorBitboard(enemyColor)) != 0) // if blocker bitboard "survives" the enemy color mask, it's an enemy
                        result.Add(new(from, blocker, pieceType, true));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target, pieceType, false));
                }
            }
        }
        return result;
    }

    public List<Move> GetBishopMoves(PieceColor bishopColor)
        => GetSliderMoves(bishopColor, PieceType.Bishop, Direction.Northeast, 2); // only odd directions (diagonals)

    public List<Move> GetRookMoves(PieceColor rookColor)
        => GetSliderMoves(rookColor, PieceType.Rook, Direction.North, 2); // only even directions (cardinals)

    public List<Move> GetQueenMoves(PieceColor queenColor)
        => GetSliderMoves(queenColor, PieceType.Queen, Direction.North, 1); // all directions

    public List<Move> GetKingMoves(PieceColor kingColor)
    {
        var kingMask = GetPieceOfColorBitboard(PieceType.King, kingColor);
        int king = Bitboards.LeastSignificantOne(kingMask);        
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

    // making and unmaking moves:

    public void MakeMove(Move move, out UndoInfo undoInfo)
    {        
        // cache from and to in mask form (we'll need both more than once)
        var fromMask = Bitboards.squareToMask[move.from];
        var toMask = Bitboards.squareToMask[move.to];
        // handle the capture first (if it is one)
        var capturedPiece = PieceType.King;
        if (move.isCapture)
        {
            for (PieceType pieceType = 0; pieceType < PieceType.King; pieceType++)
            {
                if ((pieceBitboards[(int)pieceType] & toMask) == 0)
                    continue;
                capturedPiece = pieceType;
                pieceBitboards[(int)capturedPiece] &= ~toMask;
                colorBitboards[(int)PieceColors.Opposite(colorToMove)] &= ~toMask;
                break;
            }
        }
        undoInfo = new(capturedPiece);
        //            
        var change = fromMask | toMask;
        colorBitboards[(int)colorToMove] ^= change;
        pieceBitboards[(int)move.piece] ^= change;
        emptySquareSet |= fromMask;
        emptySquareSet &= ~toMask;
        colorToMove = PieceColors.Opposite(colorToMove);
    }

    public void UnmakeMove(Move move, UndoInfo undoInfo)
    {
        colorToMove = PieceColors.Opposite(colorToMove);
        // cache from and to in mask form (we'll need both more than once)
        var fromMask = Bitboards.squareToMask[move.from];
        var toMask = Bitboards.squareToMask[move.to];
        //        
        var change = fromMask | toMask;
        colorBitboards[(int)colorToMove] ^= change;
        pieceBitboards[(int)move.piece] ^= change;
        emptySquareSet ^= change;
        // handle capture
        if (move.isCapture)
        {
            if (undoInfo.capturedPiece == PieceType.King)
                throw new UnreachableException();
            pieceBitboards[(int)undoInfo.capturedPiece] |= toMask;
            colorBitboards[(int)PieceColors.Opposite(colorToMove)] |= toMask;
            emptySquareSet &= ~toMask;
        }
    }

    // checks, move legality:

    public bool IsKingInCheck(PieceColor kingColor)
    {
        var kingMask = GetPieceOfColorBitboard(PieceType.King, kingColor);
        int king = Bitboards.LeastSignificantOne(kingMask);
        var enemyColor = PieceColors.Opposite(kingColor);
        //
        var checkingPawns = GetPieceOfColorBitboard(PieceType.Pawn, enemyColor) & Bitboards.GetPawnCaptures(kingColor, king);
        if (checkingPawns != 0)
            return true;
        //
        var checkingKnights = GetPieceOfColorBitboard(PieceType.Knight, enemyColor) & Bitboards.knightMoves[king];
        if (checkingKnights != 0)
            return true;
        //
        var enemyQueens = GetPieceOfColorBitboard(PieceType.Queen, enemyColor);
        var enemyCardinalSliders = GetPieceOfColorBitboard(PieceType.Rook, enemyColor) | enemyQueens;
        var blockers = 0ul;
        for (Direction direction = Direction.North; (int)direction < 8; direction += 2)
        {
            var ray = Bitboards.GetRayMask(direction, king);
            var piecesOnRay = ray & ~emptySquareSet;
            if (piecesOnRay == 0)
                continue;
            bool directionIsPositive = Bitboards.IsPositive(direction);
            int blocker = directionIsPositive ?
                Bitboards.LeastSignificantOne(piecesOnRay) :
                Bitboards.MostSignificantOne(piecesOnRay);
            blockers |= Bitboards.squareToMask[blocker];
        }
        if ((blockers & enemyCardinalSliders) != 0)
            return true;
        //
        var enemyDiagonalSliders = GetPieceOfColorBitboard(PieceType.Bishop, enemyColor) | enemyQueens;
        blockers = 0ul;
        for (Direction direction = Direction.Northeast; (int)direction < 8; direction += 2)
        {
            var ray = Bitboards.GetRayMask(direction, king);
            var piecesOnRay = ray & ~emptySquareSet;
            if (piecesOnRay == 0)
                continue;
            bool directionIsPositive = Bitboards.IsPositive(direction);
            int blocker = directionIsPositive ?
                Bitboards.LeastSignificantOne(piecesOnRay) :
                Bitboards.MostSignificantOne(piecesOnRay);
            blockers |= Bitboards.squareToMask[blocker];
        }
        if ((blockers & enemyDiagonalSliders) != 0)
            return true;
        //
        return false;
    }

    public List<Move> FilterLegalMoves(List<Move> pseudoLegalMoves)
    {
        var kingColor = colorToMove;
        List<Move> result = [];
        for (int i = 0; i < pseudoLegalMoves.Count; i++)
        {
            MakeMove(pseudoLegalMoves[i], out var undoInfo);
            if (!IsKingInCheck(kingColor))
                result.Add(pseudoLegalMoves[i]);
            UnmakeMove(pseudoLegalMoves[i], undoInfo);
        }
        return result;
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