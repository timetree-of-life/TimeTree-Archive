using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using TimeTreeShared;
using System.Xml.Linq;
using Ionic.Zip;
using System.Net;

namespace TopoTimeShared
{
    public static class TreeIOService
    {
        private static TopoTimeTree LoadTree(Stream file)
        {
            try
            {
                XmlAttributeOverrides overrideList = new XmlAttributeOverrides();
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlIgnore = true;
                overrideList.Add(typeof(ChildPairDivergence), "metadata", attrs);

                System.Xml.Serialization.XmlSerializer x = new XmlSerializer(typeof(SerializableNode), overrideList);
                SerializableNode rootData = (SerializableNode)x.Deserialize(file);
                return rootData.DeserializedTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return null;

        }

        private static TopoTimeTree LoadCompressedTree(Stream stream)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry archivedFile = archive.Entries.First(x => x.FullName.EndsWith(".tss"));
                return LoadTree(archivedFile.Open());
            }

            return null;
        }

        public static TopoTimeTree LoadTreeFile(string filename)
        {
            using (Stream file = File.Open(filename, FileMode.Open))
            {
                if (filename.EndsWith(".tss"))
                    return LoadTree(file);
                else if (filename.EndsWith(".tsz") || filename.EndsWith(".zip"))
                    return LoadCompressedTree(file);


                return null;
            }
        }

        public static void SaveTreeFile(string filename, TopoTimeTree activeTree, TopoTimeNode selectedNode = null)
        {
            using (Stream file = File.Open(filename, FileMode.Create))
            {
                TopoTimeNode rootNode = selectedNode;
                if (rootNode == null)
                    rootNode = activeTree.root;

                if (filename.EndsWith(".tss"))
                    SaveTree(file, rootNode, activeTree);
                else if (filename.EndsWith(".tsz"))
                    SaveCompressedTree(file, filename.Split('\\').Last(), rootNode, activeTree);

            }
        }

        private static void SaveTree(Stream file, TopoTimeNode root, TopoTimeTree tree)
        {
            XmlAttributeOverrides overrideList = new XmlAttributeOverrides();
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlIgnore = true;
            overrideList.Add(typeof(ChildPairDivergence), "metadata", attrs);

            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(root.SerializedData.GetType(), overrideList);
            x.Serialize(file, tree.CreateSerializedNode(root));
        }

        public static void SaveCompressedTree(Stream file, String filename, TopoTimeNode root, TopoTimeTree tree)
        {
            using (ZipArchive archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                ZipArchiveEntry compressedTree = archive.CreateEntry(filename.Substring(0, filename.Length - 4) + ".tss");
                SaveTree(compressedTree.Open(), root, tree);
            }
        }

    }

    public static class TaxonomyIOService
    {
        public static ExtendedNode GetTaxonomyStructureFromStream(Stream stream, Dictionary<int, ExtendedNode> nodeList, Dictionary<int, int> parentList)
        {
            StreamReader reader = new StreamReader(stream);

            TopoTimeNode LifeRoot = null;

            string line;
            string[] splitter = { "\t|\t" };
            while ((line = reader.ReadLine()) != null)
            {
                string[] lineSplit = line.Split(splitter, StringSplitOptions.None);
                TopoTimeNode newNode = new TopoTimeNode();
                //newNode.Source = "NCBI";

                int nodeID = Int32.Parse(lineSplit[0]);
                int parentID = Int32.Parse(lineSplit[1]);

                newNode.TaxonID = nodeID;
                newNode["Rank"] = lineSplit[2];

                switch (lineSplit[4])
                {
                    case "0":
                        newNode["FamilyNCBI"] = "Bacteria";
                        break;
                    case "1":
                        newNode["FamilyNCBI"] = "Invertebrates";
                        break;
                    case "2":
                        newNode["FamilyNCBI"] = "Mammals";
                        break;
                    case "3":
                        newNode["FamilyNCBI"] = "Phages";
                        break;
                    case "4":
                        newNode["FamilyNCBI"] = "Plants";
                        break;
                    case "5":
                        newNode["FamilyNCBI"] = "Primates";
                        break;
                    case "6":
                        newNode["FamilyNCBI"] = "Rodents";
                        break;
                    case "7":
                        newNode["FamilyNCBI"] = "Synthetic";
                        break;
                    case "8":
                        newNode["FamilyNCBI"] = "Unassigned";
                        break;
                    case "9":
                        newNode["FamilyNCBI"] = "Viruses";
                        break;
                    case "10":
                        newNode["FamilyNCBI"] = "Vertebrates";
                        break;
                    case "11":
                        newNode["FamilyNCBI"] = "Environmental samples";
                        break;
                }

                if (parentID == nodeID)
                    continue;

                nodeList.Add(nodeID, newNode);
                parentList.Add(nodeID, parentID);

                // only add the cellular organisms node, ID 131567
                if (parentID == 1 && nodeID == 131567)
                    LifeRoot = newNode;
            }

            reader.Close();

            return LifeRoot;
        }

        public static void GetTaxonomyNamesFromStream(Stream stream, Dictionary<int, ExtendedNode> nodeList)
        {
            StreamReader reader = new StreamReader(stream);

            string line;
            string[] splitter = { "\t|\t" };
            while ((line = reader.ReadLine()) != null)
            {
                string[] lineSplit = line.Split(splitter, StringSplitOptions.None);

                if (lineSplit[0] == "1")
                    continue;

                if (lineSplit[3] == "scientific name\t|")
                {
                    int nodeID = Int32.Parse(lineSplit[0]);
                    nodeList[nodeID].Text = lineSplit[1] + " [" + nodeID + "]";
                    nodeList[nodeID].TaxonName = lineSplit[1];
                }
                else if (lineSplit[3] == "blast name\t|")
                {
                    int nodeID = Int32.Parse(lineSplit[0]);
                    nodeList[nodeID]["BlastName"] = lineSplit[1];
                }
                else
                {
                    int nodeID = Int32.Parse(lineSplit[0]);
                    if (nodeList[nodeID]["SynonymList"] == null)
                        nodeList[nodeID]["SynonymList"] = new Dictionary<string, string>();

                    string synonymText = lineSplit[1];
                    string synonymType = lineSplit[3].Substring(0, lineSplit[3].Length - 2);

                    (nodeList[nodeID]["SynonymList"] as Dictionary<string, string>)[synonymText] = synonymType;
                }
            }

            reader.Close();
        }

        public static void UpdateTaxonIDsFromStream(Stream stream, Dictionary<int, int> updateList)
        {
            StreamReader reader = new StreamReader(stream);
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                char[] delimiters = { '|', '\t' };
                string[] lineSplit = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                int oldTaxonID = Int32.Parse(lineSplit[0]);
                int newTaxonID = Int32.Parse(lineSplit[1]);

                updateList[oldTaxonID] = newTaxonID;
            }
        }

        private static MemoryStream ZipExtract(Stream zipFile, string specificFile)
        {
            MemoryStream data = new MemoryStream();
            zipFile.Seek(0, SeekOrigin.Begin);
            using (ZipFile zip = ZipFile.Read(zipFile))
            {
                zip[specificFile].Extract(data);
            }
            data.Seek(0, SeekOrigin.Begin);
            return data;
        }

        public static Stream FileStream(string FileName)
        {
            return File.Open(FileName, FileMode.Open);
        }
        public static MemoryStream DownloadStream()
        {
            return new MemoryStream(new WebClient().DownloadData("https://ftp.ncbi.nlm.nih.gov/pub/taxonomy/taxdmp.zip"));
        }

        public static TopoTimeNode BuildTaxonomyTree(Stream DataStream, out Dictionary<int, int> UpdateList)
        {
            Dictionary<int, ExtendedNode> nodeList = new Dictionary<int, ExtendedNode>();
            UpdateList = new Dictionary<int, int>();
            Dictionary<int, int> parentList = new Dictionary<int, int>();

            ExtendedNode LifeRoot = null;
            {
                /*
                MemoryStream data = new MemoryStream();
                using (ZipFile zip = ZipFile.Read(file))
                {
                    zip["nodes.dmp"].Extract(data);
                }
                data.Seek(0, SeekOrigin.Begin);
                */


                MemoryStream structureStream = ZipExtract(DataStream, "nodes.dmp");
                LifeRoot = GetTaxonomyStructureFromStream(structureStream, nodeList, parentList);
                structureStream.Close();

                MemoryStream namesStream = ZipExtract(DataStream, "names.dmp");
                GetTaxonomyNamesFromStream(namesStream, nodeList);
                namesStream.Close();

                MemoryStream updateStream = ZipExtract(DataStream, "merged.dmp");
                UpdateTaxonIDsFromStream(updateStream, UpdateList);
                updateStream.Close();
            }

            foreach (KeyValuePair<int, int> pair in parentList)
            {
                if (pair.Value != pair.Key && pair.Value != 1)
                    nodeList[pair.Value].Nodes.Add(nodeList[pair.Key]);
            }

            return (TopoTimeNode)LifeRoot;
        }

    }
}
