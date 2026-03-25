using System;
using System.Text;

namespace ThirdPersonController
{
    public static class LocalizationPseudoLocalizer
    {
        public static string PseudoLocalize(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return "[[~]]";
            }

            var builder = new StringBuilder(source.Length + 16);
            builder.Append("[[");
            int transformedLetters = 0;

            for (int i = 0; i < source.Length; i++)
            {
                char current = source[i];

                if (current == '{')
                {
                    int end = source.IndexOf('}', i + 1);
                    if (end > i)
                    {
                        builder.Append(source, i, end - i + 1);
                        i = end;
                        continue;
                    }
                }

                if (current == '<')
                {
                    int end = source.IndexOf('>', i + 1);
                    if (end > i)
                    {
                        builder.Append(source, i, end - i + 1);
                        i = end;
                        continue;
                    }
                }

                if (char.IsLetter(current))
                {
                    builder.Append(TransformLetter(current));
                    transformedLetters++;
                }
                else
                {
                    builder.Append(current);
                }
            }

            int padCount = Math.Max(2, (int)Math.Ceiling(transformedLetters * 0.25f));
            builder.Append('~', padCount);
            builder.Append("]]");
            return builder.ToString();
        }

        private static char TransformLetter(char value)
        {
            char lower = char.ToLowerInvariant(value);
            switch (lower)
            {
                case 'a':
                    return '4';
                case 'e':
                    return '3';
                case 'i':
                    return '1';
                case 'o':
                    return '0';
                default:
                    return char.IsLower(value)
                        ? char.ToUpperInvariant(value)
                        : char.ToLowerInvariant(value);
            }
        }
    }
}
