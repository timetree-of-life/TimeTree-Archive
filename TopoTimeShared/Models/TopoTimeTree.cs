using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Npgsql;
using TimeTreeShared;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Drawing2D;

namespace TopoTimeShared
{
    [Serializable]
    public class TopoTimeTree : TimeTree
    {
        public HashSet<TopoTimeNode> refreshList;
        public Dictionary<int, TopoTimeNode> leafDictionary;

        public List<string> OperationHistory;
        public SerializableDictionary<int, Study> includedStudies;
        public SerializableDictionary<int, string> includedTaxa;

        public List<SplitData> splitData;
        public Dictionary<SplitData, ExtendedNode> nodeAssociation;

        public int currentFloatingIndex = -1;


        private bool useMedianTimes = false;
        public bool UseMedianTimes
        {
            get { return useMedianTimes; }
        }

        public new TopoTimeNode root
        {
            get { return (TopoTimeNode)base.root; }
            set { base.root = value; }
        }

        public TopoTimeTree(bool UseMedianTimes = false) : base()
        {
            refreshList = new HashSet<TopoTimeNode>();
            OperationHistory = new List<string>();
            this.useMedianTimes = UseMedianTimes;
            includedStudies = new SerializableDictionary<int, Study>();
            includedTaxa = new SerializableDictionary<int, string>();
        }

        public TopoTimeTree(SerializationInfo info, StreamingContext ctx)
        {
            root = (TopoTimeNode)info.GetValue("root", typeof(TopoTimeNode));
        }

        public new void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            base.GetObjectData(info, ctx);
            info.AddValue("refreshList", refreshList);
            info.AddValue("OperationHistory", OperationHistory);
        }

        public TopoTimeTree Clone()
        {
            TopoTimeNode cloneRoot = new TopoTimeNode();
            TopoTimeTree cloneTree = new TopoTimeTree();

            cloneTree.useMedianTimes = this.useMedianTimes;

            cloneTree.refreshList = new HashSet<TopoTimeNode>();

            using (HugeMemoryStream stream = new HugeMemoryStream())
            {
                XmlAttributeOverrides overrideList = new XmlAttributeOverrides();
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlIgnore = true;
                overrideList.Add(typeof(ChildPairDivergence), "metadata", attrs);

                XmlSerializer x = new XmlSerializer(typeof(SerializableNode), overrideList);

                x.Serialize(stream, root.SerializedData);
                stream.Position = 0;

                SerializableNode rootData = (SerializableNode)x.Deserialize(stream);
                cloneRoot = rootData.DeserializedNode(cloneTree);
            }

            cloneTree.root = cloneRoot;
            cloneTree.AddNodesToTree(cloneTree.root);

            return cloneTree;
        }

        // this should theoretically work, but a number of nodes appear to be missing in the copy
        // TO-DO: review why
        public TopoTimeTree Clone2()
        {
            SerializableNode cloneRoot = CreateSerializedNode(root);
            return cloneRoot.DeserializedTree();
        }

        public SerializableNode CreateSerializedNode(TopoTimeNode targetRoot)
        {
            SerializableNode root = new SerializableNode(targetRoot);
            root.OperationHistory = this.OperationHistory;
            root.IsMedian = this.UseMedianTimes;
            root.includedTaxa = this.includedTaxa;
            root.includedStudies = this.includedStudies;

            if (targetRoot != this.root)
                root.OperationHistory.Add("Tree split into new session");

            return root;
        }

        public void UpdateNodeTextRequest(object sender, EventArgs e)
        {
            TopoTimeNode node = (TopoTimeNode)sender;
            UpdateNodeText(node);
        }

        public void UpdateNodeText(TopoTimeNode node)
        {
            // Color scheme
            // Color.DimGray - No partition favored
            // Color.Blue - Node height is adjusted
            // Color.Black - node tips
            // Color.ForestGreen - Normal node with calculated time
            // Color.IndianRed - Partition conflict
            // Color.LightBLue - floating
            // Color.LightGreen - node's calculated time contains topology conflicts

            string text = "";
            double age = GetNodeHeight(node, usingChildHeights: false);

            if (node.TaxonName != null && node.TaxonName != "")
                text = node.TaxonName + " ";

            if (node.HasValidTaxon)
                text += "[" + node.TaxonID + "] ";

            if (node.Nodes.Count > 0)
            {


                double adjustedHeight = node.StoredAdjustedHeight;

                if (adjustedHeight != 0 && adjustedHeight != age)
                {
                    node.ForeColor = Color.Blue;
                    if (node.HasValidTaxon)
                        text += "(" + adjustedHeight.ToString("0.00") + " | " + age.ToString("0.00") + ") ";
                    else
                        text += adjustedHeight.ToString("0.00") + " | " + age.ToString("0.00") + " ";
                }
                else
                {
                    if (node.Nodes.Count == 2)
                    {
                        if (node.ChildDivergences.Any(x => x.IsConflict))
                            node.ForeColor = Color.LightGreen;
                        else
                            node.ForeColor = Color.ForestGreen;

                        if (node.HasValidTaxon)
                            text += "(" + age.ToString("0.00") + ") ";
                        else
                            text += age.ToString("0.00") + " ";
                    }
                    else
                    {
                        node.ForeColor = Color.Black;
                    }
                }

            }
            else
            {
                node.ForeColor = Color.Black;
            }

            if (node.ChildDivergences.Count > 0)
            {
                text += "{" + node.ChildDivergences.Count + "}";
                if (node.Nodes.Count > 2)
                    text += " ???";
            }

            if (node.percentSupport > 0)
            {
                text += " [" + node.percentSupport.ToString("0%");
                if (node.TotalStudies > 0)
                {
                    text += " - " + node.SupportingStudies + "/" + node.TotalStudies;
                }
                text += "]";
            }

            if (node.rearranged && node.HasValidTaxon)
                text = "(unsupported) " + text;

            UpdateNodeColor(node);

            if (node.Floating)
                node.ForeColor = Color.LightBlue;

            node.Text = text;
        }

        public void UpdateNodeColor(TopoTimeNode node)
        {
            if (node.PartitionData != null)
            {
                if (node.PartitionData.FavorAB > node.PartitionData.FavorAC && node.PartitionData.FavorAB > node.PartitionData.FavorBC)
                    node.ForeColor = Color.ForestGreen;
                else if (node.PartitionData.FavorAB == node.PartitionData.FavorAC && node.PartitionData.FavorAB == node.PartitionData.FavorBC)
                    node.ForeColor = Color.DarkGray;
                else
                    node.ForeColor = Color.IndianRed;
            }
        }

        public double GetNodeHeight(TopoTimeNode node, bool usingChildHeights = true, bool ignoreConflictingTimes = true)
        {
            if (this.useMedianTimes)
                return node.getNodeMedian(usingChildHeights);
            else
                return node.getNodeHeight(usingChildHeights, ignoreConflictingTimes);
        }


        public void AddNode(TopoTimeNode node, TopoTimeNode parent)
        {
            parent.Nodes.Add(node);
            nodeList.Add(node);
        }

        public void CollapseIntoParent(TopoTimeNode node)
        {
            TopoTimeNode parent = node.Parent;
            if (parent == null)
                return;

            if (!node.HasValidTaxon && parent.storedAdjustedHeight == node.storedAdjustedHeight)
            {
                List<TopoTimeNode> children = node.Nodes.Cast<TopoTimeNode>().ToList();
                if (children.Count > 0)
                {
                    parent.Nodes.Remove(node);
                    foreach (TopoTimeNode child in children)
                    {
                        node.Nodes.Remove(child);
                        parent.Nodes.Add(child);
                    }
                    this.nodeList.Remove(node);
                }
            }
        }

        public void DeleteNode(TopoTimeNode selectedNode)
        {
            TopoTimeNode parent = (TopoTimeNode)selectedNode.Parent;

            this.nodeList.Remove(selectedNode);
            this.leafList.Remove(selectedNode);

            if (parent != null)
            {
                parent.Nodes.Remove(selectedNode);

                foreach (TopoTimeNode child in selectedNode.Nodes)
                {
                    if (child != null)
                        ClearNode(child);
                }

                // refresh the leaf list
                /*
                ExtendedNode selectedAncestor = parent;
                while (selectedAncestor != null)
                {
                    selectedAncestor.storedLeafList = null;
                    selectedAncestor.getLeaves(false);

                    selectedAncestor = (ExtendedNode)selectedAncestor.Parent;
                }
                 */
                // TO-DO: start thinking about unit tests for this highly essential function

                if (parent.Nodes.Count == 1)
                {
                    TopoTimeNode child = (TopoTimeNode)parent.Nodes[0];
                    // Different behavior if parent is a named high-level node and child is not
                    if (parent.TaxonID > 0 && child.TaxonID == 0)
                    {
                        for (int i = child.Nodes.Count - 1; i >= 0; i--)
                        {
                            TopoTimeNode grandchild = (TopoTimeNode)child.Nodes[i];
                            child.Nodes.Remove(grandchild);
                            parent.Nodes.Add(grandchild);
                        }

                        parent.ChildDivergences.Clear();
                        for (int i = child.ChildDivergences.Count - 1; i >= 0; i--)
                        {
                            parent.ChildDivergences.Add(child.ChildDivergences[i]);
                            child.ChildDivergences.RemoveAt(i);
                        }
                        parent.storedAdjustedHeight = child.storedAdjustedHeight;

                        parent.Nodes.Remove(child);
                        this.nodeList.Remove(child);
                        this.leafList.Remove(child);

                        parent.Text = parent.TaxonName + " [" + parent.TaxonID + "] (" + parent.getNodeHeight(true).ToString("0.00") + ")" + " {" + parent.ChildDivergences.Count + "}";
                    }
                    else if (parent.Parent != null)
                    {
                        TopoTimeNode node = (TopoTimeNode)parent.Nodes[0];
                        TopoTimeNode grandparent = (TopoTimeNode)parent.Parent;

                        int oldParentIndex = grandparent.Nodes.IndexOf(parent);
                        parent.Nodes.Remove(node);

                        List<TopoTimeNode> tempList = new List<TopoTimeNode>();
                        tempList.AddRange(grandparent.Nodes.Cast<TopoTimeNode>().ToList());
                        grandparent.Nodes.Clear();

                        grandparent.Nodes.Add(node);
                        foreach (TopoTimeNode displacedNode in tempList)
                            grandparent.Nodes.Add(displacedNode);

                        grandparent.Nodes.Remove(parent);

                        this.nodeList.Remove(parent);
                    }
                    else
                    {
                        TopoTimeNode oldRoot = parent;
                        if (oldRoot == this.root)
                        {
                            TopoTimeNode newRoot = (TopoTimeNode)oldRoot.Nodes[0];
                            this.nodeList.Remove(oldRoot);
                            oldRoot.Nodes.Remove(newRoot);

                            while (newRoot.Nodes.Count == 1)
                            {
                                oldRoot = newRoot;
                                newRoot = (TopoTimeNode)oldRoot.Nodes[0];
                                this.nodeList.Remove(oldRoot);
                                oldRoot.Nodes.Remove(newRoot);
                            }

                            if (this.root.TreeView != null)
                            {
                                TreeView treeViewer = (TreeView)this.root.TreeView;
                                treeViewer.Nodes.RemoveAt(0);
                                treeViewer.Nodes.Add(newRoot);
                            }
                            this.root = newRoot;
                        }
                    }
                }
            }
        }

        public void MoveNode(TopoTimeNode source, TopoTimeNode targetParent)
        {
            TopoTimeNode oldParent = (TopoTimeNode)source.Parent;
            TopoTimeNode commonAncestor = TopoTimeTree.getCommonAncestor((TopoTimeNode)source, (TopoTimeNode)targetParent);

            TopoTimeNode ancestor;
            ancestor = (TopoTimeNode)source.Parent;

            source.Remove();
            targetParent.Nodes.Add(source);

            if (oldParent.Nodes.Count == 0)
                DeleteNode(oldParent);

            // we need to find the nearest common ancestor of both the source and target node
            // and reset leaves up to there, also add them to the refresh list and mark red

            while (ancestor != null && ancestor != commonAncestor)
            {
                ancestor.storedLeafList = null;
                ancestor.getLeaves(false);
                ancestor.ForeColor = Color.IndianRed;
                refreshList.Add(ancestor);

                ancestor = (TopoTimeNode)ancestor.Parent;
            }

            ancestor = (TopoTimeNode)targetParent;

            while (ancestor != null && ancestor != commonAncestor)
            {
                ancestor.storedLeafList = null;
                ancestor.getLeaves(false);
                ancestor.ForeColor = Color.IndianRed;
                refreshList.Add(ancestor);

                ancestor = (TopoTimeNode)ancestor.Parent;
            }

            commonAncestor.ForeColor = Color.IndianRed;
            refreshList.Add(commonAncestor);
        }

        public void MoveNode2(TopoTimeNode source, TopoTimeNode targetParent)
        {
            TopoTimeNode oldParent = (TopoTimeNode)source.Parent;
            source.Remove();
            targetParent.Nodes.Add(source);

            CollapseMonotypicGroups(oldParent);
        }

        public void CollapseMonotypicGroups(TopoTimeNode target)
        {
            TopoTimeNode currentNode = target;
            TopoTimeNode nextParent = target.Parent;

            while (currentNode.Nodes.Count == 1 && nextParent != null)
            {
                TopoTimeNode loneChild = currentNode.Nodes[0];
                loneChild.Remove();
                nextParent.Nodes.Add(loneChild);

                currentNode.Remove();
                currentNode = nextParent;
                nextParent = currentNode.Parent;
            }
        }

        public void ReduceToLeaf(TopoTimeNode targetNode)
        {
            if (targetNode.Nodes.Count > 0)
            {
                foreach (TopoTimeNode child in targetNode.Nodes)
                    ClearNode(child);

                targetNode.Nodes.Clear();

                this.leafList.Add(targetNode);
            }
        }

        private void ClearNode(TopoTimeNode node)
        {
            this.nodeList.Remove(node);
            this.leafList.Remove(node);

            foreach (TopoTimeNode child in node.Nodes)
            {
                if (child != null)
                    ClearNode(child);
            }
        }

        // This should be bound up in the TimeTree class or something
        public static TopoTimeNode getCommonAncestor(List<TopoTimeNode> nodes)
        {
            if (nodes.Count < 2)
                return null;

            TopoTimeNode first = nodes[0];
            List<TopoTimeNode> ancestors = new List<TopoTimeNode>();

            TopoTimeNode parent = (TopoTimeNode)first.Parent;
            while (parent != null)
            {
                ancestors.Add(parent);
                parent = (TopoTimeNode)parent.Parent;
            }

            int ancestorIndex = 0;
            TopoTimeNode foundAncestor = null;

            for (int i = 1; i < nodes.Count; i++)
            {
                TopoTimeNode temp = nodes[i];
                TopoTimeNode tempParent = (TopoTimeNode)temp.Parent;

                bool found = false;
                while (tempParent != null && !found)
                {
                    int lastIndex = ancestors.IndexOf(tempParent);
                    if (lastIndex >= ancestorIndex)
                    {
                        ancestorIndex = lastIndex;
                        found = true;
                    }
                    else
                        tempParent = (TopoTimeNode)tempParent.Parent;
                }

                if (found)
                    foundAncestor = tempParent;
            }

            return foundAncestor;
        }

        // This should be bound up in the TimeTree class or something
        public static TopoTimeNode getCommonAncestor(TopoTimeNode nodeA, TopoTimeNode nodeB)
        {
            if (nodeA == null || nodeB == null)
                return null;

            List<TopoTimeNode> ancestorsA = new List<TopoTimeNode>();
            TopoTimeNode ancestor = nodeA;

            while (ancestor != null)
            {
                ancestorsA.Add(ancestor);
                ancestor = (TopoTimeNode)ancestor.Parent;
            }

            ancestor = nodeB;

            while (ancestor != null)
            {
                if (ancestorsA.Contains(ancestor))
                    return ancestor;

                ancestor = (TopoTimeNode)ancestor.Parent;
            }

            return ancestor;
        }

        // used on trees that have duplicate taxa from being reduced to a higher taxonomic level
        public string AuditPolyphyleticGroups(TopoTimeNode selectedNode = null)
        {
            StringBuilder result = new StringBuilder();
            IEnumerable<IGrouping<int, ExtendedNode>> duplicateChildren;

            if (selectedNode == null)
                duplicateChildren = this.leafList.GroupBy(x => x.TaxonID);
            else
                duplicateChildren = selectedNode.getLeaves(true).GroupBy(x => x.TaxonID);

            foreach (IGrouping<int, TopoTimeNode> taxonGroup in duplicateChildren)
            {
                int taxonCount = taxonGroup.Count();
                if (taxonCount > 1)
                {
                    TopoTimeNode leafTaxon = taxonGroup.First();

                    TopoTimeNode foundMRCA = getCommonAncestor(taxonGroup.Select(x => x).ToList());
                    List<TopoTimeNode> foundGroup = foundMRCA.getLeaves(true);
                    int groupCount = foundGroup.Count();
                    IEnumerable<IGrouping<int, TopoTimeNode>> distinctGroups = foundGroup.GroupBy(x => x.TaxonID);
                    int distinctGroupCount = distinctGroups.Count();
                    result.AppendLine(leafTaxon.TaxonName + "," + taxonCount + "," + groupCount + "," + distinctGroupCount);
                }
            }

            return result.ToString();
        }



        public TopoTimeNode TimeSource(TopoTimeNode parent, double parentHeight, int parentStudies, List<TopoTimeNode> childList)
        {
            if (parent.storedAdjustedHeight == parentHeight)
            {
                childList.Add(parent);
                if (parent.ChildDivergences.Count > 0)
                {
                    return parent;
                }
            }
            else
                return null;

            TopoTimeNode timeSource = null;

            foreach (TopoTimeNode child in parent.Nodes)
            {
                TopoTimeNode temp = TimeSource(child, parentHeight, parentStudies, childList);
                if (temp != null)
                    timeSource = temp;
            }

            return timeSource;
        }
        /*
        public ExtendedNode TimeSourceMax(ExtendedNode node)
        {
            if (node.Nodes.Count == 0)
                return node;

            ExtendedNode currentMax;

            double actualTimeSourceHeight = parent.Nodes.Cast<ExtendedNode>().Max(x => x.getNodeHeight(true, ignoreIncompatibleTimes: true));

        }
        */
        public void PushNodes(TopoTimeNode node, TopoTimeNode parent, int level, Dictionary<TopoTimeNode, string> adjustmentMethod)
        {
            if (parent != null && node.storedAdjustedHeight > parent.storedAdjustedHeight)
            {
                // check a node's descendants and see whether either of them are causing a negative branch
                // if multple nodes do, pick the one that a) has the most node support and b) is the highest

                foreach (TopoTimeNode child in parent.Nodes)
                {
                    if (child.storedAdjustedHeight > parent.storedAdjustedHeight)
                    {
                        if (child.storedEffectiveStudies >= node.storedEffectiveStudies && child.storedAdjustedHeight >= node.storedAdjustedHeight)
                            node = child;
                    }
                }

                // check if the node's time was affected by its descendants and find the original source
                // use TimeSource to identify the original source of that node's time and identify all the nodes that need to be edited as a result

                List<TopoTimeNode> childNodes = new List<TopoTimeNode>();
                TopoTimeNode originalNode = node;
                node = TimeSource(node, node.StoredAdjustedHeight, node.storedEffectiveStudies, childNodes);
                if (node == null)
                    node = originalNode;

                // if parent node is timeless, go up until you find the ancestor that isn't and use that

                Queue<TopoTimeNode> nodeQueue = new Queue<TopoTimeNode>();
                //nodeQueue.Enqueue(node);

                TopoTimeNode tempParent = parent;
                while (tempParent != null && tempParent.storedAdjustedHeight == 0)
                {
                    nodeQueue.Enqueue(tempParent);
                    tempParent = (TopoTimeNode)tempParent.Parent;
                }

                // extra check
                // maybe should remove this or throw exception
                if (originalNode == null)
                    throw (new ArgumentNullException());

                if (node.storedAdjustedHeight > tempParent.storedAdjustedHeight)
                {
                    /*
                    if (tempParent.storedEffectiveStudies > node.storedEffectiveStudies)
                    {
                        foreach (ExtendedNode repChild in childNodes)
                        {
                            repChild.storedEffectiveStudies = tempParent.storedEffectiveStudies;
                            repChild.storedAdjustedHeight = tempParent.storedAdjustedHeight;

                            if (!adjustmentMethod.ContainsKey(repChild))
                                adjustmentMethod.Add(repChild, "");

                            adjustmentMethod[repChild] = "Child pushed down";
                        }

                        // this handles timeless nodes
                        while (nodeQueue.Count > 0)
                        {
                            ExtendedNode next = nodeQueue.Dequeue();
                            next.storedAdjustedHeight = tempParent.storedAdjustedHeight;

                            if (!adjustmentMethod.ContainsKey(next))
                                adjustmentMethod.Add(next, "");

                            adjustmentMethod[next] = "Timeless node between negative branches, pushed down";
                        }

                        // this is 100% perfect, don't touch
                        ExtendedNode actualParent = (ExtendedNode)node.Parent;
                        foreach (ExtendedNode child in actualParent.Nodes)
                            PushNodes(child, actualParent, level + 1, adjustmentMethod);

                        foreach (ExtendedNode child in node.Nodes)
                            PushNodes(child, node, level + 1, adjustmentMethod);
                    }
                    else if (tempParent.storedEffectiveStudies < node.storedEffectiveStudies)
                    {
                        tempParent.storedEffectiveStudies = node.storedEffectiveStudies;
                        tempParent.storedAdjustedHeight = node.storedAdjustedHeight;

                        if (!adjustmentMethod.ContainsKey(tempParent))
                            adjustmentMethod.Add(tempParent, "");

                        adjustmentMethod[tempParent] = "Parent pushed up";

                        // this handles timeless nodes
                        while (nodeQueue.Count > 0)
                        {
                            ExtendedNode next = nodeQueue.Dequeue();
                            next.storedAdjustedHeight = node.storedAdjustedHeight;

                            if (!adjustmentMethod.ContainsKey(next))
                                adjustmentMethod.Add(next, "");

                            adjustmentMethod[next] = "Timeless node between negative branches, pushed up";
                        }

                        // this is 100% perfect, don't touch
                        PushNodes(tempParent, (ExtendedNode)tempParent.Parent, level + 1, adjustmentMethod);
                    }                        
                    else if (tempParent.storedEffectiveStudies == node.storedEffectiveStudies)            
                     */
                    {
                        //double average = Functions.RoundToSignificantDigits((tempParent.storedAdjustedHeight + node.storedAdjustedHeight) / 2.0, 1);
                        double average = Functions.RoundByLevel((tempParent.StoredAdjustedHeight + node.StoredAdjustedHeight) / 2.0, level);

                        if (level > 1000)
                            average = node.StoredAdjustedHeight;

                        tempParent.storedAdjustedHeight = average;
                        foreach (TopoTimeNode repChild in childNodes)
                        {
                            repChild.StoredAdjustedHeight = average;

                            if (!adjustmentMethod.ContainsKey(repChild))
                                adjustmentMethod.Add(repChild, "");

                            adjustmentMethod[repChild] = "Child pushed down (averaged)";
                        }

                        if (!adjustmentMethod.ContainsKey(tempParent))
                            adjustmentMethod.Add(tempParent, "");

                        adjustmentMethod[tempParent] = "Parent pushed up (averaged)";

                        // this handles timeless nodes
                        while (nodeQueue.Count > 0)
                        {
                            TopoTimeNode next = nodeQueue.Dequeue();
                            next.StoredAdjustedHeight = average;

                            if (!adjustmentMethod.ContainsKey(next))
                                adjustmentMethod.Add(next, "");

                            adjustmentMethod[next] = "Timeless node between negative branches, set to an average";
                        }

                        if (level < 1150)
                        {
                            if (tempParent != null)
                                PushNodes(tempParent, (TopoTimeNode)tempParent.Parent, level + 1, adjustmentMethod);

                            if (node.Parent != null)
                                foreach (TopoTimeNode child in node.Parent.Nodes)
                                    PushNodes(child, node.Parent, level + 1, adjustmentMethod);

                            foreach (TopoTimeNode child in node.Nodes)
                                PushNodes(child, node, level + 1, adjustmentMethod);
                        }
                    }
                }
            }
        }

        public void MarkNodes(TopoTimeNode node, TopoTimeNode parent, int level, Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary, HashSet<TopoTimeNode> NodeGroup)
        {
            if (parent != null && node.storedAdjustedHeight > parent.storedAdjustedHeight)
            {
                // check a node's descendants and see whether either of them are causing a negative branch
                // if multple nodes do, pick the one that a) has the most node support and b) is the highest

                foreach (TopoTimeNode child in parent.Nodes)
                {
                    if (child.storedAdjustedHeight > parent.storedAdjustedHeight)
                    {
                        if (child.storedEffectiveStudies >= node.storedEffectiveStudies && child.storedAdjustedHeight >= node.storedAdjustedHeight)
                            node = child;
                    }
                }

                // check if the node's time was affected by its descendants and find the original source
                // use TimeSource to identify the original source of that node's time and identify all the nodes that need to be edited as a result

                List<TopoTimeNode> childNodes = new List<TopoTimeNode>();
                TopoTimeNode originalNode = node;
                node = TimeSource(node, node.StoredAdjustedHeight, node.storedEffectiveStudies, childNodes);
                if (node == null)
                    node = originalNode;

                // if parent node is timeless, go up until you find the ancestor that isn't and use that

                Queue<TopoTimeNode> nodeQueue = new Queue<TopoTimeNode>();
                //nodeQueue.Enqueue(node);

                TopoTimeNode tempParent = parent;
                while (tempParent != null && tempParent.storedAdjustedHeight == 0)
                {
                    nodeQueue.Enqueue(tempParent);
                    tempParent = (TopoTimeNode)tempParent.Parent;
                }

                // extra check
                // maybe should remove this or throw exception
                if (originalNode == null)
                    throw (new ArgumentNullException());

                if (node.storedAdjustedHeight > tempParent.storedAdjustedHeight)
                {


                    //double average = Functions.RoundToSignificantDigits((tempParent.StoredAdjustedHeight + node.StoredAdjustedHeight) / 2.0, 1);
                    double average = (tempParent.StoredAdjustedHeight + node.StoredAdjustedHeight) / 2.0;

                    HashSet<TopoTimeNode> currentGroup;
                    HashSet<TopoTimeNode> tempParentGroup;
                    HashSet<TopoTimeNode> tempGroup;

                    NodeGroupDictionary.TryGetValue(node, out currentGroup);
                    NodeGroupDictionary.TryGetValue(tempParent, out tempParentGroup);

                    if (currentGroup != NodeGroup || tempParentGroup != NodeGroup)
                    {
                        AddToNodeGroup(node, NodeGroup, NodeGroupDictionary);

                        tempParent.storedAdjustedHeight = average;
                        AddToNodeGroup(tempParent, NodeGroup, NodeGroupDictionary);

                        foreach (TopoTimeNode repChild in childNodes)
                        {
                            tempGroup = null;
                            NodeGroupDictionary.TryGetValue(repChild, out tempGroup);

                            if (tempGroup != NodeGroup)
                            {
                                repChild.StoredAdjustedHeight = average;
                                AddToNodeGroup(repChild, NodeGroup, NodeGroupDictionary);
                            }
                        }

                        // this handles timeless nodes
                        while (nodeQueue.Count > 0)
                        {
                            TopoTimeNode next = nodeQueue.Dequeue();
                            tempGroup = null;
                            NodeGroupDictionary.TryGetValue(next, out tempGroup);

                            if (tempGroup != NodeGroup)
                            {
                                next.StoredAdjustedHeight = average;
                                AddToNodeGroup(next, NodeGroup, NodeGroupDictionary);
                            }
                        }

                        if (level < 1150)
                        {
                            if (tempParent != null)
                                MarkNodes(tempParent, (TopoTimeNode)tempParent.Parent, level + 1, NodeGroupDictionary, NodeGroup);

                            if (node.Parent != null)
                                foreach (TopoTimeNode child in node.Parent.Nodes)
                                    MarkNodes(child, node.Parent, level + 1, NodeGroupDictionary, NodeGroup);

                            foreach (TopoTimeNode child in node.Nodes)
                                MarkNodes(child, node, level + 1, NodeGroupDictionary, NodeGroup);
                        }
                    }

                    /*
                    if ((node.NodeFont == null || !node.NodeFont.Bold) || (tempParent.NodeFont == null || !tempParent.NodeFont.Bold))                    
                    {
                        if (node.NodeFont == null || !node.NodeFont.Bold)
                        {
                            node.NodeFont = new Font(node.TreeView.Font, FontStyle.Bold);
                            
                        }
                        NodeGroup.Add(node);

                        if (tempParent.NodeFont == null || !tempParent.NodeFont.Bold)
                        {
                            tempParent.storedAdjustedHeight = average;
                            tempParent.NodeFont = new Font(tempParent.TreeView.Font, FontStyle.Bold);
                            
                        }
                        NodeGroup.Add(tempParent);

                        foreach (TopoTimeNode repChild in childNodes)
                        {
                            if (repChild.NodeFont == null || !repChild.NodeFont.Bold)
                            {
                                repChild.StoredAdjustedHeight = average;
                                repChild.NodeFont = new Font(repChild.TreeView.Font, FontStyle.Bold);
                                
                            }
                            NodeGroup.Add(repChild);
                        }

                        // this handles timeless nodes
                        while (nodeQueue.Count > 0)
                        {
                            TopoTimeNode next = nodeQueue.Dequeue();

                            if (next.NodeFont == null || !next.NodeFont.Bold)
                            {
                                next.StoredAdjustedHeight = average;
                                next.NodeFont = new Font(next.TreeView.Font, FontStyle.Bold);
                                
                            }
                            NodeGroup.Add(next);
                        }

                        if (level < 1150)
                        {
                            if (tempParent != null)
                                MarkNodes(tempParent, (TopoTimeNode)tempParent.Parent, level + 1, NodeGroupDictionary, NodeGroup);

                            if (node.Parent != null)
                                foreach (TopoTimeNode child in node.Parent.Nodes)
                                    MarkNodes(child, node.Parent, level + 1, NodeGroupDictionary, NodeGroup);

                            foreach (TopoTimeNode child in node.Nodes)
                                MarkNodes(child, node, level + 1, NodeGroupDictionary, NodeGroup);
                        }
                    }
                    */
                }
            }
        }

        private void AddToNodeGroup(TopoTimeNode node, HashSet<TopoTimeNode> NodeGroup, Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary)
        {
            HashSet<TopoTimeNode> FoundGroup;
            NodeGroupDictionary.TryGetValue(node, out FoundGroup);

            if (FoundGroup == null)
            {
                NodeGroupDictionary[node] = NodeGroup;
                NodeGroup.Add(node);
            }
            else if (FoundGroup != NodeGroup)
            {
                // merge all other members of the other group into the current one
                foreach (TopoTimeNode otherNode in FoundGroup)
                {
                    NodeGroup.Add(otherNode);
                    NodeGroupDictionary[otherNode] = NodeGroup;
                }
                FoundGroup.Clear();
            }
        }

        public List<HashSet<TopoTimeNode>> MarkNegativeNodes()
        {
            Dictionary<TopoTimeNode, string> adjustmentMethod = new Dictionary<TopoTimeNode, string>();

            List<TopoTimeNode> updateList = new List<TopoTimeNode>();

            /*
            foreach (ExtendedNode node in this.nodeList)
            {
                node.storedAdjustedHeight = node.getNodeHeight(false);
                node.storedEffectiveStudies = node.ChildDivergences.Count();
            }
             */

            foreach (TopoTimeNode node in this.nodeList)
            {
                TopoTimeNode parent = (TopoTimeNode)node.Parent;
                if (parent == null)
                    continue;

                if (node.storedAdjustedHeight > parent.storedAdjustedHeight)
                {
                    updateList.Add(node);
                }

                if (node.storedAdjustedHeight != null && parent.storedAdjustedHeight == null)
                    updateList.Add(node);
            }

            HashSet<TopoTimeNode> adjustList = new HashSet<TopoTimeNode>();

            // timeless node fix
            foreach (TopoTimeNode node in updateList)
            {
                if (node.ChildDivergences.Count > 0)
                {
                    TopoTimeNode parent = node.Parent;
                    if (parent == null)
                        continue;

                    /*
                    if (parent != null)
                    {
                        double parentHeightConflictInclusive = GetNodeHeight(parent, false);
                        double parentHeightConflictExclusive = parent.getNodeHeight(false, ignoreIncompatibleTimes: true);

                        // in this case, the parent has no divergence data - its time is inferred by its oldest descendant
                        if (parentHeightConflictExclusive == 0)
                        {
                            Stack<TopoTimeNode> nodeStack = new Stack<TopoTimeNode>();

                            // find out which child of the parent has the highest time
                            // use that for averages
                            //ExtendedNode actualTimeSource;

                            // NOTE to self: changed useChildHeights to true to fix possible bug, did it work?
                            double actualTimeSourceHeight = parent.Nodes.Cast<TopoTimeNode>().Max(x => x.getNodeHeight(true, ignoreIncompatibleTimes: true));

                            while (parent != null && parent.getNodeHeight(false, ignoreIncompatibleTimes: true) == 0)
                            {
                                nodeStack.Push(parent);
                                parent = (TopoTimeNode)parent.Parent;
                            }

                            if (nodeStack.Count > 0 && parent != null)
                            {
                                // check whether it isn't a negative branch situation
                                // if it is, leave it to the negative branch fix
                                parentHeightConflictExclusive = parent.getNodeHeight(false, ignoreIncompatibleTimes: true);

                                if (parentHeightConflictExclusive >= actualTimeSourceHeight)
                                {
                                    double range = parentHeightConflictExclusive - actualTimeSourceHeight;
                                    int divisions = nodeStack.Count + 1;
                                    double split = range / (double)divisions;

                                    for (int startCount = 1; startCount < divisions; startCount++)
                                    {
                                        TopoTimeNode next = nodeStack.Pop();
                                        double tempHeight = parentHeightConflictExclusive - (split * (double)startCount);
                                        if (tempHeight > next.storedAdjustedHeight)
                                        {
                                            if (!adjustmentMethod.ContainsKey(next))
                                                adjustmentMethod.Add(next, "");
                                            adjustmentMethod[next] = "Timeless Node";

                                            next.storedAdjustedHeight = tempHeight;
                                        }

                                        if (!adjustList.Contains(next))
                                            adjustList.Add(next);
                                    }
                                }
                            }
                        }
                        */
                    /*
                        if (parent != null)
                        {
                            double parentHeightConflictInclusive = GetNodeHeight(parent, usingChildHeights: false);
                            double parentHeightConflictExclusive = GetNodeHeight(parent, usingChildHeights: true, ignoreConflictingTimes: false);

                            // in this case, the parent has no divergence data - its time is inferred by its oldest descendant
                            if (parentHeightConflictExclusive == 0)
                            {
                                Stack<TopoTimeNode> nodeStack = new Stack<TopoTimeNode>();

                                // find out which child of the parent has the highest time
                                // use that for averages
                                //ExtendedNode actualTimeSource;

                                // NOTE to self: changed useChildHeights to true to fix possible bug, did it work?
                                double actualTimeSourceHeight = parent.Nodes.Cast<TopoTimeNode>().Max(x => GetNodeHeight(x, usingChildHeights: true, ignoreConflictingTimes: true));

                                while (parent != null && parent.getNodeHeight(false, ignoreIncompatibleTimes: true) == 0)
                                {
                                    nodeStack.Push(parent);
                                    parent = (TopoTimeNode)parent.Parent;
                                }

                                if (nodeStack.Count > 0 && parent != null)
                                {
                                    // check whether it isn't a negative branch situation
                                    // if it is, leave it to the negative branch fix
                                    parentHeightConflictExclusive = GetNodeHeight(parent, usingChildHeights: false, ignoreConflictingTimes: false);

                                    if (parentHeightConflictExclusive >= actualTimeSourceHeight)
                                    {
                                        double range = parentHeightConflictExclusive - actualTimeSourceHeight;
                                        int divisions = nodeStack.Count + 1;
                                        double split = range / (double)divisions;

                                        for (int startCount = 1; startCount < divisions; startCount++)
                                        {
                                            TopoTimeNode next = nodeStack.Pop();
                                            double tempHeight = parentHeightConflictExclusive - (split * (double)startCount);
                                            if (tempHeight > next.storedAdjustedHeight || next.storedAdjustedHeight == null)
                                            {
                                                if (!adjustmentMethod.ContainsKey(next))
                                                    adjustmentMethod.Add(next, "");
                                                adjustmentMethod[next] = "Timeless Node";

                                                next.storedAdjustedHeight = tempHeight;
                                            }

                                            if (!adjustList.Contains(next))
                                                adjustList.Add(next);
                                        }
                                    }
                                }
                            }
                        }
                    */

                    TopoTimeNode CurrentNode = parent;
                    while (CurrentNode.storedAdjustedHeight == null)
                    {
                        CurrentNode.storedAdjustedHeight = parent.Nodes.Cast<TopoTimeNode>().Max(x => GetNodeHeight(x, usingChildHeights: true, ignoreConflictingTimes: true));
                        adjustList.Add(CurrentNode);

                        if (CurrentNode.Parent != null)
                            CurrentNode = CurrentNode.Parent;
                    }

                }
            }

            OperationHistory.Add("Negative branch marking applied");

            Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary = new Dictionary<TopoTimeNode, HashSet<TopoTimeNode>>();

            // Negative branch group search v1
            // MarkDownTheTree(root, NodeGroupDictionary);

            // Negative branch group search v2
            foreach (TopoTimeNode paradoxNode in this.nodeList.Where(x => x.Parent != null && x.StoredAdjustedHeight > x.Parent.StoredAdjustedHeight))
                MarkUpTheTree(paradoxNode, NodeGroupDictionary);

            return NodeGroupDictionary.Values.Distinct().ToList();
        }

        public Dictionary<TopoTimeNode, string> CalculateAdjustedNodeHeights(bool useScaling)
        {
            Dictionary<TopoTimeNode, string> adjustmentMethod = new Dictionary<TopoTimeNode, string>();

            List<TopoTimeNode> updateList = new List<TopoTimeNode>();

            /*
            foreach (ExtendedNode node in this.nodeList)
            {
                node.storedAdjustedHeight = node.getNodeHeight(false);
                node.storedEffectiveStudies = node.ChildDivergences.Count();
            }
             */

            foreach (TopoTimeNode node in this.nodeList)
            {
                TopoTimeNode parent = (TopoTimeNode)node.Parent;
                if (parent != null && node.storedAdjustedHeight > parent.storedAdjustedHeight)
                {
                    updateList.Add(node);
                }
            }

            HashSet<TopoTimeNode> adjustList = new HashSet<TopoTimeNode>();

            // timeless node fix
            foreach (TopoTimeNode node in updateList)
            {
                if (node.ChildDivergences.Count > 0)
                {
                    TopoTimeNode parent = node.Parent;

                    if (parent != null)
                    {
                        double parentHeightConflictInclusive = GetNodeHeight(parent, false);
                        double parentHeightConflictExclusive = parent.getNodeHeight(false, ignoreIncompatibleTimes: true);

                        // in this case, the parent has no divergence data - its time is inferred by its oldest descendant
                        if (parentHeightConflictExclusive == 0)
                        {
                            Stack<TopoTimeNode> nodeStack = new Stack<TopoTimeNode>();

                            // find out which child of the parent has the highest time
                            // use that for averages
                            //ExtendedNode actualTimeSource;

                            // NOTE to self: changed useChildHeights to true to fix possible bug, did it work?
                            double actualTimeSourceHeight = parent.Nodes.Cast<TopoTimeNode>().Max(x => x.getNodeHeight(true, ignoreIncompatibleTimes: true));

                            while (parent != null && parent.getNodeHeight(false, ignoreIncompatibleTimes: true) == 0)
                            {
                                nodeStack.Push(parent);
                                parent = (TopoTimeNode)parent.Parent;
                            }

                            if (nodeStack.Count > 0 && parent != null)
                            {
                                // check whether it isn't a negative branch situation
                                // if it is, leave it to the negative branch fix
                                parentHeightConflictExclusive = parent.getNodeHeight(false, ignoreIncompatibleTimes: true);

                                if (parentHeightConflictExclusive >= actualTimeSourceHeight)
                                {
                                    double range = parentHeightConflictExclusive - actualTimeSourceHeight;
                                    int divisions = nodeStack.Count + 1;
                                    double split = range / (double)divisions;

                                    for (int startCount = 1; startCount < divisions; startCount++)
                                    {
                                        TopoTimeNode next = nodeStack.Pop();
                                        double tempHeight = parentHeightConflictExclusive - (split * (double)startCount);
                                        if (tempHeight > next.storedAdjustedHeight)
                                        {
                                            if (!adjustmentMethod.ContainsKey(next))
                                                adjustmentMethod.Add(next, "");
                                            adjustmentMethod[next] = "Timeless Node";

                                            next.storedAdjustedHeight = tempHeight;
                                        }

                                        if (!adjustList.Contains(next))
                                            adjustList.Add(next);
                                    }
                                }
                            }
                        }
                    }

                }
            }



            // Negative branch fix
            DownTheTree(root, adjustmentMethod);

            foreach (TopoTimeNode node in this.nodeList)
            {
                UpdateNodeText(node);
            }

            OperationHistory.Add("Negative branch fix applied");

            return adjustmentMethod;
        }

        public void AddNodesToTree(TopoTimeNode currentNode)
        {
            AddNewNodeToTree(currentNode);

            foreach (TopoTimeNode child in currentNode.Nodes)
                AddNodesToTree(child);

        }

        public void AddNewNodeToTree(TopoTimeNode currentNode, bool addLeaf = true)
        {
            this.nodeList.Add(currentNode);
            currentNode.UpdateTextRequest += this.UpdateNodeTextRequest;

            UpdateNodeText(currentNode);

            if (addLeaf)
                AddNewLeaf(currentNode);
        }

        public void AddNewLeaf(TopoTimeNode currentNode)
        {
            if (currentNode.Nodes.Count == 0)
                this.leafList.Add(currentNode);
        }

        public void StoreFloatingNode(TopoTimeNode parentNode, TopoTimeNode childNode)
        {
            if (!childNode.Floating)
                return;

            parentNode.StoreFloatingNode(childNode, currentFloatingIndex);
            currentFloatingIndex--;
        }

        public void DownTheTree(TopoTimeNode start, Dictionary<TopoTimeNode, string> adjustmentMethod)
        {
            foreach (TopoTimeNode child in start.Nodes)
            {
                if (start != null && child.storedAdjustedHeight > start.storedAdjustedHeight)
                {
                    PushNodes(child, start, 0, adjustmentMethod);
                }

                DownTheTree(child, adjustmentMethod);
            }
        }

        public void MarkDownTheTree(TopoTimeNode start, Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary)
        {
            HashSet<TopoTimeNode> NodeGroup = new HashSet<TopoTimeNode>();

            foreach (TopoTimeNode child in start.Nodes)
            {
                if (child.NodeFont == null && start.NodeFont == null)
                {
                    if (child.storedAdjustedHeight > start.storedAdjustedHeight)
                    {
                        //child.NodeFont = new Font(child.TreeView.Font, FontStyle.Bold);
                        MarkNodes(child, start, 0, NodeGroupDictionary, NodeGroup);
                    }
                }

                MarkDownTheTree(child, NodeGroupDictionary);
            }

            //if (NodeGroup.Count > 0)
            //    NodeGroupList.Add(NodeGroup);
        }

        public void MarkUpTheTree(TopoTimeNode start, Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary)
        {
            if (NodeGroupDictionary.ContainsKey(start))
                return;

            HashSet<TopoTimeNode> NodeGroup = new HashSet<TopoTimeNode>();
            AddToNodeGroup(start, NodeGroup, NodeGroupDictionary);
            TopoTimeNode currentParent = start.Parent;

            double average = (currentParent.StoredAdjustedHeight + start.StoredAdjustedHeight) / 2.0;

            foreach (TopoTimeNode child in start.Nodes)
            {
                if (child.StoredAdjustedHeight > average)
                    AddToNodeGroup(child, NodeGroup, NodeGroupDictionary);
            }

            while (currentParent.storedAdjustedHeight < start.storedAdjustedHeight)
            {
                AddToNodeGroup(currentParent, NodeGroup, NodeGroupDictionary);

                if (currentParent.Parent != null)
                {
                    bool hasParent = currentParent.Parent != null;
                    bool hasGrandparent = hasParent && currentParent.Parent.Parent != null;
                    bool ParadoxParent = hasGrandparent && currentParent.Parent.StoredAdjustedHeight >= currentParent.Parent.Parent.StoredAdjustedHeight;

                    //average = (currentParent.StoredAdjustedHeight + start.StoredAdjustedHeight) / 2.0;
                    average = (currentParent.StoredAdjustedHeight + start.StoredAdjustedHeight + start.StoredAdjustedHeight) / 3.0;

                    if (currentParent.Parent.StoredAdjustedHeight < average)
                        currentParent = currentParent.Parent;
                    else if (ParadoxParent)
                    {
                        currentParent = currentParent.Parent;
                        //average = (currentParent.Parent.StoredAdjustedHeight + start.StoredAdjustedHeight) / 2.0;
                        average = (currentParent.Parent.StoredAdjustedHeight + start.StoredAdjustedHeight + start.StoredAdjustedHeight) / 3.0;
                        if (currentParent.Parent.StoredAdjustedHeight < average)
                        {
                            AddToNodeGroup(currentParent, NodeGroup, NodeGroupDictionary);
                            currentParent = currentParent.Parent;
                        }
                    }
                    else
                        break;
                }
                else
                    break;
            }
        }

        public void MarkDownTheTree(TopoTimeNode start, HashSet<TopoTimeNode> NodeGroup, Dictionary<TopoTimeNode, HashSet<TopoTimeNode>> NodeGroupDictionary)
        {
            foreach (TopoTimeNode child in start.Nodes)
            {
                //if (child.StoredAdjustedHeight > average)
                {
                    AddToNodeGroup(child, NodeGroup, NodeGroupDictionary);
                }
            }
        }

        public void SmoothNegativeBranches(HashSet<TopoTimeNode> NodeGroup)
        {
            Dictionary<TopoTimeNode, double> NodeAges = NodeGroup.ToDictionary(x => x, y => y.StoredAdjustedHeight);
            TopoTimeNode root = NodeGroup.MinBy(x => x.Level);

            int MinLevel = NodeGroup.Min(x => x.Level);
            int MaxLevel = NodeGroup.Max(x => x.Level);
            int CurrentLevel = MinLevel;

            while (CurrentLevel < MaxLevel)
            {
                foreach (TopoTimeNode currentNode in NodeGroup.Where(x => x.Level == CurrentLevel))
                {
                    List <TopoTimeNode> descendants = NodeGroup.Where(x => x.Level > CurrentLevel).OrderByDescending(x => x.StoredAdjustedHeight).ToList();

                    foreach (TopoTimeNode paradoxNode in descendants)
                    {
                        TopoTimeNode currentParent = paradoxNode.Parent;
                        bool foundParent = currentParent == currentNode;
                        while (currentParent != root && foundParent == false)
                        {
                            currentParent = currentParent.Parent;

                            if (currentParent == currentNode)
                                foundParent = true;                            
                        }

                        if (foundParent && paradoxNode.StoredAdjustedHeight > currentNode.StoredAdjustedHeight)
                        {
                            double average = (currentNode.StoredAdjustedHeight + paradoxNode.StoredAdjustedHeight) / 2.0;
                            currentNode.StoredAdjustedHeight = average;
                            paradoxNode.StoredAdjustedHeight = average;
                        }
                    }
                }
                CurrentLevel++;
            }

            foreach (TopoTimeNode currentNode in NodeGroup)
            {
                UpdateNodeText(currentNode);
            }
        }        
    }

    

    class HugeMemoryStream : Stream
    {
        #region Fields

        private const int PAGE_SIZE = 1024000;
        private const int ALLOC_STEP = 1024;

        private byte[][] _streamBuffers;

        private int _pageCount = 0;
        private long _allocatedBytes = 0;

        private long _position = 0;
        private long _length = 0;

        #endregion Fields

        #region Internals

        private int GetPageCount(long length)
        {
            int pageCount = (int)(length / PAGE_SIZE) + 1;

            if ((length % PAGE_SIZE) == 0)
                pageCount--;

            return pageCount;
        }

        private void ExtendPages()
        {
            if (_streamBuffers == null)
            {
                _streamBuffers = new byte[ALLOC_STEP][];
            }
            else
            {
                byte[][] streamBuffers = new byte[_streamBuffers.Length + ALLOC_STEP][];

                Array.Copy(_streamBuffers, streamBuffers, _streamBuffers.Length);

                _streamBuffers = streamBuffers;
            }

            _pageCount = _streamBuffers.Length;
        }

        private void AllocSpaceIfNeeded(long value)
        {
            if (value < 0)
                throw new InvalidOperationException("AllocSpaceIfNeeded < 0");

            if (value == 0)
                return;

            int currentPageCount = GetPageCount(_allocatedBytes);
            int neededPageCount = GetPageCount(value);

            while (currentPageCount < neededPageCount)
            {
                if (currentPageCount == _pageCount)
                    ExtendPages();

                _streamBuffers[currentPageCount++] = new byte[PAGE_SIZE];
            }

            _allocatedBytes = (long)currentPageCount * PAGE_SIZE;

            value = Math.Max(value, _length);

            if (_position > (_length = value))
                _position = _length;
        }

        #endregion Internals

        #region Stream

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get { return _position; }
            set
            {
                if (value > _length)
                    throw new InvalidOperationException("Position > Length");
                else if (value < 0)
                    throw new InvalidOperationException("Position < 0");
                else
                    _position = value;
            }
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int currentPage = (int)(_position / PAGE_SIZE);
            int currentOffset = (int)(_position % PAGE_SIZE);
            int currentLength = PAGE_SIZE - currentOffset;

            long startPosition = _position;

            if (startPosition + count > _length)
                count = (int)(_length - startPosition);

            while (count != 0 && _position < _length)
            {
                if (currentLength > count)
                    currentLength = count;

                Array.Copy(_streamBuffers[currentPage++], currentOffset, buffer, offset, currentLength);

                offset += currentLength;
                _position += currentLength;
                count -= currentLength;

                currentOffset = 0;
                currentLength = PAGE_SIZE;
            }

            return (int)(_position - startPosition);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;

                case SeekOrigin.Current:
                    offset += _position;
                    break;

                case SeekOrigin.End:
                    offset = _length - offset;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            return Position = offset;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new InvalidOperationException("SetLength < 0");

            if (value == 0)
            {
                _streamBuffers = null;
                _allocatedBytes = _position = _length = 0;
                _pageCount = 0;
                return;
            }

            int currentPageCount = GetPageCount(_allocatedBytes);
            int neededPageCount = GetPageCount(value);

            // Removes unused buffers if decreasing stream length
            while (currentPageCount > neededPageCount)
                _streamBuffers[--currentPageCount] = null;

            AllocSpaceIfNeeded(value);

            if (_position > (_length = value))
                _position = _length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int currentPage = (int)(_position / PAGE_SIZE);
            int currentOffset = (int)(_position % PAGE_SIZE);
            int currentLength = PAGE_SIZE - currentOffset;

            long startPosition = _position;

            AllocSpaceIfNeeded(_position + count);

            while (count != 0)
            {
                if (currentLength > count)
                    currentLength = count;

                Array.Copy(buffer, offset, _streamBuffers[currentPage++], currentOffset, currentLength);

                offset += currentLength;
                _position += currentLength;
                count -= currentLength;

                currentOffset = 0;
                currentLength = PAGE_SIZE;
            }
        }

        #endregion Stream
    }
}
