using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    public partial class FindForm : Form
    {
        private Form1 myParent = null;
        public FindForm(Form1 myParent)
        {
            InitializeComponent();
            this.TopMost = true;
            this.myParent = myParent;
            this.button1.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            myParent.FindString(textBox1.Text);
        }

        public void FindClose()
        {
            this.Close();
        }

        private void FindForm_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void FindForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                myParent.FindString(textBox1.Text);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                myParent.FindString(textBox1.Text);
        }
    }
}
