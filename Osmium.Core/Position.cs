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

    public ulong GetPieceBitboard(PieceType pieceType)
        => pieceBitboards[(int)pieceType];

    public ulong GetColorBitboard(PieceColor pieceColor)
        => colorBitboards[(int)pieceColor];

    public ulong GetPieceOfColorBitboard(PieceType pieceType, PieceColor pieceColor)
        => pieceBitboards[(int)pieceType] & colorBitboards[(int)pieceColor];

    ulong GetPawnPushTargetBitboard(PieceColor pawnColor)
    {
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        return (pawnColor == PieceColor.White ? Bitboards.ShiftNorth(pawns) : Bitboards.ShiftSouth(pawns)) & emptySquareSet;
    }

    public List<Move> GetPawnPushes(PieceColor pawnColor)
    {        
        var pawns = GetPieceOfColorBitboard(PieceType.Pawn, pawnColor);
        var targets = (pawnColor == PieceColor.White ? Bitboards.ShiftNorth(pawns) : Bitboards.ShiftSouth(pawns)) & emptySquareSet;
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