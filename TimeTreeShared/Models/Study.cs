using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTreeShared
{
    [Serializable]
    public class Study
    {
        public string Source { get; set; }

        public string RefID { get; set; }

        public int? PubMedID { get; set; }

        public string ID
        {
            get
            {
                if (PubMedID == null)
                    return RefID;
                else
                    return PubMedID.ToString();
            }

        }

        public string Author { get; set; }
        public int Year { get; set; }

        public string Title { get; set; }

        // parameterless constructor necessary for serialization
        Study() { }

        public Study(string source, string refID, string author, int year, string title)
        {
            this.Source = source;
            this.PubMedID = null;
            this.RefID = refID;
            this.Author = author;
            this.Year = year;
            this.Title = title;
        }

        public Study(string source, int pubmedID, string author, int year, string title)
        {
            this.Source = source;
            this.PubMedID = pubmedID;
            this.RefID = "";
            this.Author = author;
            this.Year = year;
            this.Title = title;
        }
    }
}
