using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Npgsql;
using System.Globalization;
using NpgsqlTypes;
//using TimeTreeManager.Services;
//using TimeTreeManager;

namespace TimeTreeShared.Services
{
    public class DatabaseService
    {
        public NpgsqlConnection DBConnection;

        public DatabaseService(NpgsqlConnection conn)
        {
            DBConnection = conn;
        }

        #region fundamental functions
        public DataTable GetSQLResult(string sqlQuery)
        {
            NpgsqlDataAdapter da;
            DataSet set;
            DataTable table;

            da = new NpgsqlDataAdapter(sqlQuery, DBConnection);
            set = new DataSet();
            da.Fill(set);
            table = set.Tables[0];

            return table;
        }

        public object GetSingleSQL(string sqlQuery)
        {
            NpgsqlCommand command = new NpgsqlCommand(sqlQuery, DBConnection);
            return command.ExecuteScalar();
        }

        public int GetSingleSQL(NpgsqlCommand command)
        {
            command.Connection = DBConnection;
            command.Prepare();
            return command.ExecuteNonQuery();
        }

        public object GetSQLSingleResult(NpgsqlCommand command)
        {
            command.Connection = DBConnection;
            command.Prepare();
            return command.ExecuteScalar();
        }

        public DataTable GetSQLResult(NpgsqlCommand command)
        {
            NpgsqlDataAdapter da;
            DataSet set;
            DataTable table;

            da = new NpgsqlDataAdapter(command);
            set = new DataSet();
            da.Fill(set);
            table = set.Tables[0];

            return table;
        }

        public NpgsqlDataReader GetSQLResultSet(string sqlQuery)
        {
            NpgsqlCommand command = new NpgsqlCommand(sqlQuery, DBConnection);
            NpgsqlDataReader dr = command.ExecuteReader();
            return dr;
        }

        public NpgsqlBinaryImporter GetDataWriter(string sqlCommand)
        {
            return DBConnection.BeginBinaryImport(sqlCommand);
        }
        #endregion

        public void WriteTaxonomyToDatabase(ExtendedNode root, Dictionary<int, int> updatedTaxonIDs)
        {
            string newSlot = "ncbi_taxonomy_base";
            string newAliasSlot = "taxa_alias";

            // drop indices first for performance reasons
            GetSingleSQL("DROP INDEX IF EXISTS public." + newSlot + "_taxon_id_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newSlot + "_scientific_name_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newSlot + "_parent_id_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newSlot + "_rank_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newSlot + "_taxon_id_scientific_name_idx;");
            GetSingleSQL("DELETE FROM " + newSlot + ";");

            using (NpgsqlBinaryImporter writer = GetDataWriter("COPY " + newSlot + " (taxon_id, scientific_name, parent_id, rank, family, level) FROM STDIN (FORMAT BINARY)"))
            {
                WriteTaxonToTaxonomy(root, ParentNodeID: 1, level: 0, writer);
                writer.Complete();
            }

            // easier to recreate indices on a fully-loaded table than it is to update them for each inserted entry
            GetSingleSQL("CREATE INDEX " + newSlot + "_parent_id_idx ON public." + newSlot + " USING hash (parent_id);");
            GetSingleSQL("CREATE INDEX " + newSlot + "_scientific_name_idx ON public." + newSlot + " USING btree (scientific_name COLLATE pg_catalog.\"default\");");
            GetSingleSQL("CREATE INDEX " + newSlot + "_taxon_id_idx ON public." + newSlot + " USING hash (taxon_id);");
            GetSingleSQL("CREATE INDEX " + newSlot + "_rank_idx ON public." + newSlot + " USING btree (rank COLLATE pg_catalog.\"default\" ASC NULLS LAST);");
            GetSingleSQL("CREATE INDEX " + newSlot + "_taxon_id_scientific_name_idx ON public." + newSlot + " USING btree (taxon_id ASC NULLS LAST, scientific_name COLLATE pg_catalog.\"default\" ASC NULLS LAST); ");

            // now do the same for the taxa alias table
            GetSingleSQL("DROP INDEX IF EXISTS public." + newAliasSlot + "_lower_unaccent_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newAliasSlot + "_c_syn_name_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newAliasSlot + "_c_syn_type_idx;");
            GetSingleSQL("DROP INDEX IF EXISTS public." + newAliasSlot + "_taxon_id_idx;");
            GetSingleSQL("DELETE FROM " + newAliasSlot + ";");

            //GetSingleSQL("CREATE TABLE temp_alias_table (LIKE taxa_alias_b);");

            //StringBuilder sb = new StringBuilder();
            //WriteTaxonAliasDebug(root, ref sb);

            //FileIOService.WriteFile("aliasdebug.txt", sb.ToString());

            using (NpgsqlBinaryImporter writer = GetDataWriter("COPY " + newAliasSlot + " (c_syn_name, c_syn_type, taxon_id) FROM STDIN (FORMAT BINARY)"))
            {
                WriteTaxonAliasToTaxonomy(root, writer);
                writer.Complete();
            }

            //GetSingleSQL("INSERT INTO " + newAliasSlot + " SELECT DISTINCT * FROM temp_alias_table;");
            //GetSingleSQL("DROP TABLE temp_alias_table;");


            GetSingleSQL("CREATE INDEX " + newAliasSlot + "_lower_unaccent_idx ON public." + newAliasSlot + " USING btree (lower_unaccent(c_syn_name::text) COLLATE pg_catalog.\"default\");");
            GetSingleSQL("CREATE INDEX " + newAliasSlot + "_c_syn_name_idx ON public." + newAliasSlot + " USING btree (c_syn_name COLLATE pg_catalog.\"default\");");
            GetSingleSQL("CREATE INDEX " + newAliasSlot + "_c_syn_type_idx ON public." + newAliasSlot + " USING hash (c_syn_name);");
            GetSingleSQL("CREATE INDEX " + newAliasSlot + "_taxon_id_idx ON public." + newAliasSlot + " USING hash (taxon_id);");

            //GetSingleSQL("UPDATE tt_table_settings SET value = '" + newSlot + "' WHERE setting = 'active_taxonomy_slot';");
            //GetSingleSQL("UPDATE tt_table_settings SET value = '" + newAliasSlot + "' WHERE setting = 'active_taxa_alias_slot';");

            // refresh the combined NCBI+provisional taxa table
            GetSingleSQL("REFRESH MATERIALIZED VIEW ncbi_taxonomy;");

            UpdateTaxonIDs(updatedTaxonIDs);
        }

        public void UpdateTaxonIDs(Dictionary<int, int> updatedTaxonIDs)
        {
            foreach (KeyValuePair<int, int> pair in updatedTaxonIDs)
            {
                int oldTaxonID = pair.Key;
                int newTaxonID = pair.Value;
                //GetSingleSQL("UPDATE phylogeny_topology SET original_taxon_id = " + oldTaxonID + " WHERE taxon_id = " + oldTaxonID + " AND original_taxon_id IS NULL;");
                //GetSingleSQL("UPDATE phylogeny_topology SET taxon_id = " + newTaxonID + " WHERE taxon_id = " + oldTaxonID + ";");
                GetSingleSQL($"INSERT INTO taxonomy_changes (old_taxon_id, new_taxon_id) VALUES ({pair.Key}, {pair.Value}) ON CONFLICT (old_taxon_id) DO UPDATE SET new_taxon_id={pair.Value};");
            }
        }

        public void WriteTaxonToTaxonomy(ExtendedNode node, int ParentNodeID, int level, NpgsqlBinaryImporter writer)
        {
            writer.StartRow();
            writer.Write(node.TaxonID, NpgsqlTypes.NpgsqlDbType.Integer);
            writer.Write(node.TaxonName.Replace("'", ""));
            writer.Write(ParentNodeID, NpgsqlTypes.NpgsqlDbType.Integer);
            writer.Write(node["Rank"]);
            writer.Write(node["FamilyNCBI"]);
            writer.Write(level);

            foreach (ExtendedNode child in node.Nodes)
                WriteTaxonToTaxonomy(child, node.TaxonID, level + 1, writer);
        }

        public void WriteTaxonAliasDebug(ExtendedNode node, ref StringBuilder sb)
        {
            sb.AppendLine(node.TaxonName.Replace("'", "") + "\tscientific name\t" + node.TaxonID);
            if (node["SynonymList"] != null)
            {
                foreach (KeyValuePair<string, string> synonym in (node["SynonymList"] as Dictionary<string, string>))
                {
                    sb.AppendLine(synonym.Key + "\t" + synonym.Value + "\t" + node.TaxonID);
                }
            }

            foreach (ExtendedNode child in node.Nodes)
                WriteTaxonAliasDebug(child, ref sb);
        }


        public void WriteTaxonAliasToTaxonomy(ExtendedNode node, NpgsqlBinaryImporter writer)
        {
            writer.StartRow();

            string ScientificName = node.TaxonName.Replace("'", "");

            if (ScientificName.Length > 255)
                throw new Exception("Scientific name " + ScientificName + " exceeds length limit.");

            writer.Write(node.TaxonName.Replace("'", ""));
            writer.Write("scientific name");
            writer.Write(node.TaxonID, NpgsqlTypes.NpgsqlDbType.Integer);

            if (node["SynonymList"] != null)
            {
                foreach (KeyValuePair<string, string> synonym in (node["SynonymList"] as Dictionary<string, string>))
                {
                    string SynonymName = synonym.Key;

                    if (SynonymName.Length < 255)
                    {
                        writer.StartRow();
                        writer.Write(SynonymName);
                        writer.Write(synonym.Value);
                        writer.Write(node.TaxonID, NpgsqlTypes.NpgsqlDbType.Integer);
                    }
                    else
                    {

                    }
                }
            }

            foreach (ExtendedNode child in node.Nodes)
                WriteTaxonAliasToTaxonomy(child, writer);
        }

        public void UpdateProvisionalTaxonomy()
        {
            int updatedTaxaCount;
            do
            {
                updatedTaxaCount = UpdateProvisionalTaxonomyIteration();
            }
            while (updatedTaxaCount > 0);

            // TO-DO: Add code for examining problem entries not automatically updated and force their update
        }

        public int UpdateProvisionalTaxonomyIteration()
        {
            string sql = "SELECT nt.scientific_name, nt.taxon_id as new_taxon_id, pt.taxon_id as old_taxon_id, nt.parent_id, nt.rank as new_rank, pt.rank as old_rank from ncbi_taxonomy_base nt JOIN provisional_taxa pt ON nt.scientific_name=pt.scientific_name AND nt.parent_id=pt.parent_id ORDER BY nt.scientific_name;";
            DataTable table = GetSQLResult(sql);

            int rowCount = table.Rows.Count;

            foreach (DataRow row in table.Rows)
            {
                int oldTaxonID = (int)row["old_taxon_id"];
                int newTaxonID = (int)row["new_taxon_id"];

                GetSingleSQL($"UPDATE study_taxon_entries SET current_taxon_id={newTaxonID} WHERE current_taxon_id={oldTaxonID};");
                GetSingleSQL($"UPDATE provisional_taxa SET parent_id={newTaxonID} WHERE parent_id={oldTaxonID};");
                GetSingleSQL($"DELETE FROM provisional_taxa WHERE taxon_id={oldTaxonID};");

            }

            return rowCount;
        }

    }


}
