using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using TimeTreeShared;
using TimeTreeShared.Services;
using System.Linq;
using Npgsql;

namespace TopoTimeShared
{
    public static class GenerateTTService
    {
        public async static void GenerateFullTree(string filepath, string filename, TopoTimeTree activeTree, DatabaseService DBService, IProgress<string> progress, bool DirectUpload = false)
        {
            using (Stream file = File.Open(filepath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    DateTime startTime = DateTime.Now;

                    GenerateTree(activeTree, "species", "", filename, archive, DBService, progress, DirectUpload);
                    GenerateTree(activeTree, "genus", "genus_", filename, archive, DBService, progress, DirectUpload);
                    GenerateTree(activeTree, "family", "family_", filename, archive, DBService, progress, DirectUpload);
                    GenerateTree(activeTree, "order", "order_", filename, archive, DBService, progress, DirectUpload);
                    GenerateTree(activeTree, "class", "class_", filename, archive, DBService, progress, DirectUpload);
                    GenerateTree(activeTree, "phylum", "phylum_", filename, archive, DBService, progress, DirectUpload);

                    TimeSpan ticks = DateTime.Now - startTime;


                    progress.Report("Operation completed in " + string.Format("{0} minutes, {1} seconds", ticks.Minutes, ticks.Seconds));
                }
            }
        }

        private static void GenerateTree(TopoTimeTree baseTree, string rank, string prefix, string filename, ZipArchive archive, DatabaseService DBService, IProgress<string> progress, bool DirectUpload = false)
        {
            TopoTimeTree tree = baseTree;

            if (rank != "species")
            {
                progress.Report($"Cloning {rank} tree..." + Environment.NewLine);
                tree = baseTree.Clone();
                tree = TreeEditingService.trimToLevel(rank, tree, DBService);
                TreeEditingService.CollapseDuplicateTaxaGroups(tree, tree.root, markPolyphyleticDuplicates: true);
            }

            if (DirectUpload)
            {
                progress.Report($"Uploading SQL for {rank} tree..." + Environment.NewLine);
                UpdateDatabaseSupertree(DBService, tree.root, tree, prefix);
            }
            else
            {
                progress.Report($"Generating SQL for {rank} tree..." + Environment.NewLine);
                ZipArchiveEntry SQLTextFile = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}.sql");
                GenerateTTNodeFile(tree.root, tree, new StreamWriter(SQLTextFile.Open()), false, prefix);
            }

            progress.Report($"Generating TopoTime session for {rank} tree..." + Environment.NewLine);
            ZipArchiveEntry TreeSession = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}.tsz");
            TreeIOService.SaveCompressedTree(TreeSession.Open(), TreeSession.Name, tree.root, tree);

            progress.Report($"Generating main flat file for {rank} tree..." + Environment.NewLine);
            ZipArchiveEntry FlatFile = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}-ci.csv");
            GenerateTTFlatFile(tree.root, tree, new StreamWriter(FlatFile.Open()));

            progress.Report($"Generating map flat file for {rank} tree..." + Environment.NewLine);
            ZipArchiveEntry Map = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}-map.txt");
            GenerateTTMapFile(tree.root, new StreamWriter(Map.Open()));

            progress.Report($"Generating rank flat file for {rank} tree..." + Environment.NewLine);
            ZipArchiveEntry Ranks = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}-ranks.txt");
            GenerateTTRankFile(tree.root, new StreamWriter(Ranks.Open()), DBService);

            progress.Report($"Generating Newick files for {rank} tree..." + Environment.NewLine);
            ZipArchiveEntry Newick = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + $"-{rank}-tree.nwk");
            GenerateTTNewick(tree.root, new StreamWriter(Newick.Open()));            
        }

        private static void UpdateDatabaseSupertree(DatabaseService DBService, TopoTimeNode root, TopoTimeTree tree, string rankPrefix = "")
        {
            int UnusedID = 1;
            Stack<int> AncestralIDs = new Stack<int>();

            // drop indices first for performance reasons
            /*
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_node_data_precomputed_age_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_node_data_taxa_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_node_data_topology_node_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_study_times_i_phylogeny_node_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_study_times_topology_node_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_topology_topology_ancestor_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_topology_topology_node_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_topology_topology_ancestor_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "tree_topology_topology_node_id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "id_idx;");
            DBService.GetSingleSQL("DROP INDEX IF EXISTS public." + rankPrefix + "name_idx;");
            */

            // this may work for suspending index rebuilds until insertion is complete
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_node_data SET UNLOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_study_times SET UNLOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_topology SET UNLOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_topology_only SET UNLOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "ranks SET UNLOGGED;");
            DBService.GetSingleSQL("DELETE FROM " + rankPrefix + "tree_node_data;");
            DBService.GetSingleSQL("DELETE FROM " + rankPrefix + "tree_study_times;");
            DBService.GetSingleSQL("DELETE FROM " + rankPrefix + "tree_topology;");
            DBService.GetSingleSQL("DELETE FROM " + rankPrefix + "tree_topology_only;");
            DBService.GetSingleSQL("DELETE FROM " + rankPrefix + "ranks;");            
            
            //NpgsqlBinaryImporter TreeRanksWriter = DBService.GetDataWriter("COPY " + rankPrefix + "ranks (id, name, my_rank, child_nodes, no_rank, unknown, domain, superkingdom, kingdom, subkingdom, superphylum, phylum, subphylum, superclass, class, subclass, infraclass, superorder, \"order\", parvorder, suborder, infraorder, superfamily, family, subfamily, tribe, subtribe, genus, subgenus, species_group, species, species_subgroup, subspecies, varietas) FROM STDIN (FORMAT BINARY)");

            List<DBTreeNodeData> TreeNodeDataSet = new List<DBTreeNodeData>();
            List<DBTreeTopology> TreeTopologySet = new List<DBTreeTopology>();
            List<DBTreeTopologyOnly> TreeTopologyOnlySet = new List<DBTreeTopologyOnly>();
            List<DBTreeStudyTimes> TreeStudyTimesSet = new List<DBTreeStudyTimes>();

            UpdateDatabaseSupertreeNode(root, tree, TreeNodeDataSet, TreeTopologySet, TreeTopologyOnlySet, TreeStudyTimesSet, AncestralIDs, ref UnusedID);

            WriteTreeNodeData(DBService, TreeNodeDataSet, rankPrefix);
            WriteTreeTopology(DBService, TreeTopologySet, rankPrefix);
            WriteTreeTopologyOnly(DBService, TreeTopologyOnlySet, rankPrefix);
            WriteTreeStudyTimes(DBService, TreeStudyTimesSet, rankPrefix);

            //TreeRanksWriter.Complete();

            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_node_data SET LOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_study_times SET LOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_topology SET LOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "tree_topology_only SET LOGGED;");
            DBService.GetSingleSQL("ALTER TABLE public." + rankPrefix + "ranks SET LOGGED;");
        }

        private static void WriteTreeNodeData(DatabaseService DBService, List<DBTreeNodeData> TreeNodeDataSet, string rankPrefix)
        {
            NpgsqlBinaryImporter TreeNodeDataWriter = DBService.GetDataWriter("COPY " + rankPrefix + "tree_node_data (topology_node_id, precomputed_age, precomputed_median, precomputed_ci_low, precomputed_ci_high, taxa_id, adjusted_age) FROM STDIN (FORMAT BINARY)");
            foreach (DBTreeNodeData tree_node_data in TreeNodeDataSet)
            {
                TreeNodeDataWriter.StartRow();
                TreeNodeDataWriter.Write(tree_node_data.topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeNodeDataWriter.Write(tree_node_data.precomputed_age, NpgsqlTypes.NpgsqlDbType.Double);
                TreeNodeDataWriter.Write(tree_node_data.precomputed_median, NpgsqlTypes.NpgsqlDbType.Double);
                TreeNodeDataWriter.Write(tree_node_data.precomputed_ci_low, NpgsqlTypes.NpgsqlDbType.Double);
                TreeNodeDataWriter.Write(tree_node_data.precomputed_ci_high, NpgsqlTypes.NpgsqlDbType.Double);
                TreeNodeDataWriter.Write(tree_node_data.taxa_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeNodeDataWriter.Write(tree_node_data.adjusted_age, NpgsqlTypes.NpgsqlDbType.Double);
            }
            TreeNodeDataWriter.Complete();
            TreeNodeDataWriter.Close();
        }

        private static void WriteTreeTopology(DatabaseService DBService, List<DBTreeTopology> TreeTopologySet, string rankPrefix)
        {
            NpgsqlBinaryImporter TreeTopologyTextWriter = DBService.GetDataWriter("COPY " + rankPrefix + "tree_topology (topology_node_id, topology_ancestor_id, level) FROM STDIN (FORMAT BINARY)"); 
            
            foreach (DBTreeTopology tree_topology in TreeTopologySet)
            {
                TreeTopologyTextWriter.StartRow();

                TreeTopologyTextWriter.Write(tree_topology.topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeTopologyTextWriter.Write(tree_topology.topology_ancestor_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeTopologyTextWriter.Write(tree_topology.level, NpgsqlTypes.NpgsqlDbType.Integer);
            }
            TreeTopologyTextWriter.Complete();
            TreeTopologyTextWriter.Close();
        }

        private static void WriteTreeTopologyOnly(DatabaseService DBService, List<DBTreeTopologyOnly> TreeTopologyOnlySet, string rankPrefix)
        {
            NpgsqlBinaryImporter TreeTopologyOnlyTextWriter = DBService.GetDataWriter("COPY " + rankPrefix + "tree_topology_only (topology_ancestor_id, topology_node_id) FROM STDIN (FORMAT BINARY)");
            foreach (DBTreeTopologyOnly tree_topology_only in TreeTopologyOnlySet)
            {
                TreeTopologyOnlyTextWriter.StartRow();
                TreeTopologyOnlyTextWriter.Write(tree_topology_only.topology_ancestor_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeTopologyOnlyTextWriter.Write(tree_topology_only.topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);

            }
            TreeTopologyOnlyTextWriter.Complete();
            TreeTopologyOnlyTextWriter.Close();
        }

        private static void WriteTreeStudyTimes(DatabaseService DBService, List<DBTreeStudyTimes> TreeStudyTimesSet, string rankPrefix)
        {
            NpgsqlBinaryImporter TreeStudyTimesWriter = DBService.GetDataWriter("COPY " + rankPrefix + "tree_study_times (topology_node_id, i_phylogeny_node_id) FROM STDIN (FORMAT BINARY)");
            foreach (DBTreeStudyTimes tree_study_times in TreeStudyTimesSet)
            {
                TreeStudyTimesWriter.StartRow();
                TreeStudyTimesWriter.Write(tree_study_times.topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);
                TreeStudyTimesWriter.Write(tree_study_times.i_phylogeny_node_id, NpgsqlTypes.NpgsqlDbType.Integer);

            }
            TreeStudyTimesWriter.Complete();
            TreeStudyTimesWriter.Close();
        }

        private static void UpdateDatabaseSupertreeNode(TopoTimeNode root, TopoTimeTree tree, List<DBTreeNodeData> TreeNodeDataSet, List<DBTreeTopology> TreeTopologySet, List<DBTreeTopologyOnly> TreeTopologyOnlySet, List<DBTreeStudyTimes> TreeStudyTimesSet, Stack<int> AncestralIDs, ref int UnusedID)
        {
            if (root.storedAdjustedHeight == 0)
                root.storedAdjustedHeight = tree.GetNodeHeight(root, false);

            TopoTimeNode timeSource;
            if (root.ChildDivergences.Count == 0)
                timeSource = root.getTimeSource();
            else
                timeSource = root;

            List<double> times = timeSource.ChildDivergences.Select(x => (double)x.DivergenceTime).ToList();
            Tuple<double, double> confInterval;
            if (times.Count >= 2)
            {
                if (tree.UseMedianTimes)
                    confInterval = Functions.MedianConfidenceInterval(times);
                else
                    confInterval = Functions.TConfidenceInterval(times, 0.95);
            }
            else
                confInterval = new Tuple<double, double>(0, 0);

            bool UseMedianTimes = tree.UseMedianTimes;            

            int topology_node_id = UnusedID;
            double precomputed_age = tree.GetNodeHeight(root, false);
            double precomputed_median = root.getNodeMedian(true);
            double precomputed_ci_low = Math.Max(confInterval.Item1, 0);
            double precomputed_ci_high = Math.Min(confInterval.Item2, 4290);
            object taxa_id = null;
            object adjusted_age = null;

            if (root.TaxonID != 0)
                taxa_id = root.TaxonID;

            if (precomputed_age != root.StoredAdjustedHeight)
                adjusted_age = root.StoredAdjustedHeight;

            DBTreeNodeData tree_node_data = new DBTreeNodeData();
            tree_node_data.topology_node_id = topology_node_id;
            tree_node_data.precomputed_age = precomputed_age;
            tree_node_data.precomputed_median = precomputed_median;
            tree_node_data.precomputed_ci_low = precomputed_ci_low;
            tree_node_data.precomputed_ci_high = precomputed_ci_high;
            tree_node_data.taxa_id = taxa_id;
            tree_node_data.adjusted_age = adjusted_age;
            TreeNodeDataSet.Add(tree_node_data);

            /*
            
            */

            int level = AncestralIDs.Count;

            /*
            TreeTopologyOnlyTextWriter.StartRow();
            TreeTopologyOnlyTextWriter.Write(current_ancestor_id, NpgsqlTypes.NpgsqlDbType.Integer);
            TreeTopologyOnlyTextWriter.Write(topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);
            */

            if (level > 0)
            {
                DBTreeTopologyOnly tree_topology_only = new DBTreeTopologyOnly();

                int current_ancestor_id = AncestralIDs.Peek();
                tree_topology_only.topology_ancestor_id = current_ancestor_id;
                tree_topology_only.topology_node_id = UnusedID;
            }

            foreach (int ancestor_id in AncestralIDs)
            {
                DBTreeTopology tree_topology = new DBTreeTopology();
                tree_topology.topology_node_id = topology_node_id;
                tree_topology.topology_ancestor_id = ancestor_id;
                tree_topology.level = level;

                TreeTopologySet.Add(tree_topology);

                level--;
            }            

            foreach (ChildPairDivergence divergence in root.ChildDivergences)
            {
                foreach (int phylogenyNodeID in divergence.PhylogenyNodeIDs)
                {
                    DBTreeStudyTimes tree_study_times = new DBTreeStudyTimes();
                    tree_study_times.topology_node_id = topology_node_id;
                    tree_study_times.i_phylogeny_node_id = phylogenyNodeID;

                    /*
                    TreeStudyTimesWriter.StartRow();
                    TreeStudyTimesWriter.Write(topology_node_id, NpgsqlTypes.NpgsqlDbType.Integer);
                    TreeStudyTimesWriter.Write(phylogenyNodeID, NpgsqlTypes.NpgsqlDbType.Integer);
                    */

                    TreeStudyTimesSet.Add(tree_study_times);
                }
            }            

            AncestralIDs.Push(UnusedID);
            foreach (TopoTimeNode child in root.Nodes)
            {
                UnusedID++;
                UpdateDatabaseSupertreeNode(child, tree, TreeNodeDataSet, TreeTopologySet, TreeTopologyOnlySet, TreeStudyTimesSet, AncestralIDs, ref UnusedID);
            }
            AncestralIDs.Pop();
        }

        private static void GenerateTTNodeFile(TopoTimeNode root, TopoTimeTree tree, StreamWriter writerStream, bool updateTimeTree, string prefix = "")
        {
            int UnusedID = 1;
            int lineCountTT = 0;
            Stack<int> AncestralIDs = new Stack<int>();

            StringBuilder treeNodeDataText = new StringBuilder();
            StringBuilder treeTopologyText = new StringBuilder();
            StringBuilder treeTopologyOnlyText = new StringBuilder();
            StringBuilder treeStudyTimesText = new StringBuilder();

            treeNodeDataText.AppendLine($"DELETE FROM {prefix}tree_node_data;");
            treeTopologyText.AppendLine($"DELETE FROM {prefix}tree_topology;");
            treeTopologyOnlyText.AppendLine($"DELETE FROM {prefix}tree_topology_only;");
            treeStudyTimesText.AppendLine($"DELETE FROM {prefix}tree_study_times;");

            treeNodeDataText.AppendLine($"INSERT INTO {prefix}tree_node_data (topology_node_id, precomputed_age, precomputed_median, precomputed_ci_low, precomputed_ci_high, taxa_id, adjusted_age) VALUES ");
            treeTopologyText.AppendLine($"INSERT INTO {prefix}tree_topology (topology_node_id, topology_ancestor_id, level) VALUES ");
            treeTopologyOnlyText.AppendLine($"INSERT INTO {prefix}tree_topology_only (topology_node_id, topology_ancestor_id) VALUES ");
            treeStudyTimesText.AppendLine($"INSERT INTO {prefix}tree_study_times (topology_node_id, i_phylogeny_node_id) VALUES ");


            GenerateTTNodes(root, tree, treeNodeDataText, treeTopologyText, treeTopologyOnlyText, treeStudyTimesText, AncestralIDs, ref UnusedID, ref lineCountTT, updateTimeTree, prefix);


            writerStream.WriteLine(treeNodeDataText.ToString().TrimEnd(',') + ";");
            writerStream.WriteLine(treeTopologyText.ToString().TrimEnd(',') + ";");
            writerStream.WriteLine(treeTopologyOnlyText.ToString().TrimEnd(',') + ";");
            writerStream.WriteLine(treeStudyTimesText.ToString().TrimEnd(',') + ";");

            //writerStream.WriteLine($"SELECT * FROM update_{prefix}_tree();");

            writerStream.Flush();
            writerStream.Close();
        }

        private static void GenerateTTNodes(TopoTimeNode root, TopoTimeTree tree, StringBuilder treeNodeDataText, StringBuilder treeTopologyText, StringBuilder treeTopologyOnlyText, StringBuilder treeStudyTimesText, Stack<int> AncestralIDs, ref int UnusedID, ref int lineCountTT, bool updateTimeTree = false, string prefix = "", bool directLoad = false)
        {
            if (root.storedAdjustedHeight == 0)
                root.storedAdjustedHeight = tree.GetNodeHeight(root, true);

            TopoTimeNode timeSource;
            if (root.ChildDivergences.Count == 0)
                timeSource = root.getTimeSource();
            else
                timeSource = root;

            List<double> times = timeSource.ChildDivergences.Select(x => (double)x.DivergenceTime).ToList();
            Tuple<double, double> confInterval;
            if (times.Count >= 2)
            {
                if (tree.UseMedianTimes)
                    confInterval = Functions.MedianConfidenceInterval(times);
                else
                    confInterval = Functions.TConfidenceInterval(times, 0.95);
            }
            else
                confInterval = new Tuple<double, double>(0, 0);

            double updated_age = root.StoredAdjustedHeight;
            double updated_median = root.getNodeMedian(true);
            double conf_interval_low = Math.Max(confInterval.Item1, 0);
            double conf_interval_high = Math.Min(confInterval.Item2, 4290);
            string adjusted_age = "NULL";
            string taxaID = "NULL";

            if (updateTimeTree)
            {
                treeNodeDataText.AppendLine($"UPDATE {prefix}tree_node_data SET (updated_age, updated_median, updated_ci_low, updated_ci_high) VALUES ({root.StoredAdjustedHeight}, {root.getNodeMedian(true)}, {conf_interval_low}, {conf_interval_high}) WHERE topology_node_id = {UnusedID};");
            }
            else
            {
                double originalHeight = tree.GetNodeHeight(root, true);
                if (originalHeight != root.StoredAdjustedHeight)
                {
                    adjusted_age = root.StoredAdjustedHeight.ToString();
                }

                if (root.TaxonID != 0)
                    taxaID = root.TaxonID.ToString();

                treeNodeDataText.Append(Environment.NewLine + "(" + UnusedID + ", " + root.getNodeMedian(true) + ", " + root.getNodeMedian(true) + ", " + conf_interval_low + ", " + conf_interval_high + ", " + root.TaxonID + ", " + adjusted_age + "),");

                int level = AncestralIDs.Count;

                if (level > 0)
                {
                    treeTopologyOnlyText.Append(Environment.NewLine + "(" + UnusedID + ", " + AncestralIDs.Peek() + "),");
                }

                foreach (int ancestralID in AncestralIDs)
                {
                    treeTopologyText.Append(Environment.NewLine + "(" + UnusedID + ", " + ancestralID + ", " + level + ")");

                    level--;

                    if ((lineCountTT + 1) % 1000000 == 0)
                    {
                        treeTopologyText.AppendLine(";");
                        treeTopologyText.AppendLine($"INSERT INTO {prefix}tree_topology (topology_node_id, topology_ancestor_id, level) VALUES ");
                    }
                    else
                    {
                        treeTopologyText.Append(",");
                    }

                    lineCountTT++;
                }
            }

            foreach (ChildPairDivergence divergence in root.ChildDivergences)
            {
                foreach (int phylogenyNodeID in divergence.PhylogenyNodeIDs)
                {
                    treeStudyTimesText.Append(Environment.NewLine + "(" + UnusedID + ", " + phylogenyNodeID + "),");
                }
            }

            AncestralIDs.Push(UnusedID);
            foreach (TopoTimeNode child in root.Nodes)
            {
                UnusedID++;
                GenerateTTNodes(child, tree, treeNodeDataText, treeTopologyText, treeTopologyOnlyText, treeStudyTimesText, AncestralIDs, ref UnusedID, ref lineCountTT, updateTimeTree, prefix: prefix);
            }
            AncestralIDs.Pop();
        }

        private static void GenerateTTRankFile(TopoTimeNode root, StreamWriter writerStream, DatabaseService DBService)
        {
            int UnusedID = 1;
            Stack<int> AncestralIDs = new Stack<int>();

            StringBuilder fileText = new StringBuilder();
            GenerateTTRankEntry(root, fileText, AncestralIDs, ref UnusedID, DBService);

            writerStream.Write(fileText.ToString());
            writerStream.Flush();
            writerStream.Close();
        }

        private static void GenerateTTRankEntry(TopoTimeNode root, StringBuilder fileText, Stack<int> AncestralIDs, ref int UnusedID, DatabaseService DBService)
        {
            if (root.HasValidTaxon)
            {
                string sql = $"SELECT CASE WHEN rank='cohort' THEN 'no rank' WHEN rank='clade' THEN 'no rank' WHEN rank='subcohort' THEN 'no rank' WHEN rank='section' THEN 'no rank' WHEN rank='subsection' THEN 'no rank' WHEN rank='series' THEN 'no rank' ELSE rank END FROM ncbi_taxonomy WHERE taxon_id={root.TaxonID};";
                object rank = DBService.GetSingleSQL(sql);

                fileText.AppendLine(UnusedID + "=" + rank);
            }
            else
                fileText.AppendLine(UnusedID + "=");

            root.UniqueID = UnusedID;

            AncestralIDs.Push(UnusedID);
            foreach (TopoTimeNode child in root.Nodes)
            {
                UnusedID++;
                GenerateTTRankEntry(child, fileText, AncestralIDs, ref UnusedID, DBService);
            }
            AncestralIDs.Pop();
        }

        private static void GenerateTTFlatFile(TopoTimeNode root, TopoTimeTree tree, StreamWriter writerStream)
        {
            int UnusedID = 1;
            Stack<int> AncestralIDs = new Stack<int>();

            StringBuilder fileText = new StringBuilder();
            fileText.AppendLine("\"time_estimates\", \"topology_node_id\", \"precomputed_age\", \"precomputed_ci_low\", \"precomputed_ci_high\", \"adjusted_age\", \"ci_string\"");
            GenerateTTFlatFileEntry(root, tree, fileText, AncestralIDs, ref UnusedID);

            writerStream.Write(fileText.ToString());
            writerStream.Flush();
            writerStream.Close();
        }

        private static void GenerateTTFlatFileEntry(TopoTimeNode root, TopoTimeTree tree, StringBuilder flatFileText, Stack<int> AncestralIDs, ref int UnusedID)
        {
            if (root.storedAdjustedHeight == 0)
                root.storedAdjustedHeight = tree.GetNodeHeight(root, true);

            StringBuilder flatFileTimeList = new StringBuilder();
            StringBuilder flatFileStatsList = new StringBuilder();

            TopoTimeNode timeSource;
            if (root.ChildDivergences.Count == 0)
                timeSource = root.getTimeSource();
            else
                timeSource = root;

            //(CASE WHEN count(pn.f_time_estimate) > 1 THEN
            //(CASE WHEN tnd.precomputed_ci_high > 0 AND(tnd.adjusted_age IS NULL OR tnd.adjusted_age = 0 OR abs((tnd.adjusted_age - tnd.precomputed_age) / tnd.adjusted_age) < 0.05)

            List<double> times = timeSource.ChildDivergences.Select(x => (double)x.DivergenceTime).ToList();
            Tuple<double, double> confInterval;
            if (times.Count >= 2)
            {
                if (tree.UseMedianTimes)
                    confInterval = Functions.MedianConfidenceInterval(times);
                else
                    confInterval = Functions.TConfidenceInterval(times, 0.95);
            }
            else
                confInterval = new Tuple<double, double>(0, 0);

            double conf_interval_low = Math.Max(confInterval.Item1, 0);
            double conf_interval_high = Math.Min(confInterval.Item2, 4290);



            double originalHeight = tree.GetNodeHeight(root, true);
            flatFileStatsList.Append(UnusedID + "," + originalHeight + "," + conf_interval_low + "," + conf_interval_high + ",");

            bool displayRange = times.Count > 1;
            bool displayCIs = displayRange && times.Count > 2 && conf_interval_high > 0;

            if (originalHeight != root.StoredAdjustedHeight)
            {
                flatFileStatsList.Append(root.StoredAdjustedHeight);
                /*
                double difference = Math.Abs((root.StoredAdjustedHeight - originalHeight) / root.StoredAdjustedHeight);

                if (difference > 0.05)
                    displayCIs = false;
                */
            }

            int level = AncestralIDs.Count;

            string ci_string = "";

            if (displayCIs)
                ci_string = "CI: (" + Math.Round(conf_interval_low, 1) + " - " + Math.Round(conf_interval_high, 1) + ")";
            else if (displayRange)
                ci_string = "Range: (" + Math.Round(times.Min(), 1) + " - " + Math.Round(times.Max(), 1) + ")";

            flatFileStatsList.Append("," + ci_string);

            foreach (ChildPairDivergence divergence in root.ChildDivergences)
            {
                flatFileTimeList.Append(divergence.DivergenceTime + ",");
            }

            if (root.ChildDivergences.Count > 0)
                flatFileText.AppendLine("\"{" + flatFileTimeList.ToString().TrimEnd(',') + "}\"," + flatFileStatsList.ToString());

            AncestralIDs.Push(UnusedID);
            foreach (TopoTimeNode child in root.Nodes)
            {
                UnusedID++;
                GenerateTTFlatFileEntry(child, tree, flatFileText, AncestralIDs, ref UnusedID);
            }
            AncestralIDs.Pop();
        }

        private static void GenerateTTNewick(TopoTimeNode root, StreamWriter writerStream)
        {
            writerStream.Write(root.writeNode(TopoTimeNode.TreeWritingMode.UniqueIDs) + ";");
            writerStream.Flush();
            writerStream.Close();
        }

        private static void GenerateTTMapFile(TopoTimeNode root, StreamWriter writerStream)
        {
            int UnusedID = 1;
            Stack<int> AncestralIDs = new Stack<int>();

            StringBuilder fileText = new StringBuilder();
            GenerateTTMapEntry(root, fileText, AncestralIDs, ref UnusedID);

            writerStream.Write(fileText.ToString());
            writerStream.Flush();
            writerStream.Close();
        }

        private static void GenerateTTMapEntry(TopoTimeNode root, StringBuilder fileText, Stack<int> AncestralIDs, ref int UnusedID)
        {
            if (root.HasValidTaxon)
                fileText.AppendLine(UnusedID + "=" + root.TaxonName);

            AncestralIDs.Push(UnusedID);
            foreach (TopoTimeNode child in root.Nodes)
            {
                UnusedID++;
                GenerateTTMapEntry(child, fileText, AncestralIDs, ref UnusedID);
            }
            AncestralIDs.Pop();
        }
    }

    public class DBTreeNodeData
    {
        public int topology_node_id;
        public double precomputed_age;
        public double precomputed_median;
        public double precomputed_ci_low;
        public double precomputed_ci_high;
        public object taxa_id;
        public object adjusted_age;
    }

    public class DBTreeStudyTimes
    {
        public int topology_node_id;
        public int i_phylogeny_node_id;
    }

    public class DBTreeTopology
    {
        public int topology_node_id;
        public int topology_ancestor_id;
        public int level;
    }

    public class DBTreeTopologyOnly
    {
        public int topology_ancestor_id;
        public int topology_node_id;
    }
}
