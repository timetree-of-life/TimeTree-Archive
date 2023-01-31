using System;
using System.Collections.Generic;
using System.Text;

namespace TimeTreeShared
{
    [Serializable]
    public class Study
    {
        private string source;
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        private string refID;
        public string RefID
        {
            get { return refID; }
            set { refID = value; }
        }

        private int? pubmedID;
        public int? PubMedID
        {
            get { return pubmedID; }
            set { pubmedID = value; }
        }

        public string ID
        {
            get
            {
                if (pubmedID == null)
                    return refID;
                else
                    return pubmedID.ToString();
            }

        }

        private string author;
        public string Author
        {
            get { return author; }
            set { author = value; }
        }

        private int year;
        public int Year
        {
            get { return year; }
            set { year = value; }
        }

        Study() { }

        public Study(string source, string refID, string author, int year)
        {
            this.source = source;
            this.refID = refID;
            this.author = author;
            this.year = year;
        }

        public Study(string source, int pubmedID, string author, int year)
        {
            this.source = source;
            this.pubmedID = pubmedID;
            this.author = author;
            this.year = year;
        }
    }
}
