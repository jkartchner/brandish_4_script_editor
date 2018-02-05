using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    class EProcess
    {
        Process p;
        string processName;

        public EProcess(string procName)
        {
            processName = procName;
            LaunchProcess();
        }

        private void LaunchProcess()
        {
            string mrcPath = Application.StartupPath;
            p = new Process();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = mrcPath + processName;
            //psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            p.StartInfo = psi;

            try
            {
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Process Initiation Error:  " + ex.Message);
            }
        }
    }
}
