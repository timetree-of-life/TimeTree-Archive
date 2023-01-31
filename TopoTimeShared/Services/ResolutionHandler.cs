using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using TimeTreeShared;
using TimeTreeShared.Services;

namespace TopoTimeShared
{
    public class RawPartitionData
    {
        public DataRow parent;
        public DataRow RowA = null;
        public DataRow RowB = null;
        public int outgroup = 0;

        // outgroup: 0 for undefined, 1 for row A, -1 for row B

        public bool IsComplete
        {
            get { return RowA != null && RowB != null; }
        }

        public RawPartitionData(DataRow row)
        {
            parent = row;
        }

        public void AddRow(DataRow row)
        {
            if (RowA == null)
                RowA = row;
            else if (RowB == null)
                RowB = row;
        }
    }

    public class PartitionFetcher
    {
        Dictionary<TopoTimeNode, string> NamedChildren;

        // key is citation id
        // values are pubmed\ref ID, first author name, title, year
        //public Dictionary<int, Tuple<string, string, string, int>> CitationData;
        public Dictionary<int, Study> CitationData;
        //NpgsqlConnection conn;
        DatabaseService DBService;

        public PartitionFetcher(DatabaseService DBService)
        {
            NamedChildren = new Dictionary<TopoTimeNode, string>();
            this.DBService = DBService;
            BuildCitationData();
        }

        private string NamedChildrenText(TopoTimeNode node)
        {
            if (NamedChildren.TryGetValue(node, out string text))
                return text;
            else
            {
                NamedChildren[node] = String.Join(",", node.getNamedChildren().Select(x => x.TaxonID));
                return NamedChildren[node];
            }
        }

        public void BuildCitationData()
        {
            string sql = "SELECT i_citation_num, COALESCE(i_citation_id::text, ref_id), c_first_author_lname, c_title, COALESCE(i_year, 0) FROM citations ORDER BY 1;";
            DataTable table = DBService.GetSQLResult(sql);

            //CitationData = table.Rows.Cast<DataRow>().ToDictionary(x => (int)x[0], x => new Tuple<string, string, string, int>  ((string)x[1], x[2].ToString(), x[3].ToString(), (int)x[4]));
            CitationData = table.Rows.Cast<DataRow>().ToDictionary(x => (int)x[0], x => new Study(x[0].ToString(), (string)x[1], x[2].ToString(), (int)x[4], x[3].ToString()));
        }

        // Prototype: GetTimeRelevantPartitions, but on Newick files
        // Step 1: Create tree objects from Newick trees
        // Step 2: Assign unique IDs to each node (these IDs are equivalent of phylogeny_node_id for trees from TT DB)
        // Possibly create in-memory database

        private NpgsqlCommand FastDivergenceTimesCmd = null;
        private NpgsqlCommand FastPartitionsCmd = null;
        public IEnumerable<PartitionData> GetTimeRelevantPartitions(TopoTimeNode target, IEnumerable<KeyValuePair<int, TopoTimeNode>> FloatingNodes = null, bool skipNestedNodes = false)
        {

            //foreach (PartitionData data in GetTimeRelevantPartitionsDEBUG(target))
            //    yield return data;
            //yield break;

           // Dictionary<int, Divergence> result = new Dictionary<int, Divergence>();

            if (FastDivergenceTimesCmd == null)
            {
                FastDivergenceTimesCmd = new NpgsqlCommand("SELECT i_parent_phylogeny_node_id, i_phylogeny_node_ida, i_phylogeny_node_idb, i_citation_num, f_time_estimate, tree_indexa, tree_indexb FROM partition_list(@taxon_indices, @taxonIDs);");
                FastDivergenceTimesCmd.Parameters.Add("taxon_indices", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                FastDivergenceTimesCmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                FastDivergenceTimesCmd.Connection = DBService.DBConnection;
                FastDivergenceTimesCmd.Prepare();
            }

            int i = 0;

            IEnumerable<int> leafIndices = Enumerable.Empty<int>();
            IEnumerable<int> leafTaxonIDs = Enumerable.Empty<int>();

            if (FloatingNodes != null)
            {
                leafIndices = leafIndices.Concat(FloatingNodes.Select(x => x.Key));
                leafTaxonIDs = leafTaxonIDs.Concat(FloatingNodes.Select(x => x.Value.TaxonID));
            }
            
            foreach (TopoTimeNode child in target.Nodes)
            {
                List<TopoTimeNode> leaves= child.getNamedChildren(skipNestedNodes).ToList();
                leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(i, leaves.Count));
                leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, leaves.Select(node => node.TaxonID));

                i++;
            }

            FastDivergenceTimesCmd.Parameters[0].Value = leafIndices.ToArray();
            FastDivergenceTimesCmd.Parameters[1].Value = leafTaxonIDs.ToArray();

            DataTable table = GetSQLResult(FastDivergenceTimesCmd);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[3];
                int phylogeny_node = (int)row[0];
                int childA_phylogeny_node = (int)row[1];
                int childB_phylogeny_node = (int)row[2];
                double divergenceTime = (double)row[4];

                int[] nodeIndexA = (int[])row[5];
                int[] nodeIndexB = (int[])row[6];
                //int[] nodeIndexC = row[7] is DBNull ? new int[0] : (int[])row[7];

                //int[] taxaIDsA = (int[])row[7];
                //int[] taxaIDsB = (int[])row[8];

                yield return new PartitionData(citation, phylogeny_node, childA_phylogeny_node, childB_phylogeny_node, nodeIndexA.ToList(), nodeIndexB.ToList(), null, divergenceTime);
            }
        }

        public IEnumerable<PartitionData> GetPartitionsForAnalysis(IEnumerable<TopoTimeNode> nodesA, IEnumerable<TopoTimeNode> nodesB, IEnumerable<TopoTimeNode> nodesC)
        {
            if (FastPartitionsCmd == null)
            {
                FastPartitionsCmd = new NpgsqlCommand("SELECT i_parent_phylogeny_node_id, i_phylogeny_node_ida, i_phylogeny_node_idb, i_citation_num, f_time_estimate, tree_indexa, tree_indexb, taxon_ida, taxon_idb FROM partition_list(@taxon_indices, @taxonIDs);");
                FastPartitionsCmd.Parameters.Add("taxon_indices", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                FastPartitionsCmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
                FastPartitionsCmd.Connection = DBService.DBConnection;
                FastPartitionsCmd.Prepare();
            }

            HashSet<int> ignoreList = new HashSet<int>();

            IEnumerable<int> leafIndices = Enumerable.Empty<int>();
            IEnumerable<int> leafTaxonIDs = Enumerable.Empty<int>();

            leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(1, nodesA.Count()));
            leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, nodesA.Select(node => node.TaxonID));

            leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(2, nodesB.Count()));
            leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, nodesB.Select(node => node.TaxonID));

            leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(3, nodesC.Count()));
            leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, nodesC.Select(node => node.TaxonID));

            FastPartitionsCmd.Parameters[0].Value = leafIndices.ToArray();
            FastPartitionsCmd.Parameters[1].Value = leafTaxonIDs.ToArray();

            DataTable table = GetSQLResult(FastPartitionsCmd);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[3];
                int parentNodeId = (int)row[0];
                int firstNodeID = (int)row[1];
                int secondNodeID = (int)row[2];
                double divergenceTime = (double)row[4];

                int[] nodeIndexA = (int[])row[5];
                int[] nodeIndexB = (int[])row[6];
                //int[] nodeIndexC = row[7] is DBNull ? new int[0] : (int[])row[7];

                //int[] taxaIDsA = (int[])row[7];
                //int[] taxaIDsB = (int[])row[8];

                if (ignoreList.Contains(parentNodeId))
                {
                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);
                    continue;
                }

                // check to see if it might be a valid MRCA (has at least one from A, B, and C)
                int[] FirstNodeIndex = (int[])row[5];
                int[] SecondNodeIndex = (int[])row[6];

                int[] FirstNodeTaxonIDs = ((int[])row[7]);
                int[] SecondNodeTaxonIDs = ((int[])row[8]);

                int DistinctCountA = FirstNodeIndex.Distinct().Count();
                int DistinctCountB = SecondNodeIndex.Distinct().Count();

                bool firstNodeIsOutgroup = false;
                bool secondNodeIsOutgroup = false;

                // if this node only covers one group, it must be an outgroup
                if (DistinctCountA == 1)
                {
                    ignoreList.Add(firstNodeID);
                    firstNodeIsOutgroup = true;
                }

                // if both only cover one group each, this node is irrelevant to us
                if (DistinctCountB == 1)
                {
                    ignoreList.Add(secondNodeID);
                    secondNodeIsOutgroup = true;
                }

                if (firstNodeIsOutgroup && secondNodeIsOutgroup)
                    continue;

                // ...however, if one node covers three groups, and the other covers only one, then this MRCA is being too inclusive
                // make the big group the new MRCA
                if ((DistinctCountA == 3 && DistinctCountB == 1) || (DistinctCountB == 3 && DistinctCountA == 1))
                {
                    ignoreList.Add(parentNodeId);
                    continue;
                }

                
                // MRCA requirement: covers all three groups
                if (FirstNodeIndex.Concat(SecondNodeIndex).Distinct().Count() == 3)
                {
                    // we pair the parent of potential AB pairs with their sibling node, representing the outgroup
                    //List<int> studyNodesA = ((int[])row[7]).ToList();
                    //List<int> studyNodesB = ((int[])row[8]).ToList();
                    //List<int> studyOutgroup = ((int[])outgroupNodeRow[5]).ToList();

                    List<int> studyNodesA = new List<int>();
                    List<int> studyNodesB = new List<int>();
                    List<int> studyOutgroup = null;
                    

                    if (firstNodeIsOutgroup)
                    {
                        studyOutgroup = FirstNodeTaxonIDs.ToList();
                        int firstValue = SecondNodeIndex[0];

                        for (int i = 0; i < SecondNodeIndex.Length; i++)
                        {
                            if (SecondNodeIndex[i] == firstValue)
                                studyNodesA.Add(SecondNodeTaxonIDs[i]);
                            else
                                studyNodesB.Add(SecondNodeTaxonIDs[i]);
                        }                        
                    }

                    if (secondNodeIsOutgroup)
                    {
                        studyOutgroup = SecondNodeTaxonIDs.ToList();
                        int firstValue = FirstNodeIndex[0];

                        for (int i = 0; i < FirstNodeIndex.Length; i++)
                        {
                            if (FirstNodeIndex[i] == firstValue)
                                studyNodesA.Add(FirstNodeTaxonIDs[i]);
                            else
                                studyNodesB.Add(FirstNodeTaxonIDs[i]);
                        }
                    }

                    int phylogeny_node = parentNodeId;

                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);

                    if (!firstNodeIsOutgroup && !secondNodeIsOutgroup)
                        continue;

                    yield return new PartitionData(citation, phylogeny_node, studyNodesA, studyNodesB, studyOutgroup, 0);
                }
                else
                {
                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);
                }
            }
        }

        public string TimeRelevantPartitionsDebugString()
        {
            if (FastDivergenceTimesCmd != null)
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                sb.Append("SELECT i_parent_phylogeny_node_id, i_phylogeny_node_ida, i_phylogeny_node_idb, i_citation_num, f_time_estimate, tree_indexa, tree_indexb FROM partition_list(ARRAY[");
                sb2.Append("], ARRAY[");

                int[] leafIndexArray = (int[])FastDivergenceTimesCmd.Parameters[0].Value;
                int[] leafTaxonIDArray = (int[])FastDivergenceTimesCmd.Parameters[1].Value;

                sb.Append(String.Join(",", leafIndexArray));
                sb2.Append(String.Join(",", leafTaxonIDArray));

                sb.Append(sb2 + "]);");

                return sb.ToString();
            }
            return "";
        }

        public DataTable GetSQLResult(NpgsqlCommand command)
        {
            NpgsqlDataAdapter da;
            DataSet set;
            DataTable table;

            da = new NpgsqlDataAdapter(command);
            set = new DataSet();
            da.Fill(set);
            table = set.Tables[0];

            return table;
        }

        public IEnumerable<PartitionData> GetTimeRelevantPartitionsDEBUG(TopoTimeNode target)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb.Append("SELECT i_parent_phylogeny_node_id, i_phylogeny_node_ida, i_phylogeny_node_idb, i_citation_num, f_time_estimate, tree_indexa, tree_indexb FROM partition_list(ARRAY[");
            sb2.Append("], ARRAY[");

            HashSet<Tuple<int, int>> ignoreList = new HashSet<Tuple<int, int>>();

            int i = 0;
            foreach (TopoTimeNode child in target.Nodes)
            {
                List<TopoTimeNode> leaves = child.getNamedChildren().ToList();

                if (i > 0)
                {
                    sb.Append(",");
                    sb2.Append(",");
                }

                sb.Append(String.Join(",", Enumerable.Repeat(i, leaves.Count)));
                sb2.Append(String.Join(",", leaves.Select(node => node.TaxonID)));

                i++;
            }

            sb.Append(sb2 + "]);");

            string sql = sb.ToString();
            DataTable table = DBService.GetSQLResult(sql);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[3];
                int phylogeny_node = (int)row[0];
                int childA_phylogeny_node = (int)row[1];
                int childB_phylogeny_node = (int)row[2];
                double divergenceTime = (double)row[4];

                int[] nodeIndexA = (int[])row[5];
                int[] nodeIndexB = (int[])row[6];
                //int[] nodeIndexC = row[7] is DBNull ? new int[0] : (int[])row[7];

                //int[] taxaIDsA = (int[])row[7];
                //int[] taxaIDsB = (int[])row[8];

                //yield return new PartitionData(citation, phylogeny_node, childA_phylogeny_node, childB_phylogeny_node, nodeIndexA.ToList(), nodeIndexB.ToList(), null, divergenceTime, taxaA: taxaIDsA.ToList(), taxaB: taxaIDsB.ToList());
                yield return new PartitionData(citation, phylogeny_node, childA_phylogeny_node, childB_phylogeny_node, nodeIndexA.ToList(), nodeIndexB.ToList(), null, divergenceTime, taxaA: null, taxaB: null);
            }
        }

        public IEnumerable<PartitionData> GetRootPartitions(TopoTimeNode target)
        {
            foreach (PartitionData data in GetRootPartitionsDEBUG(target))
                yield return data;
            yield break;

            NpgsqlCommand rootPartitionsCommand = new NpgsqlCommand("SELECT i_phylogeny_node_id, i_parent_phylogeny_node_id, i_citation_num, f_time_estimate, taxon_ids FROM root_partition_list(@taxon_indices, @taxonIDs);");
            rootPartitionsCommand.Parameters.Add("taxon_indices", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            rootPartitionsCommand.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            rootPartitionsCommand.Connection = DBService.DBConnection;
            rootPartitionsCommand.Prepare();

            int i = 0;
            //IEnumerable<int> leafIndices = FloatingNodes.Select(x => x.Key);
            //IEnumerable<int> leafTaxonIDs = FloatingNodes.Select(x => x.Value.TaxaID);

            IEnumerable<int> leafIndices = Enumerable.Empty<int>();
            IEnumerable<int> leafTaxonIDs = Enumerable.Empty<int>();
            foreach (TopoTimeNode child in target.Nodes)
            {
                List<TopoTimeNode> leaves = child.getNamedChildren().ToList();
                leafIndices = Enumerable.Concat<int>(leafIndices, Enumerable.Repeat(i, leaves.Count));
                leafTaxonIDs = Enumerable.Concat<int>(leafTaxonIDs, leaves.Select(node => node.TaxonID));

                i++;
            }

            rootPartitionsCommand.Parameters[0].Value = leafIndices.ToArray();
            rootPartitionsCommand.Parameters[1].Value = leafTaxonIDs.ToArray();

            DataTable table = GetSQLResult(rootPartitionsCommand);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[2];
                int phylogeny_node = (int)row[0];

                double divergenceTime = (double)row[3];
                int[] taxonIDs = (int[])row[4];

                //yield return new PartitionData(citation, phylogeny_node, childA_phylogeny_node, childB_phylogeny_node, nodeIndexA.ToList(), nodeIndexB.ToList(), null, divergenceTime, taxaA: taxaIDsA.ToList(), taxaB: taxaIDsB.ToList());
                yield return new PartitionData(citation, phylogeny_node, taxonIDs.ToList(), divergenceTime);
            }
        }

        public IEnumerable<PartitionData> GetRootPartitionsDEBUG(TopoTimeNode target)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb.Append("SELECT i_phylogeny_node_id, i_parent_phylogeny_node_id, i_citation_num, f_time_estimate, taxon_ids FROM root_partition_list(ARRAY[");
            sb2.Append("], ARRAY[");

            HashSet<Tuple<int, int>> ignoreList = new HashSet<Tuple<int, int>>();

            int i = 0;
            foreach (TopoTimeNode child in target.Nodes)
            {
                List<TopoTimeNode> leaves = child.getNamedChildren().ToList();

                if (i > 0)
                {
                    sb.Append(",");
                    sb2.Append(",");
                }

                sb.Append(String.Join(",", Enumerable.Repeat(i, leaves.Count)));
                sb2.Append(String.Join(",", leaves.Select(node => node.TaxonID)));

                i++;
            }

            sb.Append(sb2 + "]);");

            string sql = sb.ToString();
            DataTable table = DBService.GetSQLResult(sql);

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[2];
                int phylogeny_node = (int)row[0];
                
                double divergenceTime = (double)row[3];
                int[] taxonIDs = (int[])row[4];

                //yield return new PartitionData(citation, phylogeny_node, childA_phylogeny_node, childB_phylogeny_node, nodeIndexA.ToList(), nodeIndexB.ToList(), null, divergenceTime, taxaA: taxaIDsA.ToList(), taxaB: taxaIDsB.ToList());
                yield return new PartitionData(citation, phylogeny_node, taxonIDs.ToList(), divergenceTime);
            }
        }



        // have to retire this because partition_list3 skips over partitions when there is a multi-generation gap between the taxa found in study trees between A and B
        // back to the slower version of filtering out nodes higher than the MRCA through TopoTime
        public IEnumerable<PartitionData> GetPartitions2(IEnumerable<TopoTimeNode> nodesA, IEnumerable<TopoTimeNode> nodesB, IEnumerable<TopoTimeNode> nodesC)
        {
            HashSet<int> ignoreList = new HashSet<int>();

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM partition_list3(ARRAY[");

            // partition_list3 calls for group indices first, taxon IDs second

            sb.Append(String.Join(",", Enumerable.Repeat(1, nodesA.Count())) + ",");
            sb.Append(String.Join(",", Enumerable.Repeat(2, nodesB.Count())) + ",");
            sb.Append(String.Join(",", Enumerable.Repeat(3, nodesC.Count())) + "], ARRAY[");

            //sb.Append(String.Join(",", nodesA.Select(node => NamedChildrenText(node))) + ",");
            //sb.Append(String.Join(",", nodesB.Select(node => NamedChildrenText(node))) + ",");
            //sb.Append(String.Join(",", nodesC.Select(node => NamedChildrenText(node))) + "]);");

            sb.Append(String.Join(",", nodesA.Select(node => node.TaxonID)) + ",");
            sb.Append(String.Join(",", nodesB.Select(node => node.TaxonID))+ ",");
            sb.Append(String.Join(",", nodesC.Select(node => node.TaxonID)) + "]);");


            string sql = sb.ToString();
            DataTable table = DBService.GetSQLResult(sql);

            Dictionary<int, DataRow> nodeSiblingPairs = new Dictionary<int, DataRow>();
            DataRow outgroupNodeRow = null;

            // NEW VERSION April 2022 - uses partition_list5, already in use for the time partitions

            // SQL function partition_list5, given a set of input taxa each mapped to a partition A, B, and C, will produce a list of valid associated partitions
            // TO-DO: Update it to utilize the study_partitions & study_taxon_entries tables instead of phylogeny topology 
            //
            /*
            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                int citation = (int)row[3];
                int parentNodeId = (int)row[0];
                int firstNodeID = (int)row[1];
                int secondNodeID = (int)row[2];

                if (ignoreList.Contains(parentNodeId))
                {
                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);
                    continue;
                }

                // check to see if it might be a valid MRCA (has at least one from A, B, and C)
                int[] firstNodeSet = (int[])row[5];
                int[] secondNodeSet = (int[])row[6];

                // if no existing MRCA is defined or current set's parent IDs do not match defined MRCA
                if (!nodeSiblingPairs.TryGetValue(parentNodeId, out outgroupNodeRow))
                {
                    int DistinctCountA = firstNodeSet.Distinct().Count();
                    int DistinctCountB = secondNodeSet.Distinct().Count();

                    // if this node only covers one group, it must be an outgroup
                    if (DistinctCountA == 1)
                        ignoreList.Add(firstNodeID);

                    // if both only cover one group each, this node is irrelevant to us
                    if (DistinctCountB == 1)
                        ignoreList.Add(secondNodeID);

                    // ...however, if one node covers three groups, and the other covers only one, then this MRCA is being too inclusive
                    // make the big group the new MRCA
                    if ((DistinctCountA == 3 && DistinctCountB == 1) || (DistinctCountB == 3 && DistinctCountA == 1))
                    {
                        ignoreList.Add(parentNodeId);
                    }
                    else
                    {
                        // MRCA requirement: covers all three groups
                        if (firstNodeSet.Concat(secondNodeSet).Distinct().Count() == 3)
                        {
                            // we pair the parent of potential AB pairs with their sibling node, representing the outgroup
                            nodeSiblingPairs[firstNodeID] = secondNode;
                            nodeSiblingPairs[secondNodeID] = firstNode;
                        }
                        else
                        {
                            ignoreList.Add(firstNodeID);
                            ignoreList.Add(secondNodeID);
                        }
                    }
                }
                else
                {
                    List<int> studyNodesA = ((int[])row[7]).ToList();
                    List<int> studyNodesB = ((int[])row[8]).ToList();
                    List<int> studyOutgroup = ((int[])outgroupNodeRow[5]).ToList();

                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);

                    if (secondNodeSet[0] == 1 || firstNodeSet[0] == 2)
                        yield return new PartitionData(citation, parentNodeId, studyNodesB, studyNodesA, studyOutgroup, 0);
                    else
                        yield return new PartitionData(citation, parentNodeId, studyNodesA, studyNodesB, studyOutgroup, 0);
                }

            }
            */

            // let's assume for the moment that all our study trees are perfectly bifurcating
            // then we can increment by 2 and assume row m+1 exists
            for (int m = 0; m < table.Rows.Count; m += 2)
            {
                DataRow firstNode = table.Rows[m];
                DataRow secondNode = table.Rows[m + 1];                

                // this should never be null, if it is something went very wrong
                int parentNodeId = (int)firstNode[1];
                int parentNodeId2 = (int)secondNode[1];

                // partition_list3 is intended to produce pairs of sibling nodes, with the same parent ID, ordered in a list
                // however, for reasons that are unclear it can yield singleton nodes we do not want included
                // this hacky fix skips them, theoretically

                while (parentNodeId != parentNodeId2)
                {
                    int citation = (int)firstNode[3];

                    // produce a blank PartitionData to notify of the error
                    yield return new PartitionData(citation, parentNodeId, new List<int>(), new List<int>(), new List<int>(), 0);

                    m++;

                    firstNode = table.Rows[m];
                    secondNode = table.Rows[m + 1];

                    parentNodeId = (int)firstNode[1];
                    parentNodeId2 = (int)secondNode[1];
                }

                int firstNodeID = (int)firstNode[0];
                int secondNodeID = (int)secondNode[0];

                if (ignoreList.Contains(parentNodeId))
                {
                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);
                    continue;
                }

                // check to see if it might be a valid MRCA (has at least one from A, B, and C)
                int[] firstNodeSet = (int[])firstNode[4];
                int[] secondNodeSet = (int[])secondNode[4];

                // if no existing MRCA is defined or current set's parent IDs do not match defined MRCA
                if (!nodeSiblingPairs.TryGetValue(parentNodeId, out outgroupNodeRow))
                {
                    int DistinctCountA = firstNodeSet.Distinct().Count();
                    int DistinctCountB = secondNodeSet.Distinct().Count();

                    // if this node only covers one group, it must be an outgroup
                    if (DistinctCountA == 1)
                        ignoreList.Add(firstNodeID);

                    // if both only cover one group each, this node is irrelevant to us
                    if (DistinctCountB == 1)
                        ignoreList.Add(secondNodeID);

                    // ...however, if one node covers three groups, and the other covers only one, then this MRCA is being too inclusive
                    // make the big group the new MRCA
                    if ((DistinctCountA == 3 && DistinctCountB == 1) || (DistinctCountB == 3 && DistinctCountA == 1))
                    {
                        ignoreList.Add(parentNodeId);
                    }
                    else
                    {
                        // MRCA requirement: covers all three groups
                        if (firstNodeSet.Concat(secondNodeSet).Distinct().Count() == 3)
                        {
                            // we pair the parent of potential AB pairs with their sibling node, representing the outgroup
                            nodeSiblingPairs[firstNodeID] = secondNode;
                            nodeSiblingPairs[secondNodeID] = firstNode;
                        }
                        else
                        {
                            ignoreList.Add(firstNodeID);
                            ignoreList.Add(secondNodeID);
                        }
                    }                    
                }
                else
                {
                    List<int> studyNodesA = ((int[])firstNode[5]).ToList();
                    List<int> studyNodesB = ((int[])secondNode[5]).ToList();
                    List<int> studyOutgroup = ((int[])outgroupNodeRow[5]).ToList();
                    int citation = (int)firstNode[3];
                    int phylogeny_node = parentNodeId;

                    ignoreList.Add(firstNodeID);
                    ignoreList.Add(secondNodeID);

                    if (secondNodeSet[0] == 1 || firstNodeSet[0] == 2)
                        yield return new PartitionData(citation, phylogeny_node, studyNodesB, studyNodesA, studyOutgroup, 0);
                    else
                        yield return new PartitionData(citation, phylogeny_node, studyNodesA, studyNodesB, studyOutgroup, 0);
                }
            }
        }

        public IEnumerable<PartitionData> GetPartitions3(IEnumerable<TopoTimeNode> nodesA, IEnumerable<TopoTimeNode> nodesB, IEnumerable<TopoTimeNode> nodesC, bool getMRCAOnly = true)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM partition_list3(ARRAY[");

            // partition_list4 calls for group indices first, taxon IDs second

            sb.Append(String.Join(",", Enumerable.Repeat(1, nodesA.Count())) + ",");
            sb.Append(String.Join(",", Enumerable.Repeat(2, nodesB.Count())) + ",");
            sb.Append(String.Join(",", Enumerable.Repeat(3, nodesC.Count())) + "], ARRAY[");

            sb.Append(String.Join(",", nodesA.Select(node => node.TaxonID)) + ",");
            sb.Append(String.Join(",", nodesB.Select(node => node.TaxonID)) + ",");
            sb.Append(String.Join(",", nodesC.Select(node => node.TaxonID)) + "]);");


            string sql = sb.ToString();
            DataTable table = DBService.GetSQLResult(sql);

            RawPartitionData foundMRCA = null;
            int rootSize = 0;
            int[] allNodes = null;

            Dictionary<int, Tuple<double, List<DataRow>, List<int>>> foundParentIDs = new Dictionary<int, Tuple<double, List<DataRow>, List<int>>>();

            //Dictionary<int, RawPartitionData> foundParents = new Dictionary<int, RawPartitionData>();

            // 0 is phylogeny_node_id
            // 1 is parent_phylogeny_node_id
            // 2 is divergence time
            // 3 is citation ID
            // 4 is list of partition indices
            // 5 is list of taxa (maps 1-to-1 with indices)
            // 6 is count of taxa

            DataRow lastRow = null;

            // set to true if we have found MRCA, the true A and B, and established which one is the outgroup
            bool doneWithA = false;
            bool doneWithB = false;
            bool doneLooking = false;

            // keep track of last node parent ID
            // if the current node:
            // a) has a parent ID matching the last node
            // b) has the same number of taxa
            // then store current node instead, last node is not MRCA

            // once MRCA is found, keep track of MRCA node A and B
            // decide which is the outgroup ??

            for (int m = 0; m < table.Rows.Count; m++)
            {
                DataRow row = table.Rows[m];

                // start counting from nodes with parent ID null - this is the root of the tree
                if (table.Rows[m][1].GetType() == typeof(DBNull))
                {
                    rootSize = (int)row[6];
                    allNodes = (int[])row[5];
                    foundMRCA = null;
                    doneWithA = false;
                    doneWithB = false;
                    doneLooking = false;
                }

                if (doneLooking)
                    continue;

                // iterate through the node list until we reach a node that doesn't contain all the relevant taxa from the root
                // this means we've a) found the MRCA and b) found one child of the MRCA
                if ((int)row[6] < rootSize && foundMRCA == null)
                {                    
                    int lastRowID = (int)lastRow[0];
                    foundMRCA = new RawPartitionData(lastRow);
                }

                // once the parent is found, start adding row to the dictionary
                if (foundMRCA != null)
                {
                    int rowID = (int)row[0];
                    int parentRowID = (int)row[1];
                    int rowTaxaCount = (int)row[6];

                    if ((int)foundMRCA.parent[0] == parentRowID)
                    {
                        foundMRCA.AddRow(row);
                    }
                    else if ((int)foundMRCA.RowA[0] == parentRowID)
                    {
                        int parentTaxaCount = (int)foundMRCA.RowA[6];
                        if (rowTaxaCount == parentTaxaCount)
                            foundMRCA.RowA = row;
                        else
                            doneWithA = true;
                    }
                    else if ((int)foundMRCA.RowB[0] == parentRowID)
                    {
                        int parentTaxaCount = (int)foundMRCA.RowB[6];
                        if (rowTaxaCount == parentTaxaCount)
                            foundMRCA.RowB = row;
                        else
                            doneWithB = true;
                    }
                }

                if (foundMRCA.IsComplete)
                {
                    int[] NodeASet = (int[])foundMRCA.RowA[4];
                    int[] NodeBSet = (int[])foundMRCA.RowA[4];

                    // proceed only if the MRCA covers A, B and C
                    if (NodeASet.Concat(NodeBSet).Distinct().Count() == 3)
                    {
                        // check how many groups each child node covers
                        // whichever one only covers one group, must be an outgroup     

                        // however, if both nodes cover multiple nodes and neither qualifies as an outgroup
                        // so we default to the node covering the fewest distinct groups as the outgroup
                        // simplest scheme to cover both scenarios is to compare counts

                        int DistinctCountA = NodeASet.Distinct().Count();
                        int DistinctCountB = NodeBSet.Distinct().Count();

                        if (DistinctCountA > DistinctCountB)
                            foundMRCA.outgroup = 1;
                        else
                            foundMRCA.outgroup = -1;
                    }
                    else
                        // the MRCA doesn't cover A, B, and C this node is irrelevant
                        doneLooking = true;
                }

                if (doneWithA && doneWithB)
                {
                    List<int> taxaA = null;
                    List<int> taxaB = null;
                    List<int> outgroup = null;

                    yield return new PartitionData((int)foundMRCA.parent[3], (int)foundMRCA.parent[0], taxaA, taxaB, outgroup, (double)foundMRCA.parent[2]);
                }

                lastRow = row;
            }

            // for later:
            // tally up all the all-encompassing partitions
            // find all the non-encompassing ones and count those votes
        }

        public IEnumerable<PartitionData> GetPartitions(IEnumerable<TopoTimeNode> nodes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM partition_list2(ARRAY[");            

            // create each group, padding with zeroes to make all the same size
            // in pgSQL, all arrays in an array must be the same size
            sb.Append(String.Join(",", nodes.Select(node => NamedChildrenText(node))) + "]);");

            string sql = sb.ToString();
            DataTable table = DBService.GetSQLResult(sql);

            bool foundMRCA = false;
            int rootSize = 0;
            int lastParent = 0;
            double? lastParentTime = 0;
            List<int> allNodes = null;

            int steps = 0;
            bool getMRCAOnly = true;

            Dictionary<int, Tuple<double, List<DataRow>, List<int>>> foundParentIDs = new Dictionary<int, Tuple<double, List<DataRow>, List<int>>>();

            for (int m = 0; m < table.Rows.Count; m++)
            {
                // start counting if node is the root of the tree
                if (table.Rows[m][1].GetType() == typeof(DBNull))
                {
                    rootSize = ((int[])table.Rows[m][4]).Length;
                    allNodes = ((int[])table.Rows[m][4]).ToList();
                    foundMRCA = false;
                    steps = 0;
                }

                if (steps != 5)
                {
                    // iterate through the node list until we reach a node that doesn't contain all the relevant taxa from the root
                    // this means we've a) found the MRCA and b) found one child of the MRCA
                    if (((int[])table.Rows[m][4]).Length < rootSize && !foundMRCA)
                    {
                        foundMRCA = true;
                    }

                    lastParent = (int)table.Rows[m][0];
                    lastParentTime = table.Rows[m][2] is DBNull ? (double?)null : (double)table.Rows[m][2];

                    if (lastParentTime != null)
                        foundParentIDs.Add(lastParent, new Tuple<double, List<DataRow>, List<int>>((double)lastParentTime, new List<DataRow>(), allNodes));

                    // once the parent is found, assign the two subsequent rows as child nodes
                    if (foundMRCA)
                    {
                        DataRow currentNode = table.Rows[m];
                        int currentNodeParent = (int)table.Rows[m][1];
                        if (foundParentIDs.ContainsKey(currentNodeParent))
                        {
                            if (getMRCAOnly)
                            {
                                if (steps == 0)
                                    steps++;
                                else if (steps == 1)
                                    steps++;
                                else if (steps == 2)
                                {
                                    foundParentIDs[currentNodeParent].Item2.Add(table.Rows[m]);
                                    steps++;
                                }
                                else if (steps == 3)
                                {
                                    foundParentIDs[currentNodeParent].Item2.Add(table.Rows[m]);
                                    steps++;
                                }
                                else if (steps == 4)
                                {
                                    foundParentIDs[currentNodeParent].Item2.Add(table.Rows[m]);
                                    steps++;
                                }
                            }
                            else
                                foundParentIDs[currentNodeParent].Item2.Add(table.Rows[m]);
                        }
                    }
                }
            }

            if (foundParentIDs != null)
            {
                foreach (KeyValuePair<int, Tuple<double, List<DataRow>, List<int>>> pair in foundParentIDs)
                {
                    if (pair.Value.Item2.Count() == 2)
                    {
                        DataRow childA = pair.Value.Item2[0];
                        DataRow childB = pair.Value.Item2[1];

                        List<int> nodesA = ((int[])pair.Value.Item2[0][4]).ToList();
                        List<int> nodesB = ((int[])pair.Value.Item2[1][4]).ToList();
                        List<int> outgroup = pair.Value.Item3.Except(nodesA.Concat(nodesB)).ToList();

                        // skip any partitions where both sides have a node in common
                        // unnecessary when wworking with raw taxon IDs
                        /*
                        if (!nodesA.Any(nodesB.Contains))
                        {
                            if (nodesA.First() < nodesB.First())
                                yield return new PartitionData(childA[3].ToString(), childA[1].ToString(), nodesA, nodesB, outgroup, pair.Value.Item1);
                            else
                                yield return new PartitionData(childA[3].ToString(), childA[1].ToString(), nodesB, nodesA, outgroup, pair.Value.Item1);

                        }
                        */
                        yield return new PartitionData((int)childA[3], (int)childA[1], nodesA, nodesB, outgroup, pair.Value.Item1);
                    }
                }
            }

            // for later:
            // tally up all the all-encompassing partitions
            // find all the non-encompassing ones and count those votes
        }
    }


}
