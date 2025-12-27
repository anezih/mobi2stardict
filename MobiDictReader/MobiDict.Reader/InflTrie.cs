namespace MobiDict.Reader;

public class InflTrie
{
    private class Node
    {
        public readonly Dictionary<byte, Node> Children = [];
        public readonly List<byte[]> Values = [];
    }

    private readonly Node root = new();

    public void InsertReversed(ReadOnlySpan<byte> keyBytes, ReadOnlySpan<byte> valueBytes)
    {
        if (keyBytes.Length == 0) return;

        var node = root;
        for (int i = keyBytes.Length - 1; i >= 0; i--)
        {
            var b = keyBytes[i];
            if (!node.Children.TryGetValue(b, out var next))
            {
                next = new Node();
                node.Children[b] = next;
            }
            node = next;
        }
        node.Values.Add(valueBytes.ToArray());
    }

    public List<byte[]> GetInflGroups(ReadOnlySpan<byte> wordBytes, int maxResults = 256, int maxWordBytes = 2048)
    {
        List<byte[]> results = [];
        var node = root;

        for (int depth = 1; depth <= wordBytes.Length; depth++)
        {
            byte b = wordBytes[wordBytes.Length - depth];

            if (!node.Children.TryGetValue(b, out node!))
                break;

            if (node.Values.Count == 0) continue;

            int prefixLen = wordBytes.Length - depth;

            foreach (var suffix in node.Values)
            {
                if (results.Count >= maxResults) return results;

                int outLen = prefixLen + suffix.Length;
                if (outLen <= 0 || outLen > maxWordBytes) continue;

                var outBytes = new byte[outLen];
                wordBytes[..prefixLen].CopyTo(outBytes);
                Buffer.BlockCopy(suffix, 0, outBytes, prefixLen, suffix.Length);
                results.Add(outBytes);
            }
        }
        return results;
    }
}
