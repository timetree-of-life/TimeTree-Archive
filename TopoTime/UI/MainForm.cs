using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;
using Npgsql;
using NpgsqlTypes;
using CredentialManagement;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Reflection.Metadata;
using System.Reflection;
using TimeTreeShared;
using TimeTreeShared.Services;
using TopoTimeShared;
using System.Xml.Linq;
using MathNet.Numerics.Distributions;

namespace TopoTime
{
    public partial class MainForm : Form
    {
        private SubtaxaForm subTaxaWindow;
        private SuspectForm suspectWindow;
        private int lastIndexSearched;
        private string lastQuerySearched = "";
        private IEnumerator<ExtendedNode> searchEnumerator;

        private HALFunctions HALService;

        private TopoTimeTree activeTree;
        private bool recalculateTimesOnDelete = false;

        // FOR PREPARED QUERIES
        NpgsqlCommand divergenceQuery;
        NpgsqlCommand divergenceQueryB;

        NpgsqlParameter divergenceParameterA;
        NpgsqlParameter divergenceParameterB;
        NpgsqlParameter divergenceParameterC;
        NpgsqlParameter divergenceParameterD;
        NpgsqlParameter divergenceParameterE;

        NpgsqlConnection openConnection;


        public MainForm()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();

            comboBoxBackboneSelector.SelectedIndex = 0;
            comboBoxTipRank.SelectedIndex = 0;

            subTaxaWindow = new SubtaxaForm();
        }



        #region supertree build functions
        private void btnConnect_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                IntroSupertree(useMedianTimes: false);
            }); ;
        }

        private void btnConnectMedian_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                IntroSupertree(useMedianTimes: true);
            });
        }

        private void IntroSupertree(bool useMedianTimes = false)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            activeTree = new TopoTimeTree(useMedianTimes);

            string rank = "species";
            string startingTaxon = txtBoxStartNode.Text;
            bool collapseSubspeciesGroups = false;
            bool storeSubspecies = true;

            if (comboBoxTipRank.Text == "no rank restrictions")
            {
                collapseSubspeciesGroups = true;
                storeSubspecies = false;
            }
            else if (comboBoxTipRank.Text == "subspecies grouping")
            {
                collapseSubspeciesGroups = false;
                storeSubspecies = false;
            }

            try
            {                
                string sql = $"SELECT taxon_id, rank FROM ncbi_taxonomy WHERE scientific_name='{startingTaxon}';";
                DBService.GetSingleSQL("SET enable_nestloop=ON;");

                if (comboBoxBackboneSelector.Text == "NCBI Backbone")
                {
                    NpgsqlDataAdapter da = new NpgsqlDataAdapter(sql, DBService.DBConnection);

                    ds.Reset();
                    da.Fill(ds);
                    dt = ds.Tables[0];

                    if (dt.Rows.Count == 1)
                    {
                        int rootNodeNum = (int)(dt.Rows[0][0]);

                        if (treeViewer.Nodes.Count == 1)
                            treeViewer.Nodes[0].Remove();

                        bool isSpecies = ((string)dt.Rows[0][1] == "species");
                        activeTree.root = new TopoTimeNode(txtBoxStartNode.Text, rootNodeNum);
                        activeTree.root.isSpecies = isSpecies;

                        activeTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: rank, isMedian: useMedianTimes, storeSubspecies: storeSubspecies, collapseSubspeciesGroups: collapseSubspeciesGroups);

                        activeTree.AddNewNodeToTree(activeTree.root);

                        activeTree.OperationHistory.Add("Tree built: " + startingTaxon + " at " + rank + " rank using " + comboBoxBackboneSelector.Text + " method");
                    }
                }
                else if (comboBoxBackboneSelector.Text == "NCBI ID Custom List")
                {
                    DebugTextForm dtf = new DebugTextForm("Enter a list of NCBI IDs, one per line", "Enter NCBI IDs", textIsReadOnly: false);
                    if (dtf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string[] lines = dtf.userText.Split('\n');
                        int number = 0;
                        List<int> ValidIDs = lines.Where(str => int.TryParse(str, out number)).Select(_ => number).ToList();

                        rank = "unspecified";
                        startingTaxon = "";

                        activeTree = TreeBuilderService.GenerateBackbone(ValidIDs, DBService, baseRank: rank, isMedian: useMedianTimes);
                        activeTree.AddNewNodeToTree(activeTree.root);

                        activeTree.OperationHistory.Add("Tree built: " + comboBoxBackboneSelector.Text + " method");
                    }
                }

                OnTreeLoad();
                CleanTree();
                
            }
            finally
            {

            }
        }
        #endregion

        #region database functions

        public NpgsqlConnection GetDatabaseConnection()
        {
            TimeTreeConnection connDialog = new TimeTreeConnection
            {
                TopMost = true,
                ShowInTaskbar = true
            };
            connDialog.SetDesktopLocation(this.Location.X, this.Location.Y);
            if (connDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    openConnection = connDialog.newConnection;
                    return openConnection;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return null;
                }
            }
            else
                return null;
        }

        internal NpgsqlConnection DBConnection
        {
            get
            {
                if (openConnection == null || openConnection.State == ConnectionState.Closed || openConnection.State == ConnectionState.Broken)
                {
                    openConnection = GetDatabaseConnection();
                }

                return openConnection;
            }
            set
            {
                openConnection = value;
            }
        }

        private DatabaseService dbService;
        internal DatabaseService DBService
        {
            get
            {
                if (dbService == null)
                {
                    dbService = new DatabaseService(DBConnection);
                }

                return dbService;
            }
            set
            {
                dbService = value;
            }
        }

        private NpgsqlConnection getConnection()
        {
            return DBConnection;


            if (openConnection == null || openConnection.State == ConnectionState.Closed || openConnection.State == ConnectionState.Broken)
            {
                TimeTreeConnection connDialog = new TimeTreeConnection
                {
                    TopMost = true,
                    ShowInTaskbar = true
                };
                connDialog.SetDesktopLocation(this.Location.X, this.Location.Y);
                if (connDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        openConnection = connDialog.newConnection;
                        //
                        return openConnection;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return null;
                    }
                }
                else
                    return null;
            }
            else
                return openConnection;

        }
        public static NpgsqlDataReader getSQLResultSet(string sqlQuery, NpgsqlConnection conn)
        {
            NpgsqlCommand command = new NpgsqlCommand(sqlQuery, conn);
            NpgsqlDataReader dr = command.ExecuteReader();
            return dr;
        }

        public static DataTable GetSQLResult(string sqlQuery, NpgsqlConnection conn)
        {
            NpgsqlDataAdapter da;
            DataSet set;
            DataTable table;

            da = new NpgsqlDataAdapter(sqlQuery, conn);
            set = new DataSet();
            da.Fill(set);
            table = set.Tables[0];

            return table;
        }

        public static DataTable getPreparedSQLResult(NpgsqlCommand cmd)
        {
            NpgsqlDataAdapter da;
            DataSet set;
            DataTable table;



            da = new NpgsqlDataAdapter(cmd);
            set = new DataSet();
            da.Fill(set);
            table = set.Tables[0];

            return table;
        }

        public static object getSingleSQL(string sqlQuery, NpgsqlConnection conn)
        {
            NpgsqlCommand command = new NpgsqlCommand(sqlQuery, conn);
            return command.ExecuteScalar();
        }
        #endregion

        #region GUI functions
        // TO-DO: Refactor this so that tree manu nodes are generated by a function within the PartitionData or ExtendedNode object, not here
        private void treeViewer_AfterSelect(object sender, TreeViewEventArgs e)
        {
            treeViewDivTimes.Nodes.Clear();
            TopoTimeNode selectNode = (TopoTimeNode)treeViewer.SelectedNode;

            toolStripStatusLabel1.Text = selectNode.Path;

            if (selectNode.ChildDivergences != null)
            {
                TreeNode stats = new TreeNode("Stats");
                treeViewDivTimes.Nodes.Add(stats);
                if (selectNode.ChildDivergences.Count > 1)
                {
                    stats.Nodes.Add(new TreeNode("Ratio of largest node to smallest - " + selectNode.DivergenceRatio));
                }



                stats.Expand();

                TreeNode divergences = new TreeNode("Divergences");
                foreach (ChildPairDivergence time in selectNode.ChildDivergences.OrderBy(x => x.DivergenceTime))
                {
                    TreeNode metadata = VisibleMetadata(activeTree, time);
                    metadata.Tag = time;

                    if (time.IsConflict)
                        metadata.ForeColor = Color.Red;

                    foreach (TreeNode node in metadata.Nodes)
                        node.ContextMenuStrip = contextMenuMetadataView;

                    divergences.Nodes.Add(metadata);

                }
                treeViewDivTimes.Nodes.Add(divergences);

                divergences.Expand();

                if (selectNode.PartitionData != null)
                {
                    TreeNode partitions = selectNode.PartitionData.PartitionDisplay(activeTree);
                    treeViewDivTimes.Nodes.Add(partitions);
                    partitions.Expand();
                }

            }

            subTaxaWindow.setList(selectNode.getLeaves(false), selectNode.storedNamedNodes, selectNode.storedFloatingNodes);
        }
        private void treeViewer_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeViewer.SelectedNode = treeViewer.GetNodeAt(e.X, e.Y);
            }
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            // TO-DO: change to match TreeValidate's behavior
            string searchString = textBoxSearchQuery.Text;
            int nodeHashCode = 0;

            if (searchString == "")
                return;
            try
            {
                if (searchString != lastQuerySearched)
                {
                    lastIndexSearched = 0;
                    lastQuerySearched = searchString;
                    searchEnumerator = activeTree.nodeList.GetEnumerator();

                    int.TryParse(searchString, out nodeHashCode);
                }

                searchEnumerator.MoveNext();

                for (int i = lastIndexSearched; i < activeTree.nodeList.Count; i++)
                {
                    ExtendedNode node = searchEnumerator.Current;

                    if (nodeHashCode > 0 && node.GetHashCode() == nodeHashCode)
                    {
                        treeViewer.SelectedNode = node;
                        node.EnsureVisible();
                        treeViewer.Focus();

                        lastIndexSearched = i;
                        break;
                    }
                    else if (node.Text.Contains(searchString))
                    {
                        treeViewer.SelectedNode = node;
                        node.EnsureVisible();
                        treeViewer.Focus();

                        lastIndexSearched = i;
                        break;
                    }
                    searchEnumerator.MoveNext();

                    if (i == activeTree.nodeList.Count - 1)
                    {
                        lastIndexSearched = 0;
                        lastQuerySearched = "";
                    }
                }
            }
            catch
            {
                lastQuerySearched = "";
            }
        }

        private void textBoxSearchQuery_TextChanged(object sender, EventArgs e)
        {
            lastIndexSearched = 0;
        }

        private void textBoxSearchQuery_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
                buttonSearch_Click(sender, (EventArgs)e);
        }

        private void treeViewer_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Move the dragged node when the left mouse button is used. 
            if (e.Button == MouseButtons.Left)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void treeViewer_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void treeViewer_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = treeViewer.PointToClient(new Point(e.X, e.Y));

            // Select the node at the mouse position.
            treeViewer.SelectedNode = treeViewer.GetNodeAt(targetPoint);
        }

        private void treeViewer_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = treeViewer.PointToClient(new Point(e.X, e.Y));

            // Retrieve the node at the drop location.
            TopoTimeNode targetNode = (TopoTimeNode)treeViewer.GetNodeAt(targetPoint);

            // Retrieve the node that was dragged.
            TopoTimeNode draggedNode = (TopoTimeNode)e.Data.GetData(typeof(TopoTimeNode));

            // Confirm that the node at the drop location is not  
            // the dragged node or a descendant of the dragged node. 
            if (!draggedNode.Equals(targetNode) && !ContainsNode(draggedNode, targetNode))
            {
                // If it is a move operation, remove the node from its current  
                // location and add it to the node at the drop location. 
                if (e.Effect == DragDropEffects.Move)
                {
                    activeTree.MoveNode(draggedNode, targetNode);
                }

                targetNode.Expand();
            }

            if (treeViewer.SelectedNode != null)
                treeViewer.SelectedNode.EnsureVisible();
        }

        // Determine whether one node is a parent  
        // or ancestor of a second node. 
        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {
            // Check the parent node of the second node. 
            if (node2.Parent == null) return false;
            if (node2.Parent.Equals(node1)) return true;

            // If the parent node is not null or equal to the first node,  
            // call the ContainsNode method recursively using the parent of  
            // the second node. 
            return ContainsNode(node1, node2.Parent);
        }
        #endregion

        #region partition debug display functions (rough)
        public static void DisplayPartitions(IEnumerable<PartitionData> partitions, List<TopoTimeNode> target)
        {
            var comboGroupsAll = partitions.Select(x => new {
                Citation = x.citation.ToString(),
                Split = String.Join(",", x.nodesA) + " | " + String.Join(",", x.nodesB) + " == " + String.Join(",", x.outgroup),
                Taxa = String.Join(",", target.Where(y => x.nodesA.Any(z => z == y.TaxonID)).Select(w => w.NamedNodeList)) + " vs. " + String.Join(",", target.Where(y => x.nodesB.Any(z => z == y.TaxonID)).Select(w => w.NamedNodeList)),
                Time = x.DivergenceTime,
                Size = x.nodesA.Count() + x.nodesB.Count()
            }).ToList();

            DebugForm originalPartitions = new DebugForm(comboGroupsAll, 0, true);
            originalPartitions.Show();
        }

        public static void DisplayPartitions(IEnumerable<PartitionData> partitions, TopoTimeNode target)
        {
            var comboGroupsAll = partitions.Select(x => new {
                Citation = x.citation.ToString(),
                Split = String.Join(",", x.nodesA) + " | " + String.Join(",", x.nodesB) + " == " + String.Join(",", x.outgroup),
                Taxa = String.Join(",", x.nodesA.Select(y => ((TopoTimeNode)target.Nodes[y]).NamedNodeList)) + " vs. " + String.Join(",", x.nodesB.Select(y => ((TopoTimeNode)target.Nodes[y]).NamedNodeList)),
                Time = x.DivergenceTime,
                Size = x.nodesA.Count() + x.nodesB.Count()
            }).ToList();

            DebugForm originalPartitions = new DebugForm(comboGroupsAll, 0, true);
            originalPartitions.Show();
        }

        public static PartitionData SelectPartition(List<PartitionData> partitions, TopoTimeNode target)
        {
            var comboGroupsSelect = partitions.Select(x => new {
                Citation = x.citation.ToString(),
                Split = String.Join(",", x.nodesA) + " | " + String.Join(",", x.nodesB) + " == " + String.Join(",", x.outgroup),
                Taxa = String.Join(",", x.nodesA.Select(y => ((TopoTimeNode)target.Nodes[y]).NamedNodeList)) + " vs. " + String.Join(",", x.nodesB.Select(y => ((TopoTimeNode)target.Nodes[y]).NamedNodeList)),
                Time = x.DivergenceTime,
                Size = x.nodesA.Count() + x.nodesB.Count()
            }).ToList();

            DebugForm userPartition = new DebugForm(comboGroupsSelect, 0);
            if (userPartition.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return partitions[(int)userPartition.returnValue];
            }
            else
                return null;
        }
        #endregion

        

        #region file loading functions
        private void LoadTree(Stream file)
        {
            try
            {
                XmlAttributeOverrides overrideList = new XmlAttributeOverrides();
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlIgnore = true;
                overrideList.Add(typeof(ChildPairDivergence), "metadata", attrs);

                System.Xml.Serialization.XmlSerializer x = new XmlSerializer(typeof(SerializableNode), overrideList);
                SerializableNode rootData = (SerializableNode)x.Deserialize(file);
                activeTree = rootData.DeserializedTree();

                OnTreeLoad();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Nodes.Clear() seems to be unstable and crashes for large trees

        }

        private void LoadCompressedTree(Stream stream)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry archivedFile = archive.Entries.First(x => x.FullName.EndsWith(".tss"));
                LoadTree(archivedFile.Open());
            }
        }

        /// <summary>
        /// File > Load Tree
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Tree sessions (*.tss, *.tsz)|*.tss;*.tsz|Zipped tree sessions (*.zip)|*.zip";
            string filename;

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = openDialog.FileName;

                activeTree = TreeIOService.LoadTreeFile(filename);
                OnTreeLoad();

                this.Text = "TopoTime - " + filename.Split('\\').Last();
            }
        }
        #endregion

        #region file saving functions

        private void saveTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Compressed tree sessions (*.tsz)|*.tsz|Tree sessions (*.tss)|*.tss";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;
                TreeIOService.SaveTreeFile(filename, activeTree);
                /*
                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    if (filename.EndsWith(".tss"))
                        SaveTree(file, activeTree.root, activeTree);
                    else if (filename.EndsWith(".tsz"))
                        SaveCompressedTree(file, filename.Split('\\').Last(), activeTree.root, activeTree);

                }
                */

                this.Text = "TopoTime - " + filename.Split('\\').Last();
            }
        }

        private void exportAsNewickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                //activeTree.SetNormalNodeHeights();

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(0);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }
        #endregion

        #region menuitems        


        

        private void HardReloadActiveTree(TopoTimeNode root)
        {
            TreeEditingService.HardReloadTree(root, ref activeTree);
            OnTreeLoad();
        }

        private void prepareDivergenceQueries(NpgsqlConnection conn)
        {
            string sql = "";
            sql = "SELECT pn.f_time_estimate, pn.i_phylogeny_node_id, pn.i_parent_phylogeny_node_id, pn.i_citation_num, c.i_citation_num, c.i_citation_id, c.ref_id, c.c_first_author_lname, c.c_title, c.i_year FROM phylogeny_node pn, phylogeny_topology pt, citations c WHERE  pn.i_citation_num = c.i_citation_num AND c.qa_complete=TRUE AND pn.i_phylogeny_node_id = pt.i_phylogeny_node_id AND pt.taxon_id = ANY(:list_a)";
            sql += "INTERSECT ";
            sql += "SELECT pn.f_time_estimate, pn.i_phylogeny_node_id, pn.i_parent_phylogeny_node_id, pn.i_citation_num, c.i_citation_num, c.i_citation_id, c.ref_id, c.c_first_author_lname, c.c_title, c.i_year FROM phylogeny_node pn, phylogeny_topology pt, citations c WHERE pn.i_citation_num = c.i_citation_num AND c.qa_complete=TRUE AND pn.i_phylogeny_node_id = pt.i_phylogeny_node_id AND pt.taxon_id = ANY(:list_b) ORDER BY 2;";

            divergenceQuery = new NpgsqlCommand(sql, conn);
            divergenceParameterA = divergenceQuery.Parameters.Add("list_a", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            divergenceParameterB = divergenceQuery.Parameters.Add("list_b", NpgsqlDbType.Array | NpgsqlDbType.Integer);

            divergenceQuery.Prepare();

            sql = "SELECT COUNT (i_phylogeny_node_id) FROM phylogeny_topology WHERE i_phylogeny_node_id=@phylogeny_node AND taxon_id IN = ANY(@list_a || @list_b);";

            divergenceQueryB = new NpgsqlCommand(sql, conn);
            divergenceParameterC = divergenceQueryB.Parameters.Add("phylogeny_node", NpgsqlDbType.Integer);
            divergenceParameterD = divergenceQueryB.Parameters.Add("list_a", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            divergenceParameterE = divergenceQueryB.Parameters.Add("list_b", NpgsqlDbType.Array | NpgsqlDbType.Integer);
        }


        private void contextMenuTreeView_Opening(object sender, CancelEventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                taxaIDToolStripMenuItem.Text = "Taxa ID " + ((TopoTimeNode)treeViewer.SelectedNode).TaxonID;
                deleteNodeToolStripMenuItem.Text = "Delete Node " + ((TopoTimeNode)treeViewer.SelectedNode).TaxonName;
                nodesToolStripMenuItem.Text = ((TopoTimeNode)treeViewer.SelectedNode).ChildDivergences.Count + " Divergences";

                taxaIDToolStripMenuItem.Enabled = true;
                deleteNodeToolStripMenuItem.Enabled = true;
            }
            else
            {
                taxaIDToolStripMenuItem.Text = "Taxa ID";
                deleteNodeToolStripMenuItem.Text = "Delete Node";
                nodesToolStripMenuItem.Text = "No Divergences";

                taxaIDToolStripMenuItem.Enabled = false;
                deleteNodeToolStripMenuItem.Enabled = false;
            }
        }

        private void deleteNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                TopoTimeNode parent = (TopoTimeNode)selectedNode.Parent;
                activeTree.DeleteNode(selectedNode);

                if (recalculateTimesOnDelete)
                {
                    try
                    {
                        if (parent != null && parent.Nodes.Count == 2)
                        {
                            NpgsqlConnection conn = getConnection();
                            HALService.setTimesHAL(parent);
                        }
                    }
                    catch
                    {
                        parent.ForeColor = Color.IndianRed;
                        activeTree.refreshList.Add(parent);
                    }
                }
            }

            if (treeViewer.SelectedNode != null)
                treeViewer.SelectedNode.EnsureVisible();
        }

        #endregion               


        private void showSubtaxaWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (subTaxaWindow == null || subTaxaWindow.IsDisposed)
                subTaxaWindow = new SubtaxaForm();

            subTaxaWindow.Show();
        }
        

        /// <summary>
        /// File > Export Node Heights
        /// </summary>
        private void exportNodeHeightsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV file (*.csv)|*.csv";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                NpgsqlConnection conn = getConnection();

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    StreamWriter writerStream = new StreamWriter(file);
                    writerStream.WriteLine("ID,Name,Study Time,Adjusted Time,Study Count,Min Time,Max Time,95% CI Lower,95% CI Upper");

                    Dictionary<TopoTimeNode, int> nodeIDs = new Dictionary<TopoTimeNode, int>();
                    int currentIDIndex = 0;

                    foreach (TopoTimeNode node in activeTree.nodeList.ToList().FindAll(item => !(item.TaxonName == null || item.TaxonName == "") && item.Nodes.Count > 0))
                    {
                        int childIndex = 0;

                        string pubmedList = "";
                        foreach (ChildPairDivergence div in node.ChildDivergences)
                        {
                            pubmedList = pubmedList + div.PublicationID + ",";
                        }

                        double mean = node.getNodeHeight(false);
                        double stderror = node.getNodeStandardError(false);
                        setConfidenceInterval(node, conn);

                        writerStream.WriteLine(currentIDIndex + "," + node.TaxonName + "," + Math.Round(node.getNodeHeight(false), 3) + "," + Math.Round(node.StoredAdjustedHeight, 3) + "," + node.ChildDivergences.Count + "," + node.DivergenceSmallest + "," + node.DivergenceLargest + "," + node.MinConfidenceInterval + "," + node.MaxConfidenceInterval + "," + pubmedList);
                        node.UniqueID = currentIDIndex;
                        currentIDIndex++;

                        foreach (TopoTimeNode child in node.Nodes)
                        {
                            exportUnnamedChildNodes(writerStream, node, child, ref childIndex, ref currentIDIndex, 0, conn);
                        }
                    }
                    /*
                    foreach (ExtendedNode node in activeTree.nodeList)
                    {
                        if (node.Nodes.Count > 0)
                        {
                            if (!nodeIDs.ContainsKey(node))
                            {
                                nodeIDs.Add(node, currentIDIndex);
                                currentIDIndex++;
                            }

                            if (node.Parent != null && !nodeIDs.ContainsKey((ExtendedNode)node.Parent))
                            {
                                nodeIDs.Add((ExtendedNode)node.Parent, currentIDIndex);
                                currentIDIndex++;
                            }

                            ExtendedNode childA = (ExtendedNode)node.Nodes[0];
                            ExtendedNode childB = (ExtendedNode)node.Nodes[1];
                            
                            if (childA.Nodes.Count > 0 && !nodeIDs.ContainsKey(childA))
                            {
                                nodeIDs.Add(childA, currentIDIndex);
                                currentIDIndex++;
                            }

                            if (childB.Nodes.Count > 0 && !nodeIDs.ContainsKey(childB))
                            {
                                nodeIDs.Add(childB, currentIDIndex);
                                currentIDIndex++;
                            }

                            string nameA = childA.Nodes.Count > 0 ? nodeIDs[childA].ToString() : childA.TaxaName;
                            string nameB = childB.Nodes.Count > 0 ? nodeIDs[childB].ToString() : childB.TaxaName;

                            writerStream.WriteLine(nodeIDs[node] + "," + node.TaxaName + "," + Math.Round(node.getInputNodeHeight() * ReltimesMultipler, 3) + "," + Math.Round(node.getNodeHeight(false), 3) + "," + nameA + "," + nameB);
                        }
                    }
                     */

                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        private void exportUnnamedChildNodes(StreamWriter writerStream, TopoTimeNode ancestor, TopoTimeNode currentChild, ref int childIndex, ref int currentIDIndex, int level, NpgsqlConnection conn)
        {
            if (currentChild.Nodes.Count > 0 && (currentChild.TaxonName == null || currentChild.TaxonName == ""))
            {
                string childIdentifier = "child " + ancestor.TaxonName + " " + childIndex + " tier " + level;
                childIndex++;

                string pubmedList = "";
                foreach (ChildPairDivergence div in currentChild.ChildDivergences)
                {
                    pubmedList = pubmedList + div.PublicationID + ",";
                }

                double mean = currentChild.getNodeHeight(false);
                double stderror = currentChild.getNodeStandardError(false);
                setConfidenceInterval(currentChild, conn);

                writerStream.WriteLine(currentIDIndex + "," + childIdentifier + "," + Math.Round(currentChild.getNodeHeight(false), 3) + "," + Math.Round(currentChild.StoredAdjustedHeight, 3) + "," + currentChild.ChildDivergences.Count + "," + currentChild.DivergenceSmallest + "," + currentChild.DivergenceLargest + "," + currentChild.MinConfidenceInterval + "," + currentChild.MaxConfidenceInterval + "," + pubmedList);
                currentChild.UniqueID = currentIDIndex;
                currentIDIndex++;

                foreach (TopoTimeNode child in currentChild.Nodes)
                {
                    exportUnnamedChildNodes(writerStream, ancestor, child, ref childIndex, ref currentIDIndex, level + 1, conn);
                }
            }
        }

        /// <summary>
        /// File > Export Newick (Newick Times, Ultrametric)
        /// </summary>
        private void exportNewickWithReltimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;
                System.Windows.Forms.DialogResult refreshTimes = MessageBox.Show("Refresh stored divergence times?  This process may take a long time if hasn't been done already.  Note that this will erase any user-specified divergence times.", "", MessageBoxButtons.YesNo);

                StringBuilder errorList = new StringBuilder();

                treeViewer.Nodes.RemoveAt(0);
                Dictionary<TopoTimeNode, string> adjustedHeights;
                if (refreshTimes == System.Windows.Forms.DialogResult.Yes)
                {
                    adjustedHeights = activeTree.CalculateAdjustedNodeHeights(false);

                    string adjustmentList = "";
                    foreach (KeyValuePair<TopoTimeNode, string> pair in adjustedHeights)
                    {
                        adjustmentList = adjustmentList + pair.Key.Text + "," + pair.Key.getNodeHeight(false) + "," + pair.Key.getNodeHeight(true) + "," + pair.Key.storedAdjustedHeight + "," + pair.Value + Environment.NewLine;
                    }

                    MessageBox.Show(adjustmentList);
                    if (adjustmentList != "")
                        Clipboard.SetText(adjustmentList);
                }

                foreach (TopoTimeNode node in activeTree.nodeList)
                {
                    TopoTimeNode parent = (TopoTimeNode)node.Parent;
                    if (parent != null)
                    {
                        if (node.storedAdjustedHeight > parent.storedAdjustedHeight)
                            errorList.AppendLine(node.Text);
                    }
                }


                DebugTextForm dtf = new DebugTextForm(errorList.ToString());
                dtf.Show();

                treeViewer.Nodes.Add(activeTree.root);

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    //ExtendedNode root = (ExtendedNode)treeViewer.Nodes[0];
                    //String tree = root.writeInputNode(ReltimesMultipler);

                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(0);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        #region legacy editing functions

        /// <summary>
        /// Edit > Fetch Named Node Data
        /// Previously used to fetch named node data for trees lacking it, no longer necessary
        /// </summary>
        private void fetchNamedNodeDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();
            if (conn == null)
                return;

            // 
            HashSet<int> usedIDs = new HashSet<int>();

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.HasValidTaxon))
            {
                if (node.Parent != null)
                {
                    TopoTimeNode namedParent = (TopoTimeNode)node.NextNamedParent;
                    string sql = "SELECT * FROM ancestral_ncbi_list(" + node.TaxonID + ") WHERE taxon_id <> " + node.TaxonID + " EXCEPT SELECT * FROM ancestral_ncbi_list(" + namedParent.TaxonID + ") ORDER BY 6;";

                    DataTable table = GetSQLResult(sql, conn);

                    if (table.Rows.Count > 0)
                    {
                        node.storedNamedNodes = new List<TopoTimeNode>();
                        foreach (DataRow row in table.Rows)
                        {
                            int taxonID = (int)row[0];
                            string taxonName = row[1].ToString();

                            if (!usedIDs.Contains(taxonID))
                            {
                                usedIDs.Add(taxonID);

                                node.storedNamedNodes.Add(new TopoTimeNode(taxonName, taxonID));
                            }
                        }
                    }
                    else
                        node.storedNamedNodes = null;
                }
            }
        }
        #endregion

        #region common function wrappers

        private void ClockingFunction(Action function)
        {
            TimeSpan ticks;
            DateTime startResolutionTime = DateTime.Now;
            toolStripStatusLabel1.Text = "";

            function();

            ticks = DateTime.Now - startResolutionTime;
            toolStripStatusLabel1.Text = "Operation completed in " + ticks.TotalSeconds.ToString() + " seconds.";

        }

        private void SuppressRedrawFunction(Action function)
        {
            treeViewer.BeginUpdate();
            function();
            treeViewer.EndUpdate();
        }
        #endregion

        private void refreshTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(RefreshDivergenceTimes);            
        }

        private void RefreshDivergenceTimes()
        {
            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            NpgsqlConnection conn = getConnection();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                //node.ChildDivergences.Clear();
                if (node.Nodes.Count == 2)
                {
                    HALService.setTimesHAL(node);
                }
                else if (node.Nodes.Count == 1)
                {
                    if (node.Parent != null)
                    {
                        TreeNode child = node.Nodes[0];
                        node.Nodes.Remove(child);
                        node.Parent.Nodes.Add(child);

                        activeTree.DeleteNode(node);
                    }
                }
                else
                {
                    activeTree.UpdateNodeText(node);
                }
            }
            activeTree.refreshList.Clear();

            treeViewer.Nodes.Add(root);


            treeViewer.Refresh();
        }

        private void resetAllDivergencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(ResetAllDivergences);            
        }

        private void ResetAllDivergences()
        {
            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                node.ChildDivergences.Clear();

                if (node.Nodes.Count == 2)
                {
                    node.getLeaves(true);
                    HALService.setTimesHAL(node);
                }
            }

            treeViewer.Nodes.Add(root);
        }

        private void addNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private IEnumerable<TopoTimeNode> FullOutgroup(TopoTimeNode node, IEnumerable<TopoTimeNode> leavesA, IEnumerable<TopoTimeNode> leavesB)
        {
            TopoTimeNode parent = node;
            while (parent != null && parent.TaxonID == 0)
                parent = parent.Parent;
            if (parent != null)
                return parent.getLeaves(false).Except(leavesA.Union(leavesB));
            else
                return null;
        }

        private TopoTimeNode BigOutgroup(TopoTimeNode node)
        {
            TopoTimeNode parent = node.Parent;

            while (parent != null && !parent.HasValidTaxon)
                parent = parent.Parent;

            return parent;
        }


        private void rearrangeByBestFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();
            treeViewer.Nodes.RemoveAt(0);

            DataTable table = new DataTable();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.PartitionData != null)
                {
                    TopoTimeNode outgroup;
                    TopoTimeNode parent = (TopoTimeNode)node.Parent;

                    if (parent != null)
                    {
                        if (parent.Nodes[1] == node)
                            outgroup = (TopoTimeNode)parent.Nodes[0];
                        else
                            outgroup = (TopoTimeNode)parent.Nodes[1];

                        if (node.PartitionData.FavorAB < node.PartitionData.FavorAC && node.PartitionData.FavorAC >= node.PartitionData.FavorBC)
                        {
                            TopoTimeNode tempB = (TopoTimeNode)node.Nodes[1];
                            node.Nodes.Remove(tempB);
                            parent.Nodes.Remove(outgroup);

                            parent.Nodes.Add(tempB);
                            node.Nodes.Add(outgroup);

                            node.getLeaves(true);
                            double oldTime = node.getNodeHeight(false);
                            double newTime;

                            HALService.setTimesHAL(node);
                            newTime = node.getNodeHeight(false);

                            table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                            node.ForeColor = Color.Blue;
                            node.rearranged = true;
                        }
                        else if (node.PartitionData.FavorAB < node.PartitionData.FavorBC)
                        {
                            TopoTimeNode tempA = (TopoTimeNode)node.Nodes[0];
                            node.Nodes.Remove(tempA);
                            parent.Nodes.Remove(outgroup);

                            parent.Nodes.Add(tempA);
                            node.Nodes.Add(outgroup);

                            node.getLeaves(true);
                            double oldTime = node.getNodeHeight(false);
                            double newTime;

                            HALService.setTimesHAL(node);
                            newTime = node.getNodeHeight(false);

                            table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                            node.ForeColor = Color.Blue;
                            node.rearranged = true;
                        }
                    }
                }
            }
            treeViewer.Nodes.Add(activeTree.root);
            DebugForm debug = new DebugForm(table);
            debug.Show();
        }

        private void dissolveBifurcationEntireTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Parent != null && ((TopoTimeNode)node.Parent).getNodeHeight(true) == node.getNodeHeight(true))
                {
                    for (int i = node.Nodes.Count - 1; i >= 0; i--)
                    {
                        TopoTimeNode child = (TopoTimeNode)node.Nodes[i];
                        node.Nodes.Remove(child);
                        node.Parent.Nodes.Add(child);
                    }
                    node.Parent.Nodes.Remove(node);
                    trashList.Add(node);
                }
            }

            foreach (TopoTimeNode node in trashList)
                activeTree.nodeList.Remove(node);
        }

        private void dissolveConflictingBifurcationsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            List<TopoTimeNode> trashList = new List<TopoTimeNode>();
            foreach (TopoTimeNode parent in activeTree.nodeList)
            {
                if (trashList.Contains(parent))
                    continue;


                TopoTimeNode suspectParent = parent;

                double suspectHeight = suspectParent.getNodeHeight(true);
                for (int j = 0; j < suspectParent.Nodes.Count; j++)
                {
                    TopoTimeNode child = (TopoTimeNode)suspectParent.Nodes[j];
                    double childNodeHeight = child.getNodeHeight(true);
                    if (childNodeHeight > suspectHeight)
                    {
                        for (int i = child.Nodes.Count - 1; i >= 0; i--)
                        {
                            TopoTimeNode grandchild = (TopoTimeNode)child.Nodes[i];
                            child.Nodes.Remove(grandchild);
                            suspectParent.Nodes.Add(grandchild);
                        }

                        trashList.Add(child);
                    }
                }
            }

            foreach (TopoTimeNode trash in trashList)
            {
                activeTree.DeleteNode(trash);
            }
        }

        private void MarkNegativeBranchLengths()
        {
            treeViewer.Nodes.RemoveAt(0);

            int passes = 0;
            StringBuilder sb = new StringBuilder();


            List<TopoTimeNode> NegativeBranchNodes = activeTree.nodeList.Where(x => x.getBranchLength() < 0).Cast<TopoTimeNode>().ToList();
            List<TopoTimeNode> LastNodeSet = NegativeBranchNodes;
            int negativeBranchCount = NegativeBranchNodes.Count;
            int lastNegativeBranchCount = 0;

            do
            {
                passes++;
                List<HashSet<TopoTimeNode>> NodeGroupList = activeTree.MarkNegativeNodes();
                sb.AppendLine("Pass " + passes + ": " + negativeBranchCount + " remaining");

                lastNegativeBranchCount = negativeBranchCount;

                foreach (HashSet<TopoTimeNode> NodeGroup in NodeGroupList)
                {
                    activeTree.SmoothNegativeBranches(NodeGroup);
                    BoldenSelectNodes(NodeGroup);
                    /*
                    foreach (TopoTimeNode node in NodeGroup)
                    {
                        sb.Append(node.GetHashCode() + ",");
                    }
                    sb.AppendLine();
                    */
                }

                NegativeBranchNodes = activeTree.nodeList.Where(x => x.getBranchLength() < 0).Cast<TopoTimeNode>().ToList();
                negativeBranchCount = NegativeBranchNodes.Count;
            }
            while (negativeBranchCount > 0 && negativeBranchCount != lastNegativeBranchCount);

            sb.AppendLine(passes + " passes");
            
            

            DebugTextForm dtf = new DebugTextForm(sb.ToString());
            dtf.Show();
            treeViewer.Nodes.Add(activeTree.root);
        }

        private void BoldenSelectNodes(IEnumerable<TopoTimeNode> NodeGroup)
        {
            Font boldFont = new Font(treeViewer.Font, FontStyle.Bold);

            foreach (TopoTimeNode node in NodeGroup)
            {
                node.NodeFont = boldFont;
            }            
        }

        private void FixNegativeBranchLengths()
        {
            string errorList = "";

            CleanTree();

            treeViewer.Nodes.RemoveAt(0);
            Dictionary<TopoTimeNode, string> adjustedHeights;

            adjustedHeights = activeTree.CalculateAdjustedNodeHeights(false);

            string adjustmentList = "";
            foreach (KeyValuePair<TopoTimeNode, string> pair in adjustedHeights)
            {
                adjustmentList = adjustmentList + pair.Key.Text + "," + pair.Key.getNodeHeight(false) + "," + pair.Key.getNodeHeight(true) + "," + pair.Key.storedAdjustedHeight + "," + pair.Value + Environment.NewLine;
            }

            DebugTextForm dtf = new DebugTextForm(adjustmentList);
            dtf.Show();

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                TopoTimeNode parent = (TopoTimeNode)node.Parent;
                if (parent != null)
                {
                    if (node.storedAdjustedHeight > parent.storedAdjustedHeight)
                        errorList = errorList + node.Text + Environment.NewLine;
                }
            }

            DebugTextForm errorTextForm = new DebugTextForm(errorList);
            errorTextForm.Show();

            treeViewer.Nodes.Add(activeTree.root);            
        }

        private void fixNegativeBranchLengthsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FixNegativeBranchLengths();
        }

        private void identifyFullyMultifurcatedLabeledNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> multifurcatedNodes = new List<TopoTimeNode>();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.TaxonName != "" && node.Nodes.Count > 0)
                {
                    bool isInvalid = false;
                    foreach (TopoTimeNode child in node.Nodes)
                    {
                        if (child.Nodes.Count > 0)
                        {
                            isInvalid = true;
                            break;
                        }
                    }

                    if (!isInvalid)
                    {
                        multifurcatedNodes.Add(node);
                    }
                }
            }

            DebugForm multifurcations = new DebugForm(multifurcatedNodes);
            multifurcations.Show();
        }

        private void dissolveBifurcationsPreserveLabeledNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Parent != null && node.TaxonName == "")
                {
                    for (int i = node.Nodes.Count - 1; i >= 0; i--)
                    {
                        TopoTimeNode child = (TopoTimeNode)node.Nodes[i];
                        node.Nodes.Remove(child);
                        node.Parent.Nodes.Add(child);
                    }
                    node.Parent.Nodes.Remove(node);
                    trashList.Add(node);
                }
            }

            foreach (TopoTimeNode node in trashList)
                activeTree.nodeList.Remove(node);
        }

        // EMPTY
        private void treeStatisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private bool isNonContributor(TopoTimeNode node)
        {
            if (node.Nodes.Count > 0 && node.ChildDivergences.Count == 0 && node.getNodeHeight(false) == 0)
            {
                foreach (TopoTimeNode child in node.Nodes)
                {
                    if (child.Nodes.Count == 0)
                        continue;

                    if (!isNonContributor(child))
                        return false;
                }
                return true;
            }

            return false;
        }

        private void pruneNonContributingTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            NpgsqlConnection conn = getConnection();

            TopoTimeNode root = activeTree.root;

            treeViewer.BeginUpdate();
            treeViewer.Nodes.RemoveAt(0);

            string log = "";

            TreeBuilderService.PruneNonContributingTaxa(activeTree, out log);

            treeViewer.Nodes.Add(activeTree.root);
            treeViewer.EndUpdate();

            DebugTextForm dtf = new DebugTextForm(log);
            dtf.Show();
        }

        private void exportDistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "CSV file (*.csv)|*.csv";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;
                
                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    StreamWriter writerStream = new StreamWriter(file);
                    writerStream.WriteLine("Name,Dispersal");

                    foreach (TopoTimeNode node in activeTree.nodeList)
                    {
                        node.storedDistinctiveness = 0;
                    }

                    foreach (TopoTimeNode leaf in activeTree.leafList)
                    {
                        if (leaf.Parent != null)
                        {
                            writerStream.WriteLine(leaf.TaxonName + "," + leaf.getDistinctiveness() + "," + leaf.Parent.Nodes.Count);
                        }
                    }

                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        private void changeUserDefinedNodeHeightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

            string height;
            if (selectedNode.storedAdjustedHeight == 0 && selectedNode.Nodes.Count == 2)
                height = selectedNode.getNodeHeight(true).ToString("0.################");
            else
                height = selectedNode.StoredAdjustedHeight.ToString("0.################");

            UserInputForm heightEdit = new UserInputForm(height);

            if (heightEdit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedNode.storedAdjustedHeight = heightEdit.newValue;
                selectedNode.UpdateText();

                /*
                if (selectedNode.TaxonName != "")
                    selectedNode.Text = selectedNode.TaxonName + " [" + selectedNode.TaxonID + "] (" + selectedNode.StoredAdjustedHeight.ToString("0.00") + " | " + selectedNode.getNodeHeight(true) + ")" + " {" + selectedNode.ChildDivergences.Count + "}";
                else
                    selectedNode.Text = selectedNode.StoredAdjustedHeight.ToString("0.00") + " | " + selectedNode.getNodeHeight(true).ToString("0.00") + " {" + selectedNode.ChildDivergences.Count + "}";
                */

                selectedNode.ForeColor = Color.Blue;
            }
        }


        private void DissolveNovelBifurcations(TopoTimeNode selectedNode)
        {
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();

            for (int i = 0; i < selectedNode.Nodes.Count; i++)
            {
                TopoTimeNode child = (TopoTimeNode)selectedNode.Nodes[i];
                if (child.TaxonID == 0)
                {
                    for (int j = child.Nodes.Count - 1; j >= 0; j--)
                    {
                        TopoTimeNode grandchild = (TopoTimeNode)child.Nodes[j];
                        child.Nodes.Remove(grandchild);
                        selectedNode.Nodes.Add(grandchild);
                    }
                    trashList.Add(child);
                }
            }

            foreach (TopoTimeNode node in trashList)
                activeTree.DeleteNode(node);
        }

        private void labelNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                UserInput input = new UserInput();
                if (input.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedNode.TaxonName = input.userInputString;

                    selectedNode.UpdateText();
                    /*

                    selectedNode.Text = selectedNode.TaxonName + " [" + selectedNode.TaxonID + "] (" + selectedNode.getNodeHeight(true).ToString("0.00") + ")" + " {" + selectedNode.ChildDivergences.Count + "}";
                    selectedNode.ForeColor = Color.ForestGreen;
                    */
                }
            }
        }

        private void cleanUpTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CleanTree();
        }

        private void CleanTree()
        {
            TreeBuilderService.CleanTree(activeTree);
        }

        private void treeStatisticsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            HashSet<string> studyList = new HashSet<string>();
            /*
            int FritzCount = 0;
            int FritzPolytomyCount = 0;
            int PolytomyCount = 0;
            int ParentCount = 0;
            int branchesInPolytomies = 0;
            int validNodes = 0;
             * */

            DataTable statTable = new DataTable();

            string[] columnHeader = { "Taxon", "NCBI ID", "Mean Time", "Median Time", "Adjusted Time", "Sample Size", "Min Time", "Max Time", "CI Low", "CI High", "Has Own Times" };

            for (int i = 0; i < columnHeader.Length; i++)
            {
                statTable.Columns.Add(columnHeader[i]);
            }            

            statTable.Rows.Add();

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                bool hasOwnTimes = true;
                TopoTimeNode timeSource;
                if (node.ChildDivergences.Count == 0)
                {
                    hasOwnTimes = false;
                    timeSource = node.getTimeSource();
                }
                else
                    timeSource = node;
                List<double> times = timeSource.ChildDivergences.Select(x => (double)x.DivergenceTime).ToList();
                Tuple<double, double> confInterval;
                if (times.Count > 2)
                {
                    if (activeTree.UseMedianTimes)
                        confInterval = Functions.MedianConfidenceInterval(times);
                    else
                        confInterval = Functions.TConfidenceInterval(times, 0.95);
                }
                else if (times.Count == 2)
                    confInterval = Tuple.Create(times.Min(), times.Max());
                else
                    confInterval = new Tuple<double, double>(0, 0);

                double minTime;
                double maxTime;

                if (timeSource.ChildDivergences.Count > 0)
                {
                    minTime = timeSource.ChildDivergences.Min(x => (double)x.DivergenceTime);
                    maxTime = timeSource.ChildDivergences.Max(x => (double)x.DivergenceTime);
                }
                else
                {
                    minTime = 0;
                    maxTime = 0;
                }

                statTable.Rows.Add(node.TaxonName, node.TaxonID, node.getNodeHeight(true).ToString("0.00"), node.getNodeMedian(true).ToString("0.00"), node.StoredAdjustedHeight.ToString("0.00"), node.getSampleSize(true), minTime, maxTime, confInterval.Item1, confInterval.Item2, hasOwnTimes);

            }                

            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = statTable.Columns.Cast<DataColumn>().
                                                Select(column => column.ColumnName);
            sb.AppendLine(string.Join("\t", columnNames));

            foreach (DataRow row in statTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join("\t", fields));
            }
            DebugTextForm dtf = new DebugTextForm(sb.ToString());
            dtf.Show();

            //DebugForm df = new DebugForm(statTable, "Node Statsistics");
            //df.Show();

            //catch { }
            //finally { }

            /*
            string log = "";            

            // cellular organisms, archaebacteria, eubacteria, eukaryotes, land plants, animals, invertebrates, vertebrates, ray-finned-fishes, cartilaginous fishes, tetrapods, amphibians, reptiles, birds, mammals)
            int[] rootIDs = { 131567, 2157, 2, 2759, 3193, 33208, 0, 7742, 7898, 7777, 32523, 8292, 8457, 8782, 40674 };

            foreach (int originID in rootIDs)
            {
                ExtendedNode origin;
                List<ExtendedNode> leaves;

                // special case for eubacteria, whose node was merged
                if (originID == 2)
                {
                    origin = activeTree.nodeList.Find(item => item.TaxaID == 2759);
                    origin = (ExtendedNode)origin.Parent;

                    leaves = new List<ExtendedNode>();
                    foreach (ExtendedNode node in origin.Nodes)
                    {
                        if (node.TaxaID != 2759)
                            leaves.AddRange(node.getLeaves(false));
                    }
                }
                // special case for invertebrates
                else if (originID == 0)
                {
                    origin = activeTree.nodeList.Find(item => item.TaxaID == 86013);
                    origin = (ExtendedNode)origin.Parent;

                    leaves = origin.getLeavesWithoutVertebrates(false);
                }
                else
                {
                    origin = activeTree.nodeList.Find(item => item.TaxaID == originID);
                    if (origin == null)
                        continue;

                    leaves = origin.getLeaves(false);
                }                

                foreach (string classification in levels)
                {
                    Dictionary<int, List<ExtendedNode>> groupAssociation = new Dictionary<int, List<ExtendedNode>>();
                    List<double> timeCollection = new List<double>();

                    foreach (ExtendedNode leaf in leaves)
                    {
                        string sql = "SELECT t.parent_id, (SELECT rank FROM ncbi_taxonomy WHERE t.parent_id=taxon_id) FROM ncbi_taxonomy t WHERE taxon_id=" + leaf.TaxaID;
                        DataTable table = getSQLResult(sql, conn);
                        int parentID;
                        string parentRank;

                        if (table.Rows.Count > 0 && table.Rows[0][0].GetType() != typeof(DBNull))
                        {
                            parentID = (int)table.Rows[0][0];
                            parentRank = table.Rows[0][1].ToString();
                        }
                        else
                        {
                            parentID = 0;
                            parentRank = "";
                        }

                        bool classMatch = false;
                        while (parentID != 0 && !classMatch)
                        {
                            sql = "SELECT t.parent_id, (SELECT rank FROM ncbi_taxonomy WHERE t.parent_id=taxon_id) FROM ncbi_taxonomy t WHERE taxon_id=" + parentID;
                            table = getSQLResult(sql, conn);

                            if (table.Rows.Count > 0 && table.Rows[0][0].GetType() != typeof(DBNull))
                            {
                                parentID = (int)table.Rows[0][0];
                                parentRank = table.Rows[0][1].ToString();
                            }
                            else
                            {
                                parentID = 0;
                                parentRank = "";
                            }

                            if (parentID == 1)
                                parentID = 0;

                            if (classification != "kingdom")
                                classMatch = (parentRank == classification);
                            else
                                classMatch = (parentRank == "kingdom" || parentRank == "superkingdom");
                        }

                        if (!groupAssociation.ContainsKey(parentID))
                            groupAssociation[parentID] = new List<ExtendedNode>();

                        groupAssociation[parentID].Add(leaf);
                    }

                    double mean = 0.0;
                    int n = 0;
                    double sem = 0.0;
                    List<double> values = new List<double>();

                    foreach (KeyValuePair<int, List<ExtendedNode>> pair in groupAssociation)
                    {
                        if (pair.Value != null)
                        {
                            ExtendedNode commonAncestor = getCommonAncestor(pair.Value);
                            if (commonAncestor != null)
                            {
                                log = log + originID + "," + classification + "," + pair.Key + "," + commonAncestor.GetHashCode() + "," + commonAncestor.storedAdjustedHeight + Environment.NewLine;

                                if (pair.Key != 0)
                                {
                                    timeCollection.Add(commonAncestor.storedAdjustedHeight);
                                    values.Add(commonAncestor.storedAdjustedHeight);
                                    mean = mean + commonAncestor.storedAdjustedHeight;
                                    n++;
                                }
                            }
                        }
                    }

                    if (n > 0)
                    {
                        mean = mean / (double)n;

                        foreach (double value in values)
                            sem = sem + Math.Pow(value - mean, 2);

                        sem = Math.Sqrt(sem / n);
                        sem = sem / Math.Sqrt(n);

                        statTable.Rows.Add(originID, classification, mean, n, sem);
                    }

                    if (groupAssociation.ContainsKey(0))
                    {
                        log = log + originID + "," + classification;
                        foreach (ExtendedNode otherTaxa in groupAssociation[0])
                        {
                            log = log + "," + otherTaxa.TaxaName;
                        }
                        log = log + Environment.NewLine;
                    }
                }
            }

            Clipboard.SetText(log);
             */
            /*
            foreach (ExtendedNode node in activeTree.nodeList)
            {
                if (node.Parent != null)
                {
                    validNodes++;

                    if (node.Parent.Nodes.Count > 2)
                        branchesInPolytomies++;
                }

                foreach (ChildPairDivergence divTime in node.ChildDivergences)
                {
                    if (!studyList.Contains(divTime.PublicationID))
                        studyList.Add(divTime.PublicationID);
                }

                if (node.Nodes.Count > 0)
                    ParentCount++;

                if (node.Nodes.Count > 2)
                    PolytomyCount++;

                if (node.ChildDivergences.Count == 1)
                {
                    if (node.ChildDivergences[0].PublicationID == "19392714")
                    {
                        FritzCount++;
                        if (node.Nodes.Count > 2)
                            FritzPolytomyCount++;
                    }
                }
            }

            int leavesInPolytomies = 0;
            int validLeaves = 0;
            string glitches = "";
            foreach (ExtendedNode leaf in activeTree.leafList)
            {
                if (leaf.Parent != null)
                {
                    validLeaves++;
                    if (leaf.Parent.Nodes.Count > 2)
                        leavesInPolytomies++;
                }
                else
                    glitches = glitches + leaf.TaxaName + Environment.NewLine;

            }

            string message = "";
            message = "Tree contains data from " + studyList.Count + " distinct studies." + Environment.NewLine;
            message = message + FritzCount + " of " + ParentCount + " nodes were uniquely from Fritz.  " + FritzPolytomyCount + " were polytomies." + Environment.NewLine;
            message = message + PolytomyCount + " nodes in the tree were polytomies." + Environment.NewLine;
            message = message + leavesInPolytomies + " of " + validLeaves + " leaves are in polytomies." + Environment.NewLine;
            message = message + branchesInPolytomies + " of " + validNodes + " branches are in polytomies." + Environment.NewLine;

            MessageBox.Show(message);
            //MessageBox.Show(glitches);
            string studyText = "";

            foreach (ExtendedNode leafNode in activeTree.leafList)
            {
                if (leafNode.Parent != null && leafNode.TaxaID > 0)
                    studyText = studyText + leafNode.TaxaName + Environment.NewLine;
            }
            Clipboard.SetText(studyText);
            */
        }

        private void compareStudiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CompareForm compareStudies = new CompareForm(activeTree);
            compareStudies.Show();
        }

        private void deleteDuplicateTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeEditingService.DeleteDuplicateTaxa(activeTree);
        }
        

        private void resetLeafCountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            activeTree.root.getLeaves(true);
        }

        private void exportNewickTreeLogTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;
                System.Windows.Forms.DialogResult refreshTimes = MessageBox.Show("Refresh stored divergence times?  This process may take a long time if hasn't been done already.  Note that this will erase any user-specified divergence times.", "", MessageBoxButtons.YesNo);

                if (refreshTimes == System.Windows.Forms.DialogResult.Yes)
                {
                    activeTree.CalculateAdjustedNodeHeights(false);
                }

                List<TopoTimeNode> trashList = new List<TopoTimeNode>();

                foreach (TopoTimeNode leaf in activeTree.leafList)
                {
                    TopoTimeNode parent = (TopoTimeNode)leaf.Parent;
                    if (parent != null)
                    {
                        TopoTimeNode grandparent = (TopoTimeNode)leaf.Parent.Parent;
                        if (parent.storedAdjustedHeight == 0)
                        {
                            if (grandparent != null)
                            {
                                for (int i = parent.Nodes.Count - 1; i >= 0; i--)
                                {
                                    TopoTimeNode child = (TopoTimeNode)parent.Nodes[i];
                                    parent.Nodes.Remove(child);
                                    grandparent.Nodes.Add(child);
                                }
                                grandparent.Nodes.Remove(parent);
                                trashList.Add(parent);
                            }
                        }
                    }
                }

                foreach (TopoTimeNode node in trashList)
                    activeTree.DeleteNode(node);

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeLogNode();

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        private void listNodesWithoutTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = activeTree.nodeList.Cast<TopoTimeNode>().ToList().FindAll(
                delegate (TopoTimeNode arg)
                {
                    if (arg.ChildDivergences.Count == 0 && arg.Nodes.Count > 0)
                        return true;
                    return false;
                });

            DataTable table = new DataTable();
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Node",
                DataType = typeof(TopoTimeNode)
            });
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Child1",
                DataType = typeof(TopoTimeNode)
            });
            table.Columns.Add(new DataColumn()
            {
                ColumnName = "Child2",
                DataType = typeof(TopoTimeNode)
            });
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();

            foreach (TopoTimeNode node in suspects)
            {
                TopoTimeNode child1 = (TopoTimeNode)node.Nodes[0];
                TopoTimeNode child2 = (TopoTimeNode)node.Nodes[1];

                double otherHeight = 0.0;
                if (child1.storedAdjustedHeight != node.storedAdjustedHeight)
                    otherHeight = child1.StoredAdjustedHeight;
                else
                    otherHeight = child2.StoredAdjustedHeight;

                table.Rows.Add(node, node.TaxonName, node.TaxonID, child1, child2, node.Nodes.Count > 2, node.storedAdjustedHeight, otherHeight, child1.Nodes.Count > 0, child2.Nodes.Count > 0, child1.ToString().Contains("."), child2.ToString().Contains("."));
            }

            DebugForm newForm = new DebugForm(table);
            newForm.Show();
        }

        private void checkForGapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable table = new DataTable();
            table.Columns.Add();
            table.Columns.Add();
            Dictionary<TopoTimeNode, int> overlapList = new Dictionary<TopoTimeNode, int>();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                TopoTimeNode parent = (TopoTimeNode)node.Parent;
                int blankCount = 0;
                while (parent != null && parent.ChildDivergences.Count == 0)
                {
                    blankCount++;

                    if (!overlapList.ContainsKey(parent))
                    {
                        overlapList.Add(parent, 0);
                    }
                    overlapList[parent]++;
                    parent = (TopoTimeNode)parent.Parent;

                }

                if (blankCount > 0)
                    table.Rows.Add(node.Text, blankCount);

                foreach (KeyValuePair<TopoTimeNode, int> pair in overlapList)
                {
                    if (pair.Value > 1)
                        table.Rows.Add(pair.Key, "Parent");
                }
            }

            DebugForm debug = new DebugForm(table);
            debug.Show();
        }


        private bool IsContiguousGroup(TopoTimeNode parent, TopoTimeNode node, Dictionary<TopoTimeNode, string> parentSpeciesList)
        {
            List<TopoTimeNode> nodeLeaves = node.getLeaves(false);
            List<TopoTimeNode> parentLeaves = parent.getLeaves(false);

            try
            {
                string higherRank = parentSpeciesList[nodeLeaves[0]];
                foreach (TopoTimeNode leaf in parentLeaves)
                {
                    if (!parentSpeciesList.ContainsKey(leaf) || parentSpeciesList[leaf] != higherRank)
                        return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void polytomyCountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int count = 0;
            int remainingBifurcations = 0;

            SortedList<int, int> nodeCount = new SortedList<int, int>();

            StringBuilder sb = new StringBuilder();

            foreach (TopoTimeNode node in activeTree.ValidNodes().Where(x => x.TaxonID != 0 && x.Nodes.Count > 2))
            {
                sb.AppendLine(node.Text + " - " + node.Nodes.Count);
            }

            sb.AppendLine();

            foreach (TopoTimeNode node in activeTree.ValidNodes())
            {
                if (node.Nodes.Count > 1)
                {
                    count++;
                    remainingBifurcations += (node.Nodes.Count - 2);

                    if (!nodeCount.ContainsKey(node.Nodes.Count))
                        nodeCount[node.Nodes.Count] = 0;
                    nodeCount[node.Nodes.Count]++;
                }
            }

            sb.AppendLine("Tree contains " + count + " total nodes, " + remainingBifurcations + " bifurcations required to resolve.");

            foreach (KeyValuePair<int, int> pair in nodeCount)
            {
                sb.AppendLine(pair.Key + ": " + pair.Value);
            }

            new DebugTextForm(sb.ToString()).Show();
        }

        private void findNextPolytomyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (searchEnumerator == null)
                searchEnumerator = activeTree.nodeList.Where(x => x.Nodes.Count > 2).GetEnumerator();

            try
            {
                searchEnumerator.MoveNext();
                ExtendedNode node = searchEnumerator.Current;

                treeViewer.SelectedNode = node;
                node.EnsureVisible();
                treeViewer.Focus();
            }
            catch
            {
                searchEnumerator = activeTree.nodeList.Where(x => x.Nodes.Count > 2).GetEnumerator();
            }
        }

        private void negativeBranchStatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> usedNodes = new List<TopoTimeNode>();
            Dictionary<int, int> negBranchCount = new Dictionary<int, int>();
            foreach (TopoTimeNode node in activeTree.ValidNodes())
            {
                if (!usedNodes.Contains(node))
                {
                    TopoTimeNode parent = (TopoTimeNode)node.Parent;
                    if (parent != null)
                    {
                        TopoTimeNode tempNode = node;
                        int negChain = 0;
                        while (tempNode.getNodeHeight(false) > parent.getNodeHeight(false))
                        {
                            negChain++;

                            if (!usedNodes.Contains(tempNode))
                                usedNodes.Add(tempNode);

                            bool childFound = false;
                            for (int i = 0; i < tempNode.Nodes.Count; i++)
                            {
                                TopoTimeNode child = (TopoTimeNode)tempNode.Nodes[i];
                                if (child.getNodeHeight(false) > parent.getNodeHeight(false))
                                {
                                    tempNode = child;
                                    childFound = true;
                                    break;
                                }
                            }

                            if (!childFound)
                                break;
                        }

                        if (!negBranchCount.ContainsKey(negChain))
                            negBranchCount[negChain] = 0;

                        negBranchCount[negChain]++;
                    }
                }
            }

            string message = "";
            foreach (KeyValuePair<int, int> pair in negBranchCount)
            {
                message = message + pair.Key + "," + pair.Value + Environment.NewLine;
            }

            message = message + "------" + Environment.NewLine;


            negBranchCount = new Dictionary<int, int>();
            foreach (TopoTimeNode node in activeTree.ValidNodes())
            {
                if (!usedNodes.Contains(node))
                {
                    TopoTimeNode parent = (TopoTimeNode)node.Parent;
                    if (parent != null)
                    {
                        TopoTimeNode tempNode = node;
                        int negChain = 0;
                        while (tempNode.storedAdjustedHeight > parent.storedAdjustedHeight)
                        {
                            negChain++;

                            if (!usedNodes.Contains(tempNode))
                                usedNodes.Add(tempNode);

                            bool childFound = false;
                            for (int i = 0; i < tempNode.Nodes.Count; i++)
                            {
                                TopoTimeNode child = (TopoTimeNode)tempNode.Nodes[i];
                                if (child.storedAdjustedHeight > parent.storedAdjustedHeight)
                                {
                                    tempNode = child;
                                    childFound = true;
                                    break;
                                }
                            }

                            if (!childFound)
                                break;
                        }

                        if (!negBranchCount.ContainsKey(negChain))
                            negBranchCount[negChain] = 0;

                        negBranchCount[negChain]++;
                    }
                }
            }

            foreach (KeyValuePair<int, int> pair in negBranchCount)
            {
                message = message + pair.Key + "," + pair.Value + Environment.NewLine;
            }

            //MessageBox.Show(message);
            if (message != "")
                Clipboard.SetText(message);
        }

        private void studyContributionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dictionary<string, int> studyCount = new Dictionary<string, int>();
            int nodeCount = 0;
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.TreeView != null && node.Nodes.Count > 0)
                {
                    nodeCount++;
                    foreach (ChildPairDivergence divergence in node.ChildDivergences)
                    {
                        string study = divergence.CitationID.ToString();
                        if (!studyCount.ContainsKey(study))
                            studyCount.Add(study, 0);

                        studyCount[study]++;
                    }
                }
            }

            DataTable studyTable = new DataTable();
            studyTable.Columns.Add();
            studyTable.Columns.Add();
            foreach (KeyValuePair<string, int> pair in studyCount)
                studyTable.Rows.Add(pair.Key, pair.Value);

            studyTable.Rows.Add("Total", nodeCount);
            DebugForm form = new DebugForm(studyTable);
            form.Show();
        }

        private List<TopoTimeNode> getAncestors(TopoTimeNode node)
        {
            List<TopoTimeNode> parents = new List<TopoTimeNode>();

            TopoTimeNode currentParent = (TopoTimeNode)node.Parent;
            while (currentParent != null)
            {
                parents.Add(currentParent);
                currentParent = (TopoTimeNode)currentParent.Parent;
            }

            return parents;
        }

        private double getDivergence(List<TopoTimeNode> nodeAParents, TopoTimeNode nodeB)
        {
            TopoTimeNode currentParent = (TopoTimeNode)nodeB.Parent;
            while (currentParent != null && !nodeAParents.Contains(currentParent))
            {
                currentParent = (TopoTimeNode)currentParent.Parent;
            }

            if (currentParent != null)
                return currentParent.StoredAdjustedHeight;
            return -1;
        }

        

        private void nodeDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string nodeAgesList = "";

            NpgsqlConnection conn = getConnection();

            Dictionary<TopoTimeNode, string> familyTable = new Dictionary<TopoTimeNode, string>();
            Dictionary<TopoTimeNode, string> orderTable = new Dictionary<TopoTimeNode, string>();

            foreach (TopoTimeNode leaf in activeTree.leafList)
            {
                string name = leaf.Text.Substring(0, leaf.Text.IndexOf(' '));
                name = Regex.Replace(name, "'", "");
                string sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE scientific_name = '" + name + "';";
                DataTable table = GetSQLResult(sql, conn);

                while (table.Rows.Count > 0)
                {
                    if (table.Rows[0][1].ToString() == "family")
                    {
                        familyTable.Add(leaf, table.Rows[0][2].ToString());
                        break;
                    }

                    sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE taxon_id = " + table.Rows[0][0] + ";";
                    table = GetSQLResult(sql, conn);
                }

                sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE scientific_name = '" + name + "';";
                table = GetSQLResult(sql, conn);

                while (table.Rows.Count > 0)
                {
                    if (table.Rows[0][1].ToString() == "order")
                    {
                        orderTable.Add(leaf, table.Rows[0][2].ToString());
                        break;
                    }

                    sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE taxon_id = " + table.Rows[0][0] + ";";
                    table = GetSQLResult(sql, conn);
                }
            }

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Parent != null && node.TreeView != null)
                {
                    List<TopoTimeNode> leafList = node.getLeaves(false);
                    string genus = "";
                    string family = "";
                    string order = "";

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        string name = leaf.Text.Substring(0, leaf.Text.IndexOf(' '));
                        name = Regex.Replace(name, "'", "");
                        if (genus == "")
                            genus = name;
                        else if (genus != "" && genus != name)
                        {
                            genus = "";
                            break;
                        }
                    }

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        if (familyTable.ContainsKey(leaf))
                        {
                            string name = familyTable[leaf];

                            if (family == "")
                                family = name;
                            else if (family != "" && family != name)
                            {
                                family = "";
                                break;
                            }
                        }
                    }

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        if (orderTable.ContainsKey(leaf))
                        {
                            string name = orderTable[leaf];

                            if (order == "")
                                order = name;
                            else if (order != "" && order != name)
                            {
                                order = "";
                                break;
                            }
                        }
                    }

                    string nodeText = node.Text;
                    int labelIndex = node.Text.IndexOf('[') - 1;

                    if (labelIndex > 0)
                        nodeText = nodeText.Remove(labelIndex);

                    nodeAgesList = nodeAgesList + ((TopoTimeNode)node.Parent).getNodeHeight(false) + "," + leafList.Count + "," + nodeText + "," + genus + "," + family + "," + order + Environment.NewLine;
                }
            }

            DebugTextForm frm = new DebugTextForm(nodeAgesList);
            frm.Show();
        }

        private void nodeAgesChildrenCrownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string nodeAgesList = "";

            NpgsqlConnection conn = getConnection();

            Dictionary<TopoTimeNode, string> familyTable = new Dictionary<TopoTimeNode, string>();
            Dictionary<TopoTimeNode, string> orderTable = new Dictionary<TopoTimeNode, string>();

            foreach (TopoTimeNode leaf in activeTree.leafList)
            {
                string name = leaf.Text.Substring(0, leaf.Text.IndexOf(' '));
                name = Regex.Replace(name, "'", "");
                string sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE scientific_name = '" + name + "';";
                DataTable table = GetSQLResult(sql, conn);

                while (table.Rows.Count > 0)
                {
                    if (table.Rows[0][1].ToString() == "family")
                    {
                        familyTable.Add(leaf, table.Rows[0][2].ToString());
                        break;
                    }

                    sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE taxon_id = " + table.Rows[0][0] + ";";
                    table = GetSQLResult(sql, conn);
                }

                sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE scientific_name = '" + name + "';";
                table = GetSQLResult(sql, conn);

                while (table.Rows.Count > 0)
                {
                    if (table.Rows[0][1].ToString() == "order")
                    {
                        orderTable.Add(leaf, table.Rows[0][2].ToString());
                        break;
                    }

                    sql = "SELECT parent_id, (SELECT rank FROM ncbi_taxonomy WHERE taxon_id=t.parent_id), (SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id=t.parent_id) FROM ncbi_taxonomy t WHERE taxon_id = " + table.Rows[0][0] + ";";
                    table = GetSQLResult(sql, conn);
                }
            }

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Parent != null && node.TreeView != null && node.Nodes.Count > 0)
                {
                    List<TopoTimeNode> leafList = node.getLeaves(false);
                    string genus = "";
                    string family = "";
                    string order = "";

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        string name = leaf.Text.Substring(0, leaf.Text.IndexOf(' '));
                        name = Regex.Replace(name, "'", "");
                        if (genus == "")
                            genus = name;
                        else if (genus != "" && genus != name)
                        {
                            genus = "";
                            break;
                        }
                    }

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        if (familyTable.ContainsKey(leaf))
                        {
                            string name = familyTable[leaf];

                            if (family == "")
                                family = name;
                            else if (family != "" && family != name)
                            {
                                family = "";
                                break;
                            }
                        }
                    }

                    foreach (TopoTimeNode leaf in leafList)
                    {
                        if (orderTable.ContainsKey(leaf))
                        {
                            string name = orderTable[leaf];

                            if (order == "")
                                order = name;
                            else if (order != "" && order != name)
                            {
                                order = "";
                                break;
                            }
                        }
                    }

                    nodeAgesList = nodeAgesList + node.getNodeHeight(false) + "," + leafList.Count + "," + genus + "," + family + "," + order + Environment.NewLine;
                }
            }

            DebugTextForm frm = new DebugTextForm(nodeAgesList);
            frm.Show();
        }

        private void studyCountByNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder nodeList = new StringBuilder();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Nodes.Count > 0)
                {
                    nodeList.AppendLine(node.Text + "," + node.ChildDivergences.Count);
                }
            }

            DebugTextForm form = new DebugTextForm(nodeList.ToString());
            form.Show();
        }

        private void nodeCountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //int countNodes = activeTree.nodeList.Count(x => x.TreeView != null);
            int countResolvedNodes = activeTree.nodeList.Count(x => (x.TreeView != null && x.TaxonName == "" && x.Nodes.Count > 0));
            int countRemainingPolytomies = activeTree.nodeList.Count(x => (x.Parent != null && ((TopoTimeNode)x.Parent).getNodeHeight(false) == ((TopoTimeNode)x).getNodeHeight(false)));
            int countNegativeBranches = activeTree.nodeList.Count(x => (((TopoTimeNode)x).LevelsFromNegativeBranch == 1));

            string message = "Resolved Nodes: " + countResolvedNodes + Environment.NewLine;
            message = message + "Remaining Polytomies: " + countRemainingPolytomies + Environment.NewLine;
            message = message + "Negative Branches Fixed: " + countNegativeBranches;

            DebugTextForm textForm = new DebugTextForm(message);
            textForm.Show();
        }

        private void filterTreeByTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            string filename;

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = openDialog.FileName;
                HashSet<string> taxaList = new HashSet<string>();
                HashSet<int> idList = new HashSet<int>();

                int strippedCount = 0;
                int remainingCount = 0;

                using (Stream file = File.Open(filename, FileMode.Open))
                {
                    StreamReader fileStream = new StreamReader(file);
                    String line;

                    while ((line = fileStream.ReadLine()) != null)
                    {
                        string name = line.Replace("\"", "").Trim();
                        int id = 0;

                        if (int.TryParse(name, out id))
                            idList.Add(id);
                        else
                            taxaList.Add(name);
                    }
                }

                List<TopoTimeNode> trashList = new List<TopoTimeNode>();
                foreach (TopoTimeNode species in activeTree.leafList)
                {
                    bool inIdList = idList.Contains(species.TaxonID);
                    bool inTaxaList = taxaList.Contains(species.TaxonName);

                    if (!inIdList && !inTaxaList)
                    {
                        trashList.Add(species);

                        if (inIdList)
                            idList.Remove(species.TaxonID);

                        if (inTaxaList)
                            taxaList.Remove(species.TaxonName);
                    }
                }

                treeViewer.Nodes.Clear();
                foreach (TopoTimeNode species in trashList)
                {
                    activeTree.DeleteNode(species);
                    strippedCount++;
                }

                string unusedList = "";
                foreach (string taxon in taxaList)
                {
                    unusedList = unusedList + taxon + " unused" + Environment.NewLine;
                }

                foreach (int taxon in idList)
                {
                    unusedList = unusedList + taxon + " unused" + Environment.NewLine;
                }

                remainingCount = taxaList.Count + idList.Count;

                unusedList = strippedCount + " removed" + Environment.NewLine + unusedList;
                unusedList = remainingCount + " remaining" + Environment.NewLine + unusedList;

                treeViewer.Nodes.Add(activeTree.root);

                DebugTextForm unusedForm = new DebugTextForm(unusedList);
                unusedForm.Show();
            }
        }

        private void showSuspectNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (suspectWindow == null || suspectWindow.IsDisposed)
                suspectWindow = new SuspectForm();

            List<TopoTimeNode> suspects = activeTree.nodeList.Cast<TopoTimeNode>().ToList().FindAll(
                delegate (TopoTimeNode arg)
                {
                    return arg.Nodes.Count > 0 && arg.TreeView != null;

                    /*
                    bool suspect = false;

                    if (arg.PartitionData != null)
                        suspect = suspect || (!(arg.PartitionData.FavorAB > arg.PartitionData.FavorAC && arg.PartitionData.FavorAB > arg.PartitionData.FavorBC));

                    if (arg.Parent != null)
                        suspect = suspect || (arg.LevelsFromNegativeBranch > -1);

                    suspect = suspect || (arg.Nodes.Count > 0) && (arg.DivergenceRatio >= 5 || arg.getNodeHeight(true) == 0);

                    return suspect;
                     */
                });

            suspectWindow.setList(suspects);
            suspectWindow.Show();
        }

        private int LinnaeanRank(string rank)
        {
            switch (rank)
            {
                case "superkingdom":
                    return 0;
                case "kingdom":
                    return 1;
                case "subkingdom":
                    return 2;
                case "superphylum":
                    return 3;
                case "phylum":
                    return 4;
                case "subphylum":
                    return 5;
                case "superclass":
                    return 6;
                case "class":
                    return 7;
                case "subclass":
                    return 8;
                case "infraclass":
                    return 8;
                case "superorder":
                    return 9;
                case "order":
                    return 10;
                case "suborder":
                    return 11;
                case "parvorder":
                    return 11;
                case "infraorder":
                    return 11;
                case "superfamily":
                    return 12;
                case "family":
                    return 13;
                case "subfamily":
                    return 14;
                case "tribe":
                    return 15;
                case "subtribe":
                    return 16;
                case "genus":
                    return 17;
                case "subgenus":
                    return 18;
                case "species group":
                    return 19;
                case "species":
                    return 20;
                case "subspecies":
                    return 21;
                case "forma":
                    return 21;
                case "species subgroup":
                    return 21;
                case "varietas":
                    return 21;
                case "no rank":
                    return -1;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Error Checking > Show Nested Linnaean Groups
        /// Generally no longer necessary since partition rearrangment appropriate breaks up groups instead of nesting them
        /// </summary>
        private void showNestedLinnaeanGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HashSet<Tuple<TopoTimeNode, TopoTimeNode>> suspects = new HashSet<Tuple<TopoTimeNode, TopoTimeNode>>();
            NpgsqlConnection conn = getConnection();

            foreach (TopoTimeNode leaf in activeTree.leafList)
            {
                int highestRank = 21;
                TopoTimeNode ancestralNode = (TopoTimeNode)leaf.Parent;
                TopoTimeNode lastChild = leaf;


                while (ancestralNode != null)
                {
                    if (ancestralNode.TaxonName != "" && ancestralNode.TaxonID > 0)
                    {
                        // get the rank
                        string sql = "SELECT rank FROM ncbi_taxonomy WHERE taxon_id = '" + ancestralNode.TaxonID + "'";
                        DataTable sqlResult = GetSQLResult(sql, conn);

                        if (sqlResult.Rows.Count > 0)
                        {
                            string rank = sqlResult.Rows[0][0].ToString();
                            if (LinnaeanRank(rank) > 0)
                            {
                                if (LinnaeanRank(rank) >= highestRank)
                                {
                                    Tuple<TopoTimeNode, TopoTimeNode> pair = new Tuple<TopoTimeNode, TopoTimeNode>(ancestralNode, lastChild);
                                    if (!suspects.Contains(pair))
                                        suspects.Add(pair);
                                }
                                else
                                    highestRank = LinnaeanRank(rank);
                            }
                        }
                        lastChild = ancestralNode;
                    }

                    ancestralNode = (TopoTimeNode)ancestralNode.Parent;
                }
            }

            string results = "";
            foreach (Tuple<TopoTimeNode, TopoTimeNode> pair in suspects)
            {
                results = results + pair.Item1.Text + "," + pair.Item2.Text + Environment.NewLine;
            }

            DebugTextForm suspectForm = new DebugTextForm(results);
            suspectForm.Show();
        }

        private void showSpeciesSubsetSuspectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<TopoTimeNode> suspects = activeTree.leafList.Cast<TopoTimeNode>().ToList().FindAll(
                delegate (TopoTimeNode arg)
                {
                    bool suspect = false;

                    if (arg.TaxonName.Contains("."))
                        return true;

                    if (arg.TaxonName.IndexOf(" ") != arg.TaxonName.LastIndexOf(" "))
                        return true;

                    if (arg.TaxonName.Contains("0"))
                        return true;
                    if (arg.TaxonName.Contains("1"))
                        return true;
                    if (arg.TaxonName.Contains("2"))
                        return true;
                    if (arg.TaxonName.Contains("3"))
                        return true;
                    if (arg.TaxonName.Contains("4"))
                        return true;
                    if (arg.TaxonName.Contains("5"))
                        return true;
                    if (arg.TaxonName.Contains("6"))
                        return true;
                    if (arg.TaxonName.Contains("7"))
                        return true;
                    if (arg.TaxonName.Contains("8"))
                        return true;
                    if (arg.TaxonName.Contains("9"))
                        return true;


                    return suspect;
                });

            string suspectList = "";
            foreach (TopoTimeNode suspect in suspects)
            {
                suspectList = suspectList + "\"" + suspect.TaxonName + ",\"" + ((TopoTimeNode)suspect.Parent).getNodeHeight(false) + Environment.NewLine;
            }

            DebugTextForm textForm = new DebugTextForm(suspectList);
            textForm.Show();
        }

        private void nodeStatisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable statTable = new DataTable();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();
            statTable.Columns.Add();

            try
            {
                NpgsqlConnection conn = getConnection();

                statTable.Rows.Add("Taxa", "Node Time From Studies", "Adjusted Node Time", "Study Count", "Min Time", "Max Time", "95% CI Lower", "95% CI Upper", "Publication IDs");

                foreach (TopoTimeNode node in activeTree.nodeList)
                {
                    if (node.Nodes.Count == 0)
                        continue;

                    /*
                        1. Node identification (Node name from NCBI, or Taxon A/ Taxon B; whatever works best)
                        2. Node time
                        3. Number of studies
                        4. Minimum time
                        5. Maximum time
                        6. 95% CI (+/- 1.96 SE of mean), lower time
                        7. 95% CI (+/- 1.96 SE of mean), upper time
                        8. The PubMed IDs of each study (for each node, they could be written in a single cell, separated by commas)
                     */

                    string identifier = node.GetHashCode().ToString();
                    if (node.TaxonName != "")
                    {
                        int bracket = node.Text.IndexOf(']') + 1;
                        if (bracket == 1)
                            bracket = node.Text.Length;

                        identifier = node.Text.Substring(0, bracket);
                    }

                    string smallestDivergenceTime = "-";
                    string largestDivergenceTime = "-";

                    if (node.SmallestDivergenceNode != null)
                        smallestDivergenceTime = node.SmallestDivergenceNode.DivergenceTime.ToString();

                    if (node.LargestDivergenceNode != null)
                        largestDivergenceTime = node.LargestDivergenceNode.DivergenceTime.ToString();

                    string pubmedList = "";
                    foreach (ChildPairDivergence div in node.ChildDivergences)
                    {
                        pubmedList = pubmedList + div.PublicationID + ",";
                    }

                    double mean = node.getNodeHeight(false);
                    double stderror = node.getNodeStandardError(false);

                    statTable.Rows.Add(identifier, node.getNodeHeight(false), node.storedAdjustedHeight, node.ChildDivergences.Count, smallestDivergenceTime, largestDivergenceTime, (mean + stderror * 1.96).ToString(), (mean - stderror * 1.96).ToString(), pubmedList);
                }

                DebugForm newForm = new DebugForm(statTable);
                newForm.Show();
            }
            finally { }
        }
        

        private void DissolveBelowFamily(TopoTimeTree selectedTree)
        {
            treeViewer.Nodes.RemoveAt(0);
            TreeEditingService.dissolveBelowLevel(18, selectedTree, DBService);
            treeViewer.Nodes.Add(selectedTree.root);
        }

        private void trimToClassToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            TrimToLevelWrapper(activeTree, "class");
        }

        private void trimToOrderToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            TrimToLevelWrapper(activeTree, "order");
        }

        private void trimToFamilyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            TrimToLevelWrapper(activeTree, "family");
        }
            
        private void trimToGenusToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            TrimToLevelWrapper(activeTree, "genus");
        }

        private void trimToPhylumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TrimToLevelWrapper(activeTree, "phylum");
        }

        private void TrimToLevelWrapper(TopoTimeTree selectedTree, string rank)
        {
            treeViewer.Nodes.RemoveAt(0);
            SuppressRedrawFunction(delegate ()
            {                
                activeTree = TreeEditingService.trimToLevel(rank, selectedTree, DBService);                
            });
            treeViewer.Nodes.Add(activeTree.root);
        }

        private void removeAllDivergenceDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.Nodes.RemoveAt(0);
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.Nodes.Count > 0)
                {
                    node.ChildDivergences.Clear();
                    node.Text = node.TaxonName + " [" + node.TaxonID + "] (" + node.getNodeHeight(false) + ") {0}";
                }
            }
            treeViewer.Nodes.Add(activeTree.root);
        }

        

        private void setConfidenceInterval(TopoTimeNode node, NpgsqlConnection conn)
        {
            node.MinConfidenceInterval = 0;
            node.MaxConfidenceInterval = 0;
            int confCount = 0;

            foreach (ChildPairDivergence divergence in node.ChildDivergences)
            {
                int phyNodeID = divergence.PhylogenyNodeID;

                if (phyNodeID == 0)
                    continue;

                string publication = divergence.PublicationID;
                string sql = "SELECT f_time_min, f_time_max, f_time_estimate FROM phylogeny_node WHERE i_phylogeny_node_id=" + phyNodeID + ";";
                DataTable table = GetSQLResult(sql, conn);

                if (table.Rows.Count == 0 || table.Rows[0][0].GetType() == typeof(DBNull) || table.Rows[0][1].GetType() == typeof(DBNull)) { }
                else
                {
                    double studyTime = (double)table.Rows[0][2];
                    double newMinCI = (double)table.Rows[0][0] * (double)node.storedAdjustedHeight / studyTime;
                    double newMaxCI = (double)table.Rows[0][1] * (double)node.storedAdjustedHeight / studyTime;

                    node.MinConfidenceInterval = node.MinConfidenceInterval + newMinCI;
                    node.MaxConfidenceInterval = node.MaxConfidenceInterval + newMaxCI;

                    confCount++;
                }
            }

            if (confCount > 0)
            {
                node.MinConfidenceInterval = node.MinConfidenceInterval / confCount;
                node.MaxConfidenceInterval = node.MaxConfidenceInterval / confCount;
            }
        }

        private void confidenceIntervalCoverageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();

            Dictionary<string, int> coverageList = new Dictionary<string, int>();
            Dictionary<TopoTimeNode, int> countCoverage = new Dictionary<TopoTimeNode, int>();
            Dictionary<int, bool> checkedNodes = new Dictionary<int, bool>();
            string results = "";

            List<TopoTimeNode> parents = activeTree.nodeList.Cast<TopoTimeNode>().ToList().FindAll(x => x.Nodes.Count > 0);

            int total = parents.Count;
            int countHasCI = 0;
            int progressCount = 0;

            foreach (TopoTimeNode node in parents)
            {
                List<string> toAdd = new List<string>();
                bool found = false;
                foreach (ChildPairDivergence divergence in node.ChildDivergences)
                {
                    int phyNodeID = divergence.PhylogenyNodeID;

                    if (phyNodeID == 0)
                        continue;

                    string publication = divergence.PublicationID;

                    /*
                    if (checkedNodes.ContainsKey(phyNodeID))
                    {
                        if (checkedNodes[phyNodeID])
                        {
                            //results = results + node.Text + " " + phyNodeID + Environment.NewLine;
                            countHasCI++;
                            found = true;
                            break;
                        }
                        else
                        {
                            toAdd.Add(publication);                            
                        }
                    }
                    else
                     */
                    {
                        string sql = "SELECT f_time_min, f_time_max, f_time_estimate FROM phylogeny_node WHERE i_phylogeny_node_id=" + phyNodeID + ";";
                        DataTable table = GetSQLResult(sql, conn);

                        if (table.Rows.Count == 0 || table.Rows[0][0].GetType() == typeof(DBNull) || table.Rows[0][1].GetType() == typeof(DBNull))
                        {
                            //checkedNodes[phyNodeID] = false;
                            toAdd.Add(publication);
                        }
                        else
                        {
                            if (!countCoverage.ContainsKey(node))
                                countCoverage[node] = 0;

                            countCoverage[node]++;

                            double studyTime = (double)table.Rows[0][2];
                            double newMinCI = (double)table.Rows[0][0] * node.StoredAdjustedHeight / studyTime;
                            double newMaxCI = (double)table.Rows[0][1] * node.StoredAdjustedHeight / studyTime;

                            //if (newMinCI > node.MinConfidenceInterval)
                            node.MinConfidenceInterval = node.MinConfidenceInterval + newMinCI;

                            //if (newMaxCI < node.MaxConfidenceInterval)
                            node.MaxConfidenceInterval = node.MaxConfidenceInterval + newMaxCI;

                            //results = results + node.Text + " " + phyNodeID + Environment.NewLine;
                            countHasCI++;
                            found = true;
                            //checkedNodes[phyNodeID] = true;
                        }
                    }
                }

                if (!found)
                {
                    //results = results + node.Text + " " + Environment.NewLine;
                    foreach (string publication in toAdd)
                    {
                        if (!coverageList.ContainsKey(publication))
                            coverageList[publication] = 0;

                        coverageList[publication]++;
                    }
                }

                progressCount++;
            }

            foreach (KeyValuePair<TopoTimeNode, int> pair in countCoverage)
            {
                pair.Key.MinConfidenceInterval = pair.Key.MinConfidenceInterval / pair.Value;
                pair.Key.MaxConfidenceInterval = pair.Key.MaxConfidenceInterval / pair.Value;
                results = results + pair.Key.Text + "," + pair.Key.storedAdjustedHeight + "," + pair.Value + "," + pair.Key.MinConfidenceInterval + "," + pair.Key.MaxConfidenceInterval + Environment.NewLine;
            }

            results = results + "Total Nodes = " + total + Environment.NewLine + "Covered Nodes = " + countHasCI + Environment.NewLine;

            foreach (TopoTimeNode otherNode in parents.FindAll(x => !countCoverage.Keys.Contains(x)))
            {
                results = results + otherNode.Text + "," + otherNode.storedAdjustedHeight + Environment.NewLine;
            }

            //foreach (KeyValuePair<string, int> pair in coverageList)
            //    results = results + pair.Key + "," + pair.Value + Environment.NewLine;

            DebugTextForm debug = new DebugTextForm(results);
            debug.Show();
        }

        private void taxaIDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

            UserInputForm heightEdit = new UserInputForm(selectedNode.TaxonID.ToString());

            if (heightEdit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedNode.TaxonID = ((int)heightEdit.newValue);
            }
        }

        private void collapseToHardPolytomyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            treeViewer.Nodes[0].Remove();
            List<TopoTimeNode> trashList = new List<TopoTimeNode>();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                TopoTimeNode parent = (TopoTimeNode)node.Parent;
                if (parent != null && node.Nodes.Count > 0 && node.storedAdjustedHeight == parent.storedAdjustedHeight)
                {
                    //if (parent.TaxaName != "")
                    {
                        while (node.Nodes.Count > 0)
                        {
                            TopoTimeNode child = (TopoTimeNode)node.Nodes[0];
                            node.Nodes.Remove(child);
                            parent.Nodes.Add(child);
                        }
                        trashList.Add(node);
                    }
                }
            }

            foreach (TopoTimeNode trashNode in trashList)
                activeTree.DeleteNode(trashNode);

            treeViewer.Nodes.Add(activeTree.root);
        }

        private void showDuplicateTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            activeTree.leafDictionary = new Dictionary<int, TopoTimeNode>();

            HashSet<TopoTimeNode> duplicateList = new HashSet<TopoTimeNode>();
            foreach (TopoTimeNode leaf in activeTree.leafList.Where(x => x.TaxonID != -1 && x.Parent != null))
            {
                if (activeTree.leafDictionary.ContainsKey(leaf.TaxonID))
                {
                    duplicateList.Add(leaf);
                    if (!duplicateList.Contains(activeTree.leafDictionary[leaf.TaxonID]))
                        duplicateList.Add(activeTree.leafDictionary[leaf.TaxonID]);
                }
                else
                    activeTree.leafDictionary[leaf.TaxonID] = leaf;
            }

            treeViewer.Nodes.RemoveAt(0);

            List<TopoTimeNode> leafList = duplicateList.ToList();
            foreach (TopoTimeNode leaf in leafList)
                activeTree.DeleteNode(leaf);

            treeViewer.Nodes.Add(activeTree.root);


            StringBuilder namedAncestors = new StringBuilder();

            foreach (TopoTimeNode leaf in duplicateList)
            {
                namedAncestors.AppendLine(leaf.TaxonName + "|" + leaf.ListNamedAncestors());
            }

            DebugTextForm debugAncestors = new DebugTextForm(namedAncestors.ToString());
            debugAncestors.Show();
        }

        public TreeNode VisibleMetadata(TopoTimeTree hostTree, ChildPairDivergence metadata)
        {
            List<MyTuple<string, string>> StatsData = metadata.StatsData;

            Study includedStudy = null;
            string studyCitation = metadata.PublicationID;

            if (hostTree.includedStudies != null)
                hostTree.includedStudies.TryGetValue(metadata.CitationID, out includedStudy);

            if (includedStudy != null)
                studyCitation = includedStudy.ID;

            TreeNode root = new TreeNode(((double)metadata.DivergenceTime).ToString("0.0") + " [" + metadata.CitationID + "," + studyCitation + "]");
            if (StatsData != null)
                foreach (Tuple<string, string> fields in StatsData)
                    root.Nodes.Add(new TreeNode(fields.Item1 + " - " + fields.Item2));

            if (includedStudy != null)
            {
                root.Nodes.Add(new TreeNode("author - " + includedStudy.Author));
                root.Nodes.Add(new TreeNode("year - " + includedStudy.Year));
            }

            TreeNode groupA = new TreeNode("Taxa Group A");
            if (metadata.TaxaGroupA != null)
                foreach (string taxa in metadata.TaxaGroupA)
                    groupA.Nodes.Add(new TreeNode(taxa));

            if (hostTree.includedTaxa != null)
                if (metadata.TaxonIDsA != null)
                    foreach (int taxa in metadata.TaxonIDsA)
                    {
                        string taxonLabel;
                        if (!hostTree.includedTaxa.TryGetValue(taxa, out taxonLabel))
                            taxonLabel = taxa.ToString();
                        groupA.Nodes.Add(new TreeNode(taxonLabel));
                    }

            TreeNode groupB = new TreeNode("Taxa Group B");
            if (metadata.TaxaGroupB != null)
                foreach (string taxa in metadata.TaxaGroupB)
                    groupB.Nodes.Add(new TreeNode(taxa));

            if (hostTree.includedTaxa != null)
                if (metadata.TaxonIDsB != null)
                    foreach (int taxa in metadata.TaxonIDsB)
                    {
                        string taxonLabel;
                        if (!hostTree.includedTaxa.TryGetValue(taxa, out taxonLabel))
                            taxonLabel = taxa.ToString();
                        groupB.Nodes.Add(new TreeNode(taxonLabel));
                    }


            root.Nodes.Add(groupA);
            root.Nodes.Add(groupB);

            groupA.Expand();
            groupB.Expand();
            //root.ExpandAll();

            return root;
        }


        private void RetrieveTaxaIDsFromTaxonomy(int selectTaxonomy)
        {
            treeViewer.Nodes.RemoveAt(0);

            NpgsqlConnection conn = getConnection();

            int taxaIDTemp;
            string sql;
            DataTable table;

            string queryRetrieveName = "";
            string queryRetrieveID = "";
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                switch (selectTaxonomy)
                {
                    // NCBI
                    case 0:
                        queryRetrieveName = "SELECT scientific_name FROM ncbi_taxonomy WHERE taxon_id = ";
                        queryRetrieveID = "SELECT taxon_id FROM ncbi_taxonomy WHERE scientific_name LIKE E'";
                        break;
                    // GBIF
                    case 1:
                        queryRetrieveName = "SELECT scientific_name FROM gbif_taxa_nodes WHERE taxon_key = ";
                        queryRetrieveID = "SELECT taxon_key FROM gbif_taxa_nodes WHERE scientific_name LIKE E'";
                        break;
                    // OpenTree
                    case 2:
                        queryRetrieveName = "SELECT scientific_name FROM otol_taxa_nodes WHERE taxa_id = ";
                        queryRetrieveID = "SELECT taxa_id FROM otol_taxa_nodes WHERE scientific_name LIKE E'";
                        break;
                }

                if (int.TryParse(node.TaxonName, out taxaIDTemp))
                {
                    node.TaxonID = taxaIDTemp;
                    node.Text = node.TaxonName;

                    sql = queryRetrieveName + node.TaxonID + "LIMIT 1;";
                    table = GetSQLResult(sql, conn);
                    if (table.Rows.Count > 0)
                    {
                        node.TaxonName = table.Rows[0][0].ToString();
                        node.Text = node.TaxonName + " [" + node.TaxonID + "]";
                    }
                }
                else
                {
                    node.TaxonName = node.TaxonName.Replace('_', ' ');
                    sql = queryRetrieveID + node.TaxonName.Replace("'", "\\'") + "';";

                    table = MainForm.GetSQLResult(sql, conn);
                    if (table.Rows.Count > 0)
                    {
                        node.TaxonID = (int)table.Rows[0][0];
                        node.Text = node.TaxonName + " [" + node.TaxonID + "]";
                    }
                    else
                    {
                        node.TaxonID = -1;
                        node.Text = node.TaxonName;
                    }
                }
            }

            treeViewer.Nodes.Add(activeTree.root);
        }

        /// <summary>
        /// Edit > Retrieve Taxonomy IDs
        /// </summary>
        /// <param name="selectTaxonomy"></param>
        private void usingNCBIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RetrieveTaxaIDsFromTaxonomy(0);
        }

        private void listBifurcationsByOriginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder results = new StringBuilder();

            foreach (TopoTimeNode node in activeTree.nodeList)
                if (node.Nodes.Count > 0)
                    if (node.ChildDivergences.Count > 0)
                        results.AppendLine(node.storedAdjustedHeight + "," + node.Nodes.Count + "," + "TRUE" + ",");
                    else
                        results.AppendLine(node.storedAdjustedHeight + "," + node.Nodes.Count + ",");

            /*if (node.ChildDivergences.Count > 0)
                results = results + node.storedAdjustedHeight + "," + node.Nodes.Cast<TreeNode>().Count(x => x.Nodes.Count > 0) + "," + node.ChildDivergences[0].StatsData[0].Item2 + "," + Environment.NewLine;
            else
                results = results + node.storedAdjustedHeight + "," + node.Nodes.Cast<TreeNode>().Count(x => x.Nodes.Count > 0) + "," + Environment.NewLine;
            */
            DebugTextForm mTimes = new DebugTextForm(results.ToString());
            mTimes.Show();
        }

        private void copyNodeTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode selectedNode = treeViewDivTimes.SelectedNode;
            string nodeText = "";

            if (selectedNode != null)
            {
                foreach (TreeNode node in selectedNode.Nodes)
                    nodeText = nodeText + node.Text + Environment.NewLine;

                Clipboard.SetText(nodeText);
            }
        }

        private void removeAllLeavesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                treeViewer.Nodes.RemoveAt(0);

                for (int i = selectedNode.Nodes.Count - 1; i >= 1; i--)
                {
                    TopoTimeNode currentNode = (TopoTimeNode)selectedNode.Nodes[i];
                    if (selectedNode.Nodes.Count == 2)
                    {
                        TopoTimeNode lastNode = i == 0 ? (TopoTimeNode)selectedNode.Nodes[1] : (TopoTimeNode)selectedNode.Nodes[0];
                        if (!currentNode.isContributingTimeData())
                            activeTree.DeleteNode(currentNode);
                        selectedNode = lastNode;
                    }
                    else
                    {
                        if (!currentNode.isContributingTimeData())
                            activeTree.DeleteNode(currentNode);
                    }
                }

                //MessageBox.Show(selectedNode.Nodes.Count + " nodes remain.");                

                treeViewer.Nodes.Add(activeTree.root);
                treeViewer.SelectedNode = selectedNode;
                treeViewer.SelectedNode.EnsureVisible();
            }
        }


        private void recalculateTimesOnNodeDeletionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            recalculateTimesOnDelete = !recalculateTimesOnDelete;
        }        

        private void listAllLeavesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Text files (*.txt)|*.txt";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    StreamWriter writerStream = new StreamWriter(file);

                    foreach (TopoTimeNode leaf in activeTree.leafList)
                    {
                        if (leaf.TreeView != null)
                            writerStream.WriteLine(leaf.TaxonID);
                    }

                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        // ??? what was this for
        private void pruneOrphanedUnresolvedTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.Nodes.RemoveAt(0);

            List<TopoTimeNode> nodeList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.Nodes.Count > 0 && (x.ChildDivergences.Count == 0 || x.ChildDivergences[0].TaxaGroupA.Count == 0)).ToList();

            foreach (TopoTimeNode selectedNode in nodeList)
            {
                List<TopoTimeNode> childList = selectedNode.Nodes.Cast<TopoTimeNode>().ToList();
                List<TopoTimeNode> isNonContributing = new List<TopoTimeNode>();

                foreach (TopoTimeNode child in childList)
                {
                    if (!child.isContributingTimeData())
                        isNonContributing.Add(child);
                }

                if (childList.Count != isNonContributing.Count)
                    foreach (TopoTimeNode child in isNonContributing)
                        activeTree.DeleteNode(child);
            }

            //MessageBox.Show(selectedNode.Nodes.Count + " nodes remain.");                

            treeViewer.Nodes.Add(activeTree.root);
        }

        

        private void pruneTreeByTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            string filename;

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = openDialog.FileName;
                Dictionary<string, bool> taxaList = new Dictionary<string, bool>();

                using (Stream file = File.Open(filename, FileMode.Open))
                {
                    StreamReader fileStream = new StreamReader(file);
                    String line;

                    while ((line = fileStream.ReadLine()) != null)
                    {
                        taxaList.Add(line.Replace("\"", "").Trim(), false);
                    }
                }

                treeViewer.BeginUpdate();

                List<TopoTimeNode> trashList = new List<TopoTimeNode>();
                foreach (TopoTimeNode species in activeTree.leafList)
                {
                    if (taxaList.ContainsKey(species.TaxonName))
                    {
                        trashList.Add(species);
                        taxaList[species.TaxonName] = true;
                    }
                }

                foreach (TopoTimeNode species in trashList)
                {
                    activeTree.DeleteNode(species);
                }

                string unusedList = "";
                foreach (KeyValuePair<string, bool> pair in taxaList)
                {
                    if (pair.Value == false)
                        unusedList = unusedList + pair.Key + " unused" + Environment.NewLine;
                }

                treeViewer.EndUpdate();

                DebugTextForm unusedForm = new DebugTextForm(unusedList);
                unusedForm.Show();
            }
        }

        private void listAllNodeRanksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialogA = new SaveFileDialog();
            saveDialogA.Filter = "Text files (*.txt)|*.txt";
            saveDialogA.Title = "Save Rank File...";
            string filenameA;

            SaveFileDialog saveDialogB = new SaveFileDialog();
            saveDialogB.Filter = "Text files (*.txt)|*.txt";
            saveDialogB.Title = "Save Map File...";
            string filenameB;

            SaveFileDialog saveDialogC = new SaveFileDialog();
            saveDialogC.Filter = "Newick files (*.nwk)|*.nwk";
            saveDialogC.Title = "Save Tree File...";
            string filenameC;

            if (saveDialogA.ShowDialog() == System.Windows.Forms.DialogResult.OK && saveDialogB.ShowDialog() == System.Windows.Forms.DialogResult.OK && saveDialogC.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filenameA = saveDialogA.FileName;
                filenameB = saveDialogB.FileName;
                filenameC = saveDialogC.FileName;

                using (Stream fileA = File.Open(filenameA, FileMode.Create))
                {
                    using (Stream fileB = File.Open(filenameB, FileMode.Create))
                    {
                        StreamWriter writerStreamA = new StreamWriter(fileA);
                        StreamWriter writerStreamB = new StreamWriter(fileB);

                        NpgsqlConnection conn = getConnection();
                        int i = 1;
                        foreach (TopoTimeNode node in activeTree.nodeList)
                        {
                            node.UniqueID = i;
                            if (node.TaxonID != -1)
                            {
                                //string sql = "SELECT rank FROM otol_taxa_nodes WHERE taxa_id=" + node.TaxaID;
                                string sql = "SELECT rank FROM ncbi_taxonomy WHERE taxon_id=" + node.TaxonID;
                                string taxaRank = (string)getSingleSQL(sql, conn);

                                writerStreamA.WriteLine(i + "=" + taxaRank);
                                writerStreamB.WriteLine(i + "=" + node.TaxonName);
                            }
                            else
                                writerStreamA.WriteLine(i + "=");
                            i++;
                        }

                        writerStreamA.Flush();
                        writerStreamA.Close();

                        writerStreamB.Flush();
                        writerStreamB.Close();
                    }
                }

                using (Stream fileC = File.Open(filenameC, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(TopoTimeNode.TreeWritingMode.UniqueIDs);

                    StreamWriter writerStreamC = new StreamWriter(fileC);

                    writerStreamC.Write(tree);
                    writerStreamC.Write(";");
                    writerStreamC.Flush();
                    writerStreamC.Close();
                }
            }
        }

        private void rearrangeUnnamedNodesOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();
            if (conn == null)
                return;

            treeViewer.Nodes.RemoveAt(0);

            DataTable table = new DataTable();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();
            table.Columns.Add();

            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.PartitionData != null)
                {
                    TopoTimeNode outgroup;
                    TopoTimeNode parent = (TopoTimeNode)node.Parent;

                    if (parent != null)
                    {

                        if (parent.Nodes[1] == node)
                            outgroup = (TopoTimeNode)parent.Nodes[0];
                        else
                            outgroup = (TopoTimeNode)parent.Nodes[1];

                        if (node.PartitionData.FavorAB < node.PartitionData.FavorAC && node.PartitionData.FavorAC >= node.PartitionData.FavorBC)
                        {
                            if (node.TaxonID <= 0 || node.PartitionData.FavorAB + 2 < node.PartitionData.FavorAC && node.PartitionData.FavorAC >= node.PartitionData.FavorBC)
                            {
                                TopoTimeNode tempB = (TopoTimeNode)node.Nodes[1];
                                node.Nodes.Remove(tempB);
                                parent.Nodes.Remove(outgroup);

                                parent.Nodes.Add(tempB);
                                node.Nodes.Add(outgroup);

                                node.getLeaves(true);
                                double oldTime = node.getNodeHeight(false);
                                double newTime;

                                HALService.setTimesHAL(node);
                                newTime = node.getNodeHeight(false);

                                HALService.setTimesHAL(parent);

                                table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                                node.ForeColor = Color.Blue;
                                node.rearranged = true;
                            }
                        }
                        else if (node.PartitionData.FavorAB < node.PartitionData.FavorBC)
                        {
                            if (node.TaxonID <= 0 || node.PartitionData.FavorAB + 2 < node.PartitionData.FavorBC)
                            {
                                TopoTimeNode tempA = (TopoTimeNode)node.Nodes[0];
                                node.Nodes.Remove(tempA);
                                parent.Nodes.Remove(outgroup);

                                parent.Nodes.Add(tempA);
                                node.Nodes.Add(outgroup);

                                node.getLeaves(true);
                                double oldTime = node.getNodeHeight(false);
                                double newTime;

                                HALService.setTimesHAL(node);
                                newTime = node.getNodeHeight(false);

                                HALService.setTimesHAL(parent);

                                table.Rows.Add(node.Text, oldTime, newTime, parent.getNodeHeight(false));

                                node.ForeColor = Color.Blue;
                                node.rearranged = true;
                            }
                        }
                    }
                }
            }
            treeViewer.Nodes.Add(activeTree.root);
            DebugForm debug = new DebugForm(table);
            debug.Show();
        }

        private void listIncorrectlyRearrangedNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (suspectWindow == null || suspectWindow.IsDisposed)
                suspectWindow = new SuspectForm();

            List<TopoTimeNode> suspects = activeTree.nodeList.Cast<TopoTimeNode>().ToList().FindAll(
                delegate (TopoTimeNode arg)
                {
                    if (arg.PartitionData != null)
                        return (arg.TaxonID >= 0 && !(arg.PartitionData.FavorAB > arg.PartitionData.FavorAC && arg.PartitionData.FavorAB > arg.PartitionData.FavorBC));
                    else
                        return false;

                    /*
                    bool suspect = false;

                    

                    if (arg.Parent != null)
                        suspect = suspect || (arg.LevelsFromNegativeBranch > -1);

                    suspect = suspect || (arg.Nodes.Count > 0) && (arg.DivergenceRatio >= 5 || arg.getNodeHeight(true) == 0);

                    return suspect;
                     */
                });

            suspectWindow.setList(suspects);
            suspectWindow.Show();
        }



        

        

        

        

        

        private void exportTreeValidateCompatibleNewickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    root.getLeaves(false);
                    String tree = root.writeNode(TopoTimeNode.TreeWritingMode.TreeValidateNewick);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        private void exportNewickTreePartitionConsensusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save Parition Consensus Tree";
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(TopoTimeNode.TreeWritingMode.PartitionPercent);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }

            saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save Parition Consensus Count Tree";
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(TopoTimeNode.TreeWritingMode.PartitionConsensus);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }


        private void dissolveUnclassifiedGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.TaxonName != null && (x.TaxonName.StartsWith("unclassified ") || x.TaxonName.Contains("ncertae "))).ToList())
            {
                DissolveNovelBifurcations(node);

                TopoTimeNode namedParent = node.Parent;
                while (namedParent.TaxonID == 0)
                {
                    namedParent = namedParent.Parent;
                }
                List<TopoTimeNode> children = node.Nodes.Cast<TopoTimeNode>().ToList();

                DissolveNovelBifurcations(namedParent);

                foreach (TopoTimeNode child in children)
                {
                    node.Nodes.Remove(child);
                    namedParent.Nodes.Add(child);
                }

                namedParent.Nodes.Remove(node);
            }

            activeTree.OperationHistory.Add("Unclassified & incertae sedis groups dissolved");

            treeViewer.Nodes.Add(root);
        }

        private void removePartitionTaxonDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                if (node.PartitionData != null)
                {
                    foreach (StudyData data in node.PartitionData.studyData)
                    {
                        data.taxaGroupA.Clear();
                        data.taxaGroupB.Clear();
                        data.taxaGroupC.Clear();

                        data.TaxaAinA.Clear();
                        data.TaxaBinB.Clear();
                        data.TaxaCinC.Clear();
                        data.TaxaAinB.Clear();
                        data.TaxaBinA.Clear();
                        data.TaxaAinC.Clear();
                        data.TaxaBinC.Clear();
                        data.TaxaCinA.Clear();
                        data.TaxaCinB.Clear();
                    }
                }

                foreach (ChildPairDivergence divergence in node.ChildDivergences)
                {
                    divergence.ClearMetadataTaxa();
                }
            }
        }

        private void recalculateAffectedNodeTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();
            List<TopoTimeNode> affectedList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.ForeColor.ToArgb() == Color.Blue.ToArgb()).ToList();
            foreach (TopoTimeNode node in affectedList)
            {
                HALService.setTimesHAL(node);
                if (node.Parent != null)
                {
                    HALService.setTimesHAL(node.Parent);
                }
            }
        }

        private void collapseSingleChildNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.Nodes.RemoveAt(0);
            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.Nodes.Count == 1))
            {
                TopoTimeNode parent = node.Parent;
                if (parent != null)
                {
                    TopoTimeNode child = (TopoTimeNode)node.Nodes[0];
                    node.Nodes.Clear();
                    parent.Nodes.Remove(node);
                    parent.Nodes.Add(child);
                }
            }
            treeViewer.Nodes.Add(activeTree.root);
        }

        private void removeNonTaxaLeavesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.BeginUpdate();
            foreach (TopoTimeNode leaf in activeTree.leafList.Where(x => x.TaxonID <= 0).ToList())
            {
                activeTree.DeleteNode(leaf);
            }
            treeViewer.EndUpdate();
        }

        private void sanityCheckToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder errorLog = new StringBuilder();
            foreach (TopoTimeNode node in activeTree.nodeList)
            {
                foreach (TopoTimeNode child in node.Nodes)
                {
                    if (child.Parent != node)
                        errorLog.AppendLine(node.Text + " " + child.Text);
                }
            }

            DebugTextForm textForm = new DebugTextForm(errorLog.ToString());
            textForm.Show();
        }

        private void collapseDuplicatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(CollapseTreeDuplicates);
        }

        private void CollapseTreeDuplicates()
        {
            treeViewer.BeginUpdate();
            treeViewer.Nodes.RemoveAt(0);
            DebugTextForm dtf = new DebugTextForm(TreeEditingService.CollapseDuplicateTaxaGroups(activeTree, activeTree.root));
            dtf.Show();
            treeViewer.Nodes.Add(activeTree.root);
            treeViewer.EndUpdate();
        }

        private void deleteDivergenceDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (treeViewDivTimes.SelectedNode.Tag is ChildPairDivergence && treeViewer.SelectedNode != null)
            {
                ChildPairDivergence divToRemove = (ChildPairDivergence)treeViewDivTimes.SelectedNode.Tag;
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                selectedNode.ChildDivergences.Remove(divToRemove);
                activeTree.UpdateNodeText(selectedNode);

                treeViewer_AfterSelect(null, null);
            
            }
        }

        private void TestMonophylyConsensus(TopoTimeNode node)
        {
            //if (!node.HasValidTaxon)
            //    return;

            NpgsqlCommand cmd = new NpgsqlCommand("SELECT i_citation_num, i_phylogeny_node_id, f_time_estimate, study_taxa_found, study_taxa_total, coverage, array_length(outgroup_taxa, 1) AS outgroup_size, array_to_string(outgroup_taxa, ',') FROM evaluate_monophyly(@taxonIDs,@rootTaxonID);", DBService.DBConnection);
            cmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            cmd.Parameters.Add("rootTaxonID", NpgsqlDbType.Integer);
            cmd.Prepare();

            List<Tuple<string, bool>> summary = new List<Tuple<string, bool>>();

            IEnumerable<int> taxonIDs = node.getNamedChildren().Select(x => x.TaxonID);
            cmd.Parameters[0].Value = taxonIDs.ToArray();
            cmd.Parameters[1].Value = node.TaxonID;

            DataTable table = DBService.GetSQLResult(cmd);

            double threshold = 0.5;
            int authorityCount = 0;
            int nonMonoCount = 0;
            int totalStudies = 0;
            foreach (DataRow row in table.Rows)
            {
                totalStudies++;
                double coverage = (double)row["coverage"];
                int outgroupSize = !(row["outgroup_size"] is DBNull) ? (int)row["outgroup_size"] : 0;

                if (coverage > threshold)
                {
                    authorityCount++;
                    if (outgroupSize > 0)
                        nonMonoCount++;
                }
            }

            MessageBox.Show($"{nonMonoCount} of {authorityCount} valid studies ({totalStudies} total) suggest dissolution.");

            DebugForm dbf = new DebugForm(table, title: node.TaxonName + " Monophyly Summary");
            dbf.Show();

            /*
            foreach (TopoTimeNode child in node.Nodes)
            {
                IEnumerable<int> taxonIDs = child.getNamedChildren().Select(x => x.TaxonID);
                cmd.Parameters[0].Value = taxonIDs.ToArray();

                DataTable table = DBService.GetSQLResult(cmd);

                bool shouldSplit = false;

                foreach (DataRow row in table.Rows)
                {
                    int studyTaxaFound = (int)row["study_taxa_found"];
                    int studyTaxaTotal = (int)row["study_taxa_total"];

                    if (studyTaxaTotal > studyTaxaFound)
                        shouldSplit = true;
                }

                if (shouldSplit)
                    child.ForeColor = Color.Orange;

                Tuple<string, bool> result = new Tuple<string, bool>(child.TaxonName, shouldSplit);
                summary.Add(result);
            }

            DebugForm dbf = new DebugForm(summary, title: node.TaxonName + " Monophyly Summary");
            dbf.Show();

            */

        }

        private void testMonophylyConsensusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();

            StringBuilder conflictLog = new StringBuilder();

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.TaxonID > 0 && x.Nodes.Count > 0))
            {
                IEnumerable<int> taxonIDs = node.getNamedChildren().Select(x => x.TaxonID);
                string sql = "SELECT * from contains_outgroup(ARRAY[" + String.Join(",", taxonIDs) + "]);";
                DataTable table = GetSQLResult(sql, conn);

                bool foundMRCA = false;
                int rootSize = 0;
                int[] lastNodes = null;
                int lastCitation = 0;
                int lastPhylogenyNode = 0;

                for (int m = 0; m < table.Rows.Count; m++)
                {
                    if (table.Rows[m][1].GetType() == typeof(DBNull))
                    {
                        rootSize = (int)table.Rows[m][5];
                        foundMRCA = false;
                    }

                    // iterate through the node list until we reach a node that doesn't contain all the relevant taxa from the root
                    // this means we've a) found the MRCA and b) found one child of the MRCA
                    if ((int)table.Rows[m][5] < rootSize && !foundMRCA)
                    {
                        foundMRCA = true;
                        List<int> outgroupTaxa = lastNodes.AsEnumerable<int>().Except(taxonIDs).ToList();

                        //Study study = new Study(lastCitation.ToString(), 0, lastCitation.ToString(), 0, "");
                        StudyData studyData;

                        if (outgroupTaxa.Count > 0)
                        {
                            studyData = new StudyData(0, 1, 0, lastPhylogenyNode.ToString(), CitationID: lastCitation);
                            studyData.taxaGroupA = lastNodes.AsEnumerable<int>().Except(outgroupTaxa).Select(x => x.ToString()).ToList();
                            studyData.taxaGroupB = new List<string>();
                            studyData.taxaGroupC = outgroupTaxa.Select(x => x.ToString()).ToList();                            
                        }
                        else
                        {
                            studyData = new StudyData(1, 0, 0, lastPhylogenyNode.ToString(), CitationID: lastCitation);
                            studyData.taxaGroupA = lastNodes.AsEnumerable<int>().Select(x => x.ToString()).ToList();
                            studyData.taxaGroupB = new List<string>();
                            studyData.taxaGroupC = new List<string>();
                        }

                        if (node.PartitionData == null)
                        {
                            node.PartitionData = new SplitData();
                            node.PartitionData.studyData = new List<StudyData>();
                        }

                        node.PartitionData.studyData.Add(studyData);
                    }

                    lastNodes = (int[])table.Rows[m][4];
                    lastCitation = (int)table.Rows[m][3];
                    lastPhylogenyNode = (int)table.Rows[m][0];
                }
            }

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.TaxonID > 0 && x.Nodes.Count > 0))
            {
                if (node.Parent != null)
                {
                    Dictionary<string, List<string>> votes = new Dictionary<string, List<string>>();
                    foreach (TopoTimeNode child in node.Nodes)
                    {
                        if (child.PartitionData == null)
                            continue;

                        foreach (StudyData studyData in child.PartitionData.studyData)
                        {
                            foreach (string taxaID in studyData.taxaGroupC)
                            {
                                if (!votes.ContainsKey(taxaID))
                                    votes[taxaID] = new List<string>();

                                votes[taxaID].Add(child.TaxonName);
                            }
                        }
                    }

                    foreach (TopoTimeNode child in node.Nodes)
                    {
                        if (child.PartitionData == null)
                            continue;

                        foreach (StudyData studyData in child.PartitionData.studyData)
                        {
                            foreach (string taxaID in studyData.taxaGroupA)
                            {
                                if (votes.ContainsKey(taxaID))
                                    votes[taxaID].Add("no change");
                            }
                        }
                    }

                    if (votes.Count > 0)
                    {
                        conflictLog.AppendLine("Node " + node.TaxonName + ":");
                        foreach (KeyValuePair<string, List<string>> keyValuePair in votes)
                        {
                            var votingGroups = keyValuePair.Value.GroupBy(x => x);
                            foreach (IGrouping<string, string> group in votingGroups)
                            {
                                if (!votingGroups.Any(x => x.Key == "no change") || group.Count() > votingGroups.First(x => x.Key == "no change").Count())
                                    conflictLog.AppendLine(keyValuePair.Key + " => " + group.First() + ": " + group.Count());
                            }
                        }
                    }
                }
            }

            DebugTextForm dtf = new DebugTextForm(conflictLog.ToString());
            dtf.Show();
        }

        private int CountExpandedNodes(TreeNode node)
        {
            if (node.Nodes.Count == 0 || !node.IsExpanded)
                return 1;

            return node.Nodes.Cast<TreeNode>().Sum(x => CountExpandedNodes(x)) + 1;
        }

        

        

        private void OnTreeLoad()
        {

            treeViewer.BeginUpdate();

            if (treeViewer.Nodes.Count > 0)
                treeViewer.Nodes.RemoveAt(0);

            if (activeTree.root != null)
            {
                activeTree.root.ExpandAll();
                treeViewer.Nodes.Add(activeTree.root);
            }

            treeViewer.EndUpdate();

            if (activeTree.UseMedianTimes)
                lblMedianStatus.Text = "Tree uses median times";
            else
                lblMedianStatus.Text = "Tree uses mean times";
        }

        

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            subTaxaWindow.Close();
        }






        

        private void ResolveByHALMethod()
        {
            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            List<TopoTimeNode> indexedNodeList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.Nodes.Count > 2).ToList();

            int unresolvedPartitions = indexedNodeList.Sum(x => x.Nodes.Count - 2);
            int remainingPartitions = unresolvedPartitions;
            toolStripStatusLabel1.Text = "";

            //System.IO.StreamWriter logFile = null;
            //string logLocation = ".\\resolution_log.txt";
            try
            {
                //logFile = new System.IO.StreamWriter(logLocation, false);
            }
            catch
            {

            }


            HALService = new HALFunctions(DBService, activeTree);

            //StringBuilder log = new StringBuilder();
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

                        TopoTimeNode resolvedRoot = HALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: true);

                        if (resolvedRoot != null)
                        {
                            TopoTimeNode newNode = resolvedRoot;
                            newNode.Expand();
                            //newNode.Text = newNode.getNodeHeight(true).ToString("0.00") + " {" + newNode.ChildDivergences.Count + "}";
                            newNode.UpdateText();
                            newNode.ForeColor = Color.ForestGreen;
                            //newNode.NodeFont = new Font(treeViewer.Font, FontStyle.Bold);

                            //timeMatrix = resolvedRoot.Item2;

                            activeTree.AddNewNodeToTree(newNode);
                        }
                    }

                    parentNode.UpdateText();
                    parentNode.ForeColor = Color.ForestGreen;


                }
                else if (parentNode.Nodes.Count == 2 && !chkBoxUpdateTimes.Checked)
                    HALService.setTimesHAL(parentNode);
            }

            activeTree.OperationHistory.Add("Tree fully resolved using old HAL");

            /*
            if (logFile != null)
            {
                logFile.Write(log.ToString());
                logFile.Flush();
                logFile.Close();
            }
            */
            OnTreeLoad();
        }

        private void byHALMethodToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(ResolveByHALMethod);
        }

        private void SpliceCompressedTree(Stream stream, TopoTimeNode selectedNode)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry archivedFile = archive.Entries.First(x => x.FullName.EndsWith(".tss"));
                SpliceTree(archivedFile.Open(), selectedNode);
            }
        }

        private void SpliceTree(Stream stream, TopoTimeNode selectedNode)
        {
            TopoTimeTree spliceTree = null;

            try
            {
                XmlAttributeOverrides overrideList = new XmlAttributeOverrides();
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlIgnore = true;
                overrideList.Add(typeof(ChildPairDivergence), "metadata", attrs);

                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(SerializableNode), overrideList);
                SerializableNode rootData = (SerializableNode)x.Deserialize(stream);
                spliceTree = new TopoTimeTree();

                TopoTimeNode root = rootData.DeserializedNode(spliceTree);

                

                spliceTree.refreshList = new HashSet<TopoTimeNode>();
                spliceTree.root = root;
            }
            catch
            {
                BinaryFormatter bf = new BinaryFormatter();
                object obj = bf.Deserialize(stream);

                if (obj.GetType() == typeof(TopoTimeTree))
                {
                    spliceTree = (TopoTimeTree)obj;
                }
                else if (obj.GetType() == typeof(TopoTimeNode))
                {
                    spliceTree = new TopoTimeTree();
                    spliceTree.root = (TopoTimeNode)obj;
                }
            }

            activeTree.AddNodesToTree(spliceTree.root);


            selectedNode.Nodes.Add(spliceTree.root);
            selectedNode.ExpandAll();
        }

        private void fromExistingSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.Filter = "Tree sessions (*.tss, *.tsz)|*.tss;*.tsz";
                string filename;

                if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    filename = openDialog.FileName;

                    using (Stream file = File.Open(filename, FileMode.Open))
                    {
                        if (filename.EndsWith(".tss"))
                            SpliceTree(file, selectedNode);
                        else if (filename.EndsWith(".tsz") || filename.EndsWith(".zip"))
                            SpliceCompressedTree(file, selectedNode);

                        /*
                        foreach (ExtendedNode arg in spliceTree.nodeList)
                        {
                            if (arg.PartitionData != null &&
                               (arg.PartitionData.FavorAB == arg.PartitionData.FavorAC && arg.PartitionData.FavorAC >= arg.PartitionData.FavorBC ||
                                arg.PartitionData.FavorAB == arg.PartitionData.FavorBC && arg.PartitionData.FavorBC >= arg.PartitionData.FavorAC))
                            {
                                arg.ForeColor = Color.DimGray;
                            }
                        }
                         */
                    }
                }
            }
        }

        private void newNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode newNode = new TopoTimeNode();
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                newNode.Text = "Custom Node";
                selectedNode.Nodes.Add(newNode);
            }
            else if (treeViewer.Nodes.Count == 0)
            {                
                newNode.Text = "Custom Node";
                treeViewer.Nodes.Add(newNode);
                activeTree = new TopoTimeTree();
                activeTree.root = newNode;
            }

            activeTree.AddNewNodeToTree(newNode, addLeaf: false);
        }

        private void SaveFileFunction(string filter, Action<Stream> function)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {                   

                    function(file);
                }
            }
        }

        private void toNewickTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileFunction("Newick files (*.nwk)|*.nwk", delegate (Stream file)
            {
                StreamWriter writerStream = new StreamWriter(file);

                TopoTimeNode root = (TopoTimeNode)treeViewer.SelectedNode;
                String tree = root.writeNode(TopoTimeNode.TreeWritingMode.Timed);
                writerStream.Write(tree);
                writerStream.Write(";");
                writerStream.Flush();
                writerStream.Close();
            });
        }

        private void toTopoTimeSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Compressed tree sessions (*.tsz)|*.tsz|Tree sessions (*.tss)|*.tss";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                TopoTimeNode root = (TopoTimeNode)treeViewer.SelectedNode;
                TreeIOService.SaveTreeFile(filename, activeTree, root);
            }
        }

        

        private void byHALMeanTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HALService = new HALFunctions(DBService, activeTree);

            HALService.RootToTipResolution((TopoTimeNode)treeViewer.SelectedNode);
        }

        private void byHALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int lastCount = -1;

            HALService = new HALFunctions(DBService, activeTree);

            while (treeViewer.SelectedNode.Nodes.Count > 2 && treeViewer.SelectedNode.Nodes.Count != lastCount)
            {
                lastCount = treeViewer.SelectedNode.Nodes.Count;
                HALService.RootToTipResolution((TopoTimeNode)treeViewer.SelectedNode);
            }
        }

        private void byFastHALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                ResolveByFastHAL(HALFunctions.TopologyConflictMode.NoConflicts);
                CleanTree();
            });
        }

        private void byFastHALIncludeTopologicalConflictTimesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate() 
            {
                ResolveByFastHAL(HALFunctions.TopologyConflictMode.UseConflictsIfNecessary);
                CleanTree();
            });
        }

        private void ResolveLocalByFastHAL(TopoTimeTree tree, TopoTimeNode parentNode, HALFunctions LocalHALService, PartitionFetcher fetcher = null, HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts, double supportThreshold = 0.5, int guideTree = 0)
        {
            PartitionFetcher fastHALfetcher = fetcher;
            if (fastHALfetcher == null)
                fastHALfetcher = new PartitionFetcher(DBService);

            if (parentNode.Nodes.Count > 2)
            {
                parentNode.ChildDivergences.Clear();
                int lastCount = -1;

                bool suppressSubspecies = false;

                while (parentNode.Nodes.Count > 2 && parentNode.Nodes.Count != lastCount)
                {
                    lastCount = parentNode.Nodes.Count;

                    TopoTimeNode resolvedRoot = LocalHALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, skipNestedNodes: suppressSubspecies, supportThreshold: supportThreshold, guideTree: guideTree);

                    if (resolvedRoot != null)
                    {
                        tree.AddNewNodeToTree(resolvedRoot);
                    }
                    else if (!suppressSubspecies)
                    {
                        // retry but suppress subspecies
                        suppressSubspecies = true;

                        resolvedRoot = LocalHALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, skipNestedNodes: suppressSubspecies, supportThreshold: supportThreshold, guideTree: guideTree);
                        if (resolvedRoot != null)
                            tree.AddNewNodeToTree(resolvedRoot);
                    }
                }

                LocalHALService.setTimesHAL(parentNode, UseConflicts: UseConflicts);
            }
            else if (parentNode.Nodes.Count == 2 && !chkBoxUpdateTimes.Checked)
                LocalHALService.setTimesHAL(parentNode, UseConflicts: UseConflicts);
        }

        private void ResolveByFastHAL(HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts, bool timedHAL = true, double SupportThreshold = 0.5, int guideTreeID = 0)
        {
            NpgsqlConnection conn = getConnection();
            if (conn == null)
                return;

            HALService = new HALFunctions(DBService, activeTree);

            List<TopoTimeNode> indexedNodeList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.Nodes.Count > 2).OrderBy(y => y.Level).ToList();

            int unresolvedPartitions = indexedNodeList.Sum(x => x.Nodes.Count - 2);
            int remainingPartitions = unresolvedPartitions;

            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            //double supportThreshold = 0.25;

            //StringBuilder log = new StringBuilder();
            for (int i = 0; i < indexedNodeList.Count; i++)
            {
                TopoTimeNode parentNode = indexedNodeList[i];
                DateTime beginNodeResolutionClock = DateTime.Now;

                ResolveSelectedNode(parentNode, fastHALfetcher, UseConflicts: UseConflicts, timedHAL: timedHAL, SupportThreshold: SupportThreshold, guideTreeID: guideTreeID);
            }

            if (UseConflicts == HALFunctions.TopologyConflictMode.NoConflicts)
                activeTree.OperationHistory.Add("Tree fully resolved using Fast HAL (excluding topological conflict times)");
            else
                activeTree.OperationHistory.Add("Tree fully resolved using Fast HAL (including topological conflict times)");

            /*
            if (logFile != null)
            {
                logFile.Write(log.ToString());
                logFile.Flush();
                logFile.Close();
            }
            */

            OnTreeLoad();
        }

        private void ResolveSelectedNode(TopoTimeNode parentNode, PartitionFetcher fastHALfetcher, HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts, bool timedHAL = true, double SupportThreshold = 0.5, int guideTreeID = 0)
        {
            if (parentNode.Nodes.Count > 2)
            {
                parentNode.ChildDivergences.Clear();
                int lastCount = -1;

                bool suppressSubspecies = false;

                while (parentNode.Nodes.Count > 2 && parentNode.Nodes.Count != lastCount)
                {
                    lastCount = parentNode.Nodes.Count;
                    //RootToTipResolution(parentNode, conn, null, true);

                    TopoTimeNode resolvedRoot = HALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: timedHAL, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, skipNestedNodes: suppressSubspecies, supportThreshold: SupportThreshold, guideTree: guideTreeID);
                    //ExtendedNode resolvedRoot = HALService.ResolveFastHAL(parentNode, null, fastHALfetcher, timedHAL: true, useOnlyConcordant: useOnlyConcordantTimes);

                    if (resolvedRoot != null)
                    {
                        activeTree.AddNewNodeToTree(resolvedRoot);
                    }
                    else if (!suppressSubspecies)
                    {
                        // retry but suppress subspecies
                        suppressSubspecies = true;

                        resolvedRoot = HALService.ResolveHAL(parentNode, null, parentNode.Nodes.Count, log: null, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, skipNestedNodes: suppressSubspecies, supportThreshold: SupportThreshold, guideTree: guideTreeID);
                        if (resolvedRoot != null)
                            activeTree.AddNewNodeToTree(resolvedRoot);
                    }
                }

                HALService.setTimesHAL(parentNode, UseConflicts: UseConflicts);
            }
            else if (parentNode.Nodes.Count == 2 && !chkBoxUpdateTimes.Checked)
                HALService.setTimesHAL(parentNode, UseConflicts: UseConflicts);
        }

        private void addDivergenceTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

                UserInputForm heightEdit = new UserInputForm("");
                if (heightEdit.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectedNode.ChildDivergences.Add(new ChildPairDivergence("placeholder", "placeholder", (double)heightEdit.newValue));
                    selectedNode.UpdateText();
                }
            }
        }

        private void deleteAllDivergenceDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode node = (TopoTimeNode)treeViewer.SelectedNode;
                if (node.Nodes.Count > 0)
                {
                    node.ChildDivergences.Clear();
                    activeTree.UpdateNodeText(node);
                }
            }
        }

        private void dissolveLocalNonNCBIResolutionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
            if (selectedNode != null)
            {
                DissolveNovelBifurcations(selectedNode);
            }
        }

        private void dissolveToLeavesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.BeginUpdate();

            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            TreeEditingService.reduceNodeToLeaves(selectedNode);
            //CleanTree();

            HardReloadActiveTree(root);

            //treeViewer.Nodes.Add(root);

            treeViewer.EndUpdate();
        }

        private void byHALPartitionRankingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                List<TopoTimeNode> indexedNodeList = activeTree.nodeList.Cast<TopoTimeNode>().Where(x => x.Nodes.Count > 2).ToList();

                int unresolvedPartitions = indexedNodeList.Sum(x => x.Nodes.Count - 2);
                int remainingPartitions = unresolvedPartitions;


                TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                treeViewer.Nodes.Remove(root);

                System.IO.StreamWriter logFile = null;
                string logLocation = ".\\resolution_log.txt";
                try
                {
                    logFile = new System.IO.StreamWriter(logLocation, false);
                }
                catch
                {

                }

                // temporary options for disabling certain searches the PostgreSQL query planner
                // because it's dumb sometimes and I don't know how to fix it

                //getSingleSQL("SET enable_nestloop=OFF;", conn);
                //getSingleSQL("SET enable_indexscan=ON;", conn);

                HALService = new HALFunctions(DBService, activeTree);

                StringBuilder log = new StringBuilder();
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
                            HALService.RootToTipResolution(parentNode, log);
                        }

                        parentNode.UpdateText();


                    }
                    else if (parentNode.Nodes.Count == 2 && !chkBoxUpdateTimes.Checked)
                        HALService.setTimesHAL(parentNode);
                }

                if (logFile != null)
                {
                    logFile.Write(log.ToString());
                    logFile.Flush();
                    logFile.Close();
                }

                OnTreeLoad();
            });
        }

        private void analyzePartitionsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            NpgsqlConnection conn = getConnection();
            if (conn == null)
                return;

            if (treeViewer.SelectedNode != null && conn != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                DateTime now = DateTime.Now;
                treeViewer.BeginUpdate();

                TreeBuilderService.NodePartitionAnalysis(selectedNode, new PartitionFetcher(DBService), true);

                treeViewer.EndUpdate();
                this.toolStripStatusLabel1.Text = "Operation completed in " + (DateTime.Now - now).TotalSeconds.ToString() + " seconds.";
            }
        }

        private void listNamedNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;
                IEnumerable<TopoTimeNode> namedNodes = selectedNode.getNamedChildren();

                DebugTextForm dtf = new DebugTextForm(String.Join(Environment.NewLine, namedNodes.Select(x => x.Text)));
                dtf.Show();
            }
        }

        private void analyzePartitionsFastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                //getSingleSQL("SET enable_nestloop=OFF;", dbConnection);

                TreeBuilderService.FastPartitionAnalysis(activeTree, DBService);
            });
        }

        private void applyIterativeRearrangementsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RearrangeIndividuals();
        }

        private void RearrangeIndividuals()
        {
            NpgsqlConnection conn = getConnection();

            if (conn == null)
                return;

            List<TopoTimeNode> affectedNodes = activeTree.nodeList.Cast<TopoTimeNode>().Where(node => node.PartitionData != null && (node.PartitionData.FavorAB < node.PartitionData.FavorAC || node.PartitionData.FavorAB < node.PartitionData.FavorBC)).ToList();

            treeViewer.BeginUpdate();
            treeViewer.Nodes.RemoveAt(0);

            //while (affectedNodes.Count > 0)
            {
                foreach (TopoTimeNode node in affectedNodes)
                {
                    PartitionVotes singleTaxonChanges = node.SingleTaxonChanges(activeTree);

                    TopoTimeNode grandparent = node.Parent;

                    IEnumerable<TopoTimeNode> conflictedChildren = singleTaxonChanges.VotingResults;

                    foreach (TopoTimeNode conflictedChild in conflictedChildren)
                    {
                        conflictedChild.Parent.Nodes.Remove(conflictedChild);
                        grandparent.Nodes.Add(conflictedChild);
                    }
                }
            }

            treeViewer.Nodes.Add(activeTree.root);
            treeViewer.EndUpdate();

        }

        private void RearrangeGroupsSingleStep()
        {

            treeViewer.BeginUpdate();
            treeViewer.Nodes.RemoveAt(0);

            TreeBuilderService.SingleGroupRearrangement(activeTree, HALService, DBService);

            treeViewer.Nodes.Add(activeTree.root);
            treeViewer.EndUpdate();

        }
        

        private void RearrangeGroups()
        {
            treeViewer.BeginUpdate();
            treeViewer.Nodes.RemoveAt(0);

            TreeBuilderService.IterativeGroupRearrangments(activeTree, DBService);

            //DebugForm debug = new DebugForm(table);
            //debug.Show();

            treeViewer.Nodes.Add(activeTree.root);
            treeViewer.EndUpdate();
        }

        private void RearrangeAndResolve(TopoTimeNode node)
        {
            if (node.Parent == null || node.Parent.Parent == null || node.PartitionData == null)
                return;

            PartitionFetcher fetcher = HALService.partitionFetcher;

            Dictionary<TopoTimeNode, int> individualTaxaCount = new Dictionary<TopoTimeNode, int>();
            List<TopoTimeNode> namedNodes = node.getNamedChildren().ToList();
            foreach (StudyData partition in node.PartitionData.studyData)
            {
                foreach (string leafTaxa in partition.taxaGroupC)
                {
                    if (leafTaxa.Contains("[A]") || leafTaxa.Contains("[B]"))
                    {
                        IEnumerable<TopoTimeNode> mappedTaxa = namedNodes.Where(x => leafTaxa.Contains(x.TaxonName));
                        if (!mappedTaxa.Any())
                            continue;

                        TopoTimeNode mappedTaxon = mappedTaxa.First();

                        if (mappedTaxon != null)
                        {
                            if (!individualTaxaCount.ContainsKey(mappedTaxon))
                                individualTaxaCount[mappedTaxon] = 0;

                            individualTaxaCount[mappedTaxon]++;
                        }                        
                    }
                }
            }

            TopoTimeNode targetParent = node.Parent.Parent;

            foreach (KeyValuePair<TopoTimeNode, int> pair in individualTaxaCount)
            {
                //if (pair.Value > 3)
                {
                    TopoTimeNode affectedNode = pair.Key;
                    activeTree.MoveNode(affectedNode, targetParent);
                }
            }

            foreach (TopoTimeNode affectedNode in activeTree.refreshList)
            {
                TreeBuilderService.NodePartitionAnalysis(affectedNode, fetcher);
                affectedNode.ForeColor = Color.AliceBlue;
            }

            activeTree.refreshList.Clear();

            HALService.RootToTipResolution(targetParent, fastHALfetcher: fetcher);
            TreeBuilderService.NodePartitionAnalysis(targetParent, fetcher);
        }

        private void testRearrangementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

            if (selectedNode == null)
                return;

            HALService = new HALFunctions(DBService, activeTree);

            RearrangeAndResolve(selectedNode);
        }

        private void applyIterativeRearrangementsWholeGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RearrangeGroups();
        }


        private void byFastHALToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {

            FastHALSingleStep((TopoTimeNode)treeViewer.SelectedNode, UseConflicts: HALFunctions.TopologyConflictMode.UseConflictsIfNecessary);
            /*

            HALService = new HALFunctions(DBService, activeTree);
            StringBuilder matrixLog = new StringBuilder();

            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            UserInputForm uif = new UserInputForm("1", "Enter the number of resolutions to perform:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int resolutionCount = (int)uif.newValue;
                int i = 0;

                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

                while (i < resolutionCount && selectedNode.Nodes.Count > 2)
                {
                    HALService.RootToTipResolution(selectedNode, log: matrixLog, fastHALfetcher: fastHALfetcher);
                    i++;
                }

                DebugTextForm dtf = new DebugTextForm(matrixLog.ToString());
                dtf.Show();
            }
            */

                
        }


        private void collapseDatalessNodesIntoPolytomiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CleanTree();

            treeViewer.Nodes.RemoveAt(0);

            foreach (TopoTimeNode node in activeTree.nodeList.ToList())
            {
                TopoTimeNode parent = node.Parent;
                if (parent == null)
                    continue;

                if (node.ChildDivergences.Count == 0 && node.Nodes.Count > 0)
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
                        activeTree.DeleteNode(node);
                    }
                }
            }
                //activeTree.CollapseIntoParent(node);

            treeViewer.Nodes.Add(activeTree.root);
        }

        private void displayConflictSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder conflictLog = new StringBuilder();
            List<TopoTimeNode> affectedNodes = activeTree.nodeList.Cast<TopoTimeNode>().Where(node => node.PartitionData != null).ToList();
            foreach (TopoTimeNode node in affectedNodes)
            {
                PartitionVotes singleTaxonChanges = node.SingleTaxonChanges(activeTree);
                IEnumerable<TopoTimeNode> conflictedChildren = singleTaxonChanges.VotingResults;

                foreach (TopoTimeNode conflictedChild in conflictedChildren)
                {
                    conflictLog.AppendLine(node.TaxonName + "," + node.TaxonID + "," + conflictedChild.TaxonName);
                }
            }

            DebugTextForm dtf = new DebugTextForm(conflictLog.ToString());
            dtf.Show();
        }

        private async void generateFlatFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Zipped flat file set (*.zip)|*.zip";

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filepath = saveDialog.FileName;
                string filename = filepath.Split('\\').Last();

                IssueChecklist();

                DebugTextForm dtf = new DebugTextForm();
                var progressIndicator = new Progress<string>(dtf.ReportProgress);

                dtf.Show();
                

                await Task.Run(() =>
                    GenerateTTService.GenerateFullTree(filepath, filename, activeTree, DBService, progressIndicator));
            }         

            
        }        

        private void exportNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void reTimeSkippedNodesFastHALBugfixToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeViewer.Nodes.RemoveAt(0);

            foreach (TopoTimeNode parentNode in activeTree.nodeList.Where(x => x.HasValidTaxon && x.Nodes.Count > 0))
            {
                if (parentNode.ChildDivergences.Count == 0)
                    HALService.setTimesHAL(parentNode);
            }

            treeViewer.Nodes.Add(activeTree.root);
            
        }

        private void wholeGroupRearrangementSingleStepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RearrangeGroupsSingleStep();
        }

        private void recalculateTimeHALVersionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

                HALService = new HALFunctions(DBService, activeTree);
                HALService.setTimesHAL(selectedNode);
            }
        }

        private void familyLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DissolveBelowFamily(activeTree);
        }

        private void FastHALSingleStep(TopoTimeNode selectedNode, HALFunctions.TopologyConflictMode UseConflicts = HALFunctions.TopologyConflictMode.NoConflicts, double supportThreshold = 0.5, int guideTree = 0)
        {
            HALService = new HALFunctions(DBService, activeTree);
            StringBuilder matrixLog = new StringBuilder();

            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            UserInputForm uif = new UserInputForm("1", "Enter the number of resolutions to perform:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int resolutionCount = (int)uif.newValue;
                int i = 0;

                while (i < resolutionCount && selectedNode.Nodes.Count > 2)
                {
                    //HALService.RootToTipResolution(selectedNode, log: matrixLog, fastHALfetcher: fastHALfetcher, useOnlyConcordant: true);

                    TopoTimeNode resolvedRoot = HALService.ResolveHAL(selectedNode, null, selectedNode.Nodes.Count, log: matrixLog, supportThreshold: supportThreshold, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, guideTree: guideTree);
                    i++;

                    if (resolvedRoot != null)
                    {
                        activeTree.AddNewNodeToTree(resolvedRoot);
                        resolvedRoot.Expand();
                    }
                    else
                    {
                        // retry but suppress subspecies
                        resolvedRoot = HALService.ResolveHAL(selectedNode, null, selectedNode.Nodes.Count, log: matrixLog, supportThreshold: supportThreshold, timedHAL: true, fastHALfetcher: fastHALfetcher, UseConflicts: UseConflicts, skipNestedNodes: true, guideTree: guideTree);
                        if (resolvedRoot != null)
                        {
                            activeTree.AddNewNodeToTree(resolvedRoot);
                            resolvedRoot.Expand();
                        }
                    }
                }

                if (selectedNode.Nodes.Count == 2)
                    HALService.setTimesHAL(selectedNode, UseConflicts: HALFunctions.TopologyConflictMode.NoConflicts);

                //DebugTextForm dtf = new DebugTextForm(matrixLog.ToString());
                //dtf.Show();
            }
        }

        private void FastHALXSingleStep(TopoTimeNode selectedNode, bool excludeConflicts = true, double supportThreshold = 0.5, int GuideTreeID = 0)
        {
            HALService = new HALFunctions(DBService, activeTree);
            StringBuilder matrixLog = new StringBuilder();

            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            UserInputForm uif = new UserInputForm("1", "Enter the number of resolutions to perform:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int resolutionCount = (int)uif.newValue;
                int lastNodeCount = 0;
                int i = 0;

                while (i < resolutionCount && selectedNode.Nodes.Count > 2 && lastNodeCount != selectedNode.Nodes.Count)
                {
                    //HALService.RootToTipResolution(selectedNode, log: matrixLog, fastHALfetcher: fastHALfetcher, useOnlyConcordant: true);

                    lastNodeCount = selectedNode.Nodes.Count;

                    IEnumerable<TopoTimeNode> resolvedNodes = HALService.ResolveFastHALX(selectedNode, fastHALfetcher, log: matrixLog, supportThreshold: supportThreshold, timedHAL: true, useOnlyConcordant: excludeConflicts, GuideTreeID: GuideTreeID);
                    foreach (TopoTimeNode resolvedRoot in resolvedNodes)
                    {                        
                        activeTree.AddNewNodeToTree(resolvedRoot);
                        resolvedRoot.Expand();
                    }

                    i++;
                }

                if (selectedNode.Nodes.Count == 2)
                    HALService.setTimesHAL(selectedNode, UseConflicts: HALFunctions.TopologyConflictMode.NoConflicts);

                //DebugTextForm dtf = new DebugTextForm(matrixLog.ToString());
                //dtf.Show();
            }
        }

        private void FastHALXConsensusStep(TopoTimeNode selectedNode)
        {
            HALService = new HALFunctions(DBService, activeTree);
            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            Tuple<TopoTimeNode, TopoTimeNode> resolvedRoot = HALService.ResolveFastHAlXConsensus(selectedNode, fastHALfetcher);
        }

        private void PlainConsensusStep(TopoTimeNode selectedNode)
        {
            HALService = new HALFunctions(DBService, activeTree);
            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

            List<TopoTimeNode> NewNodes = HALService.ResolveUsingConsensus(selectedNode, fastHALfetcher);
        }
        private void byFastHALexcludeTopologicalConflictsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                FastHALSingleStep((TopoTimeNode)treeViewer.SelectedNode);
                CleanTree();
            });            
        }

        private void listPolyphyleticGroupMixingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugTextForm dtf = new DebugTextForm(activeTree.AuditPolyphyleticGroups());
            dtf.Show();
        }

        private void listPolyphyeticGroupMixingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

            DebugTextForm dtf = new DebugTextForm(activeTree.AuditPolyphyleticGroups(selectedNode));
            dtf.Show();
        }

        private void exportNewickTaxonIDsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Newick files (*.nwk)|*.nwk";
            string filename;

            if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = saveDialog.FileName;

                using (Stream file = File.Open(filename, FileMode.Create))
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    String tree = root.writeNode(TopoTimeNode.TreeWritingMode.TaxonIDs);

                    StreamWriter writerStream = new StreamWriter(file);

                    writerStream.Write(tree);
                    writerStream.Write(";");
                    writerStream.Flush();
                    writerStream.Close();
                }
            }
        }

        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugForm historyForm = new DebugForm(activeTree.OperationHistory.Select(o => new
            { Column1 = o }).ToList(), title: "Operation History");
            historyForm.Show();
        }

        private void listConfidenceIntervalsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

            List<double> times = selectedNode.ChildDivergences.Select(x => (double)x.DivergenceTime).ToList();

            if (times.Count() > 2)
            {
                Tuple<double, double> confIntervalNormal = Functions.TConfidenceInterval(times, 0.95);
                Tuple<double, double> confIntervalMedian = Functions.MedianConfidenceInterval(times);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(String.Join(",", times));
                sb.AppendLine("Normal CI (" + confIntervalNormal.Item1 + ", " + confIntervalNormal.Item2 + ")");
                sb.AppendLine("Median CI (" + confIntervalMedian.Item1 + ", " + confIntervalMedian.Item2 + ")");

                DebugTextForm dtf = new DebugTextForm(sb.ToString());
                dtf.Show();
            }
        }

        private void IssueChecklist()
        {
            // check for negative branches
            bool hasNegativeBranches = activeTree.nodeList.Where(x => x.getBranchLength() < 0).Any();
            if (hasNegativeBranches)
                MessageBox.Show("Warning: tree still contains negative branches.");
        }

        private void findNextNegativeBranchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (activeTree.SearchEnumerator == null || activeTree.SearchEnumerator.Current == null)
            {
                activeTree.SearchEnumerator = activeTree.nodeList.Where(x => x.getBranchLength() < 0).GetEnumerator();
            }


            activeTree.SearchEnumerator.MoveNext();
            ExtendedNode searchResult = activeTree.SearchEnumerator.Current;

            treeViewer.SelectedNode = searchResult;
            if (searchResult != null)
                searchResult.EnsureVisible();
            treeViewer.Focus();
        }

        private void recalculateRootTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeViewer.SelectedNode != null)
            {
                TopoTimeNode selectedNode = (TopoTimeNode)treeViewer.SelectedNode;

                HALService = new HALFunctions(DBService, activeTree);
                HALService.setTimesRootHAL(selectedNode);
            }
        }

        private void markNegativeBranchNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MarkNegativeBranchLengths();
        }

        private void exportStudyNodeTimeComparisonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("NodeID,NodeName,NodeAge,AdjustedAge,NamedParent,ConflictTimes,Kingdom,CitationID,StudyAge,StudyNodeID,StudyNodeCount,TopologicalConflict");
            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.Nodes.Count > 0))
            {
                string nodeID = node.GetHashCode().ToString();
                string nodeAge = activeTree.GetNodeHeight(node).ToString();
                string adjustedAge = node.StoredAdjustedHeight.ToString();
                string NamedParent = node.GetNamedParent.TaxonName;
                string NodeName = node.TaxonName;
                string Kingdom = node.GetKingdom;

                string ConflictTimes = "";
                bool UsesConflictTimes = false;

                if (node.ChildDivergences.Any())
                {
                    UsesConflictTimes = node.ChildDivergences.First().IsConflict;
                    ConflictTimes = UsesConflictTimes.ToString();
                }


                /*
                foreach (ChildPairDivergence div in node.ChildDivergences)
                {                    
                    string studyID = div.CitationID.ToString();
                    string studyTime = div.DivergenceTime.ToString();
                    string isConflict = div.IsConflict.ToString();
                    string studyNodeID = div.PhylogenyNodeIDs.First().ToString();
                    string countIDs = div.PhylogenyNodeIDs.Count.ToString();

                    sb.AppendLine($"{nodeID},{nodeAge},{adjustedAge},{studyID},{studyTime},{studyNodeID},{countIDs},{isConflict}");

                }
                */
                sb.AppendLine($"{nodeID},{NodeName},{nodeAge},{adjustedAge},{NamedParent},{ConflictTimes},{Kingdom}");
            }

            DebugTextForm dtf = new DebugTextForm(sb.ToString());
            dtf.Show();
        }

        private void byFastHALexcludeConflictsReducedSupportThresholdToolStripMenuItem_Click(object sender, EventArgs e)
        {        
            SuppressRedrawFunction(delegate ()
            {
                FastHALSingleStep((TopoTimeNode)treeViewer.SelectedNode, supportThreshold: 0.1);
                CleanTree();
            });
        }

        private void ResolveFullTreeFindOptimalStrategy()
        {
            HALService = new HALFunctions(DBService, activeTree);
            StringBuilder analysisLog = new StringBuilder();
            Dictionary<int, int> StrategyList = new Dictionary<int, int>();

            DebugTextForm dtf2 = new DebugTextForm("Enter a list of NCBI IDs, and strategy index separated by tab characters", "Enter NCBI IDs", textIsReadOnly: false);
            if (dtf2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] lines = dtf2.userText.Split('\n');
                foreach (string line in lines)
                {
                    string[] taxaInfo = line.Split('\t');
                    if (taxaInfo.Length == 2)
                    {
                        int TaxonID = 0;
                        int Strategy = 0;

                        if (int.TryParse(taxaInfo[0], out TaxonID) && int.TryParse(taxaInfo[1], out Strategy))
                        {
                            StrategyList[TaxonID] = Strategy;
                        }

                    }
                }
            }

            ClockingFunction(delegate ()
            {
                SuppressRedrawFunction(delegate ()
                {
                    TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                    treeViewer.Nodes.Remove(root);
                    foreach (TopoTimeNode NamedNode in activeTree.nodeList.Where(x => x.Nodes.Count > 2).ToList())
                    {
                        if (StrategyList.ContainsKey(NamedNode.TaxonID))
                        {
                            switch (StrategyList[NamedNode.TaxonID])
                            {
                                case 0:
                                    ResolveLocalByFastHAL(activeTree, NamedNode, HALService);
                                    TreeBuilderService.CleanTree(activeTree, removeViewless: false);

                                    if (analysisLog != null)
                                        analysisLog.AppendLine($"{NamedNode.TaxonName},{NamedNode.TaxonID},>50% threshold,{NamedNode.StoredAdjustedHeight},{NamedNode.getLeaves(false).Count()},{NamedNode.Nodes.Count}");
                                    break;
                                case 1:
                                    ResolveLocalByFastHAL(activeTree, NamedNode, HALService, supportThreshold: 0.499);
                                    TreeBuilderService.CleanTree(activeTree, removeViewless: false);

                                    if (analysisLog != null)
                                        analysisLog.AppendLine($"{NamedNode.TaxonName},{NamedNode.TaxonID},>=50% threshold,{NamedNode.StoredAdjustedHeight},{NamedNode.getLeaves(false).Count()},{NamedNode.Nodes.Count}");
                                    break;
                                case 2:
                                    Dictionary<int, double> coverage = HALService.CoverageAnalysis(NamedNode, null);
                                    int bestStudy = coverage.MaxBy(x => x.Value).Key;

                                    ResolveLocalByFastHAL(activeTree, NamedNode, HALService, guideTree: bestStudy);
                                    TreeBuilderService.CleanTree(activeTree, removeViewless: false);

                                    if (analysisLog != null)
                                        analysisLog.AppendLine($"{NamedNode.TaxonName},{NamedNode.TaxonID},guide tree,{NamedNode.StoredAdjustedHeight},{NamedNode.getLeaves(false).Count()},{NamedNode.Nodes.Count},{bestStudy},{coverage[bestStudy]}");
                                    break;
                                case 3:
                                    ResolveLocalByFastHAL(activeTree, NamedNode, HALService, UseConflicts: HALFunctions.TopologyConflictMode.UseConflictsIfNecessary);
                                    TreeBuilderService.CleanTree(activeTree, removeViewless: false);

                                    if (analysisLog != null)
                                        analysisLog.AppendLine($"{NamedNode.TaxonName},{NamedNode.TaxonID},using conflicts,{NamedNode.StoredAdjustedHeight},{NamedNode.getLeaves(false).Count()},{NamedNode.Nodes.Count}");
                                    break;



                            }
                        }
                        else
                            ResolveByAlternateStrategy(NamedNode, analysisLog);

                        NamedNode.ExpandAll();
                    }
                    treeViewer.Nodes.Add(root);
                });
            });

            DebugTextForm dtf = new DebugTextForm(analysisLog.ToString());
            dtf.Show();
        }

        private void ResolveByAlternateStrategy(TopoTimeNode SelectedNode, StringBuilder analysisLog = null)
        {
            Dictionary<int, double> coverage = HALService.CoverageAnalysis(SelectedNode, null);
            int bestStudy = coverage.MaxBy(x => x.Value).Key;

            // original preferred strategy: >50% support
            // commented out because this is already done by the default Fast HAL process
            /*
            ResolveLocalByFastHAL(activeTree, SelectedNode, HALService);
            TreeBuilderService.CleanTree(activeTree, removeViewless: false);

            if (analysisLog != null)
                analysisLog.AppendLine($"{SelectedNode.TaxonName},{SelectedNode.TaxonID},>50% threshold,{SelectedNode.StoredAdjustedHeight},{SelectedNode.getLeaves(false).Count()},{SelectedNode.Nodes.Count}");

            if (SelectedNode.Nodes.Count == 2)
                return;

            // if resolution fails, dissolve HAL-formed resolutions and try again
            DissolveNovelBifurcations(SelectedNode);
            */

            // first strategy tested: 50% support, inclusive 
            ResolveLocalByFastHAL(activeTree, SelectedNode, HALService, supportThreshold: 0.499);
            TreeBuilderService.CleanTree(activeTree, removeViewless: false);

            if (analysisLog != null)
                analysisLog.AppendLine($"{SelectedNode.TaxonName},{SelectedNode.TaxonID},>=50% threshold,{SelectedNode.StoredAdjustedHeight},{SelectedNode.getLeaves(false).Count()},{SelectedNode.Nodes.Count}");

            if (SelectedNode.Nodes.Count == 2)
                return;

            // if resolution fails, dissolve HAL-formed resolutions and try again
            DissolveNovelBifurcations(SelectedNode);

            // second strategy tested: use a guide tree 
            ResolveLocalByFastHAL(activeTree, SelectedNode, HALService, guideTree: bestStudy);
            TreeBuilderService.CleanTree(activeTree, removeViewless: false);

            if (analysisLog != null)
                analysisLog.AppendLine($"{SelectedNode.TaxonName},{SelectedNode.TaxonID},guide tree,{SelectedNode.StoredAdjustedHeight},{SelectedNode.getLeaves(false).Count()},{SelectedNode.Nodes.Count},{bestStudy},{coverage[bestStudy]}");
            if (SelectedNode.Nodes.Count == 2)
                return;

            // if resolution fails, dissolve HAL-formed resolutions and try again
            DissolveNovelBifurcations(SelectedNode);

            // final strategy: resolve with inclusion of topologically-conflicting data and occasional empty nodes
            // this is not considered optimal but must be done to resolve the tree

            ResolveLocalByFastHAL(activeTree, SelectedNode, HALService, UseConflicts: HALFunctions.TopologyConflictMode.UseConflictsIfNecessary);
            TreeBuilderService.CleanTree(activeTree, removeViewless: false);

            if (analysisLog != null)
                analysisLog.AppendLine($"{SelectedNode.TaxonName},{SelectedNode.TaxonID},using conflicts,{SelectedNode.StoredAdjustedHeight},{SelectedNode.getLeaves(false).Count()},{SelectedNode.Nodes.Count}");

        }

        private void analyzeResolutionOptimzationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder analysisLog = new StringBuilder();
            PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);
            foreach (TopoTimeNode NamedNode in activeTree.nodeList.Where(x => x.Nodes.Count > 2).ToList())
            {
                int rootNodeNum = NamedNode.TaxonID;

                // first round: species level, standard support threshold (>50%)
                TopoTimeTree tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: true, collapseSubspeciesGroups: true);
                HALFunctions HALServiceSpecies = new HALFunctions(DBService, tempTree);


                Dictionary<int, double> coverage = HALServiceSpecies.CoverageAnalysis(tempTree.root, null);
                int bestStudy = coverage.MaxBy(x => x.Value).Key;                               

                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSpecies);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},species level & >50% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");

                int nodeCount = tempTree.root.Nodes.Count;
                if (nodeCount == 2 && tempTree.root.StoredAdjustedHeight > 0)
                    continue;

                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: true, collapseSubspeciesGroups: true);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSpecies, guideTree: bestStudy);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},guide tree,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count},{bestStudy},{coverage[bestStudy]}");

                nodeCount = tempTree.root.Nodes.Count;
                if (nodeCount == 2 && tempTree.root.StoredAdjustedHeight > 0)
                    continue;

                // second round: subspecies level, standard support threshold (>50%)
                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: false, collapseSubspeciesGroups: true);
                HALFunctions HALServiceSubspecies = new HALFunctions(DBService, tempTree);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSubspecies);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},subspecies level & >50% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");

                nodeCount = tempTree.root.Nodes.Count;
                if (nodeCount == 2 && tempTree.root.StoredAdjustedHeight > 0)
                    continue;

                // third round: species level, reduced support threshold (>=50%)
                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: true, collapseSubspeciesGroups: true);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSpecies, supportThreshold: 0.499);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},species level & >=50% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");

                nodeCount = tempTree.root.Nodes.Count;
                if (nodeCount == 2 && tempTree.root.StoredAdjustedHeight > 0)
                    continue;

                // fourth round: subspecies level, reduced support threshold (>=50%)
                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: false, collapseSubspeciesGroups: true);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSubspecies, supportThreshold: 0.499);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},subspecies level & >=50% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");

                nodeCount = tempTree.root.Nodes.Count;
                if (nodeCount == 2 && tempTree.root.StoredAdjustedHeight > 0)
                    continue;

                /*
                // fifth round: species level, reduced support threshold (>30%)
                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: true, collapseSubspeciesGroups: true);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSpecies, supportThreshold: 0.1);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},species level & >10% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");

                // sixth round: subspecies level, reduced support threshold (>30%)
                tempTree = TreeBuilderService.GenerateBackbone(rootNodeNum, DBService, baseRank: "species", isMedian: true, storeSubspecies: false, collapseSubspeciesGroups: true);
                ResolveLocalByFastHAL(tempTree, tempTree.root, HALServiceSubspecies, supportThreshold: 0.1);
                TreeBuilderService.CleanTree(tempTree, removeViewless: false);
                analysisLog.AppendLine($"{NamedNode.TaxonName},{rootNodeNum},subspecies level & >10% threshold,{tempTree.root.StoredAdjustedHeight},{tempTree.root.getLeaves(false).Count()},{tempTree.root.Nodes.Count}");
                */
            }

            DebugTextForm dtf = new DebugTextForm(analysisLog.ToString());
            dtf.Show();
        }

        private void byFastHALexcludeConflicts50SupportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                FastHALSingleStep((TopoTimeNode)treeViewer.SelectedNode, supportThreshold: 0.49999);
                CleanTree();
            });
        }

        private void usingFastHALXexcludeConflictsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                FastHALXSingleStep((TopoTimeNode)treeViewer.SelectedNode);
                CleanTree();
            });            
        }

        private void usingFastHALXConsensusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                FastHALXConsensusStep((TopoTimeNode)treeViewer.SelectedNode);
                CleanTree();
            });
        }

        private void usingPlainConsensusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                PlainConsensusStep((TopoTimeNode)treeViewer.SelectedNode);
                CleanTree();
            });
        }

        private void byFastHALusingSingleGuideTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserInputForm uif = new UserInputForm("1", "Enter the citation ID of the desired guide tree:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SuppressRedrawFunction(delegate ()
                {
                    int guideTreeID = (int)uif.newValue;
                    FastHALSingleStep((TopoTimeNode)treeViewer.SelectedNode, guideTree: guideTreeID);
                });
            }                
        }

        private void analyzeStudyCoverageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StringBuilder analysisLog = new StringBuilder();

            HALService = new HALFunctions(DBService, activeTree);

            HALService.CoverageAnalysis((TopoTimeNode)treeViewer.SelectedNode, analysisLog);

            DebugTextForm dtf = new DebugTextForm(analysisLog.ToString());
            dtf.Show();
        }

        private void byFastHAL50SupportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                ResolveByFastHAL(HALFunctions.TopologyConflictMode.NoConflicts, SupportThreshold: 0.499);
                CleanTree();
            });
        }

        private void byFastHALusingGuideTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserInputForm uif = new UserInputForm("1", "Enter the citation ID of the desired guide tree:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int guideTreeID = (int)uif.newValue;

                ClockingFunction(delegate ()
                {
                    SuppressRedrawFunction(delegate ()
                    {
                        ResolveByFastHAL(HALFunctions.TopologyConflictMode.NoConflicts, guideTreeID: guideTreeID);
                        CleanTree();
                    });
                });
            }

            
        }

        private void byStudyConsensusToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void evaluateMonophylyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TestMonophylyConsensus((TopoTimeNode)treeViewer.SelectedNode);
        }

        private void dissolveUnsupportedNCBIGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NpgsqlCommand cmd = new NpgsqlCommand("SELECT i_citation_num, i_phylogeny_node_id, f_time_estimate, study_taxa_found, study_taxa_total, coverage, array_length(outgroup_taxa, 1) AS outgroup_size FROM evaluate_monophyly(@taxonIDs,@rootTaxonID);", DBService.DBConnection);
            cmd.Parameters.Add("taxonIDs", NpgsqlDbType.Array | NpgsqlDbType.Integer);
            cmd.Parameters.Add("rootTaxonID", NpgsqlDbType.Integer);
            cmd.Prepare();

            StringBuilder log = new StringBuilder();
            log.AppendLine($"TaxonName,TaxonID,ShouldDissolve,NonMonophyleticSupport,AuthorityCount,TotalStudies");

            treeViewer.BeginUpdate();

            TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
            treeViewer.Nodes.Remove(root);

            foreach (TopoTimeNode node in activeTree.nodeList.Where(x => x.HasValidTaxon && x.Nodes.Count > 0))
            {
                IEnumerable<int> taxonIDs = node.getNamedChildren().Select(x => x.TaxonID);
                cmd.Parameters[0].Value = taxonIDs.ToArray();
                cmd.Parameters[1].Value = node.TaxonID;

                if ((cmd.Parameters[0].Value as int[]).Length > 1000)
                {
                    log.AppendLine($"{node.TaxonName},{node.TaxonID},SKIPPED");
                    continue;
                }

                DataTable table = DBService.GetSQLResult(cmd);

                double threshold = 0.5;
                double authorityCount = 0;
                double nonMonoCount = 0;
                double totalStudies = 0;
                foreach (DataRow row in table.Rows)
                {
                    totalStudies++;
                    double coverage = (double)row["coverage"];
                    int outgroupSize = !(row["outgroup_size"] is DBNull) ? (int)row["outgroup_size"] : 0;

                    if (coverage > threshold)
                    {
                        authorityCount++;
                        if (outgroupSize > 0)
                            nonMonoCount++;
                    }
                }

                if (nonMonoCount >= authorityCount / 2.0)
                {
                    log.AppendLine($"{node.TaxonName},{node.TaxonID},TRUE,{nonMonoCount},{authorityCount},{totalStudies}");
                    node.ForeColor = Color.Orange;
                    TreeEditingService.MergeNodeIntoParent(node);
                }
                else
                {
                    log.AppendLine($"{node.TaxonName},{node.TaxonID},FALSE,{nonMonoCount},{authorityCount},{totalStudies}");
                }
            }

            

            HardReloadActiveTree(root);
            treeViewer.EndUpdate();

            

            DebugTextForm dtf = new DebugTextForm(log.ToString());
            dtf.Show();
        }

        private void mergeIntoParentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeEditingService.MergeNodeIntoParent((TopoTimeNode)treeViewer.SelectedNode);
        }

        private void usingFastHALXusingGuideTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserInputForm uif = new UserInputForm("1", "Enter the citation ID of the desired guide tree:");
            if (uif.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int GuideTreeID = (int)uif.newValue;

                FastHALXSingleStep((TopoTimeNode)treeViewer.SelectedNode, GuideTreeID: GuideTreeID);
            }
        }

        private void byFastHALsupportOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                SuppressRedrawFunction(delegate ()
                {
                    ResolveByFastHAL(HALFunctions.TopologyConflictMode.NoConflicts, timedHAL: false);
                    CleanTree();
                });
            });
        }

        private void smartPruneProblemTaxaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClockingFunction(delegate ()
            {
                SuppressRedrawFunction(delegate ()
                {
                    TopoTimeNode OriginalNode = (TopoTimeNode)treeViewer.SelectedNode;

                    if (OriginalNode.Nodes.Count < 3)
                        return;

                    HALService = new HALFunctions(DBService, activeTree);
                    PartitionFetcher fastHALfetcher = new PartitionFetcher(DBService);

                    TopoTimeNode ParentNode = OriginalNode.Parent;

                    if (ParentNode == null)
                        treeViewer.Nodes.Remove(OriginalNode);
                    else
                        ParentNode.Nodes.Remove(OriginalNode);

                    TopoTimeNode SelectedNode = OriginalNode.CloneForTree(activeTree);
                    List<TopoTimeNode> ChildNodes = SelectedNode.Nodes.Cast<TopoTimeNode>().ToList();
                    SelectedNode.Nodes.Clear();                    

                    TopoTimeNode DiagnosticNode = new TopoTimeNode();

                    DiagnosticNode.TaxonName = "Diagnostic";
                    DiagnosticNode.Nodes.Add(SelectedNode);
                    SelectedNode.Nodes.Add(ChildNodes[0]);
                    SelectedNode.Nodes.Add(ChildNodes[1]);

                    foreach (TopoTimeNode childNode in ChildNodes.Skip(2))
                    {
                        bool failed = false;

                        SelectedNode.Nodes.Add(childNode);
                        ResolveSelectedNode(SelectedNode, fastHALfetcher);

                        if (SelectedNode.Nodes.Count > 2)
                            failed = true;

                        DissolveNovelBifurcations(SelectedNode);

                        if (failed)
                        {
                            SelectedNode.Nodes.Remove(childNode);
                            DiagnosticNode.Nodes.Add(childNode);
                        }
                    }

                    if (ParentNode == null)
                        treeViewer.Nodes.Add(DiagnosticNode);
                    else
                        ParentNode.Nodes.Add(DiagnosticNode);

                    activeTree.AddNodesToTree(DiagnosticNode);

                    DiagnosticNode.ExpandAll();

                    /*
                    foreach (TopoTimeNode node in SizeOrderedNodeList)
                    {
                        TopoTimeNode MergedNode = MergeAndDissolve(LargestNode, node);
                        ResolveSelectedNode(MergedNode, fastHALfetcher);

                        if (MergedNode.Nodes.Count > 2)
                            ProblemNodes.Add(node);
                        else
                            LargestNode = MergedNode;
                    }
                    */

                    /*
                    TopoTimeNode DiagnosticNode = new TopoTimeNode();

                    DiagnosticNode.TaxonName = "Diagnostic";
                    DiagnosticNode.Nodes.Add(LargestNode);
                    foreach (TopoTimeNode node in SizeOrderedNodeList)
                        DiagnosticNode.Nodes.Add(node);

                    activeTree.AddNodesToTree(DiagnosticNode);

                    if (ParentNode == null)
                        treeViewer.Nodes.Add(DiagnosticNode);
                    else
                        ParentNode.Nodes.Add(DiagnosticNode);

                    DiagnosticNode.ExpandAll();
                    */

                    CleanTree();
                });
            });
        }

        private TopoTimeNode MergeAndDissolve(TopoTimeNode NodeA, TopoTimeNode NodeB)
        {
            TopoTimeNode CloneA = (TopoTimeNode)NodeA.CloneForTree(activeTree);
            TopoTimeNode CloneB = (TopoTimeNode)NodeB.CloneForTree(activeTree);

            List<TopoTimeNode> RemovedNodes = CloneB.Nodes.Cast<TopoTimeNode>().ToList();
            CloneB.Nodes.Clear();
            foreach (TopoTimeNode node in RemovedNodes)
                CloneA.Nodes.Add(node);

            DissolveNovelBifurcations(CloneA);
            return CloneA;
        }

        private void SplitAndResolve(TopoTimeNode SelectedNode, PartitionFetcher fastHALfetcher, HashSet<TopoTimeNode> NodeList)
        {
            (TopoTimeNode FirstSplit, TopoTimeNode SecondSplit) = HalveNode(SelectedNode);
            NodeList.Add(FirstSplit);
            NodeList.Add(SecondSplit);

            ResolveSelectedNode(FirstSplit, fastHALfetcher);
            ResolveSelectedNode(SecondSplit, fastHALfetcher);

            if (FirstSplit.Nodes.Count > 2)
            {
                NodeList.Remove(FirstSplit);
                DissolveNovelBifurcations(FirstSplit);
                SplitAndResolve(FirstSplit, fastHALfetcher, NodeList);
            }

            if (SecondSplit.Nodes.Count > 2)
            {
                NodeList.Remove(SecondSplit);
                DissolveNovelBifurcations(SecondSplit);
                SplitAndResolve(SecondSplit, fastHALfetcher, NodeList);
            }
        }

        private (TopoTimeNode, TopoTimeNode) HalveNode(TopoTimeNode SelectedNode, bool CreateTempParent = false)
        {
            TopoTimeNode ParentNode = SelectedNode.Parent;            

            if (ParentNode == null)
                treeViewer.Nodes.Remove(SelectedNode);
            else
                ParentNode.Nodes.Remove(SelectedNode);

            TopoTimeNode FirstSplit = (TopoTimeNode)SelectedNode.CloneForTree(activeTree);
            TopoTimeNode SecondSplit = (TopoTimeNode)SelectedNode.CloneForTree(activeTree);

            int NodeCount = SelectedNode.Nodes.Count;
            int NodeCountSplit = NodeCount / 2;

            if (FirstSplit.Nodes.Count > 2)
            {
                for (int i = NodeCountSplit; i < NodeCount; i++)
                    FirstSplit.Nodes.RemoveAt(NodeCountSplit);
            }

            if (SecondSplit.Nodes.Count > 2)
            {
                for (int i = 0; i < NodeCountSplit; i++)
                    SecondSplit.Nodes.RemoveAt(0);
            }

            if (CreateTempParent)
            {
                TopoTimeNode DiagnosticNode = new TopoTimeNode();

                DiagnosticNode.TaxonName = "Diagnostic";
                DiagnosticNode.Nodes.Add(FirstSplit);
                DiagnosticNode.Nodes.Add(SecondSplit);

                activeTree.AddNodesToTree(DiagnosticNode);

                if (ParentNode == null)
                    treeViewer.Nodes.Add(DiagnosticNode);
                else
                    ParentNode.Nodes.Add(DiagnosticNode);

                DiagnosticNode.ExpandAll();
            }

            return (FirstSplit, SecondSplit);
        }

        private void dissolvePartiallyResolvedGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                TopoTimeNode root = (TopoTimeNode)treeViewer.Nodes[0];
                treeViewer.Nodes.Remove(root);

                foreach (TopoTimeNode UnresolvedNode in activeTree.nodeList.Where(x => x.Nodes.Count > 2).ToList())
                {
                    DissolveNovelBifurcations(UnresolvedNode);
                }

                foreach (TopoTimeNode UnresolvedNode in activeTree.nodeList.Where(x => x.Nodes.Count == 2 && x.ChildDivergences.Count == 0).ToList())
                {
                    DissolveNovelBifurcations(UnresolvedNode);
                }

                treeViewer.Nodes.Add(root);
            });

            activeTree.OperationHistory.Add("Dissolved novel bifurcations in partially resolved groups");
        }

        private void toListOfNCBIIDsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopoTimeNode SelectedNode = (TopoTimeNode)treeViewer.SelectedNode;
            List<TopoTimeNode> leaves = SelectedNode.getLeaves(false);

            string nodeIDs = String.Join(Environment.NewLine, leaves.Select(x => x.TaxonID));
            DebugTextForm dtf = new DebugTextForm(nodeIDs);
            dtf.Show();
        }

        private void pruneOrphanedFloatingTaxaToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SuppressRedrawFunction(delegate ()
            {
                foreach (TopoTimeNode node in activeTree.nodeList.ToList())
                {
                    TopoTimeNode parent = node.Parent;
                    if (parent == null)
                        continue;

                    if (node.Floating && node.Nodes.Count == 0 && parent.Nodes.Count > 2)
                    {
                        activeTree.DeleteNode(node);
                    }
                }
                CleanTree();
            });
        }

        private void byFastHALusingBestAvailableStrategyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResolveFullTreeFindOptimalStrategy();
        }

        private void removeDuplicateStoredNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TopoTimeNode Node in activeTree.nodeList.Where(x => x.HasValidTaxon))
            {
                
            }
        }

        private void reTimeAdjustedZeroNodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (TopoTimeNode Node in activeTree.nodeList.Where(x => x.Nodes.Count > 0 && x.storedAdjustedHeight == null))
            {
                Node.storedAdjustedHeight = activeTree.GetNodeHeight(Node);
            }
        }



        // note to self: create MoveNode function which returns a list of affected nodes, probably will make your life easier
    }

    
}
