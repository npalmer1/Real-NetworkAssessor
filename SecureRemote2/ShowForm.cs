using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SecureRemote2
{
    public partial class ShowForm : Form
    {
        private string[] Nm = new string[2];
        public string[] Passvalue
        {
            get { return Nm; }
            set { Nm = value; }
        }
        public ShowForm()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            label1.Text = Nm[0];
            timer1.Interval = Convert.ToInt32(Nm[1]);
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            this.Close();
        }
    }
}
