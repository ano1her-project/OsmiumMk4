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

    ulong GetPawnPushTargetBitboard(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        return (pawnColor == PieceColor.White ? Bitboards.ShiftNorth(pawns) : Bitboards.ShiftSouth(pawns)) & emptySquareSet;
    }

    public List<Move> GetPawnPushes(PieceColor pawnColor)
    {
        var targets = GetPawnPushTargetBitboard(pawnColor);
        List<Move> result = [];
        while (targets != 0)
        {
            targets = Bitboards.PopLeastSignificantOne(targets, out int target);
            result.Add(new(target - 8, target));
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
            var targets = Bitboards.pawnCaptures[(int)pawnColor][from] & enemies;
            while (targets != 0)
            {
                targets = Bitboards.PopLeastSignificantOne(targets, out int target);
                result.Add(new(from, target));
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
}