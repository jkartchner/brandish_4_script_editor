using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    public partial class Form1 : Form
    {
        private string m_FileName;
        private string m_LibFileName;
        private string m_TemporaryFolderPath;
        private BinaryReader binRead;
        private BinaryWriter binWrite;
        private Encoding m_enc;
        private bool m_isLib = false;
        private bool m_is_SY = false;
        private bool m_isParsed = false;
        private bool m_isSplit = false;
        private bool m_isSaved = true;
        private bool m_isLoadedChanged = false;
        public bool m_isHexTable = false;
        public bool m_isColorTable = false;
        private bool m_isIntelliText = true;
        /// <summary>
        /// holds the opened file in a byte array
        /// </summary>
        private byte[] m_ReadBuffer;

        private HexForm hexForm;
        private FindForm findForm;
        private ColorFormatTable colorForm;

        public Form1()
        {
            InitializeComponent();
            hexForm = new HexForm(this);
            colorForm = new ColorFormatTable(this);
            textBox1.AcceptsTab = true;
            m_enc = Encoding.GetEncoding(932);

            aUpdate();
        }

        private void aUpdate()
        {
            AutoUpdate update = new AutoUpdate("http://www.scribesslate.com/brandish4/script/BR4SCRIPT2.exe", "BR4SCRIPT2.exe", "BR4SCRIPT1.exe");

            if (update.Update() == 0)
                this.Close();
        }


        private void AddNewLine()
        {
            if (m_isIntelliText)
                textBox1.SelectionBackColor = Color.CornflowerBlue;
            textBox1.SelectedText = "<";
            textBox1.SelectionBackColor = Color.White;
        }

        private void AddDialogNewLine()
        {
            if (m_isIntelliText)
                textBox1.SelectionBackColor = Color.LightCoral;
            textBox1.SelectedText = "";
            textBox1.SelectionBackColor = Color.White;
        }

        private void ActivateIntelliText()
        {
            if (m_isIntelliText)
            {
                m_isIntelliText = false;
                textBox1.SelectAll();
                textBox1.SelectionBackColor = Color.White;
                textBox1.DeselectAll();
                intelliTextToolStripMenuItem.Checked = false;
            }
            else
            {
                m_isIntelliText = true;
                IntelliText();
                intelliTextToolStripMenuItem.Checked = true;
            }
        }

        public void FindString(string sToFind)
        {
            int location = textBox1.Find(sToFind);
            if (location != -1)
            {
                textBox1.SelectionStart = location;
                textBox1.SelectionLength = sToFind.Length;
            }
            else
            {
                hexForm.TopMost = false;
                if (MessageBox.Show("Could not find string") == DialogResult.OK)
                    hexForm.TopMost = true;
            }
            findForm.FindClose();
        }

        private string STRFormatClip(string item)
        {
            string[] strTest = { item[item.Length - 3].ToString(),
                item[item.Length - 2].ToString(), item[item.Length -1].ToString()};
            string strResult = string.Concat(strTest);
            return strResult;
        }

        private void OpenDecompression()
        {
            switch (STRFormatClip(m_FileName))
            {
                case "LIB":
                    Package pckge = new Package();
                    string hdrFile = dlg.FileName.Substring(0, dlg.FileName.Length - dlg.SafeFileName.Length) +
                        dlg.SafeFileName.Substring(0, dlg.SafeFileName.Length - 4) + ".HDR";
                    pckge.InitializeDump(dlg.FileName, hdrFile);
                    pckge.VIDump();
                    m_TemporaryFolderPath = pckge.TempFolderPath;
                    m_LibFileName = dlg.FileName;

                    m_isLib = true;     // flag to indicate lib unpacking
                    dlg1.InitialDirectory = m_TemporaryFolderPath;
                    dlg1.RestoreDirectory = true;
                    dlg1.Title = "Select the library file to work on";
                    dlg1.Filter = "Unpacked Library Text Files (*._sy)|*._sy";
                    while (true)
                    {
                        if (dlg1.ShowDialog() == DialogResult.OK)
                        {
                            m_FileName = dlg1.FileName;
                            CalcCenter(dlg1.SafeFileName);
                            break;
                        }
                    }
                    goto case "_sy";
                case "_sy":
                    VIDecrypt vDecrypt = new VIDecrypt();
                    vDecrypt.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.LastIndexOf('\\')) +
                        m_FileName.Substring(m_FileName.LastIndexOf('\\'), m_FileName.Length - m_FileName.LastIndexOf('\\') - 3) + "txt");  // = path + name + .txt
                    if (vDecrypt.Decrypt())
                    {
                        m_FileName = m_FileName.Substring(0, m_FileName.LastIndexOf('\\')) +
                            m_FileName.Substring(m_FileName.LastIndexOf('\\'), m_FileName.Length - m_FileName.LastIndexOf('\\') - 3) + "txt";
                        vDecrypt.WriteFile();
                        m_is_SY = true;
                    }
                    else
                    {
                        MessageBox.Show("There was an error decompressing this system file. Try again", "oopsy");
                        try
                        {
                            if (m_TemporaryFolderPath != "" && m_TemporaryFolderPath != null)
                                System.IO.Directory.Delete(m_TemporaryFolderPath, true);
                        }
                        catch (Exception delexe)
                        {
                            MessageBox.Show("Couldn't delete all of the temporary files: " + delexe.Message, "Oops");
                        }
                        m_FileName = "";
                        m_TemporaryFolderPath = "";
                        m_is_SY = false;
                        m_isLib = false;
                        return;
                    }
                    goto case "txt";
                case "txt":
                    if (m_FileName.Contains("itemlist"))
                    {
                        ItemsSplitter splitter = new ItemsSplitter();
                        splitter.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.Length - 4) + "split.txt");
                        splitter.Split();
                        m_FileName = m_FileName.Substring(0, m_FileName.Length - 4) + "split.txt";
                        m_isSplit = true;
                        goto default;
                    }
                    Parser parse = new Parser();
                    parse.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.Length - 4) + "parsed.txt");
                    if (!parse.TryParse())
                    {
                        parse.Parse();
                        m_FileName = m_FileName.Substring(0, m_FileName.Length - 4) + "parsed.txt";
                        m_isParsed = true;
                    }
                    else
                    {
                        parse.CloseOut();
                        System.IO.File.Delete(m_FileName.Substring(0, m_FileName.Length - 4) + "parsed.txt");
                    }
                    goto default;
                default:
                    CalcCenter(m_FileName.Substring(m_FileName.LastIndexOf('\\') + 1, m_FileName.Length - (m_FileName.LastIndexOf('\\') + 1)));
                    FileInfo info = new FileInfo(m_FileName);
                    FileStream stream = info.OpenRead();
                    binRead = new BinaryReader(stream);
                    m_ReadBuffer = binRead.ReadBytes((int)stream.Length);
                    Encoding enc = Encoding.GetEncoding(932);
                    //stream.Close();
                    info = new FileInfo(m_FileName);
                    TextReader streamr = info.OpenText();

                    stream.Close();

                    for (int i = 0; i < m_ReadBuffer.Length; i++)
                    {
                        if (m_ReadBuffer[i] == 0x00)               // 0x00 is read as a string termination
                            m_ReadBuffer[i] = 0x40;                // 0x40 is unlikely (impossible) to occur in a script file
                    }
                    textBox1.Text = enc.GetString(m_ReadBuffer);
                    IntelliText();
                    if (m_isHexTable)
                    {
                        return;
                    }
                    hexForm = new HexForm(this);
                    hexForm.Show();
                    m_isHexTable = true;
                    m_enc = enc;
                    stream.Close();
                    streamr.Close();
                    binRead = null;
                    break;
            }
        }

        private void IntelliText()
        {
            if (!m_isIntelliText)
                return;
            if (textBox1.Text == "")
                return;
            string[] searchText = new string[] {   "@", "", "", "(", "<", "", "x" };
            int[] textLength = new int[] { 1, 1, 2, 3, 3, 2, 2 };
            Color[] cArray = new Color[] { Color.LightBlue, Color.LightPink, Color.PaleVioletRed, Color.LightGreen, Color.CornflowerBlue, Color.LightCoral, Color.Yellow };
            ProgressForm.ShowProgressForm(10500 + 10500, "Identifying Formatting...");
            for (int j = 0; j < 7; j++)
            {
                int[] newLineArray1 = new int[1500];                // could be a misnomer--not really sure if this is a newline
                int m_readPos = 0;
                for (int i = 0; m_readPos < textBox1.Text.Length; i++)
                {
                    newLineArray1[i] = textBox1.Text.IndexOf(searchText[j], m_readPos + 1, (textBox1.Text.Length - 1) - m_readPos);
                    if (newLineArray1[i] == -1)
                    {                                       //    i > 0 && newLineArray1[i] < newLineArray1[i - 1])
                        m_readPos = textBox1.Text.Length;
                        newLineArray1[i] = 0;
                    }
                    else
                        m_readPos = newLineArray1[i];
                    ProgressForm.UpdateStatusp("Searching for     " + searchText[j]);
                }
                int holder = 0;
                for (int i = 0; i < newLineArray1.Length; i++)              // resize the array
                {
                    if (newLineArray1[i] != 0)
                        holder++;
                }
                ProgressForm.UpdateStatusp(false, 1500 - holder);
                ProgressForm.UpdateStatusp(false, 1500 - holder);
                int[] newLineArray = new int[holder];
                for (int i = 0; i < newLineArray.Length; i++)
                    newLineArray[i] = newLineArray1[i];
                for (int i = 0; i < newLineArray.Length; i++)
                {
                    textBox1.SelectionStart = newLineArray[i];
                    textBox1.SelectionLength = textLength[j];
                    textBox1.SelectionBackColor = cArray[j];
                    ProgressForm.UpdateStatusp("Searching for     " + searchText[j]);
                }
            }
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = 0;
            /*if (!m_isColorTable)
            {
                colorForm = new ColorFormatTable(this);
                colorForm.Show();
                m_isColorTable = true;
            }*/
        }

        private void SaveFile()
        {
            if (File.Exists(m_FileName))
            {
                File.Delete(m_FileName);
            }
            else
            {
                dlgSave.Filter = "Text File (*.txt)|*.txt";
                if (dlgSave.ShowDialog() == DialogResult.OK)
                {
                    m_FileName = dlgSave.FileName;
                    CalcCenter(dlgSave.FileName.Substring(dlgSave.FileName.LastIndexOf('\\') + 1, m_FileName.Length - (m_FileName.LastIndexOf('\\') + 1)));
                }
                else
                    return;
            }
            byte[] convertBuffer = m_enc.GetBytes(textBox1.Text);
            for (int a = 0; a < convertBuffer.Length; a++)
            {
                if ((convertBuffer[a] == 0x95 || convertBuffer[a] == 0x45) && convertBuffer[a + 1] == 0xFF)
                    convertBuffer[a] = 0xFF;
            }
            /*int j = 0;
            for (int a = 0; a < convertBuffer.Length; a++)
            {
                if (convertBuffer[a] == 0x00)
                    j++;
            }
            byte[] newBuffer = new byte[convertBuffer.Length - j];
            int c = 0;
            for (int b = 0; b < convertBuffer.Length; b++)
            {
                if(convertBuffer[b] != 0x00)
                {
                    newBuffer[c] = convertBuffer[b];
                    c++;
                }
            }*/
            for (int i = 0; i < convertBuffer.Length; i++)
            {
                if (convertBuffer[i] == 0x40 && convertBuffer[i-1] != 0x81)               // 0x00 is read as a string termination
                    convertBuffer[i] = 0x00;                                         // 0x40 is unlikely (impossible) to occur in a script file
            }
            FileInfo info = new FileInfo(m_FileName);
            FileStream stream = info.OpenWrite();
            binWrite = new BinaryWriter(stream);
            binWrite.Write(convertBuffer);
            binWrite.Close();
            string FileName = "";
            if (m_isSplit)
            {
                ItemsSplitter splitter = new ItemsSplitter();
                splitter.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.Length - 9) + "joined.txt");
                splitter.Join(m_FileName.Substring(0, m_FileName.Length - 9) + ".txt");
                ItemsParser iParsed = new ItemsParser();
                iParsed.Initialize(m_FileName.Substring(0, m_FileName.Length - 9) + "joined.txt", m_FileName.Substring(0, m_FileName.Length - 9) + ".txt");
                m_FileName = m_FileName.Substring(0, m_FileName.Length - 9) + ".txt";
                if (m_is_SY)
                {
                    VICompress vCompress = new VICompress();
                    vCompress.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.Length - 3) + "_sy");
                    vCompress.Compress();
                }
            }
            if (m_isParsed)
            {
                Parser parse = new Parser();
                parse.Initialize(m_FileName, m_FileName.Substring(0, m_FileName.Length - 10) + ".txt");
                if (!parse.RePack())
                {
                    MessageBox.Show("There was a problem saving this file. Make sure you're working only on files that are proven to be ready.", "Oopsy");
                    m_isSaved = false;
                    return;
                }
                else
                {
                    FileName = m_FileName.Substring(0, m_FileName.Length - 10) + ".txt";

                    if (m_is_SY)
                    {
                        VICompress vCompress = new VICompress();
                        vCompress.Initialize(FileName, FileName.Substring(0, FileName.Length - 3) + "_sy");
                        vCompress.Compress();
                    }
                }
            }
            m_isSaved = true;
            CalcCenter(m_FileName.Substring(m_FileName.LastIndexOf('\\') + 1, m_FileName.Length - (m_FileName.LastIndexOf('\\') + 1)));
        }

        private void CloseFile()
        {
            if (!m_isLoadedChanged)             // do nothing if the loaded file has not been changed at all
                return;
            if (!m_isSaved)
            {
                hexForm.TopMost = false;
                if (MessageBox.Show("Are you sure you want to close this file without saving first?", "Oopsy", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    SaveFile();
                    hexForm.TopMost = true;
                    return;
                }
                else
                {
                    textBox1.Text = "";
                    m_isSaved = true;
                    m_isParsed = false;
                    m_isSplit = false;
                    m_is_SY = false;
                    m_isLib = false;
                    m_FileName = "";
                    this.Text = "B4 Script Editor";
                    hexForm.TopMost = true;
                }
            }
            else
            {
                if (m_isLib)
                {
                    Package pckge = new Package();
                    string orighdrFile = m_LibFileName.Substring(0, m_LibFileName.Length - 3) + "HDR";
                    string hdrFile = m_LibFileName.Substring(0, m_LibFileName.Length - 4) + "1.HDR";
                    string hdrSafeFile = hdrFile.Substring(hdrFile.LastIndexOf('\\') + 1, hdrFile.Length - (hdrFile.LastIndexOf('\\') + 1));
                    pckge.InitializeRepackage(orighdrFile, m_TemporaryFolderPath + @"\\", hdrFile, hdrSafeFile);
                    pckge.VIPackage();
                }
                textBox1.Text = "";
                m_isSaved = true;
                m_isParsed = false;
                m_is_SY = false;
                m_isLib = false;
                m_FileName = "";
                this.Text = "B4 Script Editor";
            }

        }

        private void PrepFileName()
        {
            string fname = string.Empty;
            dlg = new OpenFileDialog();

            dlg.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            dlg.Filter = "Brandish 4 Library (*.LIB)|*.LIB|Compressed Text Reference (*._SY)|*._SY|Parsed Text File (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.FilterIndex = 1;
            dlg.RestoreDirectory = true;
        }

        /// <summary>
        /// used to redraw the form text so that the current filename is centered
        /// </summary>
        private void CalcCenter()
        {
            if (dlg == null || dlg.SafeFileName.Equals(""))
                return;
            int center = this.Size.Width / 2;                           // center of the screen
            int textCenter = dlg.SafeFileName.Length / 2;               // center of the text we'll be using
            int multiplier = center / 5;                                // guesstimate for the space (0x20) char = 5 pixels?
            multiplier -= 20 + textCenter;                              // take out the text already being written
            string gap = "";
            for (int i = 0; i < multiplier; i++)
                gap += " ";
            this.Text = "B4 Script Editor" + gap + dlg.SafeFileName;

        }

        /// <summary>
        /// used to redraw the form text so that the current filename is centered
        /// </summary>
        private void CalcCenter(string SafeFileName)
        {
            if (SafeFileName == null || SafeFileName == "")
                return;
            int center = this.Size.Width / 2;                           // center of the screen
            int textCenter = SafeFileName.Length / 2;               // center of the text we'll be using
            int multiplier = center / 5;                                // guesstimate for the space (0x20) char = 5 pixels?
            multiplier -= 20 + textCenter;                              // take out the text already being written
            string gap = "";
            for (int i = 0; i < multiplier; i++)
                gap += " ";
            this.Text = "B4 Script Editor" + gap + SafeFileName;

        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        //CODE FOR CONTROLS
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// makes sure the text box remains consistent with the size of the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Resize(object sender, EventArgs e)
        {
            Control control = (Control)sender;

            textBox1.Size = new Size(control.Size.Width - 15, control.Size.Height - 80);       // resize textbox
            if (statusStrip1.Visible)
                textBox1.Height = control.Size.Height - 100;
            else
                textBox1.Height = control.Size.Height - 80;

            if (m_isHexTable)
                hexForm.HandleParentRedraw();                                                   // adjust text box location
            if (m_isColorTable)
                colorForm.HandleParentRedraw();
            CalcCenter();                                                                       // center top text
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            if (m_isHexTable)
                hexForm.HandleParentRedraw();                                                   // adjust text box location
            if (m_isColorTable)
                colorForm.HandleParentRedraw();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_FileName != "" && m_FileName != null)
                CloseFile();
            try
            {
                if(m_TemporaryFolderPath != "" && m_TemporaryFolderPath != null)
                    System.IO.Directory.Delete(m_TemporaryFolderPath, true);
            }
            catch(Exception delexe)
            {
                MessageBox.Show("Couldn't delete all of the temporary files: " + delexe.Message, "Oops");
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //fontDialog = new FontDialog();
            fontDialog.ShowEffects = false;
            fontDialog.ShowColor = false;
            fontDialog.AllowScriptChange = false;
            if (fontDialog.ShowDialog() != DialogResult.Cancel)
            {
                textBox1.Font = fontDialog.Font;
            }
        }

        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeWordWrapped();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Paste();
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            textBox1.Text = "";
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Cut();
        }

        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Cut();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            textBox1.Copy();
        }

        private void pastseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Paste();
        }

        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            textBox1.Text = "";
        }

        private void toolStripComboBox1_TextChanged(object sender, EventArgs e)
        {
            byte[] convertBuffer;
            switch (toolStripComboBox1.Text)
            {
                case "Shift-JIS":
                    if (m_enc == Encoding.GetEncoding(932))
                        return;
                    m_enc = Encoding.GetEncoding(932);
                    convertBuffer = m_enc.GetBytes(textBox1.Text);
                    textBox1.Text = m_enc.GetString(convertBuffer);
                    break;
                case "UTF-16":
                    if (m_enc == Encoding.GetEncoding("UTF-16"))
                        return;
                    m_enc = Encoding.GetEncoding("UTF-16");
                    convertBuffer = m_enc.GetBytes(textBox1.Text);
                    textBox1.Text = m_enc.GetString(convertBuffer);
                    break;
                case "UTF-8":
                    if (m_enc == Encoding.GetEncoding("UTF-8"))
                        return;
                    m_enc = Encoding.GetEncoding("UTF-8");
                    convertBuffer = m_enc.GetBytes(textBox1.Text);
                    textBox1.Text = m_enc.GetString(convertBuffer);
                    break;
                default:
                    break;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrepFileName();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                m_FileName = dlg.FileName;
            }
            else
                return;
            if (!m_FileName.Equals(""))
                CalcCenter();
            else
                return;
            OpenDecompression();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (m_FileName != "" && m_FileName != null)
            {
                if (!m_isSaved)
                    m_isLoadedChanged = true;
                m_isSaved = false;
                CalcCenter("* " + m_FileName.Substring(m_FileName.LastIndexOf('\\') + 1, m_FileName.Length - (m_FileName.LastIndexOf('\\') + 1)));
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Redo();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.SelectAll();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            textBox1.Undo();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            textBox1.Redo();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            textBox1.Cut();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            textBox1.Paste();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ChangeWordWrapped();
        }

        private void ChangeWordWrapped()
        {
            if (textBox1.WordWrap)
            {
                textBox1.WordWrap = false;
                wordWrapToolStripMenuItem1.Checked = false;
                toolStripButton2.Checked = false;
            }
            else
            {
                textBox1.WordWrap = true;
                wordWrapToolStripMenuItem1.Checked = true;
                toolStripButton2.Checked = true;
            }
        }

        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            textBox1.Copy();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            PrepFileName();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                m_FileName = dlg.FileName;
            }
            else
                return;
            if (!m_FileName.Equals(""))
                CalcCenter();
            else
                return;
            OpenDecompression();
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            CloseFile();
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ChangeWordWrapped();
        }

        private void fontToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            fontDialog.ShowEffects = false;
            fontDialog.ShowColor = false;
            fontDialog.AllowScriptChange = false;
            if (fontDialog.ShowDialog() != DialogResult.Cancel)
            {
                textBox1.Font = fontDialog.Font;
            }
        }

        private void hexTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_isHexTable)
            {
                return;
            }
            hexForm = new HexForm(this);
            hexForm.Show();
            m_isHexTable = true;
        }

        private void textBox1_SelectionChanged(object sender, EventArgs e)
        {
            if (!m_isHexTable)
                return;
            byte[] tempBuffer;
            string selection = textBox1.SelectedText;
            tempBuffer = m_enc.GetBytes(selection);
            hexForm.HexConverter(tempBuffer);

            if (statusStrip1.Visible)
                toolStripStatusLabel1.Text = "Length:       " + textBox1.SelectedText.Length.ToString();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            if (m_isHexTable)
                return;
            hexForm = new HexForm(this);
            hexForm.Show();
            m_isHexTable = true;
        }

        private void viewToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }



        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            findForm = new FindForm(this);
            findForm.Show();
        }

        private void intelliTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ActivateIntelliText();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            ActivateIntelliText();
        }

        private void newLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddNewLine();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            AddNewLine();
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            AddDialogNewLine();
        }

        private void newDialogLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddDialogNewLine();
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!statusStrip1.Visible)
            {
                statusStrip1.Visible = true;
                textBox1.Height -= 20;
            }
            else
            {
                statusStrip1.Visible = false;
                textBox1.Height += 20;
            }
        }
    }
}
