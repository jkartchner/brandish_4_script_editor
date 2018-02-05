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
    public partial class HexForm : Form
    {
        private byte[] hexTable;
        Form1 myParent = null;
        public HexForm(Form1 myParent)
        {
            InitializeComponent();
            this.TopMost = true;
            this.myParent = myParent;
            this.Location = new Point((myParent.Location.X + myParent.Size.Width) - (this.Size.Width + 30), 
                (myParent.Location.Y + myParent.Size.Height) - (this.Size.Height + 15));

        }

        public void HexConverter(byte[] hexSource)
        {
            label1.Text = "";
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
            hexTable = new byte[hexSource.Length];
            hexSource.CopyTo(hexTable, 0);
            string hex = BitConverter.ToString(hexTable);
            hex = hex.Replace("-", "  ");
            if (hex.Length > 36)
            {
                label1.Text = hex.Substring(0, 36);
                if (hex.Length > 72)
                {
                    label2.Text = hex.Substring(36, 36);
                    if (hex.Length > 108)
                    {
                        label3.Text = hex.Substring(72, 36);
                        if (hex.Length > 144)
                        {
                            label4.Text = hex.Substring(108, 36);
                            if(hex.Length > 180)
                                label5.Text = hex.Substring(144, 36);
                            else
                                label5.Text = hex.Substring(144, hex.Length - 144);
                        }
                        else
                            label4.Text = hex.Substring(108, hex.Length - 108);
                    }
                    else
                        label3.Text = hex.Substring(72, hex.Length - 72);
                }
                else
                    label2.Text = hex.Substring(36, hex.Length - 36);
            }
            else
                label3.Text = hex;
        }

        private void HexForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            myParent.m_isHexTable = false;
        }

        public void HandleParentRedraw()
        {
            this.Location = new Point((myParent.Location.X + myParent.Size.Width) - (this.Size.Width + 30),
                (myParent.Location.Y + myParent.Size.Height) - (this.Size.Height + 15));
            this.Refresh();
        }
    }
}
