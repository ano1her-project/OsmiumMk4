using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Osmium.Core
{
    public readonly struct Vector2
    {
        public readonly int file, rank; // which file = x, which rank = y

        public Vector2(int p_file, int p_rank)
        {
            file = p_file;
            rank = p_rank;
        }

        public static readonly Vector2 up = new(0, 1);
        public static readonly Vector2 right = new(1, 0);
        public static readonly Vector2 down = new(0, -1);
        public static readonly Vector2 left = new(-1, 0);
        public static readonly Vector2 one = new(1, 1);

        public static readonly Vector2[] orthogonalDirections = [up, right, down, left];
        public static readonly Vector2[] diagonalDirections = [one, right + down, -one, left + up];
        public static readonly Vector2[] allDirections = [up, one, right, right + down, down, -one, left, left + up];
        public static readonly Vector2[] hippogonalDirections = [new(1, 2), new(2, 1), new(2, -1), new(1, -2), new(-1, -2), new(-2, -1), new(-2, 1), new(-1, 2)];

        public override bool Equals(object? obj)
            => obj is Vector2 v && this == v;

        public static bool operator ==(Vector2 a, Vector2 b)
            => a.file == b.file && a.rank == b.rank;

        public static bool operator !=(Vector2 a, Vector2 b)
            => !(a == b);

        public override int GetHashCode()
            => HashCode.Combine(rank, file);

        public static Vector2 operator -(Vector2 v)
            => new(-v.file, -v.rank);

        public static Vector2 operator +(Vector2 a, Vector2 b)
            => new(a.file + b.file, a.rank + b.rank);

        public static Vector2 operator -(Vector2 a, Vector2 b)
            => new(a.file - b.file, a.rank - b.rank);

        public static Vector2 FromString(string str) // assuming a string in the format of, for instance, e4
            => new(str[0] - 'a', str[1] - '0' - 1);

        public override string ToString()
            => (char)('a' + file) + (rank + 1).ToString();

        public bool IsInBounds()
            => rank >= 0 && rank < 8 && file >= 0 && file < 8;
    }

    public readonly struct Piece
    {
        public readonly Type type;
        public readonly bool isWhite;

        public enum Type
        {
            Pawn,
            Bishop,
            Knight,
            Rook,
            Queen,
            King
        }

        public Piece(Type p_type, bool p_isWhite)
        {
            type = p_type;
            isWhite = p_isWhite;
        }      
        
        public static Piece FromChar(char ch)
        {
            bool isWhite = char.IsUpper(ch);
            return char.ToLower(ch) switch
            {
                'p' => new(Type.Pawn, isWhite),
                'b' => new(Type.Bishop, isWhite),
                'n' => new(Type.Knight, isWhite),
                'r' => new(Type.Rook, isWhite),
                'q' => new(Type.Queen, isWhite),
                'k' => new(Type.King, isWhite),
                _ => throw new Exception()
            };
        }

        public override bool Equals(object? obj)
            => obj is Piece piece && this == piece;

        public static bool operator ==(Piece a, Piece b)
            => a.type == b.type && a.isWhite == b.isWhite;

        public static bool operator !=(Piece a, Piece b)
            => !(a == b);

        public override int GetHashCode()
            => HashCode.Combine(type, isWhite);

        public char ToChar()
        {
            char ch = type switch
            {
                Type.Pawn => 'p',
                Type.Bishop => 'b',
                Type.Knight => 'n',
                Type.Rook => 'r',
                Type.Queen => 'q',
                Type.King => 'k',
                _ => throw new Exception()
            };
            return isWhite ? char.ToUpper(ch) : ch;
        }

        public override string ToString()
            => ToChar().ToString();

        public Piece GetInverted() // only used in the PrettyPrinter when inverting colors
            => new(type, !isWhite); 
    }

    public class Position
    {
        public Piece?[,] board = new Piece?[8, 8];
        Vector2 whiteKing, blackKing;
        public bool whiteToMove;
        CastlingAvailability castlingAvailability;
        Vector2? enPassantSquare;
        int halfmoveClock;
        int fullmoves;

        [Flags]
        public enum CastlingAvailability : byte
        {
            None = 0,
            WhiteKingside = 1,
            WhiteQueenside = 2,
            BlackKingside = 4,
            BlackQueenside = 8
        }

        public static CastlingAvailability CastlingRightsFromString(string str)
        {
            var output = CastlingAvailability.None;
            if (str == "-")
                return output;
            foreach (var ch in str.ToCharArray())
            {
                output |= ch switch
                {
                    'K' => CastlingAvailability.WhiteKingside,
                    'Q' => CastlingAvailability.WhiteQueenside,
                    'k' => CastlingAvailability.BlackKingside,
                    'q' => CastlingAvailability.BlackQueenside,
                    _ => throw new Exception(),
                };
            }
            return output;
        }

        public static string CastlingRightsToString(CastlingAvailability castlingAvailability)
        {
            string[] options = ["-", "K", "Q", "KQ", "k", "Kk", "Qk", "KQk", "q", "Kq", "Qq", "KQq", "kq", "Kkq", "Qkq", "KQkq"];
            return options[(int)castlingAvailability];
        }

        public Position(Piece?[,] p_board, Vector2 p_whiteKing, Vector2 p_blackKing, bool p_whiteToMove, CastlingAvailability p_castlingAvailability, Vector2? p_enPassantSquare, int p_halfmoveClock, int p_fullmoves)
        {
            board = p_board;
            whiteKing = p_whiteKing;
            blackKing = p_blackKing;
            whiteToMove = p_whiteToMove;
            castlingAvailability = p_castlingAvailability;
            enPassantSquare = p_enPassantSquare;
            halfmoveClock = p_halfmoveClock;
            fullmoves = p_fullmoves;
        }

        public static readonly Position startingPosition = FromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        public static Position FromFEN(string fen)
        {
            var fields = fen.Split(' ');
            // 0th (1st) field = piece placement data
            Piece?[,] board = new Piece?[8, 8];
            Vector2 whiteKing = -Vector2.one;
            Vector2 blackKing = -Vector2.one;
            var ranks = fields[0].Split('/');
            for (int rank = 7; rank >= 0; rank--)
            {
                int file = 0;
                foreach (var ch in ranks[7 - rank].ToCharArray())
                {
                    if (char.IsDigit(ch))
                        file += ch - '0'; // effectively converts ch to an int
                    else
                    {
                        board[rank, file] = Piece.FromChar(ch);
                        if (ch == 'K')
                            whiteKing = new(file, rank);
                        else if (ch == 'k')
                            blackKing = new(file, rank);
                        file++;
                    }
                }
            }
            if (whiteKing == -Vector2.one || blackKing == -Vector2.one)
                throw new Exception();
            // 1st (2nd) field = active color
            bool whiteToMove = fields[1] == "w";
            // 2nd (3rd) field = castling availability
            var castlingAvailability = CastlingRightsFromString(fields[2]);
            // 3rd (4th) field = en passant target square
            Vector2? enPassantSquare = fields[3] == "-" ? null : Vector2.FromString(fields[3]);
            // 4th (5th) field = halfmove clock used for the fifty move rule
            int halfmoveClock = int.Parse(fields[4]);
            // 5th (6th) field = fullmove number
            int fullmoves = int.Parse(fields[5]);
            //
            return new(board, whiteKing, blackKing, whiteToMove, castlingAvailability, enPassantSquare, halfmoveClock, fullmoves);
        }

        public Position DeepCopy()
        {
            Piece?[,] newBoard = new Piece?[8, 8];
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                    newBoard[rank, file] = GetPiece(rank, file);
            }
            Vector2 newWhiteKing = whiteKing;
            Vector2 newBlackKing = whiteKing;
            bool newWhiteToMove = whiteToMove;
            var newCastlingAvailability = castlingAvailability;
            Vector2? newEnPassantSquare = enPassantSquare is null ? null : enPassantSquare;
            int newHalfmoveClock = halfmoveClock;
            int newFullmoves = fullmoves;
            return new(newBoard, newWhiteKing, newBlackKing, newWhiteToMove, newCastlingAvailability, newEnPassantSquare, newHalfmoveClock, newFullmoves);
        }

        public override string ToString()
            => ToFEN();

        public string ToFEN()
        {
            string output = "";
            // 0th (1st) field = piece placement data
            for (int rank = 7; rank >= 0; rank--)
            {
                int consecutiveEmptySquares = 0;
                for (int file = 0; file < 8; file++)
                {
                    if (GetPiece(rank, file) is null)
                        consecutiveEmptySquares++;
                    else
                    {
                        if (consecutiveEmptySquares != 0)
                            output += consecutiveEmptySquares.ToString();
                        output += GetPiece(rank, file)?.ToString();
                        consecutiveEmptySquares = 0;
                    }
                }
                output += consecutiveEmptySquares == 0 ? "" : consecutiveEmptySquares.ToString();
                output += rank == 0 ? " " : "/";
            }
            // 1st (2nd) field = active color
            output += (whiteToMove ? "w" : "b") + " ";
            // 2nd (3rd) field = castling availability
            output += CastlingRightsToString(castlingAvailability) + " ";
            // 3rd (4th) field = en passant target square
            output += (enPassantSquare is null ? "-" : enPassantSquare.ToString()) + " ";
            // 4th (5th) field = halfmove clock used for the fifty move rule
            output += halfmoveClock.ToString() + " ";
            // 5th (6th) field = fullmove number
            output += fullmoves.ToString();
            //
            return output;
        }

        public Piece? GetPiece(int rank, int file)
            => board[rank, file];

        public Piece? GetPiece(Vector2 v)
            => GetPiece(v.rank, v.file);

        public void SetPiece(int rank, int file, Piece? piece)
            => board[rank, file] = piece;

        public void SetPiece(Vector2 v, Piece? piece)
            => SetPiece(v.rank, v.file, piece);

        public void MakeMove(Move move, out UndoInfo undoInfo)
        {
            // record undo info before doing anything else
            Piece? captured;
            if (move.flag == Move.Flag.EnPassant)
                captured = GetPiece(move.from.rank, move.to.file);
            else
                captured = GetPiece(move.to);
            Vector2? previousEnPassantSquare = enPassantSquare;
            CastlingAvailability previousCastlingAvailability = castlingAvailability;
            // standard move
            var piece = GetPiece(move.from) ?? throw new Exception();
            SetPiece(move.from, null);
            SetPiece(move.to, piece);
            whiteToMove = !whiteToMove;
            enPassantSquare = null;
            // set castling availability
            if (move.from == new Vector2(7, 0) || move.to == new Vector2(7, 0))
                castlingAvailability &= ~CastlingAvailability.WhiteKingside;
            if (move.from == new Vector2(0, 0) || move.to == new Vector2(0, 0))
                castlingAvailability &= ~CastlingAvailability.WhiteQueenside;
            if (move.from == new Vector2(7, 7) || move.to == new Vector2(7, 7))
                castlingAvailability &= ~CastlingAvailability.BlackKingside;
            if (move.from == new Vector2(0, 7) || move.to == new Vector2(0, 7))
                castlingAvailability &= ~CastlingAvailability.BlackQueenside;
            if (piece.type == Piece.Type.King && piece.isWhite)
            {
                whiteKing = move.to;
                castlingAvailability &= ~(CastlingAvailability.WhiteKingside | CastlingAvailability.WhiteQueenside);
            }
            if (piece.type == Piece.Type.King && !piece.isWhite)
            {
                blackKing = move.to;
                castlingAvailability &= ~(CastlingAvailability.BlackKingside | CastlingAvailability.BlackQueenside);
            }
            // special flags
            int rank;
            switch (move.flag)
            {
                case Move.Flag.None:
                    break;
                case Move.Flag.CastlingKingside:
                    rank = piece.isWhite ? 0 : 7;
                    SetPiece(rank, 7, null);
                    SetPiece(rank, 5, new(Piece.Type.Rook, piece.isWhite));
                    castlingAvailability &= piece.isWhite ? ~CastlingAvailability.WhiteKingside : ~CastlingAvailability.BlackKingside;
                    castlingAvailability &= piece.isWhite ? ~CastlingAvailability.WhiteQueenside : ~CastlingAvailability.BlackQueenside;
                    break;
                case Move.Flag.CastlingQueenside:
                    rank = piece.isWhite ? 0 : 7;
                    SetPiece(rank, 0, null);
                    SetPiece(rank, 3, new(Piece.Type.Rook, piece.isWhite));
                    castlingAvailability &= piece.isWhite ? ~CastlingAvailability.WhiteKingside : ~CastlingAvailability.BlackKingside;
                    castlingAvailability &= piece.isWhite ? ~CastlingAvailability.WhiteQueenside : ~CastlingAvailability.BlackQueenside;
                    break;
                case Move.Flag.TwoSquarePawnPush:
                    enPassantSquare = move.from + (piece.isWhite ? Vector2.up : Vector2.down);
                    break;
                case Move.Flag.EnPassant:
                    SetPiece(move.to + (piece.isWhite ? Vector2.down : Vector2.up), null);
                    break;
                case Move.Flag.PromotionToQueen:
                    SetPiece(move.to, new(Piece.Type.Queen, piece.isWhite));
                    break;
                case Move.Flag.PromotionToRook:
                    SetPiece(move.to, new(Piece.Type.Rook, piece.isWhite));
                    break;
                case Move.Flag.PromotionToKnight:
                    SetPiece(move.to, new(Piece.Type.Knight, piece.isWhite));
                    break;
                case Move.Flag.PromotionToBishop:
                    SetPiece(move.to, new(Piece.Type.Bishop, piece.isWhite));
                    break;
                default:
                    throw new Exception();
            }
            undoInfo = new(captured, previousEnPassantSquare, previousCastlingAvailability);
        }

        public void UnmakeMove(Move move, UndoInfo undoInfo)
        {
            var piece = GetPiece(move.to) ?? throw new InvalidOperationException();
            whiteToMove = !whiteToMove;
            enPassantSquare = undoInfo.previousEnPassantSquare;
            castlingAvailability = undoInfo.previousCastlingAvailability;
            if (piece.type == Piece.Type.King && piece.isWhite)
                whiteKing = move.from;
            if (piece.type == Piece.Type.King && !piece.isWhite)
                blackKing = move.from;
            int rank;
            switch (move.flag)
            {
                case Move.Flag.None:
                    SetPiece(move.from, piece);
                    SetPiece(move.to, undoInfo.captured);
                    break;
                case Move.Flag.CastlingKingside:
                    rank = piece.isWhite ? 0 : 7;
                    SetPiece(move.from, piece);
                    SetPiece(move.to, null);
                    SetPiece(rank, 5, null);
                    SetPiece(rank, 7, new(Piece.Type.Rook, piece.isWhite));
                    break;
                case Move.Flag.CastlingQueenside:
                    rank = piece.isWhite ? 0 : 7;
                    SetPiece(move.from, piece);
                    SetPiece(move.to, null);
                    SetPiece(rank, 3, null);
                    SetPiece(rank, 0, new(Piece.Type.Rook, piece.isWhite));
                    break;
                case Move.Flag.TwoSquarePawnPush:
                    SetPiece(move.from, piece);
                    SetPiece(move.to, null);
                    break;
                case Move.Flag.EnPassant:
                    SetPiece(move.from, piece);
                    SetPiece(move.to, null);
                    SetPiece(move.from.rank, move.to.file, undoInfo.captured);
                    break;
                case Move.Flag.PromotionToQueen:
                case Move.Flag.PromotionToRook:
                case Move.Flag.PromotionToKnight:
                case Move.Flag.PromotionToBishop:
                    SetPiece(move.from, new(Piece.Type.Pawn, piece.isWhite));
                    SetPiece(move.to, undoInfo.captured);
                    break;
                default:
                    throw new Exception();
            }
        }

        // move legality and move generation:

        public Piece? Raycast(Vector2 origin, Vector2 direction)
        {
            Vector2 pos = origin;
            while (true)
            {
                pos += direction;
                if (!pos.IsInBounds())
                    return null;
                var piece = GetPiece(pos);
                if (piece is not null)
                    return piece;
            }
        }

        public Vector2? RaycastForHitpoint(Vector2 origin, Vector2 direction)
        {
            Vector2 pos = origin;
            while (true)
            {
                pos += direction;
                if (!pos.IsInBounds())
                    return null;
                var piece = GetPiece(pos);
                if (piece is not null)
                    return pos;
            }
        }

        public bool IsKingInCheck(bool kingColor)
        {
            // find king
            var king = kingColor ? whiteKing : blackKing;
            // check for attacking pawns
            int forward = kingColor ? 1 : -1;
            for (int fileDelta = -1; fileDelta <= 1; fileDelta += 2 /* = 1 */)
            {
                int rank = king.rank + forward;
                if (rank >= 8 || rank < 0)
                    continue;
                int file = king.file + fileDelta;
                if (file >= 8 || file < 0)
                    continue;
                var piece = GetPiece(rank, file);
                if (piece is not null && piece?.isWhite != kingColor && piece?.type == Piece.Type.Pawn)
                    return true;
            }
            // check for attacking knights
            for (int i = 0; i < 8; i++)
            {
                var square = king + Vector2.hippogonalDirections[i];
                if (!square.IsInBounds())
                    continue;
                var piece = GetPiece(square);
                if (piece is not null && piece?.type == Piece.Type.Knight && piece?.isWhite != kingColor)
                    return true;
            }
            // check for attacking kings
            for (int i = 0; i < 8; i++)
            {
                var square = king + Vector2.allDirections[i];
                if (!square.IsInBounds())
                    continue;
                var piece = GetPiece(square);
                if (piece is not null && (piece?.type == Piece.Type.King || piece?.type == Piece.Type.Queen) && piece?.isWhite != kingColor)
                    return true;
            }
            // most expensive for last..
            // check for attacking rooks, queens
            for (int i = 0; i < 4; i++)
            {
                var hit = Raycast(king, Vector2.orthogonalDirections[i]);
                if (hit is null || hit?.isWhite == kingColor)
                    continue;
                if (hit?.type == Piece.Type.Rook || hit?.type == Piece.Type.Queen)
                    return true;
            }
            // check for attacking bishops, queens
            for (int i = 0; i < 4; i++)
            {
                var hit = Raycast(king, Vector2.diagonalDirections[i]);
                if (hit is null || hit?.isWhite == kingColor)
                    continue;
                if (hit?.type == Piece.Type.Bishop || hit?.type == Piece.Type.Queen)
                    return true;
            }
            return false;
        }

        Vector2? FindPiece(Piece target)
        {
            Vector2 result;
            int startRank = target.isWhite ? 0 : 7;
            int rankIncrement = target.isWhite ? 1 : -1;
            for (int rank = startRank; rank >= 0 && rank < 8; rank += rankIncrement)
            {
                for (int file = 0; file < 8; file++)
                {
                    Piece? piece = GetPiece(rank, file);
                    if (piece is null || piece != target)
                        continue;
                    result = new(file, rank);
                    return result;
                }
            }
            return null;
        }

        List<Move> GetMovesAlongRay(Vector2 origin, Vector2 direction, bool attackerColor)
        {
            List<Move> result = [];
            Vector2 pos = origin;
            while (true)
            {
                pos += direction;
                if (!pos.IsInBounds())
                    return result;
                var piece = GetPiece(pos);
                if (piece is null || piece?.isWhite != attackerColor)
                    result.Add(new(origin, pos));
                if (piece is not null)
                    return result;
            }
        }

        public List<Move> GetAllLegalMoves()
        {
            List<Move> result = [];
            // get all moves not acknowledging checks
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    var piece = GetPiece(rank, file);
                    if (piece is null || piece?.isWhite != whiteToMove)
                        continue;
                    result.AddRange(GetPieceMoves((Piece)piece, new(file, rank)));
                }
            }
            // filter out every move that'd leave the king in check
            return FilterLegalMoves(result);
        }

        public List<Move> GetPieceMoves(Piece piece, Vector2 v)
        {
            return piece.type switch
            {
                Piece.Type.Pawn => GetPawnMoves(v, piece.isWhite),
                Piece.Type.Bishop => GetRiderMoves(v, piece.isWhite, Vector2.diagonalDirections),
                Piece.Type.Knight => GetLeaperMoves(v, piece.isWhite, Vector2.hippogonalDirections),
                Piece.Type.Rook => GetRiderMoves(v, piece.isWhite, Vector2.orthogonalDirections),
                Piece.Type.Queen => GetRiderMoves(v, piece.isWhite, Vector2.allDirections),
                Piece.Type.King => GetKingMoves(v, piece.isWhite),
                _ => throw new Exception()
            };
        }

        public List<Move> FilterLegalMoves(List<Move> moves) // filter out moves that'd leave the king in check
        {
            List<Move> result = [];
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                MakeMove(moves[i], out var undoInfo);
                if (!IsKingInCheck(!whiteToMove)) // !whiteToMove cuz it has flipped
                    result.Add(moves[i]);
                UnmakeMove(moves[i], undoInfo);
            }
            return result;
        }

        List<Move> GetPawnMoves(Vector2 pawn, bool pawnColor)
        {
            List<Move> result = [];
            Vector2 forward = pawnColor ? Vector2.up : Vector2.down;
            bool isAboutToPromote = pawn.rank == (pawnColor ? 6 : 1);
            // push
            var oneStepForward = pawn + forward;
            if (GetPiece(oneStepForward) is null)
            {
                // push 1 square forward
                    result.AddRange(MoveWithOrWithoutPromotions(pawn, oneStepForward, isAboutToPromote));
                // push 2 squares forward
                var twoStepsForward = oneStepForward + forward;
                if (pawn.rank == (pawnColor ? 1 : 6) && GetPiece(twoStepsForward) is null)
                    result.Add(new(pawn, twoStepsForward, Move.Flag.TwoSquarePawnPush)); // cannot land on the last rank (and promote)
            }
            // captures
            Vector2 leftCapture = oneStepForward + Vector2.left;
            if (leftCapture.file > 0) // is in bounds
            {
                var leftCapturePiece = GetPiece(leftCapture);
                if (leftCapturePiece is not null && leftCapturePiece?.isWhite != pawnColor)
                    result.AddRange(MoveWithOrWithoutPromotions(pawn, leftCapture, isAboutToPromote));
                else if (enPassantSquare is not null && enPassantSquare == leftCapture)
                    result.Add(new(pawn, leftCapture, Move.Flag.EnPassant)); // cannot capture en passant and land on the last rank
            }
            Vector2 rightCapture = oneStepForward + Vector2.right;
            if (rightCapture.file <= 8) // is in bounds
            {
                var rightCapturePiece = GetPiece(rightCapture);
                if (rightCapturePiece is not null && rightCapturePiece?.isWhite != pawnColor)
                    result.AddRange(MoveWithOrWithoutPromotions(pawn, rightCapture, isAboutToPromote));
                else if (enPassantSquare is not null && enPassantSquare == rightCapture)
                    result.Add(new(pawn, rightCapture, Move.Flag.EnPassant)); // cannot capture en passant and land on the last rank
            }
            //
            return result;
        }

        Move[] MoveWithOrWithoutPromotions(Vector2 pawn, Vector2 destination, bool isAboutToPromote)
            => isAboutToPromote ? [
                new(pawn, destination, Move.Flag.PromotionToQueen), 
                new(pawn, destination, Move.Flag.PromotionToRook), 
                new(pawn, destination, Move.Flag.PromotionToKnight), 
                new(pawn, destination, Move.Flag.PromotionToBishop)] 
            : [new(pawn, destination)];

        List<Move> GetRiderMoves(Vector2 rider, bool riderColor, Vector2[] directions) // generalized method for rooks, bishops and queens
        {
            List<Move> result = [];
            foreach (var direction in directions)
                result.AddRange(GetMovesAlongRay(rider, direction, riderColor));
            return result;
        }

        List<Move> GetLeaperMoves(Vector2 leaper, bool leaperColor, Vector2[] directions) // generalized method for knights and kings
        {
            List<Move> result = [];
            foreach (var direction in directions)
            {
                if (!(leaper + direction).IsInBounds())
                    continue;
                var piece = GetPiece(leaper + direction);
                if (piece is null || piece?.isWhite != leaperColor)
                    result.Add(new(leaper, leaper + direction));
            }
            return result;
        }

        List<Move> GetKingMoves(Vector2 king, bool kingColor) // GetLeaperMoves() + castling
        {
            // leap moves
            List<Move> result = [];
            result.AddRange(GetLeaperMoves(king, kingColor, Vector2.allDirections));
            // castling kingside
            if (kingColor && castlingAvailability.HasFlag(CastlingAvailability.WhiteKingside) && GetPiece(0, 5) is null && GetPiece(0, 6) is null)
                result.Add(new(king, new(6, 0), Move.Flag.CastlingKingside));
            else if (!kingColor && castlingAvailability.HasFlag(CastlingAvailability.BlackKingside) && GetPiece(7, 5) is null && GetPiece(7, 6) is null)
                result.Add(new(king, new(6, 7), Move.Flag.CastlingKingside));
            // castling queenside
            if (kingColor && castlingAvailability.HasFlag(CastlingAvailability.WhiteQueenside) && GetPiece(0, 3) is null && GetPiece(0, 2) is null && GetPiece(0, 1) is null)
                result.Add(new(king, new(2, 0), Move.Flag.CastlingQueenside));
            else if (!kingColor && castlingAvailability.HasFlag(CastlingAvailability.BlackQueenside) && GetPiece(7, 3) is null && GetPiece(7, 2) is null && GetPiece(7, 1) is null)
                result.Add(new(king, new(2, 7), Move.Flag.CastlingQueenside));
            //
            return result;
        }
    }

    public readonly struct Move
    {
        public readonly Vector2 from, to;
        public readonly Flag flag;

        public Move(Vector2 p_from, Vector2 p_to, Flag p_flag)
        {
            from = p_from;
            to = p_to;
            flag = p_flag;
        }

        public Move(Vector2 p_from, Vector2 p_to) : this(p_from, p_to, Flag.None) { }

        public enum Flag
        {
            None,
            CastlingKingside,
            CastlingQueenside,
            TwoSquarePawnPush,
            EnPassant,
            PromotionToQueen,
            PromotionToRook,
            PromotionToKnight,
            PromotionToBishop
        }

        public override string ToString()
            => $"{from}{to}" + (flag != Flag.None ? "*" : "");
    }

    public readonly struct UndoInfo
    {
        public readonly Piece? captured;
        public readonly Vector2? previousEnPassantSquare;
        public readonly Position.CastlingAvailability previousCastlingAvailability;

        public UndoInfo(Piece? p_captured, Vector2? p_previousEnPassantSquare, Position.CastlingAvailability p_previousCastlingAvailability)
        {
            captured = p_captured;
            previousEnPassantSquare = p_previousEnPassantSquare;
            previousCastlingAvailability = p_previousCastlingAvailability;            
        }
    }
}
