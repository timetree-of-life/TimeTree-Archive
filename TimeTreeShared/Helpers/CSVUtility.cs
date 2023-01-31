using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Drawing;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace TimeTreeShared
{   

    public static class CSVUtility
    {
        public static string ToCSV(this DataTable dtDataTable)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Join(",", dtDataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName)));
            foreach (DataRow row in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(row[i]))
                    {
                        string value = row[i].ToString();
                        if (value.Contains(',') && !value.Contains('"'))
                        {
                            value = String.Format("\"{0}\"", value);
                            sb.Append(value);
                        }
                        else
                        {
                            sb.Append(value);
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.AppendLine();
            }
            

            return sb.ToString();

        }
    }
}
