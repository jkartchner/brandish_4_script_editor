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
    public partial class ColorFormatTable : Form
    {
        Form1 myParent = null;
        public ColorFormatTable(Form1 myParent)
        {
            InitializeComponent();
            this.TopMost = true;
            this.myParent = myParent;
            this.Location = new Point((myParent.Location.X + myParent.Size.Width) - (this.Size.Width + 15),
                (myParent.Location.Y + myParent.Size.Height) - (this.Size.Height + 225 + 15));
        }

        private void ColorFormatTable_FormClosing(object sender, FormClosingEventArgs e)
        {
            myParent.m_isColorTable = false;
        }

        public void HandleParentRedraw()
        {
            this.Location = new Point((myParent.Location.X + myParent.Size.Width) - (this.Size.Width + 40),
                (myParent.Location.Y + myParent.Size.Height) - (this.Size.Height + 225 + 15));
            this.Refresh();
        }
    }
}
