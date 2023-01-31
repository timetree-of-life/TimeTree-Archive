using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using TimeTreeShared.Services;
using TimeTreeShared;
using System.Xml.Linq;
using System.Drawing;
using Npgsql;
using NpgsqlTypes;

namespace TopoTimeShared
{
    public static class TreeEditingService
    {
        #region common group definitions

        private static Dictionary<int, TopoTimeNode> NodeDictionary(TopoTimeTree selectedTree)
        {
            Dictionary<int, TopoTimeNode> nodeDictionary = selectedTree.nodeList.Cast<TopoTimeNode>().Where(node => node.HasValidTaxon).ToDictionary(node => node.TaxonID, node => node);
            return nodeDictionary;
        }

        private static IEnumerable<IGrouping<int, TopoTimeNode>> NodeGroupDictionary(TopoTimeTree selectedTree)
        {
            IEnumerable<IGrouping<int, TopoTimeNode>> nodeGroupDictionary = selectedTree.nodeList.Cast<TopoTimeNode>().Where(node => node.HasValidTaxon).GroupBy(x => x.TaxonID);
            return nodeGroupDictionary;
        }

        private static ILookup<int, TopoTimeNode> NodeLookup(TopoTimeTree selectedTree)
        {
            ILookup<int, TopoTimeNode> nodeLookup = selectedTree.nodeList.Cast<TopoTimeNode>().Where(node => node.HasValidTaxon).ToLookup(x => x.TaxonID);
            return nodeLookup;
        }        

        private static ILookup<int, TopoTimeNode> NodeLookup(TopoTimeNode selectedNode)
        {
            ILookup<int, TopoTimeNode> nodeLookup = selectedNode.GetDescendants().Where(node => node.HasValidTaxon).ToLookup(x => x.TaxonID);
            return nodeLookup;
        }

        private static Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> NamedNodeDictionary(TopoTimeTree selectedTree)
        {
            // key: named node taxon ID
            // values: host node, named node name, named node

            Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> namedNodeDictionary = new Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>>();
            foreach (TopoTimeNode node in selectedTree.nodeList.Cast<TopoTimeNode>().Where(x => x.storedNamedNodes != null && x.storedNamedNodes.Count > 0))
            {
                foreach (TopoTimeNode namedNode in node.storedNamedNodes)
                {
                    namedNodeDictionary[namedNode.TaxonID] = new Tuple<TopoTimeNode, string, TopoTimeNode>(node, namedNode.TaxonName, namedNode);
                }
            }

            return namedNodeDictionary;
        }

        private static Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> NamedNodeDictionary(IEnumerable<TopoTimeNode> selectedNodes)
        {
            // key: named node taxon ID
            // values: host node, named node name, named node

            Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> namedNodeDictionary = new Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>>();
            foreach (TopoTimeNode node in selectedNodes.Where(x => x.storedNamedNodes != null && x.storedNamedNodes.Count > 0))
            {
                foreach (TopoTimeNode namedNode in node.storedNamedNodes)
                {
                    namedNodeDictionary[namedNode.TaxonID] = new Tuple<TopoTimeNode, string, TopoTimeNode>(node, namedNode.TaxonName, namedNode);
                }
            }

            return namedNodeDictionary;
        }

        #endregion

        #region divergence queries
        public static string nodeIDList(List<TopoTimeNode> nodes)
        {
            IEnumerable<int> taxaList = nodes.Select(x => x.TaxonID);
            return String.Join(",", taxaList.ToArray());
        }

        public static string nodeIDList(List<int> nodes)
        {
            return String.Join(",", nodes.ToArray());
        }



        #endregion

        public static TopoTimeTree trimToLevel(string selectedLevel, TopoTimeTree selectedTree, DatabaseService DBService, bool deleteUnsupportedGroups = true, bool deleteIncompleteChains = true, bool isActiveTree = false)
        {
            TopoTimeTree prunedTree;
            switch (selectedLevel)
            {
                case "genus":
                    // 22 is the hierarchy level of genus
                    // 29 is the hierarchy level of species
                    prunedTree = trimToLevel2(22, 29, selectedTree, DBService, isActiveTree: isActiveTree);
                    break;
                case "family":
                    // 18 is the hierachy level of family
                    // 22 is the hierarchy level of genus
                    prunedTree = trimToLevel2(18, 22, selectedTree, DBService, isActiveTree: isActiveTree);
                    break;
                case "order":
                    // 13 is the hierachy level of order
                    // 18 is the hierachy level of family
                    prunedTree = trimToLevel2(13, 18, selectedTree, DBService, isActiveTree: isActiveTree);
                    break;
                case "class":
                    // 7 is the hierachy level of class
                    // 13 is the hierachy level of order
                    prunedTree = trimToLevel2(7, 13, selectedTree, DBService, isActiveTree: isActiveTree);
                    break;
                case "phylum":
                    // 4 is the hierarchy level of phylum, although many higher-level groups are sub- or superphylum and this may be unintentionally excluding desired groups
                    // 7 is the hierachy level of class
                    prunedTree = trimToLevel2(4, 7, selectedTree, DBService, isActiveTree: isActiveTree);
                    break;
                default:
                    throw new Exception("undefined Linnaean rank");
            }

            prunedTree.OperationHistory.Add("Tree tips reduced to " + selectedLevel + " level");
            return prunedTree;
        }

        public static TopoTimeTree trimToLevel(int primaryLevel, int secondaryLevel, TopoTimeTree selectedTree, DatabaseService DBService, bool deleteUnsupportedGroups = true, bool deleteIncompleteChains = true, bool isActiveTree = false)
        {    

            StringBuilder log = new StringBuilder();

            // NEW METHOD 12-8-2020
            // Assumes that named node data is correctly assigned

            // preparation: make sure all taxon IDs are up-to-date
            UpdateTaxonIDs(selectedTree, DBService);

            // first step: simply find all visible nodes representing the rank and strip their descendants, retaining named node data
            //Dictionary<int, ExtendedNode> nodeDictionary = NodeDictionary(selectedTree);

            ILookup<int, TopoTimeNode> nodeLookup = NodeLookup(selectedTree);

            string taxonIDs = nodeIDList(nodeLookup.SelectMany(x => x).ToList());
            string sql = $"SELECT taxon_id FROM ncbi_taxonomy tt JOIN unnest(ARRAY[{taxonIDs}]) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy = {primaryLevel} ORDER BY level ASC;";
            DataTable taxaList = DBService.GetSQLResult(sql);

            foreach (DataRow row in taxaList.Rows)
            {
                int taxonID = (int)row[0];
                //CollapseNodeData(nodeDictionary[taxonID]);

                foreach (TopoTimeNode node in nodeLookup[taxonID])
                    CollapseNodeData(node, selectedTree);
            }

            
            Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> namedNodeDictionary = NamedNodeDictionary(selectedTree);
            string namedNodeTaxonIDs = nodeIDList(namedNodeDictionary.Keys.ToList());
            if (namedNodeTaxonIDs != "")
            {
                string sql2 = $"SELECT taxon_id FROM ncbi_taxonomy tt JOIN unnest(ARRAY[{namedNodeTaxonIDs}]) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy = {primaryLevel} ORDER BY level ASC;";
                DataTable namedNodeTaxaList = DBService.GetSQLResult(sql2);

                foreach (DataRow row in namedNodeTaxaList.Rows)
                {
                    int taxonID = (int)row[0];

                    TopoTimeNode node = namedNodeDictionary[taxonID].Item1;
                    string newTaxonName = namedNodeDictionary[taxonID].Item2;
                    TopoTimeNode oldNamedNode = namedNodeDictionary[taxonID].Item3;

                    if (node.Parent != null)
                    {
                        oldNamedNode.TaxonID = node.TaxonID;
                        oldNamedNode.TaxonName = node.TaxonName;

                        node.TaxonID = taxonID;
                        node.TaxonName = newTaxonName;

                        CollapseNodeData(node, selectedTree);

                        //node.UpdateText();
                    }
                }
            }
            

            // reset leaf lists
            foreach (TopoTimeNode node in selectedTree.nodeList)
                node.getLeaves(true);

            //if (isActiveTree)
            //    treeViewer.Nodes.RemoveAt(0);

            HardReloadTree(selectedTree.root, ref selectedTree);


            // legacy handling: older TopoTime files which do not have taxon representative information
            // go to each named node and check its rank
            // if it is lower rank than the desired cutoff, collapse it

            List<TopoTimeNode> trashList = new List<TopoTimeNode>();

            /*
            ILookup<int, TopoTimeNode> nodeLookup2 = NodeLookup(selectedTree);

            string taxonIDs2 = nodeIDList(nodeLookup2.SelectMany(x => x).ToList());
            string sql3 = $"SELECT taxon_id, (SELECT taxon_id FROM ancestral_ncbi_list(tt.taxon_id) tt2 JOIN rank_hierarchy rh2 ON tt2.rank=rh2.rank AND rh2.hierarchy={primaryLevel}), (SELECT scientific_name FROM ancestral_ncbi_list(tt.taxon_id) tt2 JOIN rank_hierarchy rh2 ON tt2.rank=rh2.rank AND rh2.hierarchy={primaryLevel}) FROM ncbi_taxonomy tt JOIN unnest(ARRAY[{taxonIDs2}]) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy > {primaryLevel};";
            DataTable taxaList2 = getSQLResult(sql3, conn);

            

            foreach (DataRow row in taxaList2.Rows)
            {
                int originalTaxonID = (int)row[0];

                foreach (TopoTimeNode currentNode in nodeLookup2[originalTaxonID])
                {
                    bool hasReplacement = !(row[1] is DBNull);

                    // if newTaxonID is null, then this node has no replacement and should be pruned...

                    if (hasReplacement)
                    {
                        TopoTimeNode newNamedTaxonNode = new TopoTimeNode();
                        newNamedTaxonNode.TaxonID = currentNode.TaxonID;
                        newNamedTaxonNode.TaxonName = currentNode.TaxonName;

                        int newTaxonID = (int)row[1];
                        string newTaxonName = row[2].ToString();
                        currentNode.TaxonID = newTaxonID;
                        currentNode.TaxonName = newTaxonName;

                        currentNode.StoreNamedNode(newNamedTaxonNode);
                    }
                    else if (deleteIncompleteChains)
                    {
                        trashList.Add(currentNode);
                    }
                    else
                    {
                        // TODO: develop some kind of system for placeholder monotypic groups 
                        // (i.e. making a placeholder for a class to hold a single order when the class doesn't exist)
                    }

                    CollapseNodeData(currentNode);
                }
            }

            foreach (TopoTimeNode trashNode in trashList)
                selectedTree.DeleteNode(trashNode);

            */

            // reset leaf lists
            foreach (TopoTimeNode node in selectedTree.nodeList)
                node.getLeaves(true);
            /*
            {
                HardReloadTree(selectedTree.root, ref selectedTree);
                DeleteDuplicateTaxa(selectedTree);
                HardReloadTree(selectedTree.root, ref selectedTree);
            }

            // final sweep: delete every node that is below the desired rank
            // this is a placeholder - rather than simply delete nodes, we would prefer them to collapse

            
            ILookup<int, TopoTimeNode> nodeLookup3 = NodeLookup(selectedTree);

            string taxonIDs3 = nodeIDList(nodeLookup3.SelectMany(x => x).ToList());
            string sql4 = $"SELECT DISTINCT taxon_id FROM unnest(ARRAY[{taxonIDs3}]) u JOIN ncbi_taxonomy tt ON u=tt.taxon_id JOIN rank_hierarchy rh ON tt.rank=rh.rank WHERE rh.hierarchy > {primaryLevel}";
            DataTable taxaList3 = DBService.GetSQLResult(sql4);

            trashList = new List<TopoTimeNode>();
            foreach (DataRow row in taxaList3.Rows)
            {
                int taxonID = (int)row[0];
                foreach (TopoTimeNode currentNode in nodeLookup3[taxonID])
                {
                    trashList.Add(currentNode);
                }
            }

            foreach (TopoTimeNode trashNode in trashList)
                selectedTree.DeleteNode(trashNode);

            {
                HardReloadTree(selectedTree.root, ref selectedTree);
                DeleteDuplicateTaxa(selectedTree);
                HardReloadTree(selectedTree.root, ref selectedTree);
            }
            
            */

            return selectedTree;



            // sanity checks : TODO
            // check to make sure all leaves are unique
            // check to make sure all groups of specified rank in the original tree are represented in the final tree
            // check to make sure all leaves are of specified rank
            // check to make sure all parents are higher than specified rank


        }

        public static TopoTimeTree trimToLevel2(int primaryLevel, int secondaryLevel, TopoTimeTree selectedTree, DatabaseService DBService, bool deleteUnsupportedGroups = true, bool deleteIncompleteChains = true, bool isActiveTree = false)
        {  

            StringBuilder log = new StringBuilder();
            NpgsqlCommand TaxaFetchCommand = new NpgsqlCommand("SELECT taxon_id FROM ncbi_taxonomy tt JOIN unnest(@taxonIDs) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy >= @primaryLevel AND rh.hierarchy < @secondaryLevel ORDER BY level ASC;"); ;
            TaxaFetchCommand.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            TaxaFetchCommand.Parameters.Add("primaryLevel", NpgsqlDbType.Integer);
            TaxaFetchCommand.Parameters.Add("secondaryLevel", NpgsqlDbType.Integer);
            TaxaFetchCommand.Connection = DBService.DBConnection;
            TaxaFetchCommand.Prepare();

            // NEW METHOD 12-8-2020
            // Assumes that named node data is correctly assigned

            // preparation: make sure all taxon IDs are up-to-date
            UpdateTaxonIDs(selectedTree, DBService);

            // first step: simply find all visible nodes representing the rank and strip their descendants, including named nodes as they bloat the session file sizes.
            
            

            //Dictionary<int, ExtendedNode> nodeDictionary = NodeDictionary(selectedTree);

            ILookup<int, TopoTimeNode> nodeLookup = NodeLookup(selectedTree.root);

            //string taxonIDs = nodeIDList(nodeLookup.SelectMany(x => x).ToList());
            //string sql = $"SELECT taxon_id FROM ncbi_taxonomy tt JOIN unnest(ARRAY[{taxonIDs}]) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy = {primaryLevel} ORDER BY level ASC;";

            TaxaFetchCommand.Parameters[0].Value = nodeLookup.Select(x => x.Key).ToArray();
            TaxaFetchCommand.Parameters[1].Value = primaryLevel;
            TaxaFetchCommand.Parameters[2].Value = secondaryLevel;
            DataTable taxaList = DBService.GetSQLResult(TaxaFetchCommand);

            foreach (DataRow row in taxaList.Rows)
            {
                int taxonID = (int)row[0];

                foreach (TopoTimeNode node in nodeLookup[taxonID])
                {
                    node.Nodes.Clear();
                    node.storedAdjustedHeight = 0;
                    node.ChildDivergences.Clear();

                    selectedTree.UpdateNodeText(node);
                }
            }

            // second step: because of partition rearrangement, some taxa groups will no longer be monophyletic.
            // so, all remaining tips below the specified level will be changed to the parent taxon of specified level (species, subgenus, subspecies etc. -> genus)
            // then we collapse the monophyletic groups of duplicates into a single taxon

            IEnumerable<TopoTimeNode> RemainingLeaves = selectedTree.root.GetDescendants().Where(x => x.Nodes.Count == 0);
            Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> namedNodeDictionary = NamedNodeDictionary(RemainingLeaves);

            if (namedNodeDictionary.Any())
            {
                TaxaFetchCommand.Parameters[0].Value = namedNodeDictionary.Keys.ToArray();
                TaxaFetchCommand.Parameters[1].Value = primaryLevel;
                TaxaFetchCommand.Parameters[2].Value = primaryLevel + 1;
                taxaList = DBService.GetSQLResult(TaxaFetchCommand);

                foreach (DataRow row in taxaList.Rows)
                {
                    int taxonID = (int)row[0];

                    TopoTimeNode node = namedNodeDictionary[taxonID].Item1;
                    string newTaxonName = namedNodeDictionary[taxonID].Item2;
                    TopoTimeNode oldNamedNode = namedNodeDictionary[taxonID].Item3;

                    if (node.Parent != null)
                    {
                        node.storedNamedNodes.Remove(oldNamedNode);

                        node.SetTaxonData(TaxonID: taxonID, TaxonName: newTaxonName);
                        //node.Text = node.TaxonIDLabel;

                        selectedTree.UpdateNodeText(node);
                    }
                }
            }

            ILookup<int, TopoTimeNode> leafLookup = selectedTree.root.GetDescendants().Where(node => node.HasValidTaxon && node.Nodes.Count == 0).ToLookup(x => x.TaxonID);
            TaxaFetchCommand = new NpgsqlCommand($"SELECT taxon_id, (SELECT taxon_id FROM ancestral_ncbi_list(tt.taxon_id) tt2 JOIN rank_hierarchy rh2 ON tt2.rank=rh2.rank AND rh2.hierarchy={primaryLevel}), (SELECT scientific_name FROM ancestral_ncbi_list(tt.taxon_id) tt2 JOIN rank_hierarchy rh2 ON tt2.rank=rh2.rank AND rh2.hierarchy={primaryLevel}) FROM ncbi_taxonomy tt JOIN unnest(@taxonIDs) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy > {primaryLevel} OR rh.hierarchy = -1;"); ;
            TaxaFetchCommand.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            TaxaFetchCommand.Connection = DBService.DBConnection;
            TaxaFetchCommand.Prepare();

            TaxaFetchCommand.Parameters[0].Value = leafLookup.Select(x => x.Key).ToArray();

            taxaList = DBService.GetSQLResult(TaxaFetchCommand);
            foreach (DataRow row in taxaList.Rows)
            {
                int oldTaxonID = (int)row[0];                

                foreach (TopoTimeNode currentNode in leafLookup[oldTaxonID])
                {
                    bool hasReplacement = !(row[1] is DBNull);

                    if (hasReplacement)
                    {
                        int newTaxonID = (int)row[1];

                        if (oldTaxonID != newTaxonID)
                        {
                            string newTaxonName = row[2].ToString();
                            currentNode.SetTaxonData(TaxonID: newTaxonID, TaxonName: newTaxonName);
                            selectedTree.UpdateNodeText(currentNode);
                        }
                    }
                    else
                    {
                        // third step: some tips do not have a parent of the specified rank, and must be deleted outright
                        selectedTree.DeleteNode(currentNode);
                    }
                }
            }

            DeleteDuplicateTaxa(selectedTree.root);            

            // reset leaf lists
            selectedTree.root.getLeaves(true);

            //HardReloadTree(selectedTree.root, ref selectedTree);            

            return selectedTree;


            // sanity checks : TODO
            // check to make sure all leaves are unique
            // check to make sure all groups of specified rank in the original tree are represented in the final tree
            // check to make sure all leaves are of specified rank
            // check to make sure all parents are higher than specified rank


        }
        public static void UpdateTaxonIDs(TopoTimeTree selectedTree, DatabaseService DBService)
        {

            Dictionary<int, TopoTimeNode> nodeDictionary = NodeDictionary(selectedTree);

            string taxonIDs = nodeIDList(nodeDictionary.Keys.ToList());
            string sql = $"SELECT u, CASE WHEN tt.taxon_id IS NULL AND new_taxon_id IS NOT NULL THEN new_taxon_id WHEN tt.taxon_id IS NOT NULL THEN tt.taxon_id ELSE 0 END as taxon_id, CASE WHEN tt.taxon_id IS NULL AND new_taxon_id IS NOT NULL THEN tt2.scientific_name WHEN tt.taxon_id IS NOT NULL THEN tt.scientific_name ELSE '' END as scientific_name FROM unnest(ARRAY[{taxonIDs}]) u LEFT JOIN taxonomy_changes tc on u=tc.old_taxon_id LEFT JOIN ncbi_taxonomy tt ON tt.taxon_id=u LEFT JOIN ncbi_taxonomy tt2 ON tt2.taxon_id=new_taxon_id;";
            DataTable taxaList = DBService.GetSQLResult(sql);

            foreach (DataRow row in taxaList.Rows)
            {
                int oldTaxonID = (int)row[0];
                int newTaxonID = (int)row[1];
                string newTaxonName = row[2].ToString();

                TopoTimeNode node = nodeDictionary[oldTaxonID];
                if (node.TaxonID != newTaxonID)
                {
                    node.TaxonID = newTaxonID;
                    node.TaxonName = newTaxonName;

                    node.UpdateText();
                }
            }

            Dictionary<int, Tuple<TopoTimeNode, string, TopoTimeNode>> namedNodeDictionary = NamedNodeDictionary(selectedTree);
            string namedNodeTaxonIDs = nodeIDList(namedNodeDictionary.Keys.ToList());
            if (namedNodeTaxonIDs != "")
            {
                string sql2 = $"SELECT taxon_id, u, scientific_name FROM taxonomy_changes tc JOIN unnest(ARRAY[{namedNodeTaxonIDs}]) u on u=tc.old_taxon_id JOIN ncbi_taxonomy tt ON tt.taxon_id=tc.new_taxon_id;";
                DataTable changedNodeTaxaList = DBService.GetSQLResult(sql2);

                foreach (DataRow row in changedNodeTaxaList.Rows)
                {
                    int newTaxonID = (int)row[0];
                    int oldTaxonID = (int)row[1];
                    string newTaxonName = row[2].ToString();

                    TopoTimeNode node = namedNodeDictionary[oldTaxonID].Item3;
                    if (node.TaxonID != newTaxonID)
                    {
                        node.TaxonID = newTaxonID;
                        node.TaxonName = newTaxonName;

                        selectedTree.includedTaxa[newTaxonID] = newTaxonName;

                        selectedTree.UpdateNodeText(node);
                    }
                }
            }

            //if (selectedTree == activeTree)
            //   treeViewer.Nodes.Add(selectedTree.root);
        }

        public static void dissolveBelowLevel(int primaryLevel, TopoTimeTree selectedTree, DatabaseService DBService)
        {
            TopoTimeNode root = selectedTree.root;

            // preparation: make sure all taxon IDs are up-to-date
            TreeEditingService.UpdateTaxonIDs(selectedTree, DBService);

            ILookup<int, TopoTimeNode> nodeLookup = NodeLookup(selectedTree);

            string taxonIDs = nodeIDList(nodeLookup.SelectMany(x => x).ToList());
            string sql = $"SELECT taxon_id FROM ncbi_taxonomy tt JOIN unnest(ARRAY[{taxonIDs}]) u on u=tt.taxon_id JOIN rank_hierarchy rh ON rh.rank=tt.rank WHERE rh.hierarchy = {primaryLevel};";
            DataTable taxaList = DBService.GetSQLResult(sql);

            foreach (DataRow row in taxaList.Rows)
            {
                int taxonID = (int)row[0];
                //CollapseNodeData(nodeDictionary[taxonID]);

                foreach (TopoTimeNode node in nodeLookup[taxonID])
                    reduceNodeToLeaves(node);
            }

            HardReloadTree(root, ref selectedTree);
                
        }

        public static void reduceNodeToLeaves(TopoTimeNode selectedNode)
        {

            if (selectedNode.Nodes.Count == 0)
                return;

            List<TopoTimeNode> leaves = selectedNode.getLeaves(true);
            List<TopoTimeNode> namedParents = selectedNode.getNamedChildren(skipNestedNodes: true).Except(leaves.Append(selectedNode)).ToList();

            foreach (TopoTimeNode NamedParent in namedParents)
                selectedNode.StoreNamedNode(NamedParent);

            selectedNode.Nodes.Clear();
            selectedNode.Nodes.AddRange(leaves.ToArray());

            // old and complicated??
            /*
            for (int i = selectedNode.Nodes.Count - 1; i >= 0; i--)
                selectedNode.Nodes.Remove(selectedNode.Nodes[i]);

            foreach (ExtendedNode leaf in leaves)
            {
                ExtendedNode ancestor = (ExtendedNode)leaf.Parent;
                ExtendedNode currentNode = leaf;

                if (ancestor != null)
                    ancestor.Nodes.Remove(leaf);

                while (ancestor != null)
                {
                    activeTree.nodeList.Remove(ancestor);
                    ancestor = (ExtendedNode)ancestor.Parent;
                }
            }

            foreach (ExtendedNode leaf in leaves)
            {
                selectedNode.Nodes.Add(leaf);
            }
            */

            selectedNode.ChildDivergences.Clear();
        }

        public static void MergeNodeIntoParent(TopoTimeNode node)
        {
            if (node.Parent == null)
                return;

            TopoTimeNode parent = node.Parent;
            int currentIndex = node.Index;

            List<TopoTimeNode> children = node.Nodes.Cast<TopoTimeNode>().ToList();
            node.Nodes.Clear();

            foreach (TopoTimeNode child in children)
                parent.Nodes.Insert(currentIndex, child);

            parent.Nodes.Remove(node);
            parent.StoreNamedNode(node);

        }

        private static void CollapseNodeData(TopoTimeNode node, TopoTimeTree selectedTree)
        {
            // removes descendants from this node but also preserves data by folding in named descendants as named nodes
            List<TopoTimeNode> namedChildren = node.getNamedChildren().ToList();

            node.storedNamedNodes = namedChildren.Where(x => x != node).ToList();

            // scrub all stored node data to prevent ballooning of file size
            // commented out because it takes too long, exclusion of extra data is now done at serialization time
            /*
            foreach (TopoTimeNode namedNode in node.storedNamedNodes)
            {
                if (namedNode.Text.Length > 0)
                {
                    namedNode.ChildDivergences.Clear();
                    namedNode.PartitionData = null;
                    namedNode.storedNamedNodes = null;
                    namedNode.Nodes.Clear();
                    //namedNode.Text = "";
                    namedNode.ForeColor = Color.FromArgb(0);
                    namedNode.storedAdjustedHeight = null;
                    namedNode.percentSupport = 0;
                    namedNode.SupportingStudies = 0;
                    namedNode.TotalStudies = 0;
                }
            }
            */

            List<TopoTimeNode> trashNodes = node.Nodes.Cast<TopoTimeNode>().ToList();

            node.Nodes.Clear();
            node.ChildDivergences.Clear();

            foreach (TopoTimeNode child in trashNodes)
                selectedTree.DeleteNode(child);

            node.storedAdjustedHeight = 0;

            //node.UpdateText();
        }

        public static string CollapseDuplicateTaxaGroups(TimeTree tree, ExtendedNode node, bool markPolyphyleticDuplicates = true)
        {
            // Go down the tree, checking each node whether its leaves all consist of a single taxa
            // If yes, collapse that node and remove all duplicates
            // If no, continue checking

            StringBuilder result = new StringBuilder();

            if (node == null)
                return "";

            IEnumerable<IGrouping<int, ExtendedNode>> duplicateChildren = node.getLeaves(true).GroupBy(x => x.TaxonID);
            if (duplicateChildren.Count() == 1)
            {
                result.AppendLine(node.Text + "*");
                node.ReduceNodeToLeaves();
                if (node.Nodes.Count > 1)
                {
                    foreach (ExtendedNode leaf in node.Nodes.Cast<ExtendedNode>().ToList().Skip(1))
                    {
                        tree.DeleteNode(leaf);
                    }
                    //DeleteNode((TVNode)node.Nodes[1]);
                }
            }
            else
            {


                result.AppendLine(node.Text);
                foreach (ExtendedNode child in node.Nodes.Cast<ExtendedNode>().ToList())
                {
                    result.Append(CollapseDuplicateTaxaGroups(tree, child, markPolyphyleticDuplicates));
                }
            }

            if (markPolyphyleticDuplicates)
            {
                duplicateChildren = node.getLeaves(true).GroupBy(x => x.TaxonID);
                foreach (IGrouping<int, ExtendedNode> taxaGroup in duplicateChildren)
                {
                    if (taxaGroup.Count() > 1)
                    {
                        foreach (ExtendedNode leaf in taxaGroup)
                        {
                            leaf.TaxonName = leaf.TaxonName.TrimEnd('*') + "*";
                            leaf.UpdateNode();
                        }
                    }
                }
            }

            return result.ToString();

            /*

            StringBuilder allChanges = new StringBuilder();

            Dictionary<int, TVNode> taxaList = new Dictionary<int, TVNode>();
            List<TVNode> trashList = new List<TVNode>();

            StringBuilder errors = new StringBuilder();

            foreach (TVNode node in this.nodeList)
            {
                IEnumerable<IGrouping<int, TVNode>> duplicateChildren = node.Nodes.Cast<TVNode>().Where(x => x.Nodes.Count == 0).GroupBy(x => x.TaxaID);
                foreach (IGrouping<int, TVNode> set in duplicateChildren)
                {
                    foreach (TVNode child in set.Skip(1))
                    {
                        errors.AppendLine("removed duplicate " + child.TaxaName);
                        trashList.Add(child);
                    }
                }
            }

            foreach (TVNode trashLeaf in trashList)
                this.DeleteNode(trashLeaf);

            trashList.Clear();

            foreach (TVNode leaf in this.leafList)
            {
                if (leaf.TaxaID != -1)
                {

                    if (!taxaList.ContainsKey(leaf.TaxaID))
                        taxaList[leaf.TaxaID] = leaf;
                    else
                    {
                        if (trashList.Contains(leaf) || leaf == taxaList[leaf.TaxaID])
                            continue;

                        // find a contiguous block of duplicate taxa
                        // start with the first instance found
                        // go up to its parent and get its leaves
                        // if all its leaves are of the same species, go up another level

                        TVNode repSpecies = leaf;
                        TVNode highestBlockAncestor = repSpecies;
                        bool foundHighestParent = false;

                        while (!foundHighestParent && highestBlockAncestor.Parent != null)
                        {
                            TVNode parent = (TVNode)highestBlockAncestor.Parent;
                            List<TVNode> parentLeaves = parent.getLeaves(false);

                            for (int i = 0; i < parentLeaves.Count; i++)
                            {
                                TVNode child = parentLeaves[i];
                                if (leaf.TaxaID != child.TaxaID)
                                {
                                    foundHighestParent = true;
                                    break;
                                }
                            }

                            if (!foundHighestParent)
                                highestBlockAncestor = parent;
                        }

                        if (highestBlockAncestor.Parent == null)
                        {
                            errors.Clear();
                            errors.AppendLine("Tree consists entirely of the same species");
                            continue;
                        }

                        List<TVNode> duplicateList;
                        if (repSpecies == highestBlockAncestor)
                        {
                            errors.AppendLine("paraphyletic group found on duplicate of " + leaf.TaxaName + " (" + leaf.TaxaID + ")");

                            // clear out all the other matching taxa on the parent so only one remains

                            TVNode parent = (TVNode)repSpecies.Parent;
                            duplicateList = new List<TVNode>();

                            foreach (TVNode otherLeaf in parent.Nodes)
                                if (otherLeaf.TaxaID == repSpecies.TaxaID && otherLeaf != taxaList[leaf.TaxaID])
                                    duplicateList.Add(otherLeaf);

                            highestBlockAncestor = parent;
                        }
                        else
                            duplicateList = highestBlockAncestor.getLeaves(false);

                        if (duplicateList.Count > 1)
                        {
                            for (int i = duplicateList.Count - 1; i > 0; i--)
                            {
                                //selectedTree.DeleteNode(duplicateList[i]);
                                trashList.Add(duplicateList[i]);
                                errors.AppendLine("removed duplicate " + duplicateList[i].TaxaName);
                            }

                            taxaList[leaf.TaxaID] = duplicateList[0];
                            errors.AppendLine("kept duplicate " + duplicateList[0].TaxaName + Environment.NewLine);
                        }
                    }
                }
            }

            foreach (TVNode trashLeaf in trashList)
                this.DeleteNode(trashLeaf);

            this.root.getLeaves(true);

            return errors.ToString();
            
                */
        }

        public static void DeleteDuplicateTaxa(TopoTimeTree selectedTree)
        {
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();

            foreach (TopoTimeNode leaf in selectedTree.leafList)
            {
                TopoTimeNode parent = (TopoTimeNode)leaf.Parent;
                if (parent != null)
                {
                    foreach (IGrouping<string, TopoTimeNode> duplicates in parent.Nodes.Cast<TopoTimeNode>().GroupBy(x => x.TaxonIDLabel))
                    {
                        if (duplicates.Count() > 1)
                        {
                            foreach (TopoTimeNode child in duplicates.Skip(1))
                            {
                                trashList.Add(child);
                            }
                        }
                    }
                }
            }

            foreach (TopoTimeNode node in trashList)
                selectedTree.DeleteNode(node);
        }

        public static void DeleteDuplicateTaxa(TopoTimeNode selectedNode)
        {
            int deletedNodes = 0;

            do
            {
                deletedNodes = 0;

                foreach (TopoTimeNode leaf in selectedNode.GetDescendants().Where(x => x.Nodes.Count == 0).ToList())
                {
                    TopoTimeNode parent = (TopoTimeNode)leaf.Parent;
                    if (parent != null)
                    {
                        foreach (IGrouping<string, TopoTimeNode> duplicates in parent.Nodes.Cast<TopoTimeNode>().GroupBy(x => x.TaxonIDLabel))
                        {
                            if (duplicates.Count() > 1)
                            {
                                foreach (TopoTimeNode child in duplicates.Skip(1))
                                {
                                    parent.Nodes.Remove(child);
                                    deletedNodes++;
                                }

                                if (parent.Nodes.Count == 1 && parent.Parent != null)
                                {
                                    TopoTimeNode grandparent = parent.Parent;
                                    TopoTimeNode child = parent.Nodes[0];

                                    parent.Nodes.Remove(child);
                                    grandparent.Nodes.Remove(parent);
                                    grandparent.Nodes.Add(child);
                                }
                            }
                        }
                    }
                }
            }
            while (deletedNodes != 0);
        }

        public static void HardReloadTree(TopoTimeNode root, ref TopoTimeTree selectedTree, bool useMedianTimes = false)
        {
            SerializableDictionary<int, Study> includedStudies = selectedTree.includedStudies;
            SerializableDictionary<int, string> includedTaxa = selectedTree.includedTaxa;
            List<string> OperationHistory = selectedTree.OperationHistory;
            selectedTree = new TopoTimeTree(selectedTree.UseMedianTimes);
            selectedTree.OperationHistory = OperationHistory;
            selectedTree.root = root;

            selectedTree.AddNodesToTree(root);

            selectedTree.includedStudies = includedStudies;
            selectedTree.includedTaxa = includedTaxa;
        }

    }
}
