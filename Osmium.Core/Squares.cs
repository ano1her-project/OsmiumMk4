namespace Osmium.Core;

public static class Squares
{
    public static string ToString(int square)
    {
        int rank = square / 8;
        int file = square % 8;
        return (char)(file + 'a') + (rank + 1).ToString();
    }

    public static int FromString(string str)
    {
        var chars = str.ToCharArray();
        int rank = chars[1] - '1';
        int file = chars[0] - 'a';
        return 8 * rank + file;
    }

    public static bool IsPromotionSquare(int square, PieceColor pawnColor)
        => (pawnColor == PieceColor.White) ?
        (square >= 56) : // white
        (square < 8);   // black

    static readonly int[][] enPassantSquare = PrecalculateEnPassantSquares();

    static int[][] PrecalculateEnPassantSquares()
    {
        int[][] result = [new int[8], new int[8]];
        for (int file = 0; file < 8; file++)
        {
            result[(int)PieceColor.White][file] = 40 + file;
            result[(int)PieceColor.Black][file] = 16 + file;
        }
        return result;
    }

    public static int GetEnPassantSquare(int enPassantFile, PieceColor colorToMove)
        => enPassantSquare[(int)colorToMove][enPassantFile];
}
