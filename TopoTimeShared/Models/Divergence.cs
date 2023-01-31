using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoTimeShared
{
    public struct DivergenceEstimate
    {
        public double DivergenceTime { get; set; }
        public int PhylogenyNodeID { get; set; }
        public bool IsTopologicalConflict { get; set; }

        public DivergenceEstimate(double divergenceTime, int phyloNodeID, bool IsTopologicalConflict)
        {
            DivergenceTime = divergenceTime;
            PhylogenyNodeID = phyloNodeID;
            this.IsTopologicalConflict = IsTopologicalConflict;
        }
    }

    [Serializable]
    public class Divergence
    {
        public int CitationID;
        //public string AuthorName;
        //public string PubTitle;
        //public object PubMedID;
        //public object ReferenceID;        
        //public int Year;
        public bool IsConflict;

        public string PhylogenyNodes
        {
            get { return String.Join(",", DivergenceTimes.Select(x => x.PhylogenyNodeID.ToString())); }
        }

        public double DivergenceTime
        {
            get
            {
                IEnumerable<DivergenceEstimate> validTimes = DivergenceTimes.Where(x => x.DivergenceTime > 0);
                if (validTimes.Any())
                    return validTimes.Average(x => x.DivergenceTime);

                return 0;
            }
        }

        public List<DivergenceEstimate> DivergenceTimes;

        public Divergence(double DivergenceTime, int PhylogenyNodeID, int citationID, bool IsConflict = false)
        {
            DivergenceTimes = new List<DivergenceEstimate>();
            DivergenceTimes.Add(new DivergenceEstimate(DivergenceTime, PhylogenyNodeID, IsConflict));

            //this.AuthorName = AuthorName;
            //this.PubTitle = PubTitle;
            //this.PubMedID = PubMedID;
            //this.ReferenceID = ReferenceID;
            //this.Year = Year;
            this.IsConflict = IsConflict;
            this.CitationID = citationID;
        }

        public void AddDivergence(double newDivergenceTime, int PhylogenyNodeID, bool IsConflict)
        {
            DivergenceTimes.Add(new DivergenceEstimate(newDivergenceTime, PhylogenyNodeID, IsConflict));
            if (IsConflict)
                this.IsConflict = true;
        }

        /*
        public void AddDivergence(double newDiv, object newPhyNode)
        {
            double tempDiv = DivergenceTime * DivergenceCount;
            tempDiv = tempDiv + newDiv;
            DivergenceCount++;

            DivergenceTime = tempDiv / DivergenceCount;
            PhylogenyNodes = PhylogenyNodes + "," + newPhyNode;
        }
        */


        public override string ToString()
        {
            return DivergenceTime.ToString() + " " + IsConflict;
        }
    }
}
