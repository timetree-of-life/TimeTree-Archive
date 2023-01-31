//using NLog.Targets;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TimeTreeShared.Services;

namespace TopoTimeShared
{
    public static class TreeBuilderService
    {
        public static TopoTimeTree GenerateBackbone(int rootTaxonID, DatabaseService DBService, string baseRank = "species", bool collapseIncertaeGroups = true, bool isMedian = false, bool storeSubspecies = true, bool collapseSubspeciesGroups = false)
        {
            Dictionary<int, TopoTimeNode> nodeList = new Dictionary<int, TopoTimeNode>();
            Dictionary<int, int> parentList = new Dictionary<int, int>();

            TopoTimeTree newTree = new TopoTimeTree(isMedian);

            Dictionary<TopoTimeNode, int> highLevelLeaves = new Dictionary<TopoTimeNode, int>();
            HashSet<TopoTimeNode> speciesList = new HashSet<TopoTimeNode>();

            HALFunctions HALService = new HALFunctions(DBService, newTree);

            //string sql = "SELECT DISTINCT d.*,pt2.taxon_id FROM descendant_tt_taxonomy_list(" + rootTaxonID + ") d LEFT JOIN LATERAL (SELECT taxon_id FROM phylogeny_topology pt JOIN phylogeny_node pn ON pt.i_phylogeny_node_id=pn.i_phylogeny_node_id JOIN citations c ON c.i_citation_num=pn.i_citation_num WHERE c.qa_complete=true) pt2 ON d.taxon_id=pt2.taxon_id;";
            //string sql = $"SELECT * FROM timetree_subset((SELECT array_agg(DISTINCT d.taxon_id) FROM phylogeny_topology pt, descendant_tt_taxonomy_list({rootTaxonID}) d WHERE d.taxon_id=pt.taxon_id), {rootTaxonID});";
            // newest version using new study_taxon_entries table
            string sql;

            // use a more efficient query for "cellular organisms"
            if (rootTaxonID == 131567)
                sql = "SELECT * FROM timetree_subset((SELECT array_agg(DISTINCT st.current_taxon_id) FROM study_taxon_entries st, citations c WHERE st.i_citation_num=c.i_citation_num AND st.enabled=TRUE AND qa_complete=true));";
            else
                sql = $"SELECT * FROM timetree_subset((SELECT array_agg(DISTINCT d.taxon_id) FROM study_taxon_entries st, citations c, descendant_ncbi_list({rootTaxonID}) d WHERE st.i_citation_num=c.i_citation_num AND d.taxon_id=st.current_taxon_id AND enabled=true AND qa_complete=true), {rootTaxonID});";


            NpgsqlCommand filterCmd = new NpgsqlCommand("SELECT * FROM lone_representatives(@treeIndices, @taxonIDs, @parentID) WHERE count=0;");
            filterCmd.Parameters.Add("treeIndices", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            filterCmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            filterCmd.Parameters.Add("parentID", NpgsqlDbType.Integer);
            filterCmd.Connection = DBService.DBConnection;
            filterCmd.Prepare();

            DataTable table = DBService.GetSQLResult(sql);

            // to differentiate floating taxa in the HAL process, we assign them negative index numbers
            int floatingIndex = -1;

            if (table != null && table.Rows.Count > 0)
            {
                // process each node individually
                foreach (DataRow row in table.Rows)
                {
                    int taxonID = (int)row[0];
                    string taxonName = (string)row[1];
                    int parentID = (int)row[2];
                    string rank = (string)row[3];

                    TopoTimeNode newNode = new TopoTimeNode(taxonName, taxonID);

                    nodeList.Add(taxonID, newNode);
                    parentList.Add(taxonID, parentID);

                    newTree.includedTaxa[taxonID] = taxonName;

                    if (rank == baseRank)
                        speciesList.Add(newNode);
                }

                // attach nodes to their parents
                foreach (KeyValuePair<int, int> pair in parentList)
                {
                    int parentID = pair.Value;
                    int taxonID = pair.Key;

                    TopoTimeNode currentNode = nodeList[taxonID];

                    if (nodeList.TryGetValue(parentID, out TopoTimeNode parentNode))
                    {
                        if (collapseIncertaeGroups)
                        {
                            if (currentNode.TaxonName.StartsWith("unclassified ") || currentNode.TaxonName.Contains("ncertae "))
                                continue;

                            if (parentNode != null && parentNode.TaxonName.StartsWith("unclassified ") || parentNode.TaxonName.Contains("ncertae "))
                            {
                                //newTree.FloatingTaxa[floatingIndex] = currentNode;
                                currentNode.Floating = true;                                

                                do
                                {
                                    parentList.TryGetValue(parentID, out parentID);
                                    nodeList.TryGetValue(parentID, out parentNode);
                                }
                                while (parentNode != null && parentNode.TaxonName.StartsWith("unclassified ") || parentNode.TaxonName.Contains("ncertae "));                                
                            }
                        }

                        parentNode.Nodes.Add(nodeList[taxonID]);
                        
                    }
                }

                if (storeSubspecies)
                {
                    // check for taxa below the species level, remove them and add to species' stored named nodes list
                    foreach (TopoTimeNode speciesNode in speciesList)
                    {
                        if (speciesNode.Nodes.Count > 0)
                        {
                            TopoTimeNode currentLevel = speciesNode.Nodes[0];

                            while (speciesNode.Nodes.Count > 0)
                            {
                                while (currentLevel.Nodes.Count > 0)
                                {
                                    currentLevel = currentLevel.Nodes[0];
                                }

                                TopoTimeNode currentParent = currentLevel.Parent;
                                currentParent.Nodes.Remove(currentLevel);
                                nodeList.Remove(currentLevel.TaxonID);
                                speciesNode.StoreNamedNode(currentLevel);
                                currentLevel = currentParent;
                            }
                        }
                    }
                }
                else if (collapseSubspeciesGroups)
                {
                    // separate the subspecies as individual taxa
                    foreach (TopoTimeNode speciesNode in speciesList)
                    {
                        if (speciesNode.Nodes.Count > 1)
                        {
                            foreach (TopoTimeNode child in speciesNode.Nodes.Cast<TopoTimeNode>().ToList())
                            {
                                speciesNode.Nodes.Remove(child);
                                speciesNode.Parent.Nodes.Add(child);
                            }
                            speciesNode.Parent.StoreNamedNode(speciesNode);
                            speciesNode.Parent.Nodes.Remove(speciesNode);

                            nodeList.Remove(speciesNode.TaxonID);
                        }
                        // collapse lone subspecies into their parent
                        else if (speciesNode.Nodes.Count == 1)
                        {
                            /*
                            TopoTimeNode child = (TopoTimeNode)speciesNode.Nodes[0];
                            speciesNode.StoreNamedNode(child);
                            speciesNode.Nodes.Remove(child);
                            nodeList.Remove(child.TaxonID);
                            */

                            TopoTimeNode currentLevel = speciesNode.Nodes[0];

                            while (speciesNode.Nodes.Count > 0)
                            {
                                while (currentLevel.Nodes.Count > 0)
                                {
                                    currentLevel = currentLevel.Nodes[0];
                                }

                                TopoTimeNode currentParent = currentLevel.Parent;
                                currentParent.Nodes.Remove(currentLevel);
                                nodeList.Remove(currentLevel.TaxonID);
                                speciesNode.StoreNamedNode(currentLevel);
                                currentLevel = currentParent;
                            }
                        }
                    }
                }
                else
                {
                    // collapse lone subspecies into their parent
                    foreach (TopoTimeNode speciesNode in speciesList)
                    {
                        if (speciesNode.Nodes.Count == 1)
                        {
                            /*
                            TopoTimeNode child = (TopoTimeNode)speciesNode.Nodes[0];
                            speciesNode.StoreNamedNode(child);
                            speciesNode.Nodes.Remove(child);
                            nodeList.Remove(child.TaxonID);
                            */

                            TopoTimeNode currentLevel = speciesNode.Nodes[0];

                            while (speciesNode.Nodes.Count > 0)
                            {
                                while (currentLevel.Nodes.Count > 0)
                                {
                                    currentLevel = currentLevel.Nodes[0];
                                }

                                TopoTimeNode currentParent = currentLevel.Parent;
                                currentParent.Nodes.Remove(currentLevel);
                                nodeList.Remove(currentLevel.TaxonID);

                                speciesNode.StoreNamedNode(currentLevel);
                                currentLevel = currentParent;
                            }
                        }
                    }
                }

                // Collapse parents with only one child into their descendant
                foreach (KeyValuePair<int, TopoTimeNode> pair in nodeList.ToList())
                {
                    TopoTimeNode currentNode = pair.Value;                    

                    if (currentNode.Nodes.Count == 1)
                    {
                        TopoTimeNode child = (TopoTimeNode)currentNode.Nodes[0];
                        if (currentNode.Parent != null)
                        {
                            currentNode.Nodes.Remove(child);
                            currentNode.Parent.Nodes.Add(child);
                            currentNode.Parent.Nodes.Remove(currentNode);

                            child.Floating = currentNode.Floating;

                            child.StoreNamedNode(currentNode);
                            nodeList.Remove(currentNode.TaxonID);
                        }
                        else
                        {
                            rootTaxonID = child.TaxonID;
                            currentNode.Nodes.Remove(child);
                        }
                    }
                }                

                // Collapse leaves that are only representative of their parent in any study
                foreach (TopoTimeNode parentNode in nodeList.Values.Where(x => x.HasValidTaxon && x.Nodes.Count > 0))
                {
                    List<int> ImmediateChildIDs = parentNode.Nodes.Cast<TopoTimeNode>().Where(y => y.Nodes.Count == 0).Select(x => x.TaxonID).ToList();
                    //List<int> ImmediateChildIDs = parentNode.getNamedChildrenOnly().Select(x => x.TaxonID).ToList();
                    

                    int i = 0;
                    IEnumerable<int> leafIndices = Enumerable.Empty<int>();
                    IEnumerable<int> leafTaxonIDs = Enumerable.Empty<int>();
                    Dictionary<int, List<TopoTimeNode>> ChildIndex = new Dictionary<int, List<TopoTimeNode>>();
                    foreach (TopoTimeNode child in parentNode.Nodes)
                    {                      

                        List<TopoTimeNode> leaves = child.getNamedChildren().ToList();
                        ChildIndex[i] = leaves;


                        leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(i, leaves.Count));
                        leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, leaves.Select(node => node.TaxonID));

                        i++;
                    }

                    filterCmd.Parameters[0].Value = leafIndices.ToArray();
                    filterCmd.Parameters[1].Value = leafTaxonIDs.ToArray();
                    filterCmd.Parameters[2].Value = parentNode.TaxonID;


                    DataTable table2 = DBService.GetSQLResult(filterCmd);

                    foreach (DataRow filterRow in table2.Rows)
                    {
                        int NodeIndex = (int)filterRow[0];
                        foreach (TopoTimeNode node in ChildIndex[NodeIndex])
                        {
                            //node.BackColor = Color.Orange;
                            parentNode.Nodes.Remove(node);
                            parentNode.StoreNamedNode(node);
                        }

                        /*
                        int TaxonID = (int)filterRow[0];
                        if (nodeList.ContainsKey(TaxonID))
                        {
                            TopoTimeNode currentNode = nodeList[TaxonID];

                            parentNode.Nodes.Remove(currentNode);
                            parentNode.StoreNamedNode(currentNode);
                        }
                        */
                    }
                }

                // assign floating nodes to their respective parents' floating node groups
                foreach (TopoTimeNode parentNode in nodeList.Values)
                {
                    foreach (TopoTimeNode child in parentNode.Nodes)
                    {
                        if (!child.Floating)
                            continue;

                        newTree.StoreFloatingNode(parentNode, child);
                    }
                }

                foreach (KeyValuePair<int, TopoTimeNode> pair in nodeList)
                {
                    TopoTimeNode currentNode = pair.Value;

                    // Assign terminal nodes (species) to leaf list
                    if (currentNode.Nodes.Count == 0)
                    {
                        if (currentNode.Parent != null)
                        {
                            newTree.AddNewNodeToTree(currentNode);
                        }
                    }
                    else
                    {
                        newTree.AddNewNodeToTree(currentNode);
                        if (currentNode.Nodes.Count == 2)
                            HALService.setTimesHAL(currentNode, null, UseConflicts: HALFunctions.TopologyConflictMode.NoConflicts);
                    }
                }

                newTree.root = nodeList[rootTaxonID];
            }



            return newTree;

        }

        public static TopoTimeTree GenerateBackbone(List<int> tipTaxonIDs, DatabaseService DBService, string baseRank = "species", bool collapseIncertaeGroups = true, bool isMedian = false)
        {
            Dictionary<int, TopoTimeNode> nodeList = new Dictionary<int, TopoTimeNode>();
            Dictionary<int, int> parentList = new Dictionary<int, int>();

            TopoTimeTree newTree = new TopoTimeTree(isMedian);

            Dictionary<TopoTimeNode, int> highLevelLeaves = new Dictionary<TopoTimeNode, int>();
            HashSet<TopoTimeNode> speciesList = new HashSet<TopoTimeNode>();

            HALFunctions HALService = new HALFunctions(DBService, newTree);

            //string sql = "SELECT DISTINCT d.*,pt2.taxon_id FROM descendant_tt_taxonomy_list(" + rootTaxonID + ") d LEFT JOIN LATERAL (SELECT taxon_id FROM phylogeny_topology pt JOIN phylogeny_node pn ON pt.i_phylogeny_node_id=pn.i_phylogeny_node_id JOIN citations c ON c.i_citation_num=pn.i_citation_num WHERE c.qa_complete=true) pt2 ON d.taxon_id=pt2.taxon_id;";
            //string sql = $"SELECT * FROM timetree_subset((SELECT array_agg(DISTINCT d.taxon_id) FROM phylogeny_topology pt, descendant_tt_taxonomy_list({rootTaxonID}) d WHERE d.taxon_id=pt.taxon_id), {rootTaxonID});";
            // newest version using new study_taxon_entries table
            int rootTaxonID = 131567;  // root of all life node
            NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM timetree_subset(@taxonIDs);");
            cmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            cmd.Connection = DBService.DBConnection;
            cmd.Prepare();

            cmd.Parameters[0].Value = tipTaxonIDs.ToArray();

            DataTable table = DBService.GetSQLResult(cmd);

            // to differentiate floating taxa in the HAL process, we assign them negative index numbers
            int floatingIndex = -1;

            if (table != null && table.Rows.Count > 0)
            {
                // process each node individually
                foreach (DataRow row in table.Rows)
                {
                    int taxonID = (int)row[0];
                    string taxonName = (string)row[1];
                    int parentID = (int)row[2];
                    string rank = (string)row[3];

                    TopoTimeNode newNode = new TopoTimeNode(taxonName, taxonID);

                    nodeList.Add(taxonID, newNode);
                    parentList.Add(taxonID, parentID);

                    newTree.includedTaxa[taxonID] = taxonName;

                    if (rank == baseRank)
                        speciesList.Add(newNode);
                }

                // attach nodes to their parents
                foreach (KeyValuePair<int, int> pair in parentList)
                {
                    int parentID = pair.Value;
                    int taxonID = pair.Key;

                    TopoTimeNode currentNode = nodeList[taxonID];

                    if (nodeList.TryGetValue(parentID, out TopoTimeNode parentNode))
                    {
                        if (collapseIncertaeGroups)
                        {
                            if (currentNode.TaxonName.StartsWith("unclassified ") || currentNode.TaxonName.Contains("ncertae "))
                                continue;

                            if (parentNode != null && parentNode.TaxonName.StartsWith("unclassified ") || parentNode.TaxonName.Contains("ncertae "))
                            {
                                currentNode.Floating = true;

                                do
                                {
                                    parentList.TryGetValue(parentID, out parentID);
                                    nodeList.TryGetValue(parentID, out parentNode);
                                }
                                while (parentNode != null && parentNode.TaxonName.StartsWith("unclassified ") || parentNode.TaxonName.Contains("ncertae "));
                            }
                        }

                        parentNode.Nodes.Add(nodeList[taxonID]);
                    }
                }

                // check for taxa below the species level, remove them and add to species' stored named nodes list
                foreach (TopoTimeNode speciesNode in speciesList)
                {
                    if (speciesNode.Nodes.Count > 0)
                    {
                        TopoTimeNode currentLevel = speciesNode.Nodes[0];

                        while (speciesNode.Nodes.Count > 0)
                        {
                            while (currentLevel.Nodes.Count > 0)
                            {
                                currentLevel = currentLevel.Nodes[0];
                            }

                            TopoTimeNode currentParent = currentLevel.Parent;
                            currentParent.Nodes.Remove(currentLevel);
                            nodeList.Remove(currentLevel.TaxonID);
                            speciesNode.StoreNamedNode(currentLevel);
                            currentLevel = currentParent;
                        }
                    }
                }

                foreach (KeyValuePair<int, TopoTimeNode> pair in nodeList)
                {
                    TopoTimeNode currentNode = pair.Value;

                    // Assign terminal nodes (species) to leaf list
                    if (currentNode.Nodes.Count == 0)
                    {
                        if (currentNode.Parent != null)
                        {
                            newTree.AddNewNodeToTree(currentNode);
                        }
                    }
                    // Collapse lineages that contain only one descendant
                    else if (currentNode.Nodes.Count == 1)
                    {
                        TopoTimeNode child = (TopoTimeNode)currentNode.Nodes[0];
                        if (currentNode.Parent != null)
                        {
                            currentNode.Nodes.Remove(child);
                            currentNode.Parent.Nodes.Add(child);
                            currentNode.Parent.Nodes.Remove(currentNode);

                            child.StoreNamedNode(currentNode);
                        }
                        else
                        {
                            rootTaxonID = child.TaxonID;
                            currentNode.Nodes.Remove(child);
                        }
                    }
                    else
                    {
                        newTree.AddNewNodeToTree(currentNode);
                        if (currentNode.Nodes.Count == 2)
                            HALService.setTimesHAL(currentNode, null);
                    }
                }

                newTree.root = nodeList[rootTaxonID];
            }



            return newTree;

        }

        public static void CollapseLoneRepresentatives(TopoTimeTree selectedTree, DatabaseService DBService)
        {
            foreach (TopoTimeNode parentNode in selectedTree.nodeList.Where(x => x.HasValidTaxon && x.Nodes.Count > 0))
            {

            }
        }

        public static void ResolveByFastHAL(TopoTimeTree activeTree, DatabaseService DBService, HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts)
        {
            HALFunctions HALService = new HALFunctions(DBService, activeTree);

            List<TopoTimeNode> indexedNodeList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.Nodes.Count > 2).ToList();

            int unresolvedPartitions = indexedNodeList.Sum(x => x.Nodes.Count - 2);
            int remainingPartitions = unresolvedPartitions;

            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            for (int i = 0; i < indexedNodeList.Count; i++)
            {
                TopoTimeNode parentNode = indexedNodeList[i];
                DateTime beginNodeResolutionClock = DateTime.Now;

                if (parentNode.Nodes.Count > 2)
                {
                    parentNode.ChildDivergences.Clear();
                    int lastCount = -1;

                    while (parentNode.Nodes.Count > 2 && parentNode.Nodes.Count != lastCount)
                    {
                        lastCount = parentNode.Nodes.Count;
                        //RootToTipResolution(parentNode, conn, null, true);

                        TopoTimeNode resolvedRoot = HALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts);
                        //ExtendedNode resolvedRoot = HALService.ResolveFastHAL(parentNode, null, fastHALfetcher, timedHAL: true, useOnlyConcordant: useOnlyConcordantTimes);

                        if (resolvedRoot != null)
                        {
                            TopoTimeNode newNode = resolvedRoot;
                            activeTree.AddNewNodeToTree(newNode);

                        }
                    }

                    HALService.setTimesHAL(parentNode);
                }
                else if (parentNode.Nodes.Count == 2)
                    HALService.setTimesHAL(parentNode);
            }

            if (UseConflicts == HALFunctions.TopologyConflictMode.NoConflicts)
                activeTree.OperationHistory.Add("Tree fully resolved using Fast HAL (excluding topological conflict times)");
            else
                activeTree.OperationHistory.Add("Tree fully resolved using Fast HAL (including topological conflict times)");
        }

        public static void PruneNonContributingTaxa(TopoTimeTree activeTree, out string log)
        {
            activeTree.OperationHistory.Add("Non-contributing taxa pruned");

            StringBuilder removedLog = new StringBuilder();
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.ChildDivergences.Count == 0 && x.HasValidTaxon && x.Nodes.Count > 0).ToList())
            {
                foreach (TopoTimeNode child in node.Nodes.Cast<TopoTimeNode>().ToList())
                {
                    if (child.Nodes.Count == 0)
                    {
                        node.Nodes.Remove(child);
                        removedLog.AppendLine(child.Text);
                        activeTree.DeleteNode(child);
                        node.StoreNamedNode(child);
                    }
                }

                if (node.Nodes.Count == 0)
                    node.UpdateText();

                if (node.Nodes.Count == 1)
                {
                    TopoTimeNode child = node.Nodes[0];

                    TopoTimeNode[] grandchildren = child.Nodes.Cast<TopoTimeNode>().ToArray();
                    child.Nodes.Clear();
                    node.Nodes.Clear();
                    node.Nodes.AddRange(grandchildren);
                    node.ChildDivergences = child.ChildDivergences;
                    activeTree.DeleteNode(child);
                    node.UpdateText();
                }
            }

            foreach (TopoTimeNode trash in trashList)
            {
                activeTree.DeleteNode(trash);
            }

            log = removedLog.ToString();
        }

        public static void CleanTree(TopoTimeTree activeTree, bool removeViewless = true)
        {
            activeTree.OperationHistory.Add("Clean Tree");
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();
            if (removeViewless)
            {
                foreach (TopoTimeNode node in activeTree.nodeList)
                {
                    if (node.TreeView == null)
                        trashList.Add(node);
                }
            }

            foreach (TopoTimeNode node in trashList)
                activeTree.DeleteNode(node);

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Nodes.Count == 2)
                {
                    double age = activeTree.GetNodeHeight(node, false);
                    if (age != 0)
                    {
                        node.storedAdjustedHeight = age;
                    }
                }

                if (node.Nodes.Count == 0)
                {
                    node.ChildDivergences.Clear();
                    node.storedAdjustedHeight = 0;
                }
            }

            if (activeTree.root != null)
                activeTree.root.getLeaves(true);
        }

        public static void CollapseDuplicateTaxaGroups(TopoTimeTree activeTree)
        {
            TreeEditingService.CollapseDuplicateTaxaGroups(activeTree, activeTree.root);
        }

        public static void FastPartitionAnalysis(TopoTimeTree activeTree, DatabaseService DBService)
        {
            PartitionFetcher fetcher = new PartitionFetcher(DBService);
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                NodePartitionAnalysis(node, fetcher);
            }

            activeTree.OperationHistory.Add("Partitions fully analyzed");
        }


        public static void NodePartitionAnalysis(TopoTimeNode node, PartitionFetcher fetcher, bool displayResults = false)
        {
            if (node.Parent == null || node.Nodes.Count != 2 || node.Parent.Nodes.Count != 2)
                return;

            TopoTimeNode childA = node.Nodes[0];
            TopoTimeNode childB = node.Nodes[1];
            TopoTimeNode outgroup = node.Parent.Nodes[0] == node ? node.Parent.Nodes[1] : node.Parent.Nodes[0];

            //ExtendedNode outgroup = BigOutgroup(node);

            IEnumerable<TopoTimeNode> namedNodesA = childA.getNamedChildren();
            IEnumerable<TopoTimeNode> namedNodesB = childB.getNamedChildren();
            IEnumerable<TopoTimeNode> namedNodesC = outgroup.getNamedChildren();
            //IEnumerable<ExtendedNode> namedNodesC = outgroup.getNamedChildren().Except(namedNodesA.Concat(namedNodesB));
            IEnumerable<TopoTimeNode> allNodes = namedNodesA.Concat(namedNodesB.Concat(namedNodesC));

            Dictionary<int, TopoTimeNode> nodeIndexA = namedNodesA.ToDictionary(x => x.TaxonID, x => x);
            Dictionary<int, TopoTimeNode> nodeIndexB = namedNodesB.ToDictionary(x => x.TaxonID, x => x);
            Dictionary<int, TopoTimeNode> nodeIndexC = namedNodesC.ToDictionary(x => x.TaxonID, x => x);

            List<PartitionData> studyPartitions = fetcher.GetPartitionsForAnalysis(namedNodesA, namedNodesB, namedNodesC).ToList();
            //if (displayResults)
            //    DisplayPartitions(studyPartitions, allNodes.ToList());


            node.PartitionData = new SplitData();

            foreach (PartitionData studyPartition in studyPartitions)
            {
                int FavorAB = 0;
                int FavorAC = 0;
                int FavorBC = 0;

                bool group1A = studyPartition.nodesA.Any(nodeIndexA.Keys.Contains);
                bool group2A = studyPartition.nodesB.Any(nodeIndexA.Keys.Contains);

                bool group1B = studyPartition.nodesA.Any(nodeIndexB.Keys.Contains);
                bool group2B = studyPartition.nodesB.Any(nodeIndexB.Keys.Contains);

                bool group1C = studyPartition.nodesA.Any(nodeIndexC.Keys.Contains);
                bool group2C = studyPartition.nodesB.Any(nodeIndexC.Keys.Contains);

                bool group3A = studyPartition.outgroup.Any(nodeIndexA.Keys.Contains);
                bool group3B = studyPartition.outgroup.Any(nodeIndexB.Keys.Contains);
                bool group3C = studyPartition.outgroup.Any(nodeIndexC.Keys.Contains);


                // A and B form distinct groups, with A in first group and B in second
                if (group1A && group2B && group3C)
                {
                    FavorAB = 1;

                    if (!(group1C || group2C || group3A || group3B))
                    {
                        node.PartitionData.FavorAB++;
                    }
                    else
                    {
                        FavorAC = 1;
                        FavorBC = 1;
                    }
                }
                // A and B form distinct groups, with B in first group and A in second
                else if (group1B && group2A && group3C)
                {
                    FavorAB = 1;
                    if (!(group2C || group1C || group3A || group3B))
                    {
                        node.PartitionData.FavorAB++;
                    }
                    else
                    {
                        FavorAC = 1;
                        FavorBC = 1;
                    }
                }
                // A in first group, C in second group
                else if (group1A && group2C && group3B)
                {
                    FavorAC = 1;
                    if (!(group3A || group3C || group1B || group2B))
                    {
                        node.PartitionData.FavorAC++;
                    }
                    else
                    {
                        FavorAB = 1;
                        FavorBC = 1;
                    }
                }
                // C in first group, A in second group
                else if (group1C && group2A && group3B)
                {
                    FavorAC = 1;
                    if (!(group3C || group3A || group1B || group2B))
                    {
                        node.PartitionData.FavorAC++;
                    }
                    else
                    {
                        FavorAB = 1;
                        FavorBC = 1;
                    }
                }
                // B in first group, C in second group
                else if (group1B && group2C && group3A)
                {
                    FavorBC = 1;
                    if (!(group3B || group3C || group1A || group2A))
                    {
                        node.PartitionData.FavorBC++;
                    }
                    else
                    {
                        FavorAB = 1;
                        FavorAC = 1;
                    }
                }
                else if (group1C && group2B && group3A)
                {
                    FavorBC = 1;
                    if (!(group3C || group3B || group1A || group2A))
                    {
                        node.PartitionData.FavorBC++;
                    }
                    else
                    {
                        FavorAB = 1;
                        FavorAC = 1;
                    }
                }

                //Tuple<string, string, string, int> citationEntry = fetcher.CitationData[studyPartition.citation];

                //Study study = new Study(studyPartition.citation.ToString(), citationEntry.Item1, citationEntry.Item2, citationEntry.Item4, "");
                StudyData studyData = new StudyData(FavorAB, FavorAC, FavorBC, studyPartition.ListPhylogenyNodeIDs, CitationID: studyPartition.citation);

                /*
                foreach (int taxonID in studyPartition.nodesA)
                {
                    ExtendedNode leaf;
                    if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupA.Add(leaf.TaxaName);
                    else if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupA.Add(leaf.TaxaName + " [B]");
                    else if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupA.Add(leaf.TaxaName + " [C]");
                }

                foreach (int taxonID in studyPartition.nodesB)
                {
                    ExtendedNode leaf;
                    if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupB.Add(leaf.TaxaName);
                    else if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupB.Add(leaf.TaxaName + " [A]");
                    else if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupB.Add(leaf.TaxaName + " [C]");
                }

                foreach (int taxonID in studyPartition.outgroup)
                {
                    ExtendedNode leaf;
                    if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupC.Add(leaf.TaxaName);
                    else if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupC.Add(leaf.TaxaName + " [A]");
                    else if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.taxaGroupC.Add(leaf.TaxaName + " [B]");
                }
                */

                foreach (int taxonID in studyPartition.nodesA)
                {
                    TopoTimeNode leaf;
                    if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.TaxaAinA.Add(taxonID);
                    else if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.TaxaBinA.Add(taxonID);
                    else if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.TaxaCinA.Add(taxonID);
                }

                foreach (int taxonID in studyPartition.nodesB)
                {
                    TopoTimeNode leaf;
                    if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.TaxaBinB.Add(taxonID);
                    else if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.TaxaAinB.Add(taxonID);
                    else if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.TaxaCinB.Add(taxonID);
                }

                foreach (int taxonID in studyPartition.outgroup)
                {
                    TopoTimeNode leaf;
                    if (nodeIndexC.TryGetValue(taxonID, out leaf))
                        studyData.TaxaCinC.Add(taxonID);
                    else if (nodeIndexA.TryGetValue(taxonID, out leaf))
                        studyData.TaxaAinC.Add(taxonID);
                    else if (nodeIndexB.TryGetValue(taxonID, out leaf))
                        studyData.TaxaBinC.Add(taxonID);
                }

                node.PartitionData.studyData.Add(studyData);

            }
            //node.UpdateText();

        }

        public static void IterativeGroupRearrangments(TopoTimeTree activeTree, DatabaseService DBService)
        {
            HALFunctions HALService = new HALFunctions(DBService, activeTree);

            PartitionFetcher fetcher = new PartitionFetcher(DBService);

            List<TopoTimeNode> affectedNodes = activeTree.nodeList.Cast<TopoTimeNode>().Where(node => node.PartitionData != null && (node.PartitionData.FavorAB < node.PartitionData.FavorAC || node.PartitionData.FavorAB < node.PartitionData.FavorBC)).ToList();
            HashSet<TopoTimeNode> updatedNodes = new HashSet<TopoTimeNode>();
            HashSet<TopoTimeNode> updatedTimes = new HashSet<TopoTimeNode>();

            Dictionary<TopoTimeNode, string> lastRearrangeOperation = new Dictionary<TopoTimeNode, string>();

            while (affectedNodes.Count > 0)
            {
                WholeGroupRearrangeStep(ref affectedNodes, lastRearrangeOperation, updatedNodes, updatedTimes, fetcher);
            }

            foreach (TopoTimeNode node in updatedTimes)
            {
                if (!updatedNodes.Contains(node))
                    HALService.setTimesHAL(node);
            }

            foreach (TopoTimeNode node in updatedNodes)
            {
                HALService.setTimesHAL(node);
                if (node.TaxonID != 0)
                    node.ForeColor = Color.Red;
                else
                    node.ForeColor = Color.Blue;
            }

            activeTree.OperationHistory.Add("Iterative whole group rearrangements applied");
        }

        public static void SingleGroupRearrangement(TopoTimeTree activeTree, HALFunctions HALService, DatabaseService DBService)
        {
            PartitionFetcher fetcher = new PartitionFetcher(DBService);

            List<TopoTimeNode> affectedNodes = activeTree.nodeList.Cast<TopoTimeNode>().Where(node => node.PartitionData != null && (node.PartitionData.FavorAB < node.PartitionData.FavorAC || node.PartitionData.FavorAB < node.PartitionData.FavorBC)).ToList();
            HashSet<TopoTimeNode> updatedNodes = new HashSet<TopoTimeNode>();
            HashSet<TopoTimeNode> updatedTimes = new HashSet<TopoTimeNode>();

            Dictionary<TopoTimeNode, string> lastRearrangeOperation = new Dictionary<TopoTimeNode, string>();


            if (affectedNodes.Count > 0)
            {
                WholeGroupRearrangeStep(ref affectedNodes, lastRearrangeOperation, updatedNodes, updatedTimes, fetcher);
            }

            foreach (TopoTimeNode node in updatedTimes)
            {
                if (!updatedNodes.Contains(node))
                    HALService.setTimesHAL(node);
            }

            foreach (TopoTimeNode node in updatedNodes)
            {
                HALService.setTimesHAL(node);
                if (node.TaxonID != 0)
                    node.ForeColor = Color.Red;
                else
                    node.ForeColor = Color.Blue;
            }
        }

        private static void WholeGroupRearrangeStep(ref List<TopoTimeNode> affectedNodes, Dictionary<TopoTimeNode, string> lastRearrangeOperation, HashSet<TopoTimeNode> updatedNodes, HashSet<TopoTimeNode> updatedTimes, PartitionFetcher fetcher)
        {
            List<TopoTimeNode> rearrangedNodes = new List<TopoTimeNode>();

            foreach (TopoTimeNode node in affectedNodes)
            {
                TopoTimeNode outgroup;
                TopoTimeNode parent = node.Parent;

                if (parent == null || node.PartitionData == null || node.Nodes.Count == 0)
                    continue;

                if (parent.Nodes[1] == node)
                    outgroup = parent.Nodes[0];
                else
                    outgroup = parent.Nodes[1];

                if (rearrangedNodes.Contains(node))
                    continue;

                if (node.PartitionData.FavorAB < node.PartitionData.FavorAC && node.PartitionData.FavorAC >= node.PartitionData.FavorBC)
                {
                    // if the node has been rearranged once previously, then any AC operation is effectively an undo

                    /*
                    if (lastRearrangeOperation.TryGetValue(node, out string operation))
                    {
                        lastRearrangeOperation[node] = "";
                    }
                    else
                    */
                    {
                        TopoTimeNode nodeA = node.Nodes[0];
                        TopoTimeNode tempB = node.Nodes[1];

                        rearrangedNodes.Add(node);
                        rearrangedNodes.Add(parent);
                        rearrangedNodes.Add(nodeA);
                        rearrangedNodes.Add(tempB);
                        rearrangedNodes.Add(outgroup);

                        node.Nodes.Remove(tempB);
                        parent.Nodes.Remove(outgroup);

                        parent.Nodes.Add(tempB);
                        node.Nodes.Add(outgroup);

                        // move the taxon label since it no longer applies
                        if (node.HasValidTaxon)
                        {
                            // if the unmoved child already has a taxon label, we must merge them
                            if (nodeA.HasValidTaxon)
                            {
                                if (node.storedNamedNodes != null)
                                {
                                    if (nodeA.storedNamedNodes != null)
                                        nodeA.storedNamedNodes.AddRange(node.storedNamedNodes);
                                    else
                                        nodeA.storedNamedNodes = node.storedNamedNodes;
                                }
                            }
                            else
                            {
                                nodeA.TaxonID = node.TaxonID;
                                nodeA.TaxonName = node.TaxonName;
                                nodeA.storedNamedNodes = node.storedNamedNodes;
                            }

                            node.TaxonID = 0;
                            node.TaxonName = null;
                            node.storedNamedNodes = null;

                            //nodeA.ForeColor = Color.Red;
                            //nodeA.rearranged = true;
                        }

                        node.getLeaves(true);
                        //double oldTime = node.getNodeHeight(false);
                        //double newTime;

                        //setTimes(node, conn, false);
                        //newTime = node.getNodeHeight(false);

                        //table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                        node.ForeColor = Color.Blue;
                        node.rearranged = true;

                        updatedNodes.Add(node);
                        updatedTimes.Add(parent);
                        if (nodeA.Nodes.Count > 0)
                            updatedTimes.Add(nodeA);
                        if (tempB.Nodes.Count > 0)
                            updatedTimes.Add(tempB);
                        updatedTimes.Add(outgroup);

                        lastRearrangeOperation[node] = "AC";
                    }
                }
                else if (node.PartitionData.FavorAB < node.PartitionData.FavorBC)
                {
                    TopoTimeNode tempA = node.Nodes[0];
                    TopoTimeNode nodeB = node.Nodes[1];

                    rearrangedNodes.Add(node);
                    rearrangedNodes.Add(parent);
                    rearrangedNodes.Add(tempA);
                    rearrangedNodes.Add(nodeB);
                    rearrangedNodes.Add(outgroup);

                    node.Nodes.Remove(tempA);
                    parent.Nodes.Remove(outgroup);

                    parent.Nodes.Add(tempA);
                    node.Nodes.Add(outgroup);

                    // move the taxon label since it no longer applies
                    if (node.HasValidTaxon)
                    {
                        // if the unmoved child already has a taxon label, we must merge them
                        if (nodeB.HasValidTaxon)
                        {
                            if (node.storedNamedNodes != null)
                            {
                                if (nodeB.storedNamedNodes != null)
                                    nodeB.storedNamedNodes.AddRange(node.storedNamedNodes);
                                else
                                    nodeB.storedNamedNodes = node.storedNamedNodes;
                            }
                        }
                        else
                        {
                            nodeB.TaxonID = node.TaxonID;
                            nodeB.TaxonName = node.TaxonName;
                            nodeB.storedNamedNodes = node.storedNamedNodes;
                        }

                        node.TaxonID = 0;
                        node.TaxonName = null;
                        node.storedNamedNodes = null;

                        //nodeB.ForeColor = Color.Red;
                        //nodeB.rearranged = true;
                    }

                    node.getLeaves(true);
                    //double oldTime = node.getNodeHeight(false);
                    //double newTime;

                    //setTimes(node, conn, false);
                    //newTime = node.getNodeHeight(false);

                    //table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                    node.ForeColor = Color.Blue;
                    node.rearranged = true;

                    node.UpdateText();

                    updatedNodes.Add(node);
                    updatedTimes.Add(parent);
                    if (tempA.Nodes.Count > 0)
                        updatedTimes.Add(tempA);
                    if (nodeB.Nodes.Count > 0)
                        updatedTimes.Add(nodeB);
                    updatedTimes.Add(outgroup);

                    lastRearrangeOperation[node] = "BC";
                }
            }

            affectedNodes = rearrangedNodes;

            foreach (TopoTimeNode node in affectedNodes)
            {
                TreeBuilderService.NodePartitionAnalysis(node, fetcher);
            }
        }

        public static void TrifurcateRoot(TopoTimeTree ActiveTree)
        {
            TopoTimeNode root = ActiveTree.root;

            if (root.Nodes.Count == 2)
            {
                TopoTimeNode SelectedNode;

                if (root.Nodes[0].TaxonName == null)
                    SelectedNode = root.Nodes[0];
                else if (root.Nodes[1].TaxonName == null)
                    SelectedNode = root.Nodes[1];
                else
                    return;

                foreach (TopoTimeNode ChildNode in SelectedNode.Nodes.Cast<TopoTimeNode>().ToList())
                {
                    ActiveTree.MoveNode(ChildNode, root);
                }

                ActiveTree.DeleteNode(SelectedNode);
            }
        }
    }

    public static class TaxonomyDBService
    {

    }
}
