using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml.Serialization;
using TimeTreeShared;

namespace TopoTimeShared
{


    [Serializable]

    public class TopoNodeCollection : IList
    {
        private ExtendedNodeCollection baseCollection;

        public TopoNodeCollection(ExtendedNodeCollection baseCollection)
        {
            this.baseCollection = baseCollection;
        }

        public TopoTimeNode this[int i]
        {
            get { return (TopoTimeNode)baseCollection[i]; }
            set { baseCollection[i] = value; }
        }

        object IList.this[int index]
        {
            get { return baseCollection[index]; }
            set { baseCollection[index] = (TopoTimeNode)value; }
        }

        public int Count
        {
            get { return baseCollection.Count; }
        }


        #region functions that must be implemented in order to implement IList
        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            // TO-DO: Verify this works, alternate may be "return this"
            get { return baseCollection; }
        }

        public bool IsReadOnly
        {
            get { return baseCollection.IsReadOnly; }
        }
        #endregion

        public int Add(object child)
        {
            if (this != child)
                return baseCollection.Add((TopoTimeNode)child);
            else
                return -1;
        }

        public void AddRange(TopoTimeNode[] nodes)
        {
            baseCollection.AddRange(nodes);
        }

        public void Clear()
        {
            baseCollection.Clear();
        }

        public void CopyTo(Array dest, int index)
        {
            baseCollection.CopyTo(dest, index);
        }

        public bool Contains(object node)
        {
            return baseCollection.Contains((ExtendedNode)node);
        }

        public int IndexOf(object node)
        {
            return baseCollection.IndexOf((ExtendedNode)node);
        }

        public void Insert(int index, object node)
        {
            baseCollection.Insert(index, (ExtendedNode)node);
        }

        public void Insert(int index, ExtendedNode node)
        {
            baseCollection.Insert(index, node);
        }

        public void Remove(object node)
        {
            baseCollection.Remove((ExtendedNode)node);
        }

        public void RemoveAt(int index)
        {
            baseCollection.RemoveAt(index);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            // call the generic version of the method
            return baseCollection.GetEnumerator2();
        }

    }

    [Serializable]
    public class TopoTimeNode : ExtendedNode
    {
        public TopoTimeNode() : base()
        {
            baseTreeNodes = new TopoNodeCollection(base.Nodes);
        }

        public TopoTimeNode(string text, int taxaID) : this()
        {
            TaxonID = taxaID;

            this.Name = taxaID.ToString();
            this.TaxonName = text;
        }        

        protected TopoTimeNode(SerializationInfo info, StreamingContext ctx) : base(info, ctx)
        {
            baseTreeNodes = new TopoNodeCollection(base.Nodes);
            isSpecies = info.GetBoolean("isSpecies");
            storedInputNodeHeight = info.GetDouble("storedInputNodeHeight");
            storedAdjustedHeight = info.GetDouble("storedAdjustedHeight");
            storedDistinctiveness = info.GetDouble("storedDistinctiveness");
            storedEffectiveStudies = info.GetInt32("storedEffectiveStudies");
            UniqueID = info.GetInt32("UniqueID");
            PartitionData = (SplitData)info.GetValue("PartitionData", typeof(SplitData));
            percentSupport = info.GetDouble("percentSupport");
            TotalStudies = info.GetInt32("TotalStudies");
            SupportingStudies = info.GetInt32("SupportingStudies");

            //rearranged = info.GetBoolean("rearranged");
        }
        
        protected override void Serialize(SerializationInfo info, StreamingContext context)
        {
            base.Serialize(info, context);
            info.AddValue("isSpecies", isSpecies);
            info.AddValue("storedInputNodeHeight", storedInputNodeHeight);
            info.AddValue("storedAdjustedHeight", storedAdjustedHeight);
            info.AddValue("storedDistinctiveness", storedDistinctiveness);
            info.AddValue("storedEffectiveStudies", storedEffectiveStudies);
            info.AddValue("UniqueID", UniqueID);
            info.AddValue("PartitionData", PartitionData);
            info.AddValue("Rearranged", rearranged);
            info.AddValue("percentSupport", percentSupport);
        }

        public override object Clone()
        {
            // TO-DO: is there a way to populate the list of fields automatically?

            TopoTimeNode clone = ObjectExtensions.DeepClone(this);
            return clone;
        }

        public new TopoTimeNode Parent
        {
            get { return (TopoTimeNode)base.Parent; }
        }

        private readonly TopoNodeCollection baseTreeNodes;
        public new TopoNodeCollection Nodes
        {
            get { return baseTreeNodes; }
        }

        public override void UpdateNode()
        {
            UpdateText();
        }

        public SerializableNode SerializedData
        {
            get
            {
                return new SerializableNode(this);
            }
        }

        public TopoTimeNode CloneForTree(TopoTimeTree tree)
        {
            return SerializedData.DeserializedNode(tree);
        }

        public TopoTimeNode CloneAndShuffle(TopoTimeTree tree)
        {
            SerializableNode thisNode = SerializedData;
            ShuffleNodes(thisNode);
            return thisNode.DeserializedNode(tree);
        }

        public void ShuffleNodes(SerializableNode Node)
        {
            Random rng = new Random();
            int n = Node.Nodes.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                SerializableNode value = Node.Nodes[k];
                Node.Nodes[k] = Node.Nodes[n];
                Node.Nodes[n] = value;
            }
        }

        [OptionalField]
        public SplitData PartitionData;
        
        public double storedRandomizedHeight;
        public int TotalStudies;
        public int SupportingStudies;
        public bool Floating;

        
        

        #region unused?
        public double? HeightDiff
        {
            get
            {
                double thisHeight = this.getNodeHeight(true);
                TopoTimeNode ancestor = (TopoTimeNode)this.Parent;
                while (ancestor != null && ancestor.getNodeHeight(true) >= thisHeight)
                {
                    ancestor = (TopoTimeNode)ancestor.Parent;
                }

                if (ancestor == null)
                    return null;
                else
                    return (double)thisHeight - (double)ancestor.getNodeHeight(true);
            }
        }

        public int SupportAB
        {
            get
            {
                if (PartitionData != null)
                    return PartitionData.FavorAB;
                return 0;
            }
        }

        public int SupportAC
        {
            get
            {
                if (PartitionData != null)
                    return PartitionData.FavorAC;
                return 0;
            }
        }

        public int SupportBC
        {
            get
            {
                if (PartitionData != null)
                    return PartitionData.FavorBC;
                return 0;
            }
        }

        #endregion


        public int LevelsFromNegativeBranch
        {
            get
            {
                double thisHeight = this.getNodeHeight(true);
                TopoTimeNode ancestor = (TopoTimeNode)this.Parent;
                int level = 1;
                while (ancestor != null && ancestor.getNodeHeight(true) >= thisHeight)
                {
                    level++;
                    ancestor = (TopoTimeNode)ancestor.Parent;
                }

                if (ancestor == null)
                    return -1;
                else
                    return level;
            }
        }

        public TopoTimeNode GetDistantHigherParent
        {
            get
            {
                double thisHeight = this.getNodeHeight(true);
                TopoTimeNode ancestor = (TopoTimeNode)this.Parent;
                int level = 0;
                while (ancestor != null && ancestor.getNodeHeight(true) < thisHeight)
                {
                    level++;
                    ancestor = (TopoTimeNode)ancestor.Parent;
                }

                if (ancestor == null)
                    return null;

                if (level == 0)
                    return null;

                return ancestor;
            }
        }

        public TopoTimeNode GetNamedParent
        {
            get
            {
                TopoTimeNode NamedNode = this;
                while (NamedNode.TaxonName == null && NamedNode.Parent != null)
                {
                    NamedNode = NamedNode.Parent;
                }

                return NamedNode;
            }
        }

        public string GetKingdom
        {
            get
            {
                string kingdom = "";
                TopoTimeNode NamedNode = this;
                while (kingdom == "" && NamedNode.Parent != null)
                {
                    if (NamedNode.TaxonName != null)
                    {
                        switch (NamedNode.TaxonID)
                        {
                            case 2:
                                kingdom = "Bacteria";
                                break;
                            case 2759:
                                kingdom = "Eukaryota";
                                break;
                            case 2157:
                                kingdom = "Archaea";
                                break;
                        }
                    }

                    NamedNode = NamedNode.Parent;
                }

                return kingdom;
            }
        }

        public PartitionVotes SingleTaxonChanges(TopoTimeTree hostTree)
        {
            
            {
                PartitionVotes individualTaxaCount = new PartitionVotes(this.Parent.getNamedChildren());

                foreach (StudyData partition in this.PartitionData.studyData)
                {
                    if (partition.taxaGroupB != null)
                    {
                        foreach (string leafTaxa in partition.taxaGroupA)
                        {
                            string leafTaxonName = Functions.TruncateSubspecies(leafTaxa);

                            individualTaxaCount.IncrementFavor(leafTaxonName);
                        }

                        // for new trees (2022)
                        // incomplete, may not handle subspecies missing from TimeTree object includedTaxa list
                        foreach (int leafTaxonID in partition.TaxaAinA.Concat(partition.TaxaBinA.Concat(partition.TaxaCinA)))
                        {
                            string leafTaxonName = hostTree.includedTaxa[leafTaxonID];
                            leafTaxonName = Functions.TruncateSubspecies(leafTaxonName);

                            individualTaxaCount.IncrementFavor(leafTaxonName);
                        }

                        foreach (string leafTaxa in partition.taxaGroupB)
                        {
                            string leafTaxonName = Functions.TruncateSubspecies(leafTaxa);

                            individualTaxaCount.IncrementFavor(leafTaxonName);
                        }

                        // for new trees (2022)
                        // incomplete, may not handle subspecies missing from TimeTree object includedTaxa list
                        foreach (int leafTaxonID in partition.TaxaBinB.Concat(partition.TaxaAinB.Concat(partition.TaxaCinB)))
                        {
                            string leafTaxonName = hostTree.includedTaxa[leafTaxonID];
                            leafTaxonName = Functions.TruncateSubspecies(leafTaxonName);

                            individualTaxaCount.IncrementFavor(leafTaxonName);
                        }

                        foreach (string leafTaxa in partition.taxaGroupC)
                        {
                            if (leafTaxa.Contains("[A]") || leafTaxa.Contains("[B]"))
                            {
                                // this is an extremely inefficient way of going about thios but it beats redesigning the object structure for now

                                string leafTaxonName = Functions.TruncateSubspecies(leafTaxa);
                                individualTaxaCount.IncrementAgainst(leafTaxonName);
                            }
                        }

                        // for new trees (2022), the redesigned object structure in the aforementioned comment
                        // incomplete, may not handle subspecies missing from TimeTree object includedTaxa list
                        foreach (int leafTaxonID in partition.TaxaAinC.Concat(partition.TaxaBinC))
                        {
                            string leafTaxonName = hostTree.includedTaxa[leafTaxonID];
                            leafTaxonName = Functions.TruncateSubspecies(leafTaxonName);

                            individualTaxaCount.IncrementAgainst(leafTaxonName);
                        }
                    }
                }

                return individualTaxaCount;
            }
        }

        public bool rearranged { get; set; }
        public double percentSupport { get; set; }
        

        /*
        private readonly ExtendedNodeCollection baseTreeNodes;
        public new ExtendedNodeCollection Nodes
        {
            get { return baseTreeNodes; }
        }
        */

        public string NamedNodeList
        {
            get
            {
                if (this.TaxonName != null && this.TaxonName != "")
                    return Functions.ShortName(this.TaxonName);
                else
                    return String.Join(",", this.Nodes.Cast<TopoTimeNode>().Select(x => x.NamedNodeList));
            }
        }

        #region suspect nodes fields, should find a way to have them listed without adding specific properties
        public string DivergenceList
        {
            get
            {
                String timeList = "";

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    timeList = timeList + time.DivergenceTime.ToString() + ",";
                }

                return timeList;
            }
        }

        public double DivergenceRatio
        {
            get
            {
                double min = Double.PositiveInfinity;
                double max = Double.NegativeInfinity;

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    if ((double)time.DivergenceTime > max)
                        max = (double)time.DivergenceTime;

                    if ((double)time.DivergenceTime < min)
                        min = (double)time.DivergenceTime;
                }

                return Math.Round(max / min, 2);
            }
        }

        public string DivergenceLargest
        {
            get
            {
                double max = Double.NegativeInfinity;
                string maxStudy = "";

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    if ((double)time.DivergenceTime > max)
                    {
                        max = (double)time.DivergenceTime;
                        maxStudy = time.PublicationID + " [" + time.DivergenceTime + "]";
                    }
                }

                return maxStudy;
            }
        }

        public ChildPairDivergence LargestDivergenceNode
        {
            get
            {
                double max = Double.NegativeInfinity;
                ChildPairDivergence maxNode = null;

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    if ((double)time.DivergenceTime > max)
                    {
                        max = (double)time.DivergenceTime;
                        maxNode = time;
                    }
                }

                return maxNode;
            }
        }

        public ChildPairDivergence SmallestDivergenceNode
        {
            get
            {
                double min = Double.PositiveInfinity;
                ChildPairDivergence minNode = null;

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    if ((double)time.DivergenceTime < min)
                    {
                        min = (double)time.DivergenceTime;
                        minNode = time;
                    }
                }

                return minNode;
            }
        }

        public string DivergenceSmallest
        {
            get
            {
                double min = Double.PositiveInfinity;
                string minStudy = "";

                foreach (ChildPairDivergence time in ChildDivergences)
                {
                    if ((double)time.DivergenceTime < min)
                    {
                        min = (double)time.DivergenceTime;
                        minStudy = time.PublicationID + " [" + time.DivergenceTime + "]";
                    }
                }

                return minStudy;
            }
        }
        #endregion









        public double MinConfidenceInterval { get; set; }
        public double MaxConfidenceInterval { get; set; }

        public List<TopoTimeNode> storedNamedNodes;
        public Dictionary<TopoTimeNode, int> storedFloatingNodes;

        public bool isSpecies;
        public double? storedInputNodeHeight = null;        

        public override string ToString()
        {
            return TaxonName + TaxonIDLabel;
        }

        public string TaxonIDLabel
        {
            get
            {
                if (Nodes.Count == 0)
                {
                    if (TaxonID == -1)
                        return " [ERROR]";
                    else
                        return " [" + TaxonID + "]";
                }
                else
                    return "";
            }
        }

        public int storedEffectiveStudies;
        public double storedDistinctiveness;

        public void UpdateText()
        {
            OnUpdateTextRequest(EventArgs.Empty);
        }

        public double getDistinctiveness()
        {
            if (this.Parent == null)
                return 0;
            else
            {
                double score;
                if (storedDistinctiveness == 0)
                {
                    if (this.Nodes.Count == 0)
                        score = (double)this.getBranchLength();
                    else
                        score = (double)this.getBranchLength() / this.getLeaves(false).Count;
                    storedDistinctiveness = score;
                }
                else
                    score = storedDistinctiveness;


                return score + ((TopoTimeNode)this.Parent).getDistinctiveness();
            }          
        }

        public TopoTimeNode getTimeSource()
        {
            int divCount = ChildDivergences.Count;
            if (divCount == 0)
            {
                TopoTimeNode timeSource = null;

                double height = 0.0;
                foreach (TopoTimeNode child in this.Nodes)
                {
                    double tempHeight = child.getNodeHeight(true);

                    if (tempHeight > height)
                    {
                        height = tempHeight;
                        timeSource = child;
                    }
                }

                if (timeSource != null)
                    return timeSource.getTimeSource();
            }

            return this;            
        }

        public int getSampleSize(bool useChildHeights)
        {
            int divCount = ChildDivergences.Count;

            if (divCount == 0)
            {
                if (useChildHeights)
                {
                    TopoTimeNode timeSource = null;
                    double height = 0.0;
                    foreach (TopoTimeNode child in this.Nodes)
                    {
                        double tempHeight = child.getNodeHeight(useChildHeights);

                        if (tempHeight > height)
                        {
                            height = tempHeight;
                            timeSource = child;
                        }
                    }

                    if (timeSource != null)
                        return timeSource.getSampleSize(useChildHeights);
                }

                return 0;
            }

            return ChildDivergences.Count;
        }



        // TO-DO: Review use of double\double, can we develop some sort of custom construct
        /*
        public double getNodeHeight(bool useChildHeights, bool ignoreIncompatibleTimes = false)
        {            
            double height = 0.0;

            int divCount = ChildDivergences.Count;

            if (divCount > 0)
            {
                if (this.Nodes.Count == 2)
                {                    
                    foreach (ChildPairDivergence divergence in ChildDivergences)
                    {
                        if (!(Double.IsNaN((double)divergence.DivergenceTime) || Double.IsInfinity((double)divergence.DivergenceTime)) && (!ignoreIncompatibleTimes || !divergence.IsConflict))
                        {
                            height = height + (double)divergence.DivergenceTime;
                        }
                        else
                        {
                            divCount--;
                        }
                    }
                }
                else
                {
                    foreach (ChildPairDivergence divergence in ChildDivergences)
                    {
                        if (!(Double.IsNaN((double)divergence.DivergenceTime) || Double.IsInfinity((double)divergence.DivergenceTime)) && (!ignoreIncompatibleTimes || !divergence.IsConflict))
                        {
                            if ((double)divergence.DivergenceTime > height)
                                height = (double)divergence.DivergenceTime;

                            divCount = 1;
                        }
                    }
                }
            }
            

            if (divCount > 0)
                height = height / divCount;
            else
            {
                if (useChildHeights)
                {
                    foreach (TopoTimeNode child in this.Nodes)
                    {
                        double temp = child.getNodeHeight(useChildHeights);
                        if (temp > height)
                            height = temp;
                    }
                }
            }

            return height;
        }
        */



        public string writeLogNode()
        {
            StringBuilder sb = new StringBuilder();

            if (this.Nodes.Count == 0)
            {
                if (this.TaxonName == null)
                    this.TaxonName = "NULL";

                sb.Append("'" + this.TaxonName.Replace(' ', '_').Replace("'", "").Replace('(', '[').Replace(')', ']').Replace(':', '|') + "':" + this.getLogBranchLength().ToString());
                //sb.Append("X:" + this.getBranchLength().ToString());
            }
            else if (this.Nodes.Count == 1)
            {
                TopoTimeNode child = (TopoTimeNode)this.Nodes[0];
                sb.Append(child.writeLogNode());
            }
            else
            {
                sb.Append("(");

                int childCount = 1;
                foreach (TopoTimeNode child in this.Nodes)
                {
                    sb.Append(child.writeLogNode());
                    if (childCount != this.Nodes.Count)
                    {
                        sb.Append(",");
                    }
                    childCount++;
                }

                if (this.TaxonName == null)
                    this.TaxonName = "";

                if (this.Parent == null)
                    sb.Append(")" + this.ChildDivergences.Count);
                else
                    sb.Append(")" + this.ChildDivergences.Count + /*this.TaxaName.Replace(' ', '_') + */ ":" + this.getLogBranchLength().ToString());
            }

            return sb.ToString();
        }


        public enum TreeWritingMode
        {
            Timed,
            UniqueIDs,
            TreeValidateNewick,
            PartitionPercent,
            PartitionConsensus,
            TaxonIDs
        }

        public new List<TopoTimeNode> getLeaves(bool reset)
        {
            return base.getLeaves(reset).Cast<TopoTimeNode>().ToList();
        }



        public string writeNode(TreeWritingMode mode)
        {
            StringBuilder sb = new StringBuilder();

            if (this.Nodes.Count == 0)
            {
                string taxonName = "";
                if (this.TaxonName != null)
                    taxonName = this.TaxonName;

                // TEMP: FOR OUTPUTTING NODE IDS ONLY
                //sb.Append(this.TaxaID + ":" + this.getBranchLength().ToString());
                // ORIGINAL LINE
                if (mode == TreeWritingMode.UniqueIDs)
                    sb.Append(this.UniqueID + ":" + this.getBranchLength().ToString());
                else if (mode == TreeWritingMode.TaxonIDs)
                    sb.Append(this.TaxonID + ":" + this.getBranchLength().ToString());
                else if (mode == TreeWritingMode.TreeValidateNewick)
                    sb.Append("{" + this.TaxonID.ToString() + "} " + "'" + this.TaxonName.Replace(' ', '_').Replace("'", "").Replace('(', '[').Replace(')', ']').Replace(':', '|') + "':" + this.getBranchLength().ToString("0.################"));
                else if (mode == TreeWritingMode.PartitionPercent || mode == TreeWritingMode.PartitionConsensus)
                    sb.Append("'" + taxonName.Replace(' ', '_').Replace("'", "").Replace('(', '[').Replace(')', ']').Replace(':', '|') + "'");
                else
                    sb.Append("'" + taxonName.Replace(' ', '_').Replace("'", "").Replace('(', '[').Replace(')', ']').Replace(':', '|') + "':" + this.getBranchLength().ToString("0.################"));
                
                
                //sb.Append("X:" + this.getBranchLength().ToString());
            }
            else if (this.Nodes.Count == 1)
            {
                TopoTimeNode child = (TopoTimeNode)this.Nodes[0];
                sb.Append(child.writeNode(mode));
            }
            else
            {
                sb.Append("(");

                int childCount = 1;
                foreach (TopoTimeNode child in this.Nodes)
                {
                    sb.Append(child.writeNode(mode));
                    if (childCount != this.Nodes.Count)
                    {
                        sb.Append(",");
                    }
                    childCount++;
                }

                //string ignore = "";
                //if (this.ChildDivergences.Count == 0)
                //    ignore = "*";

                if (this.Parent == null)
                    sb.Append(")");
                else
                {
                    if (mode == TreeWritingMode.UniqueIDs)
                        sb.Append(")'" + this.UniqueID + "':" + this.getBranchLength().ToString());
                    else if (mode == TreeWritingMode.PartitionPercent)
                        if (this.PartitionData != null && (this.PartitionData.FavorAB + this.PartitionData.FavorAC + this.PartitionData.FavorBC) > 0)
                            sb.Append("):" + Math.Round((double)(this.PartitionData.FavorAB * 100 / (this.PartitionData.FavorAB + this.PartitionData.FavorAC + this.PartitionData.FavorBC)), 0));
                        else
                            sb.Append(")");
                    else if (mode == TreeWritingMode.PartitionConsensus)
                        if (this.PartitionData != null && (this.PartitionData.FavorAB + this.PartitionData.FavorAC + this.PartitionData.FavorBC) > 0)
                            sb.Append("):" + this.PartitionData.FavorAB);
                        else
                            sb.Append(")");
                    else
                        sb.Append("):" + this.getBranchLength().ToString());
                }
            }

            return sb.ToString();
        }

        public double getLogHeight()
        {         
                if (this.storedAdjustedHeight <= 1)
                    return 0;
                else
                    return Math.Log10(this.StoredAdjustedHeight);
        }

        public double getLogBranchLength()
        {
            double height = this.getLogHeight();

            TopoTimeNode parent = (TopoTimeNode)(this.Parent);
            while (parent.Nodes.Count == 1)
                parent = (TopoTimeNode)parent.Parent;

            double parentHeight = parent.getLogHeight();

            return parentHeight - height;
        }

        public void StoreNamedNode(TopoTimeNode namedNode)
        {
            if (storedNamedNodes == null)
                storedNamedNodes = new List<TopoTimeNode>();

            if (namedNode.storedNamedNodes != null)
            {
                foreach (TopoTimeNode childNamedNode in namedNode.storedNamedNodes.ToList())
                {
                    storedNamedNodes.Add(childNamedNode);
                    namedNode.storedNamedNodes.Remove(childNamedNode);
                }
            }

            storedNamedNodes.Add(namedNode);
        }

        public void RemoveDuplicateStoredNamedNodes()
        {
            if (storedNamedNodes == null)
                return;
        }

        public void StoreFloatingNode(TopoTimeNode floatingNode, int floatingIndex)
        {
            if (storedFloatingNodes == null)
                storedFloatingNodes = new Dictionary<TopoTimeNode, int>();

            storedFloatingNodes.Add(floatingNode, floatingIndex);
        }

        public IEnumerable<TopoTimeNode> GetDescendants()
        {
            return this.Nodes.Cast<TopoTimeNode>().RecursiveSelect(x => x.Nodes.Cast<TopoTimeNode>()).Append(this);
        }

        public IEnumerable<TopoTimeNode> getNamedChildren(bool skipNestedNodes = false)
        {
            if (this.HasValidTaxon)
                yield return this;

            // this is a workaround to having to rebuild an entire tree
            // ideally, I should figure out why duplicate entries are generated in the stored named lists
            foreach (TopoTimeNode namedChild in getNamedChildrenOnly(skipNestedNodes).GroupBy(x => x.TaxonID).Select(g => g.First()))
                yield return namedChild;
        }

        public IEnumerable<TopoTimeNode> getNamedChildrenOnly(bool skipNestedNodes = false)
        {           

            if (!skipNestedNodes)
            {
                if (this.storedNamedNodes != null)
                    foreach (TopoTimeNode namedParent in this.storedNamedNodes)
                        yield return namedParent;
            }

            var namedNodes = this.Nodes.Cast<TopoTimeNode>().RecursiveSelect(x => x.Nodes.Cast<TopoTimeNode>()).Where(x => x.HasValidTaxon);
            foreach (TopoTimeNode child in namedNodes)
            {
                yield return child;
                if (!skipNestedNodes)
                {
                    if (child.storedNamedNodes != null)
                        foreach (TopoTimeNode ghostParent in child.storedNamedNodes)
                            yield return ghostParent;
                }
            }
        }


        public IEnumerable<TopoTimeNode> getFloatingSiblings()
        {
            TopoTimeNode parent = this.Parent;
            while (parent != null)
            {
                if (parent.Nodes.Cast<TopoTimeNode>().Any(x => x.Floating))
                    foreach (TopoTimeNode floatingNode in parent.Nodes.Cast<TopoTimeNode>().Where(x => x.Floating))
                        yield return floatingNode;
            }
        }

        

        public string ListNamedAncestors()
        {
            TopoTimeNode currentAncestor = (TopoTimeNode)this.Parent;
            StringBuilder ancestorList = new StringBuilder();
            while (currentAncestor != null)
            {
                ancestorList.Append(currentAncestor.TaxonName + ",");
                currentAncestor = (TopoTimeNode)currentAncestor.Parent;
            }

            return ancestorList.ToString();
        }
    }

}
