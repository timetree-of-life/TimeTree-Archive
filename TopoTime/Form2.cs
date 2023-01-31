using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Npgsql;

namespace TopoTime
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string pass = "NfdB15\"";
            byte[] original = System.Text.Encoding.UTF8.GetBytes(pass);
            byte[] encrypt = new byte[original.Length];

            for (int i = 0; i < original.Length; i++)
                encrypt[i] = (byte)(original[i] ^ 0x3);

            string pass2 = new string(System.Text.Encoding.UTF8.GetChars(encrypt));

            string connstring = String.Format("Server={0};Port={1};" +
                    "User Id={2};Password={3};Database={4};",
                    "localhost", "5432", "postgres",
                    pass2, "Timeline_2012");

            NpgsqlConnection conn = new NpgsqlConnection(connstring);
            conn.Open();

            DataTable table;
            int taxaA;
            int taxaB;

            string sql = "SELECT DISTINCT t.i_node_id FROM taxa t WHERE c_node_name_scientific='" + textBox1.Text + "';";
            table = MainForm.getSQLResult(sql, conn);

            if (table.Rows.Count > 0)
            {
                taxaA = (int)(table.Rows[0][0]);
                textBox4.Text = taxaA.ToString();

                sql = "SELECT DISTINCT t.i_node_id FROM taxa t WHERE c_node_name_scientific='" + textBox2.Text + "';";
                table = MainForm.getSQLResult(sql, conn);

                /*
                if (table.Rows.Count > 0)
                {
                    taxaB = (int)(table.Rows[0][0]);
                    textBox3.Text = taxaB.ToString();

                    DataTable times = Form1.getDivergenceTable(taxaA, taxaB, conn);

                    dataGridView1.DataSource = times;
                    dataGridView1.Refresh();
                }
                 * */
            }
            
        }
    }
}
