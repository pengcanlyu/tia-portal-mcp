namespace TiaMcpServer.Contracts.Extensions;

public static class HardwareEditingRules
{
    public static List<(T Item, string[] Path)> ContainerMembers<T>(IEnumerable<T> roots,
        Func<T, IEnumerable<T>> children, Func<T, string> name, Func<T, bool> isInContainer)
    {
        var result = new List<(T, string[])>();
        var visited = 0;
        void Visit(IEnumerable<T> items, string[] parent)
        {
            foreach (var item in items)
            {
                if (++visited > 4096 || parent.Length >= 32)
                    throw new InvalidOperationException("Hardware hierarchy exceeds bounded placement discovery.");
                var path = parent.Concat(new[] { name(item) }).ToArray();
                if (isInContainer(item)) result.Add((item, path));
                Visit(children(item), path);
            }
        }
        Visit(roots, Array.Empty<string>());
        return result;
    }

    public static bool Overlaps(int start, int lengthBits, int otherStart, int otherLengthBits)
    {
        if (start < 0 || lengthBits <= 0 || otherStart < 0 || otherLengthBits <= 0)
            throw new ArgumentOutOfRangeException("Address ranges must be nonnegative with positive lengths.");
        // Openness uses byte start offsets but bit lengths (AI8 x 16 bits reports Length=128).
        return (long)start * 8 < (long)otherStart * 8 + otherLengthBits
            && (long)otherStart * 8 < (long)start * 8 + lengthBits;
    }
}
