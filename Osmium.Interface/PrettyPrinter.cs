using Osmium.Core;
using System.Numerics;
using static Osmium.Interface.PrettyPrinter;

namespace Osmium.Interface;

public static class PrettyPrinter
{
    public enum BackgroundOptions
    {
        Simple,
        Shaded,
        ShadedInverted
    }

    public static void Print(ulong bitboard)
    {
        for (int rank = 7; rank >= 0; rank--)
        {
            string s = (rank + 1) + " ";
            for (int i = rank * 8; i < (rank + 1) * 8; i++)
                s += (((1ul << i) & bitboard) == 0) ? "0 " : "1 ";
            Console.WriteLine(s);
        }
        Console.WriteLine("  a b c d e f g h ");
    }

    public static void PrintIndividualBitboards(Position position)
    {
        for (PieceType piece = 0; (int)piece < 6; piece++)
        {
            Console.WriteLine(piece.ToString() + "s");
            Print(position.GetPieceBitboard(piece));
        }
        for (PieceColor color = 0; (int)color < 2; color++)
        {
            Console.WriteLine(color.ToString());
            Print(position.GetColorBitboard(color));
        }
        Console.WriteLine("Empty");
        Print(position.GetEmptySquareSet());
    }

    public static void Print(Position position, BackgroundOptions backgroundOptions, int[] markings)
    {
        PieceType?[] pieceTypeMailbox = new PieceType?[64];
        PieceColor?[] pieceColorMailbox = new PieceColor?[64];
        // build mailbox
        for (PieceType pieceType = 0; (int)pieceType < 6; pieceType++)
        {
            var pieces = position.GetPieceBitboard(pieceType);
            while (pieces != 0)
            {
                pieces = Bitboards.PopLeastSignificantOne(pieces, out int piece);
                pieceTypeMailbox[piece] = pieceType;
            }
        }
        for (PieceColor pieceColor = 0; (int)pieceColor < 2; pieceColor++)
        {
            var pieces = position.GetColorBitboard(pieceColor);
            while (pieces != 0)
            {
                pieces = Bitboards.PopLeastSignificantOne(pieces, out int piece);
                pieceColorMailbox[piece] = pieceColor;
            }
        }
        // print mailbox
        for (int rank = 7; rank >= 0; rank--)
        {
            string s = (rank + 1) + " ";
            for (int i = rank * 8; i < (rank + 1) * 8; i++)
            {
                string square;
                if (pieceTypeMailbox[i] is null)
                    square = markings.Contains(i) ? "()" : backgroundOptions switch
                    {
                        BackgroundOptions.Simple => ". ",
                        BackgroundOptions.Shaded => GetSquareShadeString(i, false),
                        BackgroundOptions.ShadedInverted => GetSquareShadeString(i, true),
                        _ => throw new Exception()
                    };
                else
                {
                    square = pieceTypeMailbox[i] switch
                    {
                        PieceType.Pawn => "p",
                        PieceType.Bishop => "b",
                        PieceType.Knight => "n",
                        PieceType.Rook => "r",
                        PieceType.Queen => "q",
                        PieceType.King => "k",
                    } + (markings.Contains(i) ? ")" : " ");
                    if (pieceColorMailbox[i] is null)
                        throw new Exception();
                    if (pieceColorMailbox[i] == PieceColor.White)
                        square = square.ToUpper();
                }
                s += square;
            }
            Console.WriteLine(s);
        }
        Console.WriteLine("  a b c d e f g h ");
    }

    public static void Print(Position position)
        => Print(position, BackgroundOptions.ShadedInverted, []);

    public static void Print(Position position, BackgroundOptions backgroundOptions)
        => Print(position, backgroundOptions, []);

    public static void Print(Position position, int[] markings)
        => Print(position, BackgroundOptions.ShadedInverted, markings);

    static bool IsSquareWhite(int square)
        => ((square / 8) % 2 == 0) ? 
        (square % 2 == 1) :
        (square % 2 == 0);
    
    static string GetSquareShadeString(int square, bool invert)
        => (IsSquareWhite(square) ^ invert) ? "░░" : "▒▒";
}
