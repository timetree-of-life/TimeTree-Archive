using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TimeTreeShared;
using TopoTimeShared;

namespace TopoTime
{
    public partial class CompareForm : Form
    {
        private TopoTimeTree toCompare;

        public CompareForm(TopoTimeTree toCompare)
        {
            InitializeComponent();
            this.toCompare = toCompare;
            dataGridView1.AutoGenerateColumns = false;
        }

        private void btnAllSuspects_Click(object sender, EventArgs e)
        {
            String refID = textBoxPubID.Text;
            List<ComparePair> compareList = new List<ComparePair>();

            foreach (TopoTimeNode node in toCompare.nodeList)
            {
                for (int i = 0; i < node.ChildDivergences.Count; i++)
                {
                    ChildPairDivergence divergence = node.ChildDivergences[i];

                    if (divergence.PublicationID == refID)
                    {
                        for (int j = 0; j < node.ChildDivergences.Count; j++)
                        {
                            if (i != j)
                                compareList.Add(new ComparePair(refID, node.ChildDivergences[j].PublicationID, (double)divergence.DivergenceTime, (double)node.ChildDivergences[j].DivergenceTime, node.Text, node.ChildDivergences.Count));
                        }
                    }
                }
            }

            dataGridView1.DataSource = compareList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String refID = textBoxPubID.Text;
            List<ComparePair> compareList = new List<ComparePair>();

            foreach (TopoTimeNode node in toCompare.nodeList)
            {
                if (node.ChildDivergences.Count > 1)
                {
                    for (int i = 0; i < node.ChildDivergences.Count; i++)
                    {
                        ChildPairDivergence divergence = node.ChildDivergences[i];

                        if (divergence.PublicationID == refID)
                        {
                            double average = 0.0;
                            int count = 0;
                            for (int j = 0; j < node.ChildDivergences.Count; j++)
                            {
                                if (i != j)
                                {
                                    count++;
                                    average = average + (double)node.ChildDivergences[j].DivergenceTime;
                                }                                
                            }
                            average = average / (double)count;
                            compareList.Add(new ComparePair(refID, "", (double)divergence.DivergenceTime, average, node.Text, node.ChildDivergences.Count));
                        }
                    }
                }
            }

            dataGridView1.DataSource = compareList;
        }
    }

    public class ComparePair
    {
        public string studyA;
        public string studyB;
        public string nodeText;
        public double timeA;
        public double timeB;
        public int studyCount;

        public string StudyA
        {
            get { return studyA; }
        }

        public string StudyB
        {
            get { return studyB; }
        }

        public double TimeA
        {
            get { return timeA; }
        }

        public double TimeB
        {
            get { return timeB; }
        }

        public double Ratio
        {
            get { return timeA / timeB; }
        }

        public string NodeText
        {
            get { return nodeText; }
        }

        public int StudyCount
        {
            get { return studyCount; }
        }

        public ComparePair(string StudyA, string StudyB, double TimeA, double TimeB, string NodeText, int StudyCount)
        {
            this.studyA = StudyA;
            this.studyB = StudyB;
            this.timeA = TimeA;
            this.timeB = TimeB;
            this.nodeText = NodeText;
            this.studyCount = StudyCount;
        }
    }
}
