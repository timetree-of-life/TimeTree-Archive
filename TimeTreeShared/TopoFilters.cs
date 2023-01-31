using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TimeTreeShared
{
    public interface GenericFilter
    {
        bool IsFiltered(ExtendedNode taxa);
    }

    public class WordFilter : GenericFilter
    {
        protected HashSet<string> wordList;
        public HashSet<string> WordList
        {
            get { return wordList; }
            set { wordList = value; }
        }

        public bool filterOnlyLeaves = false;

        public WordFilter(HashSet<string> wordList)
        {
            this.wordList = wordList;
        }

        public WordFilter(HashSet<string> wordList, bool filterOnlyLeaves)
        {
            this.wordList = wordList;
            this.filterOnlyLeaves = filterOnlyLeaves;
        }

        public bool IsFiltered(ExtendedNode taxa)
        {
            if (filterOnlyLeaves && taxa.Nodes.Count > 0)
                return false;

            foreach (string word in wordList)
                if (taxa.TaxonName.ToLower().Contains(word))
                    return true;

            return false;
        }
    }

    public class DuplicateFilter : GenericFilter
    {
        public DuplicateFilter()
        {

        }

        public bool IsFiltered(ExtendedNode taxa)
        {
            if (taxa.TaxonName == "")
                return false;

            ExtendedNode parent = (ExtendedNode)taxa.Parent;
            if (parent != null && taxa.Nodes.Count == 0)
            {
                return (parent.Nodes.Cast<ExtendedNode>().First(x => x.TaxonName == taxa.TaxonName) != null);
            }
            return false;
        }
    }

    public class JointFilter : GenericFilter
    {
        private List<GenericFilter> filterList;

        public JointFilter()
        {
            filterList = new List<GenericFilter>();
        }

        public void AddFilter(GenericFilter filter)
        {
            filterList.Add(filter);
        }

        public void RemoveFilter(GenericFilter filter)
        {
            filterList.Remove(filter);
        }

        public bool IsFiltered(ExtendedNode taxa)
        {
            return filterList.Any(x => x.IsFiltered(taxa) == true);
        }
    }
}
