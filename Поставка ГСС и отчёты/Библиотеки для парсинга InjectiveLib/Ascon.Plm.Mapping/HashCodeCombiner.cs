namespace Ascon.Plm.Mapping
{
    internal static class HashCodeCombiner
    {
        public static int Combine(int h1, int h2) =>
            (h1 << 5) + h1 ^ h2;

        public static int Combine(int h1, int h2, int h3) =>
            Combine(Combine(h1, h2), h3);

        public static int Combine(int h1, int h2, int h3, int h4) =>
            Combine(Combine(h1, h2, h3), h4);

        public static int Combine(int h1, int h2, int h3, int h4, int h5) =>
            Combine(Combine(h1, h2, h3, h4), h5);

        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6) =>
            Combine(Combine(h1, h2, h3, h4, h5), h6);

        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6, int h7) =>
            Combine(Combine(h1, h2, h3, h4, h5, h6), h7);

        public static int Combine(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8) =>
            Combine(Combine(h1, h2, h3, h4, h5, h6, h7), h8);

        public static int Combine(IEnumerable<int> hashValues)
        {
            using var enumerator = hashValues.GetEnumerator();

            if (!enumerator.MoveNext())
                return 0;

            int hash = enumerator.Current;

            while (enumerator.MoveNext())
            {
                hash = (hash << 5) + hash ^ enumerator.Current;
            }

            return hash;
        }

        public static int Combine(int[] hashValues)
        {
            if (hashValues.Length == 0)
                return 0;

            int hash = hashValues[0];

            for (var i = 1; i < hashValues.Length; i++)
                hash = (hash << 5) + hash ^ hashValues[i];

            return hash;
        }
    }
}
