using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Srch
{
    internal class Options /* search options container class */
    {
        internal Option caseSensitive;
        internal Option wholeWordsOnly;
        internal Option NETRegEx;
        internal Option SimpleRegEx;
        internal Option Default;
        internal Option onlyShow1EntryPerLine;
        internal Option SearchSubDirectories;
        internal Option SearchFilesSubDirectories;
        internal Option SearchFilesRegEx;
        internal Option SearchMultiAllStrings;
        internal Option SearchMultiAnyString;
        internal Option SearchMultiNoneOfStrings;
        internal Option IgnoreComments;
        internal List<Option> list;
        internal enum AvailableOptions
        {
            Default = 0,
            WholeWordsOnly = 1,
            FastRegEx = 2,
            NETRegEx = 3,
            CaseSensitive = 4,
            OnlyShow1EntryPerLine = 5,
            SearchSubDirectories = 6,
            SearchFilesSubDirectories = 7,
            SearchMultiAllStrings = 8,
            SearchMultiAnyString = 9,
            SearchMultiNoneOfStrings = 10,
            IgnoreComments = 11
        };
        public Options()
        { // Constructor
            list = new List<Option>();
            caseSensitive = new Option(false, "CaseSensitive", (int)AvailableOptions.CaseSensitive);
            wholeWordsOnly = new Option(false, "WholeWordsOnly", (int)AvailableOptions.WholeWordsOnly);
            NETRegEx = new Option(false, ".NETRegEx", (int)AvailableOptions.NETRegEx);
            SimpleRegEx = new Option(false, "SimpleRegEx", (int)AvailableOptions.FastRegEx);
            Default = new Option(false, "Default", (int)AvailableOptions.Default);
            onlyShow1EntryPerLine = new Option(false, "OnlyShow1EntryPerLine", (int)AvailableOptions.OnlyShow1EntryPerLine);
            SearchSubDirectories = new Option(false, "SearchSubDirectories", (int)AvailableOptions.SearchSubDirectories);
            SearchFilesSubDirectories = new Option(false, "SearchFilesSubDirectories", (int)AvailableOptions.SearchFilesSubDirectories);
            SearchMultiAllStrings = new Option(false, "SearchMultiAllStrings", (int)AvailableOptions.SearchMultiAllStrings);
            SearchMultiAnyString = new Option(false, "SearchMultiAnyString", (int)AvailableOptions.SearchMultiAnyString);
            SearchMultiNoneOfStrings = new Option(false, "SearchMultiNoneOfStrings", (int)AvailableOptions.SearchMultiNoneOfStrings);
            IgnoreComments = new Option(false, "IgnoreComments", (int)AvailableOptions.IgnoreComments);
            list.Add(Default);
            list.Add(wholeWordsOnly);
            list.Add(SimpleRegEx);
            list.Add(NETRegEx);
            list.Add(caseSensitive);
            list.Add(onlyShow1EntryPerLine);
            list.Add(SearchSubDirectories);
            list.Add(SearchFilesSubDirectories);
            list.Add(SearchMultiAllStrings);
            list.Add(SearchMultiAnyString);
            list.Add(SearchMultiNoneOfStrings);
            list.Add(IgnoreComments);
        }
        #region GettersSetters
        public void SetValue(AvailableOptions optionId, bool value)
        {
            foreach (Option o in list)
            {
                if (o.GetId() == (int)optionId)
                {
                    o.SetValue(value);
                    break;
                }
            }
        }
        public void SetValue(int optionId, bool value)
        {
            foreach (Option o in list)
            {
                if (o.GetId() == optionId)
                {
                    o.SetValue(value);
                    break;
                }
            }
        }
        public bool GetValue(AvailableOptions id)
        {
            bool value = false;
            foreach (Option o in list)
            {
                if (o.GetId() == (int)id)
                {
                    value = o.GetValue();
                    break;
                }
            }
            return value;
        }
        public List<Option> GetList()
        {
            return list;
        }
        #endregion
    }
    public class Option : IComparable<Option>
    {
        private string name;
        private bool value;
        private int id;
        public Option(bool value, string name, int id)
        { // Constructor
            this.name = name;
            this.value = value;
            this.id = id;
        }
        override public string ToString()
        {
            return name;
        }
        public bool GetValue()
        {
            return value;
        }
        public void SetValue(bool value)
        {
            this.value = value;
        }
        public int GetId()
        {
            return id;
        }
        public int CompareTo(Option that)
        {
            if (this.id > that.id) return -1;
            if (this.id == that.id) return 0;
            return 1;
        }
    }
}
