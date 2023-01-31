using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TopoTime
{
    public partial class Form3 : Form
    {
        private DataSet distData;

        public Form3(double[,] distMatrix, int mini, int minj, int size, TreeNodeCollection targetNodes)
        {
            InitializeComponent();

            distData = new DataSet();
            DataTable dataTable = distData.Tables.Add();

            for (int i = 0; i < size; i++)
            {
                dataTable.Columns.Add(targetNodes[i].Text.Split('[')[0]);                
            }

            for (int i = 0; i < size; i++)
            {
                object[] rowData = new object[size];
                for (int m = 0; m < size; m++)
                {
                    if (distMatrix[m, i] != 0)
                        rowData[m] = distMatrix[m, i].ToString("0.000");
                    else
                        rowData[m] = "";
                }

                dataTable.Rows.Add(rowData);
            }

            dataGridView1.DataSource = dataTable;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.HeaderCell.Value = "x";
            }

            dataGridView1.CurrentCell = (dataGridView1.Rows[minj]).Cells[mini];

            this.Width = dataGridView1.Columns.GetColumnsWidth(DataGridViewElementStates.Visible) + 80;
            this.Height = dataGridView1.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 85;
        }
    }
}
