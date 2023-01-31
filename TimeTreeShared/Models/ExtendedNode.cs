using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.Serialization;



namespace TimeTreeShared
{
    [Serializable]
    public class ExtendedNodeCollection : IList
    {
        private TreeNodeCollection baseCollection;

        public ExtendedNodeCollection(TreeNodeCollection baseCollection)
        {
            this.baseCollection = baseCollection;
        }

        // new iterator
        public ExtendedNode this[int i]
        {
            get { return (ExtendedNode)baseCollection[i]; }
            set { baseCollection[i] = value; }
        }

        object IList.this[int index]
        {
            get { return baseCollection[index]; }
            set { baseCollection[index] = (ExtendedNode)value; }
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
                return baseCollection.Add((ExtendedNode)child);
            else
                return -1;
        }

        public void AddRange(ExtendedNode[] nodes)
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



        public int IndexOf(ExtendedNode node)
        {
            return baseCollection.IndexOf(node);
        }

        /*
        public ExtendedNode Insert(int index, string text)
        {
            ExtendedNode newNode = new ExtendedNode
            {
                TaxonName = text
            };
            baseCollection.Insert(index, newNode);
            return newNode;
        }
        */

        IEnumerator IEnumerable.GetEnumerator()
        {
            // call the generic version of the method
            return baseCollection.GetEnumerator();
        }

        public IEnumerator GetEnumerator2()
        {
            return baseCollection.GetEnumerator();
        }
         
    }

    [Serializable]
    public abstract class ExtendedNode : TreeNode
    {
        protected ExtendedNode(SerializationInfo info, StreamingContext ctx) : base(info, ctx)
        {
            baseTreeNodes = new ExtendedNodeCollection(base.Nodes);
            TaxonID = info.GetInt32("TaxonID");
            TaxonName = info.GetString("TaxonName");
            ChildDivergences = (List<ChildPairDivergence>)info.GetValue("ChildDivergences", typeof(List<ChildPairDivergence>));

            storedLeafList = (List<ExtendedNode>)info.GetValue("storedLeafList", typeof(List<ExtendedNode>));
            try { storedAdjustedHeight = info.GetDouble("storedAdjustedHeight"); } catch { }
        }

        protected override void Serialize(SerializationInfo info, StreamingContext context)
        {
            base.Serialize(info, context);
            info.AddValue("TaxonID", TaxonID);
            info.AddValue("TaxonName", TaxonName);
            info.AddValue("ChildDivergences", ChildDivergences);
            info.AddValue("storedLeafList", storedLeafList);
            info.AddValue("storedAdjustedHeight", storedAdjustedHeight);
            info.AddValue("UniqueID", UniqueID);
        }

        public override object Clone()
        {
            // TO-DO: is there a way to populate the list of fields automatically?

            ExtendedNode clone = ObjectExtensions.DeepClone(this);
            return clone;
        }

        public object BaseClone()
        {
            return base.Clone();
        }
        public event EventHandler UpdateTextRequest;
        protected virtual void OnUpdateTextRequest(EventArgs e)
        {
            if (UpdateTextRequest != null)
                UpdateTextRequest.Invoke(this, e);
        }

        public abstract void UpdateNode();
        public new ExtendedNode Parent
        {
            get { return (ExtendedNode)base.Parent; }
        }

        private readonly ExtendedNodeCollection baseTreeNodes;
        public new ExtendedNodeCollection Nodes
        {
            get { return baseTreeNodes; }
        }

        private int _TaxonID;
        public int TaxonID
        {
            get { return _TaxonID; }
            set
            {
                _TaxonID = value;
                UpdateNode();
            }
        }

        public void SetTaxonData(int? TaxonID = null, string TaxonName = null)
        {
            if (TaxonName != null)
                _TaxonName = TaxonName;

            if (TaxonID != null)
                _TaxonID = (int)TaxonID;
        }

        private string _TaxonName;
        public string TaxonName
        {
            get
            {
                return _TaxonName;
            }
            set
            {
                _TaxonName = value;
                UpdateNode();
            }
        }    


        public List<ChildPairDivergence> ChildDivergences { get; set; }

        public List<ExtendedNode> storedLeafList;
        public int UniqueID;

        public ExtendedNode()
        {
            ChildDivergences = new List<ChildPairDivergence>();
            baseTreeNodes = new ExtendedNodeCollection(base.Nodes);
        }

        public override string ToString()
        {
            return TaxonName + LeafLabel;
        }

        // WARNING: Dynamic fields were implemented for TreeValidate, and were never meant to be serialized
        // Do not use in TopoTime for any information that needs to be saved to file

        private Dictionary<string, object> properties;
        public Dictionary<string, object> DynamicFields
        {
            get
            {
                if (properties == null)
                    properties = new Dictionary<string, object>();

                return properties;
            }
            set { properties = value; }
        }

        // accessor for dynamic properties
        public object this[string name]
        {
            get
            {
                if (properties != null && properties.ContainsKey(name))
                {
                    return properties[name];
                }
                return null;
            }
            set
            {
                if (properties == null)
                    properties = new Dictionary<string, object>();

                properties[name] = value;
            }
        }

        public string LeafLabel
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

        


        public double StoredAdjustedHeight
        {
            get 
            { 
                if (storedAdjustedHeight != null)
                    return Math.Round((double)storedAdjustedHeight, 5);
                else
                    return 0;
            }
            set
            {
                storedAdjustedHeight = value;
            }
        }

        public double? storedAdjustedHeight;

        public bool HasValidTaxon
        {
            get { return this.TaxonID != -1 && this.TaxonID != 0; }
        }

        public double getNodeMedian(bool useChildHeights)
        {
            int divCount = ChildDivergences.Count;

            if (divCount == 0)
            {
                if (useChildHeights)
                {
                    double height = 0;
                    foreach (ExtendedNode child in this.Nodes)
                    {
                        double tempHeight = child.getNodeMedian(useChildHeights);

                        if (tempHeight > height)
                            height = tempHeight;
                    }

                    return height;
                }

                return 0;
            }

            if (divCount == 1)
                return (double)ChildDivergences[0].DivergenceTime;
            if (divCount == 2)
                return ((double)ChildDivergences[0].DivergenceTime + (double)ChildDivergences[1].DivergenceTime) / 2;

            {
                ChildDivergences.Sort();

                //get the median
                int size = ChildDivergences.Count;
                int mid = size / 2;
                double median = ((size & 1) != 0) ? (double)ChildDivergences[(size - 1) / 2].DivergenceTime : ((double)ChildDivergences[mid].DivergenceTime + (double)ChildDivergences[mid - 1].DivergenceTime) / 2;
                return median;
            }
        }

        public double getNodeStandardError(bool useChildHeights)
        {
            double mean = getNodeHeight(useChildHeights);
            double stddev = 0.0;
            foreach (ChildPairDivergence divergence in ChildDivergences)
            {
                stddev = stddev + Math.Pow((double)(mean - divergence.DivergenceTime), 2);
            }

            stddev = Math.Sqrt(stddev);

            return (double)stddev / (double)Math.Sqrt(ChildDivergences.Count);
        }

        public double getCalculatedBranchLength(bool useChildHeights)
        {
            if (Parent == null)
                return 0;

            double height = this.getNodeHeight(useChildHeights);

            ExtendedNode parent = (ExtendedNode)(this.Parent);
            while (parent.Nodes.Count == 1)
                parent = (ExtendedNode)parent.Parent;

            double parentHeight = parent.getNodeHeight(useChildHeights);

            return parentHeight - height;
        }

        public double getNodeHeight(bool useChildHeights, bool ignoreIncompatibleTimes = false)
        {

            double height = 0;

            int divCount = ChildDivergences.Count;

            if (divCount > 0)
            {
                if (this.Nodes.Count == 2)
                {
                    foreach (ChildPairDivergence divergence in ChildDivergences)
                    {
                        if (!(divergence.DivergenceTime == null) && (!ignoreIncompatibleTimes || !divergence.IsConflict))
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
                        if (!(divergence.DivergenceTime == null) && (!ignoreIncompatibleTimes || !divergence.IsConflict))
                        {
                            if (divergence.DivergenceTime > height)
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
                    foreach (ExtendedNode child in this.Nodes)
                    {
                        double temp = child.getNodeHeight(useChildHeights);
                        if (temp > height)
                            height = temp;
                    }
                }
            }

            return height;
        }

        public bool isContributingTimeData()
        {
            if (this.ChildDivergences.Count == 0)
            {
                foreach (ExtendedNode child in this.Nodes)
                    if (child.isContributingTimeData())
                        return true;
                return false;
            }
            return true;
        }

        public decimal getBranchLength()
        {
            // NOTE!!
            // the reason this returns decimal type is because double type has precision errors in basic arithmetic
            // i.e. 0.03872 - 0.03869 = 0.00003000000000000022
            // DO NOT CHANGE!

            if (Parent == null)
                return 0;

            double height = this.StoredAdjustedHeight;

            ExtendedNode parent = (ExtendedNode)(this.Parent);
            while (parent.Nodes.Count == 1)
                parent = (ExtendedNode)parent.Parent;

            double parentHeight = parent.StoredAdjustedHeight;

            return (decimal)parentHeight - (decimal)height;
        }

        public string Path
        {
            get
            {
                return String.Join(">", this.Traverse(x => x.Parent).Where(y => y.HasValidTaxon));
            }
        }

        public ExtendedNode NextNamedParent
        {
            get
            {
                ExtendedNode Parent = this.Parent;
                while (Parent != null && Parent.HasValidTaxon)
                {
                    Parent = Parent.Parent;
                }
                return Parent;
            }
        }

        public List<ExtendedNode> getLeaves(bool reset)
        {
            List<ExtendedNode> leafList;

            if (this.storedLeafList != null && reset == false)
                return storedLeafList;
            else
            {
                leafList = new List<ExtendedNode>();
                if (this.Nodes.Count == 0)
                {
                    this.storedLeafList = null;
                    leafList.Add(this);
                    return leafList;
                }
                else
                {
                    foreach (ExtendedNode child in Nodes)
                    {
                        leafList.AddRange(child.getLeaves(reset));
                    }
                    this.storedLeafList = leafList;
                    return leafList;
                }
            }
        }


        public void ReduceNodeToLeaves()
        {
            List<ExtendedNode> leaves = this.getLeaves(true);

            if (leaves.Count > 1)
            {
                foreach (ExtendedNode leaf in leaves)
                {
                    leaf.Parent.Nodes.Remove(leaf);
                }

                this.Nodes.Clear();
                this.Nodes.AddRange(leaves.ToArray());
            }
        }
    }    


    


    

}