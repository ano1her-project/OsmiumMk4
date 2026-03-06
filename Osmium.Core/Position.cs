namespace Osmium.Core;

public class Position
{
    ulong[] pieceBitboards = new ulong[12];

    public ulong GetPieceBitboard(PieceType pieceType)
        => pieceBitboards[(int)pieceType];
}