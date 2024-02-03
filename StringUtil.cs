using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srch
{
    static class StringUtil /** string utilities */
    {
        public static int GetLineNumberFromIndex(string text, int index)
        {
            int countNewLine = 1;
            if (text.Length > 1)
            {
                for (int i = 0; i < index; i++)
                {
                    if (text[i] == LanguageConventions.newLine[1])
                    { // text[i + 1]
                        countNewLine++;
                    }
                }
            }
            return countNewLine;
        }
        public static int IndexOf(string searchString, string pattern)
        { //returns the index of a pattern inside searchString
            int length = pattern.Length;
            int idx = searchString.IndexOf(pattern[0]);
            do
            {
                if (idx == -1 || ((idx + pattern.Length) > searchString.Length)) return -1;
                for (int i = 0; i < length; i++)
                {
                    if (pattern[i] != searchString[idx + i])
                    {
                        /* pattern mismatch */
                        idx = searchString.IndexOf(pattern[0], idx + i + 1);
                        break;
                    }
                    else
                    {
                        if (i == length - 1)
                        {
                            /* pattern found */
                            return idx;
                        }
                    }
                }
            } while (true);
        }
        public static int IndexOf(string searchString, string pattern, int index)
        { // returns the index of a pattern inside searchString
            int length = pattern.Length;
            int idx = searchString.IndexOf(pattern[0], index);
            do
            {
                if (idx == -1 || ((idx + pattern.Length) > searchString.Length)) return -1;
                for (int i = 0; i < length; i++)
                {
                    if (pattern[i] != searchString[idx + i])
                    {
                        /* pattern mismatch */
                        idx = searchString.IndexOf(pattern[0], idx + i + 1);
                        break;
                    }
                    else
                    {
                        if (i == length - 1)
                        {
                            /* pattern found */
                            return idx;
                        }
                    }
                }
            } while (true);
        }
        public static int LastIndexOf(string searchString, string pattern)
        { // returns the index of a pattern inside searchString starting from its end
            int length = pattern.Length - 1;
            int lastIdx = searchString.LastIndexOf(pattern[length]);
            do
            {
                if (lastIdx == -1) return -1; /* TBC this line might contain a bug if the searchString begins w/ the pattern */
                for (int i = 1; length - i + 1 > 0; i++)
                {
                    if (pattern[length - i] != searchString[lastIdx - i])
                    {
                        /* pattern mismatch */
                        lastIdx = searchString.LastIndexOf(pattern[length], lastIdx - 1);
                        break;
                    }
                    else
                    {
                        if (i == length)
                        {
                            /* pattern found */
                            return ++lastIdx; // return lastIdx - length returns the index of the first char of the pattern;
                        }
                    }
                }
            } while (true);
        }
        public static int LastIndexOf(string searchString, string pattern, int index)
        { // returns the index of a pattern inside searchString starting from its end
            int length = pattern.Length - 1;
            int lastIdx = searchString.LastIndexOf(pattern[length], index);
            do
            {
                if (lastIdx == -1) return -1; /* TBC this line might contain a bug if the searchString begins w/ the pattern */
                for (int i = 1; length - i + 1 > 0; i++)
                {
                    if (pattern[length - i] != searchString[lastIdx - i])
                    {
                        /* pattern mismatch */
                        lastIdx = searchString.LastIndexOf(pattern[length], lastIdx - 1);
                        break;
                    }
                    else
                    {
                        if (i == length)
                        {
                            /* pattern found */
                            return ++lastIdx; /* return lastIdx - length returns the index of the first char of the pattern; */
                        }
                    }
                }
            } while (true);
        }
        public static int LastIndexOfIgnoreSpaces(string searchString, string pattern)
        { // returns the index of a pattern inside searchString starting from its end and ignoring Spaces, Tabs and Newlines
            int skip;
            int length = pattern.Length - 1;
            int lastIdx = searchString.LastIndexOf(pattern[length]);
            do
            {
                skip = 0;
                if (lastIdx == -1) return -1;
                for (int i = 1; length + 1 + skip - i > 0; i++)
                {
                    if (searchString[lastIdx - i] == ' ' || searchString[lastIdx - i] == '\t' || searchString[lastIdx - i].ToString() == System.Environment.NewLine || searchString[lastIdx - i] == 10 || searchString[lastIdx - i] == 13) skip++;
                    else
                    {
                        if (pattern[length - i + skip] != searchString[lastIdx - i])
                        {
                            /* pattern mismatch */
                            lastIdx = searchString.LastIndexOf(pattern[length], lastIdx - 1);
                            break;
                        }
                        else
                        {
                            if (i == length + skip)
                            {
                                /* pattern found */
                                return ++lastIdx - skip; // return lastIdx - length returns the index of the first char of the pattern;
                            }
                        }
                    }
                }
            } while (true);
        }
        public static int LastIndexOfIgnoreSpaces(string searchString, string pattern, int index)
        { // returns the index of a pattern inside searchString starting from its end and ignoring Spaces, Tabs and Newlines
            int skip;
            int length = pattern.Length - 1;
            int lastIdx = searchString.LastIndexOf(pattern[length], index);
            do
            {
                skip = 0;
                if (lastIdx == -1) return -1;
                for (int i = 1; length + 1 + skip - i > 0; i++)
                {
                    if (searchString[lastIdx - i] == ' ' || searchString[lastIdx - i] == '\t' || searchString[lastIdx - i].ToString() == System.Environment.NewLine || searchString[lastIdx - i] == 10 || searchString[lastIdx - i] == 13)
                        skip++;
                    else
                    {
                        if (pattern[length - i + skip] != searchString[lastIdx - i])
                        {
                            /* pattern mismatch */
                            lastIdx = searchString.LastIndexOf(pattern[length], lastIdx - 1);
                            break;
                        }
                        else
                        {
                            if (i == length + skip)
                            {
                                /* pattern found */
                                return ++lastIdx - skip; // return lastIdx - length returns the index of the first char of the pattern;
                            }
                        }
                    }
                }
            } while (true);
        }
    }
}
