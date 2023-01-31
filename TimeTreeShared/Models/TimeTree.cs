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

namespace TimeTreeShared
{

    [Serializable]
    public class TimeTree
    {
        public HashSet<ExtendedNode> nodeList;
        public HashSet<ExtendedNode> leafList;
        public Dictionary<ExtendedNode, ExtendedNode> nestedNodeList;
        //public List<SplitData> splitList;
        public ExtendedNode root;
        public IEnumerator<ExtendedNode> SearchEnumerator;

        public Stack<ICommand> UndoCommandStack;
        public Stack<ICommand> RedoCommandStack;

        public TimeTree()
        {
            nodeList = new HashSet<ExtendedNode>();
            leafList = new HashSet<ExtendedNode>();
            nestedNodeList = new Dictionary<ExtendedNode, ExtendedNode>();
        }

        public TimeTree(SerializationInfo info, StreamingContext ctx)
        {
            root = (ExtendedNode)info.GetValue("root", typeof(ExtendedNode));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctx)
        {
            info.AddValue("nodeList", nodeList);
            info.AddValue("leafList", leafList);
            info.AddValue("root", root);
            //info.AddValue("splitData", splitData);
        }

        public void AddNode(ExtendedNode node, ExtendedNode parent)
        {
            parent.Nodes.Add(node);
            nodeList.Add(node);
        }

        public void DeleteNode(ExtendedNode selectedNode)
        {
            ExtendedNode parent = (ExtendedNode)selectedNode.Parent;

            this.nodeList.Remove(selectedNode);
            this.leafList.Remove(selectedNode);

            if (parent != null)
            {
                parent.Nodes.Remove(selectedNode);

                foreach (ExtendedNode child in selectedNode.Nodes)
                {
                    if (child != null)
                        ClearNode(child);
                }

                // refresh the leaf list
                /*
                TVNode selectedAncestor = parent;
                while (selectedAncestor != null)
                {
                    selectedAncestor.storedLeafList = null;
                    selectedAncestor.getLeaves(false);

                    selectedAncestor = (TVNode)selectedAncestor.Parent;
                }
                 */
                // TO-DO: start thinking about unit tests for this highly essential function

                if (parent.Nodes.Count == 1)
                {
                    ExtendedNode child = (ExtendedNode)parent.Nodes[0];
                    // Different behavior if parent is a named high-level node and child is not
                    if (parent.TaxonID > 0 && child.TaxonID == 0)
                    {
                        for (int i = child.Nodes.Count - 1; i >= 0; i--)
                        {
                            ExtendedNode grandchild = (ExtendedNode)child.Nodes[i];
                            child.Nodes.Remove(grandchild);
                            parent.Nodes.Add(grandchild);
                        }

                        parent.ChildDivergences.Clear();
                        for (int i = child.ChildDivergences.Count - 1; i >= 0; i--)
                        {
                            parent.ChildDivergences.Add(child.ChildDivergences[i]);
                            child.ChildDivergences.RemoveAt(i);
                        }

                        parent.Nodes.Remove(child);
                        this.nodeList.Remove(child);
                        this.leafList.Remove(child);

                        parent.Text = parent.TaxonName + " [" + parent.TaxonID + "] (" + parent.getNodeHeight(true).ToString("0.00") + ")" + " {" + parent.ChildDivergences.Count + "}";
                    }
                    else if (parent.Parent != null)
                    {
                        ExtendedNode node = (ExtendedNode)parent.Nodes[0];
                        ExtendedNode grandparent = (ExtendedNode)parent.Parent;

                        int oldParentIndex = grandparent.Nodes.IndexOf(parent);
                        parent.Nodes.Remove(node);

                        List<ExtendedNode> tempList = new List<ExtendedNode>();
                        tempList.AddRange(grandparent.Nodes.Cast<ExtendedNode>().ToList());
                        grandparent.Nodes.Clear();

                        grandparent.Nodes.Add(node);
                        foreach (ExtendedNode displacedNode in tempList)
                            grandparent.Nodes.Add(displacedNode);

                        grandparent.Nodes.Remove(parent);

                        this.nodeList.Remove(parent);
                    }
                    else
                    {
                        ExtendedNode oldRoot = parent;
                        if (oldRoot == this.root)
                        {
                            ExtendedNode newRoot = (ExtendedNode)oldRoot.Nodes[0];
                            this.nodeList.Remove(oldRoot);
                            oldRoot.Nodes.Remove(newRoot);

                            while (newRoot.Nodes.Count == 1)
                            {
                                oldRoot = newRoot;
                                newRoot = (ExtendedNode)oldRoot.Nodes[0];
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

        private void ClearNode(ExtendedNode node)
        {
            this.nodeList.Remove(node);
            this.leafList.Remove(node);

            foreach (ExtendedNode child in node.Nodes)
            {
                if (child != null)
                    ClearNode(child);
            }
        }

        public List<ExtendedNode> ValidNodes()
        {
            return nodeList.ToList().FindAll(delegate (ExtendedNode arg)
            {
                return arg.TreeView != null;
            });
        }        


    }
}
