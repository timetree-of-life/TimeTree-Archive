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
    public partial class DebugTextForm : Form
    {
        public string userText = "";
        private TextBox newTextBox;

        public DebugTextForm()
        {
            InitializeComponent();

            tabControl1.TabPages.Add(new TabPage("0"));
            newTextBox = new TextBox();
            newTextBox.Location = new Point(4, 7);
            newTextBox.Size = new Size(248, 226);
            newTextBox.Multiline = true;
            newTextBox.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            newTextBox.ScrollBars = ScrollBars.Both;
            newTextBox.ReadOnly = true;
            tabControl1.TabPages[0].Controls.Add(newTextBox);
        }

        public DebugTextForm(string displayText, string titleText = "DebugTextForm", bool textIsReadOnly = true) : base()
        {
            InitializeComponent();

            tabControl1.TabPages.Add(new TabPage("0"));
            newTextBox = new TextBox();
            newTextBox.Location = new Point(4, 7);
            newTextBox.Size = new Size(248, 226);
            newTextBox.Multiline = true;
            newTextBox.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            newTextBox.ScrollBars = ScrollBars.Both;
            tabControl1.TabPages[0].Controls.Add(newTextBox);

            newTextBox.ReadOnly = textIsReadOnly;            

            if (!textIsReadOnly)
            {
                button1.Text = "OK";
                button2.Text = "Cancel";

                button1.DialogResult = DialogResult.OK;
                button2.DialogResult = DialogResult.Cancel;
            }

            newTextBox.Text = displayText;
        }

        public DebugTextForm(List<string> displayText)
        {
            InitializeComponent();

            for (int i = 0; i < displayText.Count; i++)
            {
                //if (i > 0)
                {
                    tabControl1.TabPages.Add(i.ToString());
                    TextBox newTextBox = new TextBox();
                    newTextBox.Size = new Size(248, 226);
                    newTextBox.Multiline = true;
                    newTextBox.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
                    newTextBox.ScrollBars = ScrollBars.Both;
                    tabControl1.TabPages[i].Container.Add(newTextBox);

                    newTextBox.Text = displayText[i];
                }                
            }
        }

        public void ReportProgress(string value)
        {
            newTextBox.Text = newTextBox.Text + value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TextBox tabBox = (tabControl1.SelectedTab.Controls.OfType<TextBox>()).ElementAt(0);
            tabBox.SelectAll();

            userText = tabBox.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            TextBox tabBox = (tabControl1.SelectedTab.Controls.OfType<TextBox>()).ElementAt(0);

            try
            {
                Clipboard.SetText(tabBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Clipboard operation failed: " + ex.Message + " Try again.");
            }
        }
    }
}
