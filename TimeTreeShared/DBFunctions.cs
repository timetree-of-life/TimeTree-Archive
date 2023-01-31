using System;
using System.Collections.Generic;
using System.Text;
using Npgsql;
using System.Data;

namespace TimeTreeShared
{
    static class DBFunctions
    {
        public static DataTable getSQLResult(string sqlQuery, NpgsqlConnection conn)
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
    }
}
