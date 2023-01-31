using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

namespace TimeTreeShared
{
    public static class StringFunctions
    {
        public static string RemoveDiacritics(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        public static string[] SplitOnCapsOrNum(string input)
        {
            for (int pos = 0; pos < input.Length; pos++)
            {
                if (Char.IsDigit(input[pos]) || Char.IsUpper(input[pos]))
                {
                    return new string[] { input.Substring(0, pos), input.Substring(pos) };
                }
            }

            return new string[] { input };
        }

        public static List<string> SplitWithDelimiters(string input, string[] delimiters, int times)
        {
            int[] nextPosition = delimiters.Select(d => input.IndexOf(d)).ToArray();
            List<string> result = new List<string>();
            int pos = 0;

            int count = 0;
            while (true)
            {
                int firstPos = int.MaxValue;
                string delimiter = null;
                for (int i = 0; i < nextPosition.Length; i++)
                {
                    if (nextPosition[i] != -1 && nextPosition[i] < firstPos)
                    {
                        firstPos = nextPosition[i];
                        delimiter = delimiters[i];
                    }
                }
                if (firstPos != int.MaxValue && count < times)
                {
                    result.Add(input.Substring(pos, firstPos - pos));
                    result.Add(delimiter);
                    pos = firstPos + delimiter.Length;
                    for (int i = 0; i < nextPosition.Length; i++)
                    {
                        if (nextPosition[i] != -1 && nextPosition[i] < pos)
                        {
                            nextPosition[i] = input.IndexOf(delimiters[i], pos);
                        }
                    }
                    count++;
                }
                else
                {
                    result.Add(input.Substring(pos));
                    break;
                }
            }

            return result;
        }
    }
}
