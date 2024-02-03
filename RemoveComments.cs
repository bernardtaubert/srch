using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Srch
{
    class RemoveComments
    {
        // Variable Definitions
        enum ParseState
        {
            inCode,
            inCharLiteral,
            inStringLiteral,
            inSingleLineComment,
            inMultiLineComment
        };
        internal static string removeComments(string text)
        {
            StringWriter stringWriter = new StringWriter();
            int index = 0;
            int literalId = 0;
            ParseState state = ParseState.inCode;
            while ((index + 1) < text.Length)
            {
                char c1 = text[index];
                switch (state)
                {
                    case ParseState.inCode:
                        /* filtering out comments */
                        char c2 = text[index + 1];
                        if (c1.Equals(LanguageConventions.commentLineDelimiters[0, 0]) &&
                            c2.Equals(LanguageConventions.commentLineDelimiters[0, 1]))
                        {
                            while (!(c1.Equals(LanguageConventions.newLine[1])))
                            {
                                if (index + 1 < text.Length)
                                {
                                    index++;
                                    c1 = text[index];
                                }
                                else
                                {
                                    index++;
                                    break;
                                }
                            }
                        }
                        else if (c1.Equals(LanguageConventions.commentBlockDelimiters[0, 0, 0]) && c2.Equals(LanguageConventions.commentBlockDelimiters[0, 0, 1]))
                        {
                            while (!(text[index].Equals(LanguageConventions.commentBlockDelimiters[0, 1, 0]) && text[index + 1].Equals(LanguageConventions.commentBlockDelimiters[0, 1, 1])))
                            {
                                if (index + 2 < text.Length)
                                {
                                    if (text[index].Equals(LanguageConventions.newLine[1]))
                                    {
                                        stringWriter.Write(text[index]);
                                    }
                                    index++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            index += 2; // reached the end of the block comments, increment index by 2 to skip the */
                        }
                        else
                        {
                            literalId = 0;
                            foreach (char c in LanguageConventions.literalDelimiters)
                            {
                                if (c1.Equals(c))
                                {
                                    if (literalId == 0)
                                    {
                                        state = ParseState.inCharLiteral;
                                        break;
                                    }
                                    else
                                    {
                                        state = ParseState.inStringLiteral;
                                        break;
                                    }
                                }
                                literalId++;
                            }
                        }
                        if (index < text.Length)
                            stringWriter.Write(text[index]);
                        break;
                    case ParseState.inCharLiteral:
                    case ParseState.inStringLiteral:
                        stringWriter.Write(text[index]);
                        c2 = text[index - 1];
                        if (c1.Equals(LanguageConventions.literalDelimiters[literalId]) && !c2.Equals('\\'))
                        {
                            state = ParseState.inCode;
                        }
                        break;
                    default:
                        break;
                }
                index++;
            }
            while (index < text.Length) stringWriter.Write(text[index++]);
            string noComments = stringWriter.ToString();
            return noComments;
        }
    }
}
