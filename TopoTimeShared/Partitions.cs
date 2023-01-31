using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TimeTreeShared;

namespace TopoTimeShared
{
    [Serializable]
    public class SplitData
    {
        public SplitData() {
            studyData = new List<StudyData>();
        }

        // Partition analysis variables
        public int FavorAB;
        public int FavorAC;
        public int FavorBC;
        //public int ContradictAB;
        //public int ContradictAC;
        //public int ContradictBC;
        //public int SupportAfromB;
        //public int ContradictAfromB;
        public int Size;
        public int ParentSplit;
        public string SplitStatus;
        //public string TaxaName;
        //public ExtendedNode AssociatedNode;

        public List<StudyData> studyData;

        public bool ShouldSerializeParentSplit()
        { return false; }

        public bool ShouldSerializeSplitStatus()
        { return false; }

        public bool ShouldSerializeSize()
        { return false; }

        public bool ShouldSerializeFavorAB()
        { return FavorAB > 0; }

        public bool ShouldSerializeFavorAC()
        { return FavorAC > 0; }

        public bool ShouldSerializeFavorBC()
        { return FavorBC > 0; }

        

        public TreeNode PartitionDisplay (TopoTimeTree hostTree)
        {
            
            {
                TreeNode partitionDisplay = new TreeNode("Partitions");
                TreeNode ABNode = new TreeNode("AB Support - " + (this.FavorAB));
                TreeNode ACNode = new TreeNode("AC Support - " + (this.FavorAC));
                TreeNode BCNode = new TreeNode("BC Support - " + (this.FavorBC));
                TreeNode errors = new TreeNode("Anomalies? - ");
                TreeNode singleTaxonChanges = new TreeNode("Single Taxon Changes");

                Dictionary<string, int> individualTaxaCount = new Dictionary<string, int>();

                foreach (StudyData partition in studyData)
                {
                    TreeNode newStudyNode = StudyNode(partition, individualTaxaCount, hostTree);
                    if (partition.FavorAB > 0)
                    {
                        if (partition.FavorAC == 0 && partition.FavorBC == 0)
                            ABNode.Nodes.Add(newStudyNode);
                        else
                            errors.Nodes.Add(newStudyNode);
                    }
                    else if (partition.FavorAC > 0)
                    {
                        if (partition.FavorAB == 0 && partition.FavorBC == 0)
                            ACNode.Nodes.Add(newStudyNode);
                        else
                            errors.Nodes.Add(newStudyNode);
                    }
                    else if (partition.FavorBC > 0)
                    {
                        if (partition.FavorAB == 0 && partition.FavorAC == 0)
                            BCNode.Nodes.Add(newStudyNode);
                        else
                            errors.Nodes.Add(newStudyNode);
                    }
                    else
                        errors.Nodes.Add(newStudyNode);
                }

                errors.Text = "Anomalies? - " + errors.Nodes.Count;

                foreach (KeyValuePair<string, int> pair in individualTaxaCount)
                {
                    singleTaxonChanges.Nodes.Add(pair.Key + " - " + pair.Value);
                }

                if (ABNode.Nodes.Count > 0)
                    partitionDisplay.Nodes.Add(ABNode);

                if (ACNode.Nodes.Count > 0)
                    partitionDisplay.Nodes.Add(ACNode);

                if (BCNode.Nodes.Count > 0)
                    partitionDisplay.Nodes.Add(BCNode);

                if (errors.Nodes.Count > 0)
                    partitionDisplay.Nodes.Add(errors);

                if (singleTaxonChanges.Nodes.Count > 0)
                    partitionDisplay.Nodes.Add(singleTaxonChanges);

                return partitionDisplay;
            }
        }

        private TreeNode StudyNode(StudyData partition, Dictionary<string, int> individualTaxaCount, TopoTimeTree hostTree)
        {
            Study selectedStudy;

            if (hostTree.includedStudies == null || !hostTree.includedStudies.TryGetValue(partition.CitationID, out selectedStudy))
            {
                if (partition.Study != null)
                    selectedStudy = partition.Study;
                else
                    selectedStudy = new Study(partition.CitationID.ToString(), 0, "", 0, "");
            }                

            TreeNode newStudyNode = new TreeNode(selectedStudy.Source + ", " + selectedStudy.ID + ", " + selectedStudy.Author + ", " + selectedStudy.Year);
            newStudyNode.Nodes.Add(new TreeNode("phylogeny_node - " + partition.phylogeny_node));

            if (partition.taxaGroupB != null)
            {
                TreeNode taxaGroupA = new TreeNode("Taxa Group A");
                // for legacy trees
                foreach (string leafTaxa in partition.taxaGroupA)
                {
                    taxaGroupA.Nodes.Add(new TreeNode(leafTaxa));
                }

                // for new trees (2022)
                foreach (int leafTaxonID in partition.TaxaAinA)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupA.Nodes.Add(new TreeNode(TaxonName));
                }

                foreach (int leafTaxonID in partition.TaxaBinA)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupA.Nodes.Add(new TreeNode(TaxonName + " [B]"));
                }

                foreach (int leafTaxonID in partition.TaxaCinA)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupA.Nodes.Add(new TreeNode(TaxonName + " [C]"));
                }

                TreeNode taxaGroupB = new TreeNode("Taxa Group B");
                // for legacy trees
                foreach (string leafTaxa in partition.taxaGroupB)
                {
                    taxaGroupB.Nodes.Add(new TreeNode(leafTaxa));
                }

                // for new trees (2022)
                foreach (int leafTaxonID in partition.TaxaBinB)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupB.Nodes.Add(new TreeNode(TaxonName));
                }

                foreach (int leafTaxonID in partition.TaxaAinB)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupB.Nodes.Add(new TreeNode(TaxonName + " [A]"));
                }

                foreach (int leafTaxonID in partition.TaxaCinB)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupB.Nodes.Add(new TreeNode(TaxonName + " [C]"));
                }

                TreeNode taxaGroupC = new TreeNode("Taxa Group C");
                foreach (string leafTaxa in partition.taxaGroupC)
                {
                    if (leafTaxa.Contains("[A]") || leafTaxa.Contains("[B]"))
                    {
                        if (!individualTaxaCount.ContainsKey(leafTaxa))
                            individualTaxaCount[leafTaxa] = 0;

                        individualTaxaCount[leafTaxa]++;
                    }
                    taxaGroupC.Nodes.Add(new TreeNode(leafTaxa));
                }

                // for new trees (2022)
                foreach (int leafTaxonID in partition.TaxaCinC)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    taxaGroupC.Nodes.Add(new TreeNode(TaxonName));
                }

                foreach (int leafTaxonID in partition.TaxaAinC)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    if (!individualTaxaCount.ContainsKey(TaxonName))
                        individualTaxaCount[TaxonName] = 0;

                    individualTaxaCount[TaxonName]++;

                    taxaGroupC.Nodes.Add(new TreeNode(TaxonName + " [A]"));
                }

                foreach (int leafTaxonID in partition.TaxaBinC)
                {
                    if (!hostTree.includedTaxa.TryGetValue(leafTaxonID, out string TaxonName))
                        TaxonName = leafTaxonID.ToString();

                    if (!individualTaxaCount.ContainsKey(TaxonName))
                        individualTaxaCount[TaxonName] = 0;

                    individualTaxaCount[TaxonName]++;

                    taxaGroupC.Nodes.Add(new TreeNode(TaxonName + " [B]"));
                }



                taxaGroupA.ExpandAll();
                taxaGroupB.ExpandAll();
                taxaGroupC.ExpandAll();

                newStudyNode.Nodes.Add(taxaGroupA);
                newStudyNode.Nodes.Add(taxaGroupB);
                newStudyNode.Nodes.Add(taxaGroupC);
            }

            return newStudyNode;
        }

        

        
    }

    [Serializable]
    public class StudyData
    {
        public int FavorAB;
        public int FavorAC;
        public int FavorBC;
        public string phylogeny_node;
        public Study Study;
        public int CitationID;
        public List<string> taxaGroupA;
        public List<string> taxaGroupB;
        public List<string> taxaGroupC;

        public List<int> TaxaAinA;
        public List<int> TaxaBinB;
        public List<int> TaxaCinC;
        public List<int> TaxaAinB;
        public List<int> TaxaBinA;
        public List<int> TaxaAinC;
        public List<int> TaxaBinC;
        public List<int> TaxaCinA;
        public List<int> TaxaCinB;

        public bool ShouldSerializetaxaGroupA()
        { return taxaGroupA.Count > 0; }

        public bool ShouldSerializetaxaGroupB()
        { return taxaGroupB.Count > 0; }

        public bool ShouldSerializetaxaGroupC()
        { return taxaGroupC.Count > 0; }

        public bool ShouldSerializeTaxaAinA() { return TaxaAinA.Count > 0; }
        public bool ShouldSerializeTaxaBinB() { return TaxaBinB.Count > 0; }
        public bool ShouldSerializeTaxaCinC() { return TaxaCinC.Count > 0; }
        public bool ShouldSerializeTaxaAinB() { return TaxaAinB.Count > 0; }
        public bool ShouldSerializeTaxaBinA() { return TaxaBinA.Count > 0; }
        public bool ShouldSerializeTaxaAinC() { return TaxaAinC.Count > 0; }
        public bool ShouldSerializeTaxaBinC() { return TaxaBinC.Count > 0; }
        public bool ShouldSerializeTaxaCinA() { return TaxaCinA.Count > 0; }
        public bool ShouldSerializeTaxaCinB() { return TaxaCinB.Count > 0; }


        public bool ShouldSerializeFavorAB()
        { return FavorAB != 0; }

        public bool ShouldSerializeFavorAC()
        { return FavorAC != 0; }

        public bool ShouldSerializeFavorBC()
        { return FavorBC != 0; }

        StudyData()
        {
            taxaGroupA = new List<string>();
            taxaGroupB = new List<string>();
            taxaGroupC = new List<string>();

            TaxaAinA = new List<int>();
            TaxaBinB = new List<int>();
            TaxaCinC = new List<int>();
            TaxaAinB = new List<int>();
            TaxaBinA = new List<int>();
            TaxaAinC = new List<int>();
            TaxaBinC = new List<int>();
            TaxaCinA = new List<int>();
            TaxaCinB = new List<int>();
        }

        public StudyData(int FavorAB, int FavorAC, int FavorBC, string phylogeny_node, Study study = null, int CitationID = 0) : this()
        {
            this.FavorAB = FavorAB;
            this.FavorAC = FavorAC;
            this.FavorBC = FavorBC;
            this.phylogeny_node = phylogeny_node;
            this.Study = study;
            this.CitationID = CitationID;
        }
    }

    public class PartitionComparer : EqualityComparer<PartitionData>
    {
        public override bool Equals(PartitionData x, PartitionData y)
        {
            try
            {
                if (x.nodesA.SequenceEqual(y.nodesA) &&
                    x.nodesB.SequenceEqual(y.nodesB) &&
                    x.citation == y.citation &&
                    x.outgroup.SequenceEqual(y.outgroup))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode(PartitionData obj)
        {
            int hash = 17;
            foreach (var itemHash in obj.nodesA.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 31 * itemHash;
            }
            foreach (var itemHash in obj.nodesB.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 37 * itemHash;
            }
            foreach (var itemHash in obj.outgroup.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 41 * itemHash;
            }
            hash += 43 * obj.citation.GetHashCode();
            return hash;
        }
    }

    public class PartitionDistinctComparer : EqualityComparer<PartitionData>
    {
        public override bool Equals(PartitionData x, PartitionData y)
        {
            try
            {
                if (x.nodesA.SequenceEqual(y.nodesA) &&
                    x.nodesB.SequenceEqual(y.nodesB))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode(PartitionData obj)
        {
            int hash = 17;
            foreach (var itemHash in obj.nodesA.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 31 * itemHash;
            }
            foreach (var itemHash in obj.nodesB.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 37 * itemHash;
            }
            return hash;
        }
    }

    public class PartitionTimeGrouper : EqualityComparer<PartitionData>
    {
        public override bool Equals(PartitionData x, PartitionData y)
        {
            try
            {
                /*
                if (x.nodesA.Any(y.nodesA.Contains) &&
                    x.nodesB.Any(y.nodesB.Contains))
                    return true;
                    */



                if (PartitionData.PartitionAgreement(x, y) == 1)
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode(PartitionData obj)
        {
            int hash = 17;
            foreach (var itemHash in obj.nodesA.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 31 * (itemHash + 1);
            }
            foreach (var itemHash in obj.nodesB.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 37 * (itemHash + 1);
            }
            return hash;
        }
    }

    public class PartitionGrouper : EqualityComparer<PartitionData>
    {
        public override bool Equals(PartitionData x, PartitionData y)
        {
            try
            {
                if (x.nodesA.SequenceEqual(y.nodesA) &&
                    x.nodesB.SequenceEqual(y.nodesB) &&
                    x.outgroup.SequenceEqual(y.outgroup))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode(PartitionData obj)
        {
            int hash = 17;
            foreach (var itemHash in obj.nodesA.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 31 * (itemHash + 1);
            }
            foreach (var itemHash in obj.nodesB.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 37 * (itemHash + 1);
            }
            foreach (var itemHash in obj.outgroup.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 41 * (itemHash + 1);
            }
            return hash;
        }
    }

    public class ListGrouper : EqualityComparer<List<int>>
    {
        public override bool Equals(List<int> x, List<int> y)
        {
            try
            {
                if (x.SequenceEqual(y))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public override int GetHashCode(List<int> obj)
        {
            int hash = 17;
            foreach (var itemHash in obj.Select(x => x.GetHashCode()).OrderBy(h => h))
            {
                hash += 31 * (itemHash + 1);
            }
            return hash;
        }
    }

    public class PartitionData
    {
        public int citation;
       // public int phylogeny_node;
        public int phylogeny_node_id;
        public List<int> nodesA;
        public List<int> nodesB;
        public List<int> outgroup;
        public List<ChildPairDivergence> divergenceTimes;
        public int childA_phylogeny_node;
        public int childB_phylogeny_node;
        public List<int> taxaA;
        public List<int> taxaB;
        public List<int> outgroupTaxa;

        public List<int> PhylogenyNodeIDs;

        public double DivergenceTime { get; set; }

        public int Votes { get; set; }

        internal int Size
        {
            get
            {
                if (nodesA != null && nodesB != null)
                    return nodesA.Count + nodesB.Count;
                return 0;
            }
        }

        PartitionData()
        {
            PhylogenyNodeIDs = new List<int>();
        }

        public string ListPhylogenyNodeIDs
        {
            get { return String.Join(",", PhylogenyNodeIDs.Select(x => x.ToString())); }
        }

        public PartitionData(int citation, int phylogeny_node_id, List<int> outgroupTaxa, double divergenceTime) : this()
        {
            this.citation = citation;
            this.outgroupTaxa = outgroupTaxa;
            this.PhylogenyNodeIDs.Add(phylogeny_node_id);
            this.DivergenceTime = divergenceTime;
        }

        public PartitionData(int citation, List<int> nodesA, List<int> nodesB, double divergenceTime) : this()
        {
            this.citation = citation;
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.DivergenceTime = divergenceTime;
        }

        public PartitionData(int citation, List<int> phylogeny_node_ids, List<int> nodesA, List<int> nodesB, List<int> outgroup, double divergenceTime)
        {
            this.citation = citation;
            this.PhylogenyNodeIDs = phylogeny_node_ids;
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.outgroup = outgroup;
            this.DivergenceTime = divergenceTime;
        }

        public PartitionData(int citation, int phylogeny_node_id, List<int> nodesA, List<int> nodesB, List<int> outgroup, double divergenceTime)
        {
            this.citation = citation;
            this.PhylogenyNodeIDs = new List<int>();
            this.PhylogenyNodeIDs.Add(phylogeny_node_id);
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.outgroup = outgroup;
            this.DivergenceTime = divergenceTime;
        }

        public PartitionData(int citation, List<int> phylogeny_node_ids, int childA_phylogeny_node, int childB_phylogeny_node, List<int> nodesA, List<int> nodesB, List<int> outgroup, double divergenceTime, List<int> taxaA = null, List<int> taxaB = null) : this()
        {
            this.citation = citation;
            this.PhylogenyNodeIDs = phylogeny_node_ids;
            this.childA_phylogeny_node = childA_phylogeny_node;
            this.childB_phylogeny_node = childB_phylogeny_node;
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.outgroup = outgroup;
            this.DivergenceTime = divergenceTime;

            this.taxaA = taxaA;
            this.taxaB = taxaB;
        }

        public PartitionData(int citation, int phylogeny_node_id, int childA_phylogeny_node, int childB_phylogeny_node, List<int> nodesA, List<int> nodesB, List<int> outgroup, double divergenceTime, List<int> taxaA = null, List<int> taxaB = null) : this()
        {
            this.citation = citation;
            this.PhylogenyNodeIDs = new List<int>();
            this.PhylogenyNodeIDs.Add(phylogeny_node_id);
            this.childA_phylogeny_node = childA_phylogeny_node;
            this.childB_phylogeny_node = childB_phylogeny_node;
            this.nodesA = nodesA;
            this.nodesB = nodesB;
            this.outgroup = outgroup;
            this.DivergenceTime = divergenceTime;

            this.taxaA = taxaA;
            this.taxaB = taxaB;
        }

        public override string ToString()
        {
            return String.Join(",", this.nodesA) + " | " + String.Join(",", this.nodesB);
        }

        public static bool SameSplit(PartitionData cladeA, PartitionData cladeB)
        {
            if (cladeA.nodesA.Any(cladeB.nodesA.Contains) && cladeA.nodesB.Any(cladeB.nodesB.Contains))
                return true;
            return false;
        }

        public static int PartitionAgreement(PartitionData cladeA, PartitionData cladeB)
        {
            bool A1overlapB1 = cladeA.nodesA.Any(cladeB.nodesA.Contains);
            bool A2overlapB1 = cladeA.nodesB.Any(cladeB.nodesA.Contains);
            bool A1overlapB2 = cladeA.nodesA.Any(cladeB.nodesB.Contains);
            bool A2overlapB2 = cladeA.nodesB.Any(cladeB.nodesB.Contains);
            bool A1overlapOT = cladeA.nodesA.Any(cladeB.outgroup.Contains);
            bool A2overlapOT = cladeA.nodesB.Any(cladeB.outgroup.Contains);
            bool B1overlapOT = cladeB.nodesA.Any(cladeA.outgroup.Contains);
            bool B2overlapOT = cladeB.nodesB.Any(cladeA.outgroup.Contains);
            //bool outgroup = A1overlapOT || B2overlapOT ||


            // phylogenies are compatible
            if (A1overlapB1 && A2overlapB2 && !(A1overlapB2 || A2overlapB1))
                if (cladeA.nodesA.Any(cladeB.outgroup.Contains) ||
                    cladeA.nodesB.Any(cladeB.outgroup.Contains) ||
                    cladeB.nodesA.Any(cladeA.outgroup.Contains) ||
                    cladeB.nodesB.Any(cladeA.outgroup.Contains))
                    return -1;
                else
                    return 1;

            if (A1overlapB2 && A2overlapB1 && !(A1overlapB1 || A2overlapB2))
                if (cladeA.nodesA.Any(cladeB.outgroup.Contains) ||
                    cladeA.nodesB.Any(cladeB.outgroup.Contains) ||
                    cladeB.nodesA.Any(cladeA.outgroup.Contains) ||
                    cladeB.nodesB.Any(cladeA.outgroup.Contains))
                    return -1;
                else
                    return 1;

            // phylogenies incompatible
            if (A1overlapB1 && A1overlapB2 && (A2overlapB1 || A2overlapB2))
                return -1;

            if (A2overlapB1 && A2overlapB2 && (A1overlapB1 || A1overlapB2))
                return -1;

            return 0;
        }

       
    }

    public class PartitionVotes
    {
        //private Dictionary<string, ExtendedNode> taxonNames;

        private ILookup<string, TopoTimeNode> taxonNames;
        private Dictionary<TopoTimeNode, int> votesFavor;
        private Dictionary<TopoTimeNode, int> votesAgainst;


        public PartitionVotes(IEnumerable<TopoTimeNode> leafList)
        {
            //taxonNames = leafList.ToDictionary(x => x.TaxaName);
            taxonNames = leafList.ToLookup(x => x.TaxonName);
            votesFavor = leafList.ToDictionary(x => x, x => 0);
            votesAgainst = leafList.ToDictionary(x => x, x => 0);
        }

        public void IncrementFavor(string taxonName)
        {
            try
            {
                IEnumerable<TopoTimeNode> foundTaxa = taxonNames[taxonName];

                foreach (TopoTimeNode foundTaxon in foundTaxa)
                {
                    try
                    {
                        votesFavor[foundTaxon]++;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void IncrementAgainst(string taxonName)
        {
            try
            {
                IEnumerable<TopoTimeNode> foundTaxa = taxonNames[taxonName];

                foreach (TopoTimeNode foundTaxon in foundTaxa)
                {
                    try
                    {
                        votesAgainst[foundTaxon]++;
                    }
                    catch { }
                }
            }
            catch { }
        }

        public IEnumerable<TopoTimeNode> VotingResults
        {
            get
            {
                return votesFavor.Keys.Where(x => votesAgainst[x] > votesFavor[x]);
            }
        }
    }
}
