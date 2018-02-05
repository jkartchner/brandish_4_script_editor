using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;



namespace B4ScriptEdit
{
    class AutoUpdate
    {
        private static int Exit_SMOOTH = 0;
        private static int Exit_NOCLOSE = 1;
        private static int Exit_NODOWNLOAD = 2;
        string m_patchName;
        string m_prevPatchName;
        string m_uri;
        WebClient client;
        public AutoUpdate(string uri, string patchName, string prevPatchName)
        {
            m_uri = uri;
            m_patchName = patchName;
            m_prevPatchName = prevPatchName;
            client = new WebClient();
        }

        public int Update()
        {
            try
            {
                if (!File.Exists(m_patchName))
                    client.DownloadFile(m_uri, m_patchName);
                else
                {
                    Assembly oAssembly = Assembly.GetExecutingAssembly();
                    FileVersionInfo oFileVersionInfo = FileVersionInfo.GetVersionInfo(oAssembly.Location);

                    FileVersionInfo iFileVersionInfo = FileVersionInfo.GetVersionInfo(m_patchName);

                    if (oFileVersionInfo.FilePrivatePart == iFileVersionInfo.FilePrivatePart && oFileVersionInfo.FileBuildPart == iFileVersionInfo.FileBuildPart
                        && oFileVersionInfo.FileMinorPart == iFileVersionInfo.FileMinorPart)
                    {
                        File.Delete(m_prevPatchName);
                        return Exit_NODOWNLOAD;
                    }
                }
                if (MessageBox.Show("There is a new update ready to install. Would you like to install it now?", "Updater", MessageBoxButtons.YesNo)
                    == DialogResult.Yes)
                {
                    EProcess proc = new EProcess(m_patchName);
                    return Exit_SMOOTH;
                }
                return Exit_NOCLOSE;
            }
            catch
            {
                return Exit_NODOWNLOAD;
            }
        }
    }
}
