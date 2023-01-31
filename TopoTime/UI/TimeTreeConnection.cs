using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CredentialManagement;
using Npgsql;

namespace TopoTime
{
    public partial class TimeTreeConnection : Form
    {
        public NpgsqlConnection newConnection;
        private const string PasswordName = "ServerPassword";

        public TimeTreeConnection()
        {
            InitializeComponent();
            this.txtBoxDatabase.Text = Properties.Settings.Default.Database;
            this.txtBoxUsername.Text = Properties.Settings.Default.UserName;
            this.txtBoxServer.Text = Properties.Settings.Default.Server;
            this.maskedTxtBoxPort.Text = Properties.Settings.Default.Port;
            this.txtBoxPassword.Text = GetPassword();
        }

        private void TimeTreeConnection_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult != System.Windows.Forms.DialogResult.Cancel)
            {
                try
                {
                    Properties.Settings.Default.Database = this.txtBoxDatabase.Text;
                    Properties.Settings.Default.UserName = this.txtBoxUsername.Text;
                    Properties.Settings.Default.Server = this.txtBoxServer.Text;
                    Properties.Settings.Default.Port = this.maskedTxtBoxPort.Text;
                    SavePassword(txtBoxPassword.Text);
                    Properties.Settings.Default.Save();

                    string connstring = String.Format("Server={0};Port={1};" +
                            "User Id={2};Password={3};Database={4};CommandTimeout=2000;",
                            txtBoxServer.Text, maskedTxtBoxPort.Text, txtBoxUsername.Text,
                            txtBoxPassword.Text, txtBoxDatabase.Text);

                    newConnection = new NpgsqlConnection(connstring);
                    newConnection.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error connecting to database: " + ex.Message);
                    e.Cancel = true;
                }
            }
        }

        public void SavePassword(string password)
        {
            using (var cred = new Credential())
            {
                cred.Password = password;
                cred.Target = PasswordName;
                cred.Type = CredentialType.Generic;
                cred.PersistanceType = PersistanceType.LocalComputer;
                cred.Save();
            }
        }

        public string GetPassword()
        {
            using (var cred = new Credential())
            {
                cred.Target = PasswordName;
                cred.Load();
                return cred.Password;
            }
        }
    }

    
}
