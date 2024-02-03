using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Srch
{
    internal class FastRegEx
    {
        /**********************************************************************************
         * Container class used for the custom 'Fast Regex' internal representation
         **********************************************************************************
         *      allowed special characters: .*^$\
         *
         *      \.   any single character
         *      \*   zero or more of any characters
         *      \\   escape character
         *      \^   line start marker
         *      \$   line end   marker
         *      
         */
        static private int noOfChars = 0;
        static private string searchString = null;
        static private bool[] escapedCharAtIdx = null; // array which indicates if the character at a certain index is a escaped character
        static private bool[] specialCharAtIdx = null; // array which indicates if the character at a certain index is a RegEx special character
        static private char[] specialChars = { '.', '*', '^', '$' };
        internal enum availableSpecialChars
        {
            none = 0,
            dot = 1,
            asterisk = 2,
            caret = 3,
            dollar = 4,
            backslash = 5
        };
        private enum wildCard
        {
            none = 0,
            dot = 1,
            asterisk = 2
        };
        public FastRegEx(string s)
        { // Constructor
            searchString = s;
            specialCharAtIdx = new bool[s.Length];
            escapedCharAtIdx = new bool[s.Length];
            if (!Validate())
            {
                throw new System.FormatException("Error: RegEx format invalid");
            }
        }
        /* Validation of the RegEx format
         *      return
         *          true  = format valid
         *          false = format invalid
         */
        static public bool Validate()
        {
            int idx = 0;
            int i = 0;
            if (searchString.Trim().Equals(""))
            { // if string consists only of whitespace characters
                return false; // return invalid, unrecognized escape sequence
            }
            do
            { // search for escape sequences
                idx = searchString.IndexOf('\\', idx);
                if (idx != -1 && (idx + 1) < searchString.Length)
                {
                    switch (searchString[idx + 1])
                    {
                        case '.':
                            break;
                        case '*':
                            break;
                        case '^':
                            break;
                        case '$':
                            break;
                        case '\\':
                            break;
                        default:
                            return false; // unrecognized escape sequence, return invalid
                    }
                    searchString = searchString.Remove(idx, 1);  // remove the escape sign '\'
                    escapedCharAtIdx[idx] = true; // and mark this character as escaped
                    idx++;
                }
                else
                {
                    noOfChars = searchString.Length;
                    break;
                }
            } while (true);
            for (int k = 0; k < specialChars.Length; k++)
            { // iterate over each special character
                char c = specialChars[k];
                idx = 0;
                do
                { // search for special characters
                    idx = searchString.IndexOf(c, idx);
                    if (idx != -1 && ((idx + 1) <= searchString.Length))
                    {
                        if (!escapedCharAtIdx[idx])
                        {
                            specialCharAtIdx[idx] = true; // if not escaped, then it is a special char
                        }
                    }
                    else
                    {
                        break;
                    }
                    idx++;
                } while (true);
            }
            /* further validation of RegEx positions */
            int lastAsteriskPlusQuestionmarkCaretDotIdx = -1;
            for (int k = 0; k < noOfChars; k++)
            {
                if (specialCharAtIdx[k])
                {
                    if (k > 0)
                    {
                        if (searchString[k] == '^') // do not allow ^ mid string
                            return false;
                    }
                    if (searchString[k] == '$')
                    { // $ has to be be positioned at the end or RegEx is invalid
                        if (k < noOfChars - 1)
                            return false;
                    }
                    if (searchString[k] == '*' || searchString[k] == '^' || searchString[k] == '.')
                    {
                        if (k == lastAsteriskPlusQuestionmarkCaretDotIdx + 1)
                        {
                            if (k == 0 && (searchString[k] == '.' || searchString[k] == '^'))
                            {
                                lastAsteriskPlusQuestionmarkCaretDotIdx = i;
                            }
                            else if ((k != 0) && (searchString[k - 1] == '.') && (searchString[k] == '.'))
                            { // this is fine
                            }
                            else
                            {
                                if (k == 0 && searchString[0] == '*') // an initial asterisk is fine too
                                    return true;
                                return false;  // do not allow subsequent *+? or *+? followed by .
                            }
                        }
                        else
                            lastAsteriskPlusQuestionmarkCaretDotIdx = i;
                    }
                }
            }
            Boolean onlySpecialChars = true;
            for (int k = 0; k < noOfChars; k++)
            {
                if (specialCharAtIdx[k] == false)
                    onlySpecialChars = false;
            }
            if (onlySpecialChars)
                if (!((searchString[0] == '^') && (searchString[noOfChars - 1] == '$')))
                    return false; // if it contains only special chars its an invalid RegEx unless it is other than first char ^, last char $
            return true;
        }
        static public string GetSearchString()
        {
            return searchString;
        }
        static public int GetNoOfChars()
        {
            return noOfChars;
        }
        static public bool[] GetSpecialCharAtIdxArray()
        {
            return specialCharAtIdx;
        }
        public bool Match(string line, RegexOptions regExOptions)
        { // returns 1 if a match against the internally stored RegEx succeeds, returns 0 if the match fails
            return CompareRegEx(line, regExOptions);
        }
        private bool CompareRegEx(string line, RegexOptions regExOptions)
        { // performs the comparison
            int i = 0;
            int linePos = 0;
            int currentWildCard = 0;
            bool checkOnGoing = false;
            string tmpLine = null;
            string tmpSearchString = null;
            if (regExOptions == RegexOptions.IgnoreCase)
            {
                tmpLine = line.ToLower();
                tmpSearchString = searchString.ToLower();
            }
            else
            {
                tmpLine = line;
                tmpSearchString = searchString;
            }
            if (tmpLine.Length == 0)
                return false;
            if (tmpSearchString[0] == '^' && specialCharAtIdx[0])
            { // if RegEx starts with ^
                if (tmpSearchString[1] != tmpLine[0])
                    return false;
                else
                    i = 1;
            }
            while (i < noOfChars)
            {
                if (specialCharAtIdx[i])
                { // determine if there is a special char at the index
                    switch (tmpSearchString[i])
                    { // determine which special char it is
                        case '.':
                            currentWildCard = (int)wildCard.dot; // exactly one character of any kind
                            break;
                        case '*':
                            currentWildCard = (int)wildCard.asterisk; // zero or more characters of any kind
                            break;
                        case '$':
                            if (linePos >= tmpLine.Length)
                                return true;  // upon reaching the end of line - if there was not an error yet - the match is valid
                            else
                                return false; // else no match
                        default:
                            return false; // invalid RegEx
                    }
                }
                else
                {
                    currentWildCard = (int)wildCard.none;
                }
                switch (currentWildCard)
                {
                    case (int)wildCard.none:
                        if (!checkOnGoing)
                        {
                            // first, find the starting position IndexOf inside the line and adjust the linePos to the new position
                            linePos = tmpLine.IndexOf(tmpSearchString[i], linePos);
                            if (linePos == -1) return false;
                            checkOnGoing = true;
                        }
                        if (linePos < tmpLine.Length)
                        { // then check if there is a match
                            if (tmpSearchString[i] != tmpLine[linePos])
                            {
                                if (tmpSearchString[0] == '^' && specialCharAtIdx[0])
                                { // if RegEx started with ^
                                    return false; // abort search
                                }
                                else
                                {
                                    i = 0; // if RegEx didn't start with ^ then reset search
                                    checkOnGoing = false;
                                }
                                break;
                            }
                            else
                            {
                                i++; // if the chars match, simply advance both indices to proceed
                                linePos++;
                            }
                        }
                        else
                            return false;
                        break;
                    case (int)wildCard.dot:
                        if (linePos > tmpLine.Length - 1) // but abort, if the dot is located beyond the line end
                            return false;
                        i++; // no match is needed, simply advance the indices to proceed
                        linePos++;
                        currentWildCard = (int)wildCard.none;
                        break;
                    case (int)wildCard.asterisk:
                        if ((i + 1) < noOfChars)
                        {
                            if (specialCharAtIdx[i + 1])
                            { // determine if there is a special char at the NEXT index
                                switch (tmpSearchString[i + 1])
                                { // determine which special char it is
                                    case '$':
                                        if (linePos >= tmpLine.Length)
                                            return true;  // upon reaching the end of line - if there was not an error yet - the match is valid
                                        else
                                            return true; // if there was not an error yet - the *$ match is valid 
                                    default:
                                        return false; // invalid RegEx
                                }
                            }
                            else
                            {
                                string s = "";
                                int linePosStart = linePos;
                                while ((i + 1) < noOfChars)
                                {
                                    if (!specialCharAtIdx[i + 1])
                                    {
                                        s += tmpSearchString[i + 1];
                                        i++;
                                    }
                                    else
                                    {
                                        i++;
                                        break; // next char will be another special character, so break execution here
                                    }
                                }
                                // either reached the end of the string or found another RegEx character, anyway for now, just search the remaining non special chars
                                if (s == "")
                                {
                                    if (i >= tmpLine.Length)
                                        return true;  // upon reaching the end of line - if there was not an error yet - the match is valid
                                    else
                                        return false; // invalid RegEx
                                }
                                int foundAt = -1;
                                if (linePos < 1)
                                { // we might need to decrement the current linePos, if we have asterisk wildchar chains like e.g. x*x*x
                                }
                                else
                                {
                                    if (tmpLine[linePos - 1] == s[0])
                                    { // if the char before the asterisk wildcard was the same as the char after the wildcard e.g. x*x, then do not start the search at the already found character, but increment it by one
                                        linePosStart += 2;
                                        if (linePosStart > tmpLine.Length)
                                            return false;
                                    }
                                }
                                if (linePosStart < 2)
                                    foundAt = StringUtil.IndexOf(tmpLine, s, 0);
                                else
                                    foundAt = StringUtil.IndexOf(tmpLine, s, linePosStart - 2);
                                if (foundAt == -1)
                                    return false;
                                else
                                {
                                    linePos = foundAt + s.Length; // advance the linePos to the position 1 character after the found string
                                }
                                if ((i + 1) >= noOfChars)
                                {
                                    if (specialCharAtIdx[noOfChars - 1] == true && tmpSearchString[noOfChars - 1].Equals('$'))
                                    { // if the last char equals '$'
                                        if (linePos > tmpLine.Length - 1)
                                            return true;
                                        else
                                            return false;
                                    }
                                    return true; // finish, when the end of the searchString has been reached
                                }
                            }
                        }
                        else
                        { // reached the end of the string and the last character was asterisk
                            return true;
                        }
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
    }
}
