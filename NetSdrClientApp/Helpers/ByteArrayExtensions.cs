namespace NetSdrClientApp.Helpers
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] data)
            => string.Join(" ", data.Select(b => b.ToString("x2")));
    }
}
