using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    /// <summary>
    /// A class to examine Brandish 4's libraries and package altered files for a patch
    /// </summary>
    class Package
    {
        private string m_LibFilePath;
        private string m_SafeOutName;
        private string m_FileInPath;
        private string m_FileOutPath;
        private string m_TempFolderPath;
        public string TempFolderPath
        {
            get
            {
                return m_TempFolderPath;
            }
        }

        private FileStream m_FileStream;
        private FileInfo m_FileInfo;
        private FileInfo m_oFileInfo;
        private FileStream m_oFileStream;

        private ulong m_DataSize;
        private byte[] m_ReadBuffer;
        private byte[] m_WriteBuffer;

        /// <summary>
        /// number of files as indicated by the HDR
        /// </summary>
        private int m_NoFiles;
        /// <summary>
        /// = ref to read buff
        /// </summary>
        private int m_ReadPos;
        /// <summary>
        /// = ref to write buff
        /// </summary>
        //private int m_WritePos = 0;

        BinaryReader binRead;
        BinaryWriter binWrite;

        /// <summary>
        /// a buffer, like offsetbuffer and sizebuffer, to hold file names/locations in the library
        /// </summary>
        private string[] m_NameBuffer;
        private uint[] m_OffsetBuffer;
        private uint[] m_SizeBuffer;

        public Package()
        {
            m_DataSize = 0;
        }


        public void InitializeDump(string LibPath, string HDRPath)
        {
            m_LibFilePath = LibPath;
            m_FileInPath = HDRPath;

            m_FileInfo = new FileInfo(HDRPath);
            m_FileStream = m_FileInfo.OpenRead();
            binRead = new BinaryReader(m_FileStream);

            m_DataSize = (ulong)m_FileInfo.Length;
            m_NoFiles = (int)(m_DataSize - 16) / 32;

            m_ReadBuffer = binRead.ReadBytes((int)m_FileStream.Length);

            // reset buffers
            m_NameBuffer = new string[m_NoFiles];
            m_OffsetBuffer = new uint[m_NoFiles];
            m_SizeBuffer = new uint[m_NoFiles];
        }

        /// <summary>
        /// Initializes the class by filling buffers and opening streams
        /// </summary>
        /// <param name="inFilePath">path to the HDR file to model the packaging for the new library file and header</param>
        /// <param name="libFilePath">the path of the folder that contains all the library files to compress</param>
        /// <param name="outFilePath">path to the HDR file that will be produced</param>
        /// <param name="outFileSafe">the SafeFileName of the outFilePath</param>
        public void InitializeRepackage(string inFilePath, string libFilePath, string outFilePath, string outFileSafe)
        {
            m_LibFilePath = libFilePath;
            m_FileInPath = inFilePath;
            m_FileOutPath = outFilePath;
            m_SafeOutName = outFileSafe;

            m_FileInfo = new FileInfo(inFilePath);
            m_FileStream = m_FileInfo.OpenRead();
            m_oFileInfo = new FileInfo(outFilePath);
            m_oFileStream = m_oFileInfo.OpenWrite();
            binWrite = new BinaryWriter(m_oFileStream);
            binRead = new BinaryReader(m_FileStream);

            m_DataSize = (ulong)m_FileInfo.Length;
            m_ReadBuffer = new byte[m_FileInfo.Length];
            m_WriteBuffer = new byte[2000000];

            for (int i = 0; (ulong)i < m_DataSize; i++)
            {
                m_ReadBuffer[i] = binRead.ReadByte();
            }

            m_NoFiles = (int)(m_DataSize - 16) / 32;     // without header, each file will take up exactly 32 bytes in the HDR file

            // reset buffers
            m_NameBuffer = new string[m_NoFiles];
            m_OffsetBuffer = new uint[m_NoFiles];
            m_SizeBuffer = new uint[m_NoFiles];
        }

        public bool VIPackage()
        {
            try
            {
                ReadHDR();
                ReadLibFiles();
                WriteLibFiles();
                return true;
            }
            catch (Exception pckex)
            {
                MessageBox.Show(pckex.Message);
                return false;
            }
        }

        public void VIDump()
        {
            ReadinHDR();
            DumpLibFiles();
        }

        private void DumpLibFiles()
        {
            DirectoryInfo dinfo;
            try
            {
                if(Directory.Exists(Environment.GetEnvironmentVariable("TEMP") + @"\\B4LIB\\"))
                    System.IO.Directory.Delete(Environment.GetEnvironmentVariable("TEMP") + @"\\B4LIB\\", true);
                dinfo = Directory.CreateDirectory(Environment.GetEnvironmentVariable("TEMP") + @"\\B4LIB");
                m_TempFolderPath = dinfo.FullName.ToString();
            }
            catch (Exception dumpexe)
            {
                MessageBox.Show("Please run this script editor as administrator to ensure all files can be appropriately unpackaged into your temporary folder\r\n" + dumpexe.Message);
            }
            ProgressForm.ShowProgressForm(m_NoFiles, "Unpacking library...");
            m_FileInfo = new FileInfo(m_LibFilePath);
            m_FileStream = m_FileInfo.OpenRead();
            binRead = new BinaryReader(m_FileStream);
            for(int i = 0; i < m_NoFiles; i++)
            {
                m_ReadBuffer = new byte[m_SizeBuffer[i]];
                m_FileStream.Position = m_OffsetBuffer[i];
                m_ReadBuffer = binRead.ReadBytes((int)m_SizeBuffer[i]);

                m_oFileInfo = new FileInfo(m_TempFolderPath + "\\" + m_NameBuffer[i].Substring(0, m_NameBuffer[i].IndexOf('\0')));
                m_oFileStream = m_oFileInfo.OpenWrite();
                binWrite = new BinaryWriter(m_oFileStream);
                binWrite.Write(m_ReadBuffer);

                binWrite.Flush();
                m_oFileStream.Close();
                ProgressForm.UpdateStatusp(m_NameBuffer[i]);
            }

            m_FileStream.Close();
            m_oFileStream.Close();
        }

        private void ReadHDR()
        {
            // read the HDR header (16 bytes)
            for (int i = 16; i > 0; i--)
            {
                m_ReadPos++;
            }
            // gather the names in order
            m_FileStream.Position = m_ReadPos;
            for (int j = 0; j < m_NoFiles; j++)
            {
                m_NameBuffer[j] = Encoding.ASCII.GetString(binRead.ReadBytes(16));
                m_ReadPos += 32;
                m_FileStream.Position = m_ReadPos;
            }
        }

        private void ReadinHDR()
        {
            // read the HDR header (16 bytes)
            m_ReadPos = 16;
            // gather the names in order
            m_FileStream.Position = m_ReadPos;
            for (int j = 0; j < m_NoFiles; j++)
            {
                m_NameBuffer[j] = Encoding.ASCII.GetString(binRead.ReadBytes(16));
                m_OffsetBuffer[j] = (uint)binRead.ReadInt32();
                m_SizeBuffer[j] = (uint)binRead.ReadInt32();
                m_ReadPos += 32;
                m_FileStream.Position = m_ReadPos;
            }
            m_FileStream.Flush();
            m_FileStream.Close();
            binRead.Close();
        }

        /// <summary>
        /// open each file in the original HDR files and determine length to determine offset and size in the lib file
        /// </summary>
        private void ReadLibFiles()
        {
            //ProgressForm.ShowProgressForm(m_NoFiles);
            FileInfo info = null;
            int topicInt = 0;
            string topicName = "";
            for (int i = 0; i < m_NoFiles; i++)
            {
                topicInt = m_NameBuffer[i].IndexOf('\0');
                topicName = m_NameBuffer[i].Substring(0, topicInt);
                info = new FileInfo(m_LibFilePath + topicName);
                m_SizeBuffer[i] = (uint)info.Length;
                if (i == 0)
                {
                    m_OffsetBuffer[i] = 0;
                }
                else
                {
                    m_OffsetBuffer[i] = m_OffsetBuffer[i - 1] + m_SizeBuffer[i - 1];
                }
                //ProgressForm.UpdateStatusp();
            }
        }

        private void WriteLibFiles()
        {
            // start by writing the HDR file
            // write the header in the buffer to the actual HDR out file
            for (int q = 0; q < 16; q++)
            {
                binWrite.Write(m_WriteBuffer[q]);
            }
            // write the file info for the HDR file
            string s = "";
            byte[] b = new byte[16];
            for (int i = 0; i < m_NoFiles; i++)
            {
                                                        // 16 bytes     
                s = m_NameBuffer[i];
                b = Encoding.ASCII.GetBytes(s);

                binWrite.Write(b, 0, 16);             // strings start with a byte indicating how long they are; we have to get rid of that       
                binWrite.Write(m_OffsetBuffer[i]);      // 4 bytes
                binWrite.Write(m_SizeBuffer[i]);        // 4 bytes
                binWrite.Write((uint)0);                // write this for the 8 bytes of padding after the size bytes = 32 bytes total per file
                binWrite.Write((uint)0);
                //ProgressForm.UpdateStatusp(true, 0);
            }

            // now switch to writing the LIB file
            // first figure out the out lib file path
            int index =  m_FileOutPath.IndexOf(m_SafeOutName);
            string lbFilePath = m_FileOutPath.Substring(0, index);
            string lbName = m_SafeOutName.Substring(0, m_SafeOutName.Length - 4);
            string libPath = lbFilePath + lbName + @".LIB";

            // make sure previous buffers are closed
            m_FileStream.Flush();
            m_oFileStream.Flush();
            //m_FileStream.Close();
            //m_oFileStream.Close();
            binRead.Close();
            binWrite.Close();

            // now start reading into buffers to copy over
            FileInfo inInfo = null;
            FileStream inStream = null;
            FileInfo outInfo = new FileInfo(libPath);
            FileStream outStream = outInfo.OpenWrite();
            binWrite = new BinaryWriter(outStream);
            m_WriteBuffer = new byte[m_OffsetBuffer[m_NoFiles - 1] + m_SizeBuffer[m_NoFiles - 1]];
            // copy data from each file byte for byte into the new lib file
            ProgressForm.ShowProgressForm((int)(m_OffsetBuffer[m_NoFiles - 1] + m_SizeBuffer[m_NoFiles - 1]), "Packaging new B4 files...");           // start a progress bar cause this will be the majority of the package function
            int topicInt = 0;
            string topicName = "";
            for (int l = 0; l < m_NoFiles; l++)
            {
                topicInt = m_NameBuffer[l].IndexOf('\0');
                topicName = m_NameBuffer[l].Substring(0, topicInt);
                inInfo = new FileInfo(m_LibFilePath + topicName);
                inStream = inInfo.OpenRead();
                binRead = new BinaryReader(inStream);
                // read info into new file read buffer
                m_ReadBuffer = new byte[inInfo.Length];
                for (int k = 0; k < inInfo.Length; k++)
                {
                    m_ReadBuffer[k] = binRead.ReadByte();
                }

                // copy buffered data into new file
                for (int m = 0; m < m_ReadBuffer.Length; m++)
                {
                    binWrite.Write(m_ReadBuffer[m]);
                    ProgressForm.UpdateStatusp("packing " + topicName);

                }

                m_ReadBuffer = null;
                inStream.Flush();
                binRead.Close();
            }
            outStream.Flush();
            binWrite.Close();
        }
    }
}
