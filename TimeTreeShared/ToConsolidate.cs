using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Npgsql;

namespace TimeTreeShared
{
    public class TreeParser
    {
        private int currentIndex;
        private int nodeCount = 0;
        private char taxaPrefix;
        public int filtered = 0;

        public TreeParser()
        {
            currentIndex = 0;
        }

        public TreeParser(char taxaPrefix)
        {
            currentIndex = 0;
            this.taxaPrefix = taxaPrefix;
        }

    }

    public static class TreeHelper
    {
        public static void addNodesToList(ExtendedNode root, HashSet<ExtendedNode> nodeList, HashSet<ExtendedNode> leafList)
        {
            foreach (ExtendedNode child in root.Nodes)
            {
                nodeList.Add(child);
                if (child.Nodes.Count > 0)
                    addNodesToList(child, nodeList, leafList);
                else
                    leafList.Add(child);
            }
        }
    }

    
}
