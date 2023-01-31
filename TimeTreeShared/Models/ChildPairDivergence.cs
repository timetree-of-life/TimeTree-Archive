using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Security.Permissions;

namespace TimeTreeShared
{
    [Serializable]
    public class ChildPairDivergence : IComparable, ISerializable
    {
        // old version attributes, kept for backwards compatibility
        [OptionalField, XmlIgnore]
        private string firstTaxa;
        [OptionalField, XmlIgnore]
        private string secondTaxa;

        [OptionalField, XmlIgnore]
        public ExtendedNode FirstTaxa;
        [OptionalField, XmlIgnore]
        public ExtendedNode SecondTaxa;

        [OptionalField]
        public TreeNode metadata;


        // current version
        [OptionalField]
        public string FirstTaxaName;
        [OptionalField]
        public string SecondTaxaName;

        [OptionalField]
        public List<MyTuple<string, string>> StatsData;
        [OptionalField]
        public List<string> TaxaGroupA;
        [OptionalField]
        public List<string> TaxaGroupB;
        [OptionalField]
        public List<int> TaxonIDsA;
        [OptionalField]
        public List<int> TaxonIDsB;

        [OptionalField]
        public bool IsConflict;

        public bool ShouldSerializeTaxaGroupA()
        { return TaxaGroupA != null && TaxaGroupA.Count > 0; }

        public bool ShouldSerializeTaxaGroupB()
        { return TaxaGroupB != null && TaxaGroupB.Count > 0; }

        public double? DivergenceTime
        {
            get { return divergence; }
            set { divergence = value; }
        }

        private double? divergence;

        int IComparable.CompareTo(object obj)
        {
            ChildPairDivergence otherDiv = (ChildPairDivergence)obj;

            if (this.DivergenceTime > otherDiv.DivergenceTime)
                return 1;
            if (this.DivergenceTime < otherDiv.DivergenceTime)
                return -1;

            return 0;
        }

        public int CitationID
        {
            get
            {
                int citationID = 0;

                if (StatsData == null)
                    return 0;

                foreach (Tuple<string, string> data in StatsData)
                {
                    if (data.Item1 == "citation_num")
                    {
                        int.TryParse(data.Item2, out citationID);
                        return citationID;
                    }
                }

                return 0;
            }
        }

        public string PublicationID
        {
            get
            {
                if (StatsData == null)
                    return null;

                foreach (Tuple<string, string> data in StatsData)
                {
                    if (data.Item1 == "ref_id")
                        return data.Item2;

                    if (data.Item1 == "pubmed_id")
                        return data.Item2;
                }

                return null;
            }
        }

        public int PublicationYear
        {
            get
            {
                if (StatsData == null) { return 0; }

                foreach (Tuple<string, string> data in StatsData)
                {
                    if (data.Item1 == "year")
                        return Int32.Parse(data.Item2);
                }

                return 0;
            }
        }

        public int PhylogenyNodeID
        {
            get
            {
                if (StatsData == null) { return 0; }

                foreach (Tuple<string, string> data in StatsData)
                {
                    if (data.Item1 == "phylogeny_node")
                    {
                        int temp;

                        Int32.TryParse(data.Item2, out temp);
                        return temp;
                    }
                }

                return 0;
            }
        }

        public List<int> PhylogenyNodeIDs
        {
            get
            {
                List<int> temp = new List<int>();
                if (StatsData == null) { return temp; }

                foreach (Tuple<string, string> data in StatsData)
                {
                    if (data.Item1 == "phylogeny_node")
                    {
                        string[] split = data.Item2.Split(',');

                        for (int i = 0; i < split.Length; i++)
                            temp.Add(Int32.Parse(split[i]));
                    }
                }

                return temp;
            }
        }

        public void ClearMetadataTaxa()
        {
            TaxaGroupA.Clear();
            TaxaGroupB.Clear();

            if (metadata != null)
            {
                foreach (TreeNode data in metadata.Nodes)
                {
                    if (data.Text == "Taxa Group A")
                    {
                        data.Nodes.Clear();
                    }

                    if (data.Text == "Taxa Group B")
                    {
                        data.Nodes.Clear();
                    }
                }
            }

        }

        public TreeNode Metadata
        {
            get
            {
                TreeNode root = new TreeNode(((double)this.DivergenceTime).ToString("0.0") + " [" + CitationID + "," + PublicationID + "]");
                if (StatsData != null)
                    foreach (Tuple<string, string> fields in StatsData)
                        root.Nodes.Add(new TreeNode(fields.Item1 + " - " + fields.Item2));

                TreeNode groupA = new TreeNode("Taxa Group A");
                if (TaxaGroupA != null)
                    foreach (string taxa in TaxaGroupA)
                        groupA.Nodes.Add(new TreeNode(taxa));

                TreeNode groupB = new TreeNode("Taxa Group B");
                if (TaxaGroupB != null)
                    foreach (string taxa in TaxaGroupB)
                        groupB.Nodes.Add(new TreeNode(taxa));

                root.Nodes.Add(groupA);
                root.Nodes.Add(groupB);

                groupA.Expand();
                groupB.Expand();
                //root.ExpandAll();

                return root;
            }
        }

        public ChildPairDivergence()
        {

        }

        public ChildPairDivergence(ExtendedNode FirstTaxa, ExtendedNode SecondTaxa, double divergence)
        {
            // We want our taxa to be stored in a specific order based on content, no matter what order they're entered
            if (FirstTaxa.GetHashCode() > SecondTaxa.GetHashCode())
            {
                this.FirstTaxaName = SecondTaxa.TaxonName;
                this.SecondTaxaName = FirstTaxa.TaxonName;
            }
            else
            {
                this.FirstTaxaName = FirstTaxa.TaxonName;
                this.SecondTaxaName = SecondTaxa.TaxonName;
            }

            this.divergence = divergence;
        }

        protected void Serialize(SerializationInfo info, StreamingContext context)
        {

        }

        protected ChildPairDivergence(SerializationInfo info, StreamingContext ctx)
        {
            FirstTaxaName = info.GetString("firstTaxa");
            SecondTaxaName = info.GetString("secondTaxa");

            firstTaxa = FirstTaxaName;
            secondTaxa = SecondTaxaName;

            StatsData = new List<MyTuple<string, string>>();
            TaxaGroupA = new List<string>();
            TaxaGroupB = new List<string>();

            TreeNode metadata = (TreeNode)info.GetValue("metadata", typeof(TreeNode));
            foreach (TreeNode data in metadata.Nodes)
            {
                if (data.Text == "Taxa Group A")
                {
                    foreach (TreeNode taxa in data.Nodes)
                        TaxaGroupA.Add(taxa.Text);
                }

                if (data.Text == "Taxa Group B")
                {
                    foreach (TreeNode taxa in data.Nodes)
                        TaxaGroupB.Add(taxa.Text);
                }

                if (data.Text.Contains("ref_id - "))
                    StatsData.Add(new Tuple<string, string>("ref_id", data.Text.Substring(9)));

                if (data.Text.Contains("pubmed_id - "))
                    StatsData.Add(new Tuple<string, string>("pubmed_id", data.Text.Substring(12)));

                if (data.Text.Contains("year - "))
                    StatsData.Add(new Tuple<string, string>("year", data.Text.Substring(7)));

                if (data.Text.Contains("phylogeny_node - "))
                    StatsData.Add(new Tuple<string, string>("phylogeny_node", data.Text.Substring(17)));
            }

            divergence = info.GetDouble("divergence");
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

        }

        public ChildPairDivergence(string firstTaxa, string secondTaxa, double divergence)
        {
            this.FirstTaxaName = firstTaxa;
            this.SecondTaxaName = secondTaxa;
            this.divergence = divergence;
        }

        public override string ToString()
        {
            string name = "(" + FirstTaxaName + ", " + SecondTaxaName + ") [";
            //return name + divergenceTime().ToString() + "]";
            return name + DivergenceTime.ToString() + "]";
        }

        /*
        private class DivergenceComparer : IComparer
        {
            int IComparer.Compare(object a, object b)
            {
                ChildPairDivergence divA = (ChildPairDivergence)a;
                ChildPairDivergence divB = (ChildPairDivergence)b;

                if (divA.DivergenceTime > divB.DivergenceTime)
                    return 1;
                if (divA.DivergenceTime < divB.DivergenceTime)
                    return -1;

                return 0;
            }
        }

        public static IComparer<ChildPairDivergence> sortDivergenceAscending()
        {
            return (IComparer<ChildPairDivergence>)new DivergenceComparer();
        }
         * */
    }
}
