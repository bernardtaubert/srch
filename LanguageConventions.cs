using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srch
{
    static class LanguageConventions /** C-Syntax conventions */
    {
        public static char[] literalDelimiters = {
            '\'', /** char delimiter */
            '\"', /** string delimiter */
        };
        public static char[] spaces = {
            '\t',
            ' ',
        };
        public static char[] newLine = {
            (char)13, /** carriage return **/
            (char)10, /** line feed **/
        };
        public static char[, ,] commentBlockDelimiters = { 
            { {'/','*'}, {'*', '/'}, }, /** multi-line comment delimiters */
        };
        public static char[,] commentLineDelimiters = {
            {'/','/'},  /** end-of-line comment delimiters */
        };
        public static String[] keywords = { /* list of any C-Keywords which can make use of braces ') {' */
            "if",
            "for",
            "else",
            "while",
            "switch",
        };
        public static char[] charDistributionList = {
            'a',
            'b',
            'c',
            'd',
            'e',
            'f',
            'g',
            'h',
            'i',
            'j',
            'k',
            'l',
            'm',
            'n',
            'o',
            'p',
            'q',
            'r',
            's',
            't',
            'u',
            'v',
            'w',
            'x',
            'y',
            'z',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            '#',
            '_',
            '(',
            ')',
            '[',
            ']',
            '{',
            '}',
            '+',
            '-',
            '*',
            '/',
            '~',
            '%',
            '.',
            '<',
            '>',
            '^',
            '"',
            '&',
            '|',
            '=',
            ',',
            ';',
            '?',
            ':',
            '!',
            '$',
            '\''
        };
        static public int GetRarestCharIndex(string searchString) {
            int index = 0;
            int curMin = int.MaxValue;
            int j = 0;
            foreach (char c in searchString) {
                for (int i = 0; i < LanguageConventions.charDistributionList.Length; i++) {
                    if (LanguageConventions.charDistributionList[i] == c) {
                        if (LanguageConventions.charDistributionQuantity[i] < curMin) {
                            curMin = LanguageConventions.charDistributionQuantity[i];
                            index = j;
                        }
                    }
                }
                j++;
            }
            return index;
        }
        public static int[] charDistributionQuantity = {
            120974,
            28708,
            125797,
            100344,
            193418,
            100116,
            38363,
            23976,
            130426,
            3426,
            16201,
            91525,
            89857,
            126374,
            77919,
            78677,
            4449,
            111521,
            111063,
            154979,
            89519,
            41059,
            10392,
            46046,
            16995,
            1326,
            55991,
            44436,
            35155,
            32289,
            10749,
            31888,
            31540,
            6451,
            21180,
            4925,
            18690,
            259731,
            42423,
            42422,
            10236,
            10236,
            13265,
            13265,
            1863,
            4500,
            6486,
            1732,
            60,
            172,
            7058,
            2528,
            4417,
            3,
            5278,
            4012,
            1515,
            13102,
            70722,
            29366,
            77,
            842,
            1106,
            0,
            1
        };
    }
}
