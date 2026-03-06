namespace Osmium.Core;

readonly struct Move
{
    public readonly int from, to;

    public Move(int p_from, int p_to)
    {
        from = p_from;
        to = p_to;
    }
}
