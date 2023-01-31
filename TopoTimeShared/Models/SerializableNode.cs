using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Xml.Serialization;
using TimeTreeShared;

namespace TopoTimeShared
{
    public class SerializableNode
    {
        public int ForeColor { get; set; }
        public int BackColor { get; set; }
        public string Text { get; set; }
        public List<SerializableNode> Nodes { get; set; }
        public List<SerializableNode> StoredNamedNodes { get; set; }

        public int taxaID { get; set; }
        public string taxaName { get; set; }
        public List<ChildPairDivergence> childDivergences { get; set; }
        public bool isSpecies { get; set; }
        public double inputBranchLength { get; set; }
        public double? storedInputNodeHeight { get; set; }
        public double? storedAdjustedHeight { get; set; }
        public double storedDistinctiveness { get; set; }
        public int storedEffectiveStudies { get; set; }
        public int UniqueID { get; set; }
        public SplitData PartitionData { get; set; }
        public string Rank { get; set; }
        public string RepresentsPhylum { get; set; }
        public string RepresentsGenus { get; set; }
        public string RepresentsClass { get; set; }
        public string RepresentsFamily { get; set; }
        public string RepresentsOrder { get; set; }
        public double percentSupport { get; set; }
        public int SupportingStudies { get; set; }
        public int TotalStudies { get; set; }
        public bool IsMedian { get; set; }

        public bool Rearranged { get; set; }

        public SerializableDictionary<int, Study> includedStudies { get; set; }
        public SerializableDictionary<int, string> includedTaxa { get; set; }

        public List<string> OperationHistory { get; set; }

        public bool Floating { get; set; }

        public bool ShouldSerializeFloating()
        {
            return Floating;
        }

        public bool ShouldSerializeincludedStudies()
        {
            return includedStudies != null;
        }

        public bool ShouldSerializeincludedTaxa()
        {
            return includedTaxa != null;
        }

        public bool ShouldSerializeSupportingStudies()
        {
            return TotalStudies > 0;
        }

        public bool ShouldSerializeTotalStudies()
        {
            return TotalStudies > 0;
        }

        public bool ShouldSerializeOperationHistory()
        {
            return OperationHistory != null;
        }

        public bool ShouldSerializeIsMedian()
        {
            return IsMedian;
        }

        public bool ShouldSerializeRearranged()
        {
            return Rearranged;
        }

        public bool ShouldSerializeForeColor()
        {
            return ForeColor != 0;
        }

        public bool ShouldSerializeBackColor()
        { return false; }

        public bool ShouldSerializeText()
        {
            return !string.IsNullOrEmpty(Text);
        }

        public bool ShouldSerializeNodes()
        {
            return Nodes != null && Nodes.Count > 0;
        }

        public bool ShouldSerializeStoredNamedNodes()
        {
            return StoredNamedNodes != null && StoredNamedNodes.Count > 0;
        }

        public bool ShouldSerializeisSpecies()
        {
            return isSpecies;
        }

        public bool ShouldSerializeinputBranchLength()
        {
            return inputBranchLength > 0;
        }

        public bool ShouldSerializestoredInputNodeHeight()
        {
            return storedInputNodeHeight == 0;
        }

        public bool ShouldSerializechildDivergences()
        {
            return childDivergences == null || childDivergences.Count > 0;
        }

        public bool ShouldSerializestoredAdjustedHeight()
        {
            return !(storedAdjustedHeight == null || storedAdjustedHeight == 0);
        }

        public bool ShouldSerializestoredEffectiveStudies()
        {
            return storedEffectiveStudies > 0;
        }

        public bool ShouldSerializestoredDistinctiveness()
        {
            return storedDistinctiveness > 0;
        }

        public bool ShouldSerializetaxaName()
        {
            return !(taxaName == null || taxaName == "");
        }

        public bool ShouldSerializetaxaID()
        {
            return taxaID != 0 && taxaID != -1;
        }

        public bool ShouldSerializeUniqueID()
        { return UniqueID > 0; }

        public bool ShouldSerializepercentSupport()
        { return percentSupport != 0; }


        public SerializableNode()
        {
            ForeColor = 0;
            BackColor = -1;
        }

        public SerializableNode(TopoTimeNode sourceData, bool addNamedNodes = true)
        {
            
            
            if (addNamedNodes)
            {
                this.ForeColor = sourceData.ForeColor.IsEmpty ? 0x0 : sourceData.ForeColor.ToArgb();
                this.BackColor = sourceData.BackColor.IsEmpty ? -0x1 : sourceData.BackColor.ToArgb();
                this.Text = sourceData.Text;
                childDivergences = sourceData.ChildDivergences;
                storedInputNodeHeight = sourceData.storedInputNodeHeight;
                storedAdjustedHeight = sourceData.storedAdjustedHeight;
                storedDistinctiveness = sourceData.storedDistinctiveness;
                storedEffectiveStudies = sourceData.storedEffectiveStudies;
                UniqueID = sourceData.UniqueID;
                PartitionData = sourceData.PartitionData;
                Rearranged = sourceData.rearranged;
                percentSupport = sourceData.percentSupport;
                TotalStudies = sourceData.TotalStudies;
                SupportingStudies = sourceData.SupportingStudies;
                Floating = sourceData.Floating;

                if (sourceData.Nodes.Count > 0)
                {
                    this.Nodes = new List<SerializableNode>();
                    foreach (TopoTimeNode child in sourceData.Nodes)
                        this.Nodes.Add(new SerializableNode(child));
                }
            }
            else
            {
                this.ForeColor = 0x0;
                this.BackColor = -0x1;
            }

            taxaID = sourceData.TaxonID;
            taxaName = sourceData.TaxonName;            
            isSpecies = sourceData.isSpecies;        
            

            if (addNamedNodes && sourceData.storedNamedNodes != null && sourceData.storedNamedNodes.Count > 0)
            {
                this.StoredNamedNodes = new List<SerializableNode>();
                foreach (TopoTimeNode child in sourceData.storedNamedNodes)
                    this.StoredNamedNodes.Add(new SerializableNode(child, addNamedNodes: false));
            }
        }

        public TopoTimeTree DeserializedTree()
        {
            TopoTimeTree tree = new TopoTimeTree(this.IsMedian);
            tree.OperationHistory = this.OperationHistory;
            tree.includedStudies = this.includedStudies;
            tree.includedTaxa = this.includedTaxa;
            TopoTimeNode root = this.DeserializedNode(tree);
            tree.AddNodesToTree(root);
            tree.root = root;

            return tree;
        }

        public TopoTimeNode DeserializedNode(TopoTimeTree tree)
        {
            TopoTimeNode node = new TopoTimeNode();
            if (this.Nodes != null)
            {
                foreach (SerializableNode child in this.Nodes)
                {                    
                    TopoTimeNode childNode = child.DeserializedNode(tree);
                    node.Nodes.Add(childNode);

                    if (childNode.Floating)
                        tree.StoreFloatingNode(node, childNode);
                }
            }

            node.UpdateTextRequest += tree.UpdateNodeTextRequest;

            node.ForeColor = Color.FromArgb(this.ForeColor);
            node.BackColor = Color.FromArgb(this.BackColor);

            node.storedAdjustedHeight = this.storedAdjustedHeight;
            node.TaxonID = this.taxaID;
            node.TaxonName = this.taxaName;
            node.ChildDivergences = this.childDivergences;
            node.isSpecies = this.isSpecies;
            node.storedInputNodeHeight = this.storedInputNodeHeight;
            
            node.storedDistinctiveness = this.storedDistinctiveness;
            node.storedEffectiveStudies = this.storedEffectiveStudies;
            node.UniqueID = this.UniqueID;
            node.PartitionData = this.PartitionData;
            node.rearranged = this.Rearranged;
            node.storedNamedNodes = new List<TopoTimeNode>();
            node.percentSupport = this.percentSupport;
            node.Text = this.Text;
            node.TotalStudies = this.TotalStudies;
            node.SupportingStudies = this.SupportingStudies;
            node.Floating = this.Floating;

            if (this.StoredNamedNodes != null)
            {
                foreach (SerializableNode namedNode in this.StoredNamedNodes)
                    node.storedNamedNodes.Add(namedNode.DeserializedNode(tree));
            }

            return node;
        }

        protected SerializableNode(SerializationInfo info, StreamingContext ctx)
        {
            this.Nodes = (List<SerializableNode>)info.GetValue("Nodes", typeof(List<SerializableNode>));

            this.ForeColor = 0;
            this.BackColor = -1;

            this.Text = info.GetString("Text");

            taxaID = info.GetInt32("taxaID");
            taxaName = info.GetString("taxaName");
            childDivergences = (List<ChildPairDivergence>)info.GetValue("childDivergences", typeof(List<ChildPairDivergence>));
            isSpecies = info.GetBoolean("isSpecies");
            inputBranchLength = info.GetDouble("inputBranchLength");
            storedInputNodeHeight = info.GetDouble("storedInputNodeHeight");
            storedAdjustedHeight = info.GetDouble("storedAdjustedHeight");
            storedDistinctiveness = info.GetDouble("storedDistinctiveness");
            storedEffectiveStudies = info.GetInt32("storedEffectiveStudies");
            UniqueID = info.GetInt32("UniqueID");
            PartitionData = (SplitData)info.GetValue("PartitionData", typeof(SplitData));
            Rank = info.GetString("Rank");
            Rearranged = info.GetBoolean("Rearranged");
            StoredNamedNodes = (List<SerializableNode>)info.GetValue("StoredNamedNodes", typeof(List<SerializableNode>));
            percentSupport = info.GetDouble("percentSupport");
            TotalStudies = info.GetInt32("TotalStudies");
            SupportingStudies = info.GetInt32("SupportingStudies");
            Floating = info.GetBoolean("Floating");
        }

        protected void Serialize(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Nodes", Nodes);
            info.AddValue("ForeColor", this.ForeColor);
            info.AddValue("BackColor", this.BackColor);
            info.AddValue("Text", this.Text);
            info.AddValue("taxaID", taxaID);
            info.AddValue("taxaName", taxaName);
            info.AddValue("childDivergences", childDivergences);
            info.AddValue("isSpecies", isSpecies);
            info.AddValue("inputBranchLength", inputBranchLength);
            info.AddValue("storedInputNodeHeight", storedInputNodeHeight);
            info.AddValue("storedAdjustedHeight", storedAdjustedHeight);
            info.AddValue("storedDistinctiveness", storedDistinctiveness);
            info.AddValue("storedEffectiveStudies", storedEffectiveStudies);
            info.AddValue("UniqueID", UniqueID);
            info.AddValue("PartitionData", PartitionData);
            info.AddValue("Rank", Rank);
            info.AddValue("Rearranged", Rearranged);
            info.AddValue("StoredNamedNodes", StoredNamedNodes);
            info.AddValue("percentSupport", percentSupport);
            info.AddValue("TotalStudies", TotalStudies);
            info.AddValue("SupportingStudies", SupportingStudies);
        }
    }
}
