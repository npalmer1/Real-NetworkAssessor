using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SecureRemote2
{
    public partial class NetForm : Form
    {

        public string[] NetworkList = new string[50];
        public int NetSelected;
        public string SelectedIP;



        public bool selectNet;
        bool editmode = false;
        bool addmode = false;
        int index = 0;
       

        public NetForm()
        {
            InitializeComponent();

        }

        private void Form5_Load(object sender, EventArgs e)
        {
            string[] str;
           
            treeView1.Nodes.Clear();
            try
            {
                for (int i = 0; i < 50; i++)
                {

                    if (NetworkList[i] != null)
                    {
                        treeView1.Nodes.Add(NetworkList[i]);
                    }
                }
                if (selectNet)
                {
                    button3.Text = "Cancel";
                    button3.Visible = true;
                    index = 0;
                 
                    TreeNodeCollection nodes = treeView1.Nodes;
                    foreach (TreeNode n in nodes)
                    {
                        str = n.Text.Split(';');
                        if (str[0] != null)
                        {
                            if (str[0].Trim() == SelectedIP.Trim())
                            {
                                treeView1.SelectedNode = n;
                                treeView1.Focus();
                                break;
                            }
                        }
                    }                          
                }
                else
                {
                    
                    button3.Visible = true;
                    button3.Text = "Close";
                    treeView1.SelectedNode = treeView1.Nodes[0];
                    treeView1.Focus();
                }
               
            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
            }       
        }

      

        void Add_Network(bool insert)
        {
            //Add or insert new network
            string str;
            str = textBox1.Text + " ; " + textBox2.Text;
            int insI = 0;
            if (treeView1.Nodes.Count > 0)
            {
                insI = treeView1.SelectedNode.Index;
            }
            if (str.Length > 0)
            {
                if (insert)
                {
                    treeView1.Nodes.Insert(insI + 1, str);
                    treeView1.SelectedNode = treeView1.Nodes[treeView1.SelectedNode.Index+1];
                    treeView1.Focus();
                    
                }
                else
                {
                    treeView1.Nodes.Add(str);
                    treeView1.SelectedNode = treeView1.Nodes[treeView1.Nodes.Count-1];
                    treeView1.Focus();
                    
                }                
            }
        }

        

        private void button1_Click(object sender, EventArgs e)
        {   //save button
            if (IsValidIP(textBox1.Text.Trim()))
            {
                if (editmode)
                {
                    treeView1.SelectedNode.Text = textBox1.Text + " ; " + textBox2.Text;
                }
                else
                {
                    if (addmode)
                    {
                        Add_Network(false);
                        addmode = false;
                    }
                    else //insertmode
                    {
                        Add_Network(true);
                        addmode = false;

                    }
                }
                button1.Visible = false;
                button2.Visible = false;
                textBox1.Visible = false;
                textBox2.Visible = false;
                label1.Visible = false;
                label2.Visible = false;
                
            }
            else
            {
                MessageBox.Show("Invalid IP address");
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //cancel
            button1.Visible = false;
            button2.Visible = false;
            textBox1.Visible = false;
            textBox2.Visible = false;
            label1.Visible = false;
            label2.Visible = false;

        }

        

        private void addNetworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addmode = true;
            editmode = false;
           
                //alladd networks
                textBox1.Visible = true;
                button1.Visible = true;
                button2.Visible = true;
                textBox1.Text = "";
                textBox2.Text = "";
                textBox2.Visible = true;
                label1.Visible = true;
                label2.Visible = true;

            
        }

        private void contextMenuStrip1_Click_1(object sender, EventArgs e)
        {
            string[] str;
            try
            {
                textBox1.Text = "";
                textBox2.Text = "";
                //edit, insert or delete network
                if (contextMenuStrip1.Items[0].Selected)
                {
                    //EDIT network
                   
                    editmode = true;
                    button1.Visible = true;
                    button2.Visible = true;
                    textBox1.Visible = true;
                    textBox2.Visible = true;
                    label1.Visible = true;
                    label2.Visible = true;
                    try
                    {
                        str = treeView1.SelectedNode.Text.Split(';');
                        textBox1.Text = str[0].Trim();
                        textBox2.Text = str[1].Trim();
                    }
                    catch
                    {
                    }

                }
                else if (contextMenuStrip1.Items[1].Selected)
                {
                    //remove the network node (gets selected node from treeview1_NodeMouseClick):
                    DialogResult dialogResult = MessageBox.Show("Delete Yes/No?", "Remove Network", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                         NetworkList[treeView1.SelectedNode.Index] = null;
                        treeView1.Nodes[treeView1.SelectedNode.Index].Remove();
                        treeView1.SelectedNode = treeView1.Nodes[0];
                        //NetSelected = 
                    }
                }
                else if (contextMenuStrip1.Items[2].Selected) //insert network below
                {
                    addmode = false;
                    editmode = false;
                    button1.Visible = true;
                    button2.Visible = true;
                    textBox1.Visible = true;
                   
                    textBox2.Visible = true;
                    label1.Visible = true;
                    label2.Visible = true;

                }

            }
            catch (System.Exception excep)
            {
                StackTrace stackTrace = new StackTrace();
                MessageBox.Show("In: " + stackTrace.GetFrame(0).GetMethod().Name + ", " + excep.Message);
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
           
            if (selectNet)
            {

                NetSelected = treeView1.SelectedNode.Index;
                
                this.Hide();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //close button
            int i = 0;
            try
            {
                foreach (TreeNode RootNode in treeView1.Nodes)
                {
                    NetworkList[i] = RootNode.Text;
                    i++;
                }
                if (treeView1.SelectedNode != null)
                {
                    NetSelected = treeView1.SelectedNode.Index;
                }
                button1.Visible = false;
                button2.Visible = false;
                textBox1.Visible = false;
                textBox2.Visible = false;
                label1.Visible = false;
                label2.Visible = false;
            }
            catch
            {
               
            }
            this.Hide();
                
        }

        public static bool IsValidIP(string ipval)
        {
            //check to see if string passed is a valid IP address
            if (ipval.Trim() == "")
            {
                return true;
            }
            var octets = ipval.Split('.');

            // if not 4 octets return false
            if (!(octets.Length == 4))
            {

                return false;
            }

            // for each octet
            foreach (var octet in octets)
            {
                int a;
                if (!Int32.TryParse(octet, out a)
                    || !a.ToString().Length.Equals(octet.Length)
                    || a < 0
                    || a > 255) { return false; }

            }

            return true;
        }

        
    }
}
