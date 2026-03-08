namespace Osmium.Core;

public class Squares
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
}
