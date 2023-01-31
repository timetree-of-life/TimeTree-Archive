using System;
using System.Collections.Generic;
using System.Text;

namespace TopoTimeShared
{
    internal class NodeGroup
    {
        HashSet<TopoTimeNode> NodeSet;
    }

    public class NodeWrapper
    {
        TopoTimeNode Node;
        TopoTimeNode Parent;
        List<TopoTimeNode> ChildNodes;
    }
}
