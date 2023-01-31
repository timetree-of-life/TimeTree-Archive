using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopoTimeShared;

namespace TopoTimeShared
{

    public interface ICommand
    {
        void Execute();
        void UnExecute();
    }

    public class CommandBlock : ICommand
    {
        public Stack<ICommand> CommandStack { get; set; }

        public void Execute()
        {

        }

        public void UnExecute()
        {

        }
    }

    public class GroupNodeCommand : ICommand
    {
        private TopoTimeTree _tree;
        private List<TopoTimeNode> _nodeSet;
        private TopoTimeNode _parentNode;
        private Stack<ICommand> _commandBlock;
        public TopoTimeNode newGroupParent;

        public GroupNodeCommand(TopoTimeTree tree, TopoTimeNode parentNode, List<TopoTimeNode> nodeSet)
        {
            _tree = tree;
            _parentNode = parentNode;
            _nodeSet = nodeSet;
            _commandBlock = new Stack<ICommand>();
        }

        public void Execute()
        {
            if (_nodeSet.Count == 1)
            {
                newGroupParent = _nodeSet[0];
            }
            else
            {
                AddNodeCommand addGroupParent = new AddNodeCommand(_tree, _parentNode);
                addGroupParent.Execute();
                _commandBlock.Push(addGroupParent);

                newGroupParent = addGroupParent.newNode;

                foreach (TopoTimeNode childNode in _nodeSet)
                {
                    MoveNodeCommand moveToGroupParent = new MoveNodeCommand(_tree, childNode, newGroupParent);
                    moveToGroupParent.Execute();
                    _commandBlock.Push(moveToGroupParent);
                }
            }
        }

        public void UnExecute()
        {
            foreach (ICommand command in _commandBlock)
            {
                command.UnExecute();
            }
        }
    }

    public class ResolveNodeCommand : ICommand
    {
        private TopoTimeTree _tree;
        private List<TopoTimeNode> _nodeSetA;
        private List<TopoTimeNode> _nodeSetB;
        private List<TopoTimeNode> _outgroup;
        private TopoTimeNode _parentNode;
        private Stack<ICommand> _commandBlock;

        public ResolveNodeCommand(TopoTimeTree tree, TopoTimeNode parentNode, List<TopoTimeNode> nodeSetA, List<TopoTimeNode> nodeSetB, List<TopoTimeNode> outgroup = null)
        {
            _tree = tree;
            _nodeSetA = nodeSetA;
            _nodeSetB = nodeSetB;
            _outgroup = outgroup;
            _parentNode = parentNode;
            _commandBlock = new Stack<ICommand>();
        }

        public void Execute()
        {
            // group the nodes in each set under distinct nodes
            GroupNodeCommand groupNodeSetA = new GroupNodeCommand(_tree, _parentNode, _nodeSetA);
            groupNodeSetA.Execute();
            _commandBlock.Push(groupNodeSetA);

            GroupNodeCommand groupNodeSetB = new GroupNodeCommand(_tree, _parentNode, _nodeSetB);
            groupNodeSetB.Execute();
            _commandBlock.Push(groupNodeSetB);

            // if nodes outside the set remain,
            if (_outgroup != null)
            {
                // if there are still nodes outside
                List<TopoTimeNode> newGroup = new List<TopoTimeNode>() { groupNodeSetA.newGroupParent, groupNodeSetB.newGroupParent };
            }

        }

        public void UnExecute()
        {
            foreach (ICommand command in _commandBlock)
            {
                command.UnExecute();
            }
        }
    }

    public class AddNodeCommand : ICommand
    {
        private TopoTimeTree _tree;
        private TopoTimeNode _node;
        public readonly TopoTimeNode newNode;

        public AddNodeCommand(TopoTimeTree tree, TopoTimeNode node)
        {
            this._tree = tree;
            this._node = node;
            this.newNode = new TopoTimeNode();
        }

        public void Execute()
        {
            _node.Nodes.Add(newNode);
            _tree.nodeList.Add(newNode);
        }

        public void UnExecute()
        {
            _node.Nodes.Remove(newNode);
            _tree.nodeList.Remove(newNode);
            // fire event for adding _node to leafList, if applicable
            // fire event for removing _newNode from leafList and nodeList
            // fire event for updating issueList, etc.
        }
    }

    public class MoveNodeCommand : ICommand
    {
        private TopoTimeTree _tree;
        private TopoTimeNode _node;
        private TopoTimeNode _source;
        private TopoTimeNode _target;
        private int _sourceIndex;

        public MoveNodeCommand(TopoTimeTree tree, TopoTimeNode node, TopoTimeNode target)
        {
            this._tree = tree;
            this._node = node;
            this._source = node.Parent;
            this._sourceIndex = node.Index;
            this._target = target;
        }

        public void Execute()
        {
            _source.Nodes.Remove(_node);
            _target.Nodes.Add(_node);
            // handle updating leafList
        }

        public void UnExecute()
        {
            _target.Nodes.Remove(_node);
            _source.Nodes.Insert(_sourceIndex, _source);
            // handle updating leafList
        }
    }
}
