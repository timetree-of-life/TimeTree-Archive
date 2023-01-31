using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TimeTreeShared
{
    public static class MainEditingService
    {
        public static void PruneTreeToTaxa(TimeTree tree, IEnumerable<int> TaxonIDs)
        {
            HashSet<int> TaxonIDSet = TaxonIDs.ToHashSet<int>();
            foreach (ExtendedNode leaf in tree.leafList.Where(x => !TaxonIDSet.Contains(x.TaxonID)).ToList())
            {
                tree.DeleteNode(leaf);
            }
        }

        public static void PruneToCommonTaxa(TimeTree treeA, TimeTree treeB)
        {
            HashSet<int> TaxonIDSetB = treeB.leafList.Select(x => x.TaxonID).ToHashSet<int>();
            foreach (ExtendedNode leaf in treeA.leafList.Where(x => !TaxonIDSetB.Contains(x.TaxonID)).ToList())
            {
                treeA.DeleteNode(leaf);
            }

            HashSet<int> TaxonIDSetA = treeA.leafList.Select(x => x.TaxonID).ToHashSet<int>();
            foreach (ExtendedNode leaf in treeB.leafList.Where(x => !TaxonIDSetA.Contains(x.TaxonID)).ToList())
            {
                treeB.DeleteNode(leaf);
            }
        }
    }
}
