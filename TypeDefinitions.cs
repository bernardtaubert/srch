using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srch
{
    class TypeDefinitions /** C-Type Definitions */
    {
        protected String[] functionTypes = {
            "extern",
            "inline",
        };
        protected String[] dataTypes = {
            "void",
            "u8",
            "u16",
            "u24",
            "u32",
            "u64",
            "s8",
            "s16",
            "s24",
            "s32",
            "s64",

            /** GM */
            "BYTE",
            "SHORTINT",
            "WORD",
            "INTEGER",
            "LONGWORD",
            "LONGINT",
            "LONGLONGWORD",
            "LONGLONGINT",
            "FLOAT",
            "DOUBLE",

            /** AutoSAR */
            "boolean",
            "uint8",
            "sint8",
            "sint16",
            "uint16",
            "sint32",
            "uint32",
            "uint8_least",
            "uint16_least",
            "uint32_least",
            "sint8_least",
            "sint16_least",
            "sint32_least",
            "float32",
            "float64",
        };
    }
}
