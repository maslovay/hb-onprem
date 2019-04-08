using System.Collections.Generic;

namespace HBLib.Utils
{
    public static class Vowels
    {
        public static readonly Dictionary<int, char[]> VowelsDictionary;

        static Vowels()
        {
            VowelsDictionary = new Dictionary<int, char[]>
            {
                {1, new[] {'a', 'e', 'i', 'o', 'u', 'y'}},
                {2, new[] {'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я'}},
                {3, new[] {'a', 'e', 'i', 'o', 'u'}},
                {4, new[] {'a', 'e', 'i', 'o', 'u', 'y'}},
                {5, new[] {'a', 'e', 'i', 'o', 'u'}},
                {8, new[] {'a', 'ä', 'e', 'i', 'o', 'ö', 'u', 'ü'}},
                {10, new[] {'a', 'á', 'e', 'é', 'i', 'í', 'o', 'ó', 'ö', 'ő', 'u', 'ú', 'ü', 'ű'}}
            };
        }
    }
}
