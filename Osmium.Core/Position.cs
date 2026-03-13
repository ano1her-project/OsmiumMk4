using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[6];
    ulong[] colorBitboards = new ulong[2];
    ulong emptySquareSet;
    public PieceColor colorToMove;
    CastlingRights castlingRights;
    int enPassantFile;

    public Position(ulong[] p_pieceBitboards, ulong[] p_colorBitboards, PieceColor p_colorToMove, CastlingRights p_castlingRights, int p_enPassantFile)
    {
        pieceBitboards = p_pieceBitboards;
        colorBitboards = p_colorBitboards;
        emptySquareSet = ~(colorBitboards[0] | colorBitboards[1]);
        colorToMove = p_colorToMove;
        castlingRights = p_castlingRights;
        enPassantFile = p_enPassantFile;
    }

    public Position(ulong pawnBitboard, ulong bishopBitboard, ulong knightBitboard, ulong rookBitboard, ulong queenBitboard, ulong kingBitboard, ulong whiteBitboard, ulong blackBitboard, PieceColor colorToMove, CastlingRights castlingRights, int enPassantFile) :
        this([pawnBitboard, bishopBitboard, knightBitboard, rookBitboard, queenBitboard, kingBitboard], [whiteBitboard, blackBitboard], colorToMove, castlingRights, enPassantFile) {}

    public static Position StartingPosition()
    => new([
        71776119061282560ul, // pawns
        (1ul << 2) | (1ul << 5) | (1ul << 58) | (1ul << 61), // bishops
        (1ul << 1) | (1ul << 6) | (1ul << 57) | (1ul << 62), // knights
        (1ul << 0) | (1ul << 7) | (1ul << 56) | (1ul << 63), // rooks
        (1ul << 3) | (1ul << 59), // queens
        (1ul << 4) | (1ul << 60)], // kings
        [65535ul, 18446462598732840960ul], // white and black respectively
        PieceColor.White, (CastlingRights)15, -1);

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
        CastlingRights castlingRights = 0;
        foreach (var ch in fields[2].ToCharArray())
        {
            castlingRights |= ch switch { 
                'K' => CastlingRights.WhiteKingside,
                'Q' => CastlingRights.WhiteQueenside,
                'k' => CastlingRights.BlackKingside,
                'q' => CastlingRights.BlackQueenside,
                _ => 0
            };
        }
        // 3rd (4th) field = en passant target square
        int enPassantFile = (fields[3] == "-") ? -1 : fields[3][0] - 'a';
        // 4th (5th) field = halfmove clock used for the fifty move rule
        int halfmoveClock = int.Parse(fields[4]);
        // 5th (6th) field = fullmove number
        int fullmoves = int.Parse(fields[5]);
        //
        return new(pieceBitboards, colorBitboards, colorToMove, castlingRights, enPassantFile);
    }

    public string ToFEN()
    {
        string fen = "";
        // 0th (1st) field = piece placement data
        PieceType?[] pieceTypeMailbox = new PieceType?[64];
        PieceColor?[] pieceColorMailbox = new PieceColor?[64];
        // build mailbox
        for (PieceType pieceType = 0; (int)pieceType < 6; pieceType++)
        {
            var pieces = GetPieceBitboard(pieceType);
            while (pieces != 0)
            {
                pieces = Bitboards.PopLeastSignificantOne(pieces, out int piece);
                pieceTypeMailbox[piece] = pieceType;
            }
        }
        for (PieceColor pieceColor = 0; (int)pieceColor < 2; pieceColor++)
        {
            var pieces = GetColorBitboard(pieceColor);
            while (pieces != 0)
            {
                pieces = Bitboards.PopLeastSignificantOne(pieces, out int piece);
                pieceColorMailbox[piece] = pieceColor;
            }
        }
        // iterate over mailbox        
        for (int rank = 7; rank >= 0; rank--)
        {
            int consecutiveEmptySquares = 0;
            for (int i = rank * 8; i < (rank + 1) * 8; i++)
            {
                if (pieceTypeMailbox[i] is null)
                    consecutiveEmptySquares++;
                else
                {
                    if (consecutiveEmptySquares != 0)
                        fen += consecutiveEmptySquares.ToString();
                    var square = pieceTypeMailbox[i] switch
                    {
                        PieceType.Pawn => "p",
                        PieceType.Bishop => "b",
                        PieceType.Knight => "n",
                        PieceType.Rook => "r",
                        PieceType.Queen => "q",
                        PieceType.King => "k",
                    };
                    if (pieceColorMailbox[i] is null)
                        throw new Exception();
                    if (pieceColorMailbox[i] == PieceColor.White)
                        square = square.ToUpper();
                    fen += square;
                    consecutiveEmptySquares = 0;
                }
            }
            if (consecutiveEmptySquares != 0)
                fen += consecutiveEmptySquares.ToString();
            fen += (rank == 0) ? " " : "/";
        }
        // 1st (2nd) field = active color
        fen += (colorToMove == PieceColor.White ? "w" : "b") + " ";
        // 2nd (3rd) field = castling availability
        if (castlingRights == 0)
            fen += "-";
        else
        {
            if (castlingRights.HasFlag(CastlingRights.WhiteKingside))
                fen += "K";
            if (castlingRights.HasFlag(CastlingRights.WhiteQueenside))
                fen += "Q";
            if (castlingRights.HasFlag(CastlingRights.BlackKingside))
                fen += "k";
            if (castlingRights.HasFlag(CastlingRights.BlackQueenside))
                fen += "q";
        }        
        fen += " ";
        // 3rd (4th) field = en passant target square
        fen += ((enPassantFile == -1) ? "-" : Squares.GetEnPassantSquare(enPassantFile, colorToMove)) + " ";
        // 4th (5th) field = halfmove clock used for the fifty move rule
        fen += "0 ";
        // 5th (6th) field = fullmove number
        return fen + "1";
    }

    public override string ToString()
        => ToFEN();

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
            result.AddRange(WithOrWithoutPromotions(target + offset, target, false, pawnColor));
        }
        offset = pawnColor == PieceColor.White ? -16 : 16;
        while (doublePushTargets != 0)
        {
            doublePushTargets = Bitboards.PopLeastSignificantOne(doublePushTargets, out int target);
            result.Add(new(target + offset, target, PieceType.Pawn, false, Move.Flag.PawnDoublePush));
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
            var captureMask = Bitboards.GetPawnCaptures(pawnColor, from);
            var targets = captureMask & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.AddRange(WithOrWithoutPromotions(from, target, true, pawnColor));
            }
            // en passant:
            if (enPassantFile != -1)
            {
                int enPassantSquare = Squares.GetEnPassantSquare(enPassantFile, pawnColor);
                if ((captureMask & Bitboards.squareToMask[enPassantSquare]) != 0)
                    result.Add(new(from, enPassantSquare, PieceType.Pawn, true, Move.Flag.EnPassant));
            }
        }
        return result;
    }

    static Move[] WithOrWithoutPromotions(int from, int to, bool isCapture, PieceColor pawnColor)
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
                result.Add(new(from, target, PieceType.Knight, false, Move.Flag.None));
            }
            targets = Bitboards.knightMoves[from] & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target, PieceType.Knight, true, Move.Flag.None));
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
                        result.Add(new(from, blocker, pieceType, true, Move.Flag.None));
                }
                else // if there is no blocker
                    targets = ray;
                while (targets != 0)
                {
                    targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                    result.Add(new(from, target, pieceType, false, Move.Flag.None));
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
            result.Add(new(king, target, PieceType.King, false, Move.Flag.None));
        }
        var enemies = GetColorBitboard(PieceColors.Opposite(kingColor));
        targets = Bitboards.kingMoves[king] & enemies;
        while (targets != 0)
        {
            targets = Bitboards.PopLeastSignificantOne(targets, out int target);
            result.Add(new(king, target, PieceType.King, true, Move.Flag.None));
        }
        // castling kingside:
        if ((kingColor == PieceColor.White && castlingRights.HasFlag(CastlingRights.WhiteKingside)) ||
            (kingColor == PieceColor.Black && castlingRights.HasFlag(CastlingRights.BlackKingside)))
        {
            var requiredEmpty = Bitboards.GetRequiredEmptyForKingsideCastling(kingColor);
            if ((emptySquareSet & requiredEmpty) == emptySquareSet)
                result.Add(new(king, king + 2, PieceType.King, false, Move.Flag.CastlingKingside));
        }
        // castling queenside:
        if ((kingColor == PieceColor.White && castlingRights.HasFlag(CastlingRights.WhiteQueenside)) ||
            (kingColor == PieceColor.Black && castlingRights.HasFlag(CastlingRights.BlackQueenside)))
        {
            var requiredEmpty = Bitboards.GetRequiredEmptyForQueensideCastling(kingColor);
            if ((emptySquareSet & requiredEmpty) == emptySquareSet)
                result.Add(new(king, king - 2, PieceType.King, false, Move.Flag.CastlingQueenside));
        }
        //
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
        // handle capture
        var capturedPiece = PieceType.King;
        if (move.isCapture)
        {
            ulong capturedSquareMask;
            if (move.flag == Move.Flag.EnPassant)
            {
                capturedPiece = PieceType.Pawn;
                capturedSquareMask = Bitboards.Shift(colorToMove == PieceColor.White ? Direction.South : Direction.North, toMask);
                emptySquareSet |= capturedSquareMask;
            }
            else
            {
                capturedSquareMask = toMask;
                for (PieceType pieceType = 0; pieceType < PieceType.King; pieceType++)
                {
                    if ((pieceBitboards[(int)pieceType] & toMask) == 0)
                        continue;
                    capturedPiece = pieceType;
                    break;
                }
            }
            pieceBitboards[(int)capturedPiece] &= ~capturedSquareMask;
            colorBitboards[(int)PieceColors.Opposite(colorToMove)] &= ~capturedSquareMask;
            // emptySquareSet |= capturedSquareMask; // we could, but unless the move is en passant (see above) it would get instantly overriden below anyways
        }
        undoInfo = new(capturedPiece, castlingRights, enPassantFile);
        // main move element
        pieceBitboards[(int)move.piece] &= ~fromMask;
        pieceBitboards[(int)move.piece] |= toMask;
        colorBitboards[(int)colorToMove] &= ~fromMask;
        colorBitboards[(int)colorToMove] |= toMask;
        emptySquareSet |= fromMask;
        emptySquareSet &= ~toMask;
        enPassantFile = -1;
        // the remaining special move flags
        int rookFrom;
        ulong rookFromMask, rookToMask;
        switch (move.flag)
        {
            case Move.Flag.PawnDoublePush:
                enPassantFile = move.from % 8;
                break;
            case Move.Flag.CastlingKingside:
                rookFrom = colorToMove == PieceColor.White ? 7 : 63;
                rookFromMask = Bitboards.squareToMask[rookFrom];
                rookToMask = Bitboards.squareToMask[rookFrom - 2];
                pieceBitboards[(int)PieceType.Rook] &= ~rookFromMask;
                pieceBitboards[(int)PieceType.Rook] |= rookToMask;
                colorBitboards[(int)colorToMove] &= ~rookFromMask;
                colorBitboards[(int)colorToMove] |= rookToMask;
                emptySquareSet |= rookFromMask;
                emptySquareSet &= ~rookToMask;
                break;
            case Move.Flag.CastlingQueenside:
                rookFrom = colorToMove == PieceColor.White ? 0 : 56;
                rookFromMask = Bitboards.squareToMask[rookFrom];
                rookToMask = Bitboards.squareToMask[rookFrom + 3];
                pieceBitboards[(int)PieceType.Rook] &= ~rookFromMask;
                pieceBitboards[(int)PieceType.Rook] |= rookToMask;
                colorBitboards[(int)colorToMove] &= ~rookFromMask;
                colorBitboards[(int)colorToMove] |= rookToMask;
                emptySquareSet |= rookFromMask;
                emptySquareSet &= ~rookToMask;
                break;
            case Move.Flag.PromotionToQueen:
                pieceBitboards[(int)PieceType.Pawn] &= ~toMask;
                pieceBitboards[(int)PieceType.Queen] |= toMask;
                break;
            case Move.Flag.PromotionToRook:
                pieceBitboards[(int)PieceType.Pawn] &= ~toMask;
                pieceBitboards[(int)PieceType.Rook] |= toMask;
                break;
            case Move.Flag.PromotionToKnight:
                pieceBitboards[(int)PieceType.Pawn] &= ~toMask;
                pieceBitboards[(int)PieceType.Knight] |= toMask;
                break;
            case Move.Flag.PromotionToBishop:
                pieceBitboards[(int)PieceType.Pawn] &= ~toMask;
                pieceBitboards[(int)PieceType.Bishop] |= toMask;
                break;
        }
        // remove castling rights if king or rook moved
        if (move.piece == PieceType.King)
            castlingRights &= ~(colorToMove == PieceColor.White ?
                    (CastlingRights.WhiteKingside | CastlingRights.WhiteQueenside) :
                    (CastlingRights.BlackKingside | CastlingRights.BlackQueenside));
        if (move.from == 7 || move.to == 7)
            castlingRights &= ~CastlingRights.WhiteKingside;
        if (move.from == 0 || move.to == 0)
            castlingRights &= ~CastlingRights.WhiteQueenside;
        if (move.from == 63 || move.to == 63)
            castlingRights &= ~CastlingRights.BlackKingside;
        if (move.from == 56 || move.to == 56)
            castlingRights &= ~CastlingRights.BlackQueenside;
        // flip color to move
        colorToMove = PieceColors.Opposite(colorToMove);
    }

    public void UnmakeMove(Move move, UndoInfo undoInfo) 
    {
        // all elements of a Move are handled in reverse order (compared to MakeMove) - last hired first fired
        // 
        // flip color to move
        colorToMove = PieceColors.Opposite(colorToMove);
        // restore variables stored in undo info
        enPassantFile = undoInfo.previousEnPassantFile;
        castlingRights = undoInfo.previousCastlingRights;
        // cache from and to in mask form (we'll need both more than once)
        var fromMask = Bitboards.squareToMask[move.from];
        var toMask = Bitboards.squareToMask[move.to];
        // special move flags
        int rookFrom;
        ulong rookFromMask, rookToMask;
        switch (move.flag)
        {
            case Move.Flag.CastlingKingside:
                rookFrom = colorToMove == PieceColor.White ? 7 : 63;
                rookFromMask = Bitboards.squareToMask[rookFrom];
                rookToMask = Bitboards.squareToMask[rookFrom - 2];
                pieceBitboards[(int)PieceType.Rook] |= rookFromMask;
                pieceBitboards[(int)PieceType.Rook] &= ~rookToMask;
                colorBitboards[(int)colorToMove] |= rookFromMask;
                colorBitboards[(int)colorToMove] &= ~rookToMask;
                emptySquareSet &= ~rookFromMask;
                emptySquareSet |= rookToMask;
                break;
            case Move.Flag.CastlingQueenside:
                rookFrom = colorToMove == PieceColor.White ? 0 : 56;
                rookFromMask = Bitboards.squareToMask[rookFrom];
                rookToMask = Bitboards.squareToMask[rookFrom + 3];
                pieceBitboards[(int)PieceType.Rook] |= rookFromMask;
                pieceBitboards[(int)PieceType.Rook] &= ~rookToMask;
                colorBitboards[(int)colorToMove] |= rookFromMask;
                colorBitboards[(int)colorToMove] &= ~rookToMask;
                emptySquareSet &= ~rookFromMask;
                emptySquareSet |= rookToMask;
                break;
            case Move.Flag.PromotionToQueen:
                pieceBitboards[(int)PieceType.Queen] &= ~toMask;
                pieceBitboards[(int)PieceType.Pawn] |= toMask;
                break;
            case Move.Flag.PromotionToRook:
                pieceBitboards[(int)PieceType.Rook] &= ~toMask;
                pieceBitboards[(int)PieceType.Pawn] |= toMask;
                break;
            case Move.Flag.PromotionToKnight:
                pieceBitboards[(int)PieceType.Knight] &= ~toMask;
                pieceBitboards[(int)PieceType.Pawn] |= toMask;
                break;
            case Move.Flag.PromotionToBishop:
                pieceBitboards[(int)PieceType.Bishop] &= ~toMask;
                pieceBitboards[(int)PieceType.Pawn] |= toMask;
                break;
        }
        // main move element
        pieceBitboards[(int)move.piece] |= fromMask;
        pieceBitboards[(int)move.piece] &= ~toMask;
        colorBitboards[(int)colorToMove] |= fromMask;
        colorBitboards[(int)colorToMove] &= ~toMask;
        emptySquareSet &= ~fromMask;
        emptySquareSet |= toMask;
        // handle capture
        if (move.isCapture)
        {
            var capturedSquareMask = (move.flag == Move.Flag.EnPassant) ?
                Bitboards.Shift(colorToMove == PieceColor.White ? Direction.South : Direction.North, toMask) :
                toMask;
            pieceBitboards[(int)undoInfo.capturedPiece] |= capturedSquareMask;
            colorBitboards[(int)PieceColors.Opposite(colorToMove)] |= capturedSquareMask;
            emptySquareSet &= ~capturedSquareMask;
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
        var checkingKings = GetPieceOfColorBitboard(PieceType.King, enemyColor) & Bitboards.kingMoves[king];
        if (checkingKings != 0)
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

    public List<Move> GetLegalMoves()
        => FilterLegalMoves(GetPseudoLegalMoves());
}

public readonly struct UndoInfo
{
    public readonly PieceType capturedPiece;
    public readonly CastlingRights previousCastlingRights;
    public readonly int previousEnPassantFile;

    public UndoInfo(PieceType p_capturedPiece, CastlingRights p_previousCastlingRights, int p_previousEnPassantFile)
    {
        capturedPiece = p_capturedPiece;
        previousCastlingRights = p_previousCastlingRights;
        previousEnPassantFile = p_previousEnPassantFile;
    }
}