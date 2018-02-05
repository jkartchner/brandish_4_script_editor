using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    /// <summary>
    /// A class which runs the reversed compression algorithm from Brandish 4 on a given file. Users must know specifically how Brandish 4 files are stored to ensure headers are preserved for decompression (outside of the hack)
    /// </summary>
    class VICompress
    {
        private FileStream m_FileStream;
        private FileInfo m_FileInfo;
        private FileInfo m_oFileInfo;
        private FileStream m_oFileStream;
        /// <summary>
        /// = bytes to read
        /// </summary>
        private ulong m_DataSize;           // full size of file

        private byte[] m_ReadBuffer;
        private byte[] m_WriteBuffer;
        
        /// <summary>
        /// = ref to read buff
        /// </summary>
        private int m_ReadPos;                  
        /// <summary>
        /// = ref to write buff
        /// </summary>
        private int m_WritePos = 0;         
        //private uint m_calcedpalette;
        public bool m_isOverriden = false;

        BinaryReader binRead;
        BinaryWriter binWrite;
        public VICompress()
        {

        }

        /// <summary>
        /// Opens the file to compress and dumps it all into a byte buffer for analysis and compression.
        /// </summary>
        /// <param name="inFilePath">the file to decompress</param>
        /// <param name="outFilePath">the decompressed file to produce</param>
        public void Initialize(string inFilePath, string outFilePath)
        {
            m_FileInfo = new FileInfo(inFilePath);
            m_FileStream = m_FileInfo.OpenRead();
            m_oFileInfo = new FileInfo(outFilePath);
            m_oFileStream = m_oFileInfo.OpenWrite();
            binWrite = new BinaryWriter(m_oFileStream);
            binRead = new BinaryReader(m_FileStream);

            m_ReadBuffer = new byte[m_FileInfo.Length];
            m_WriteBuffer = new byte[2000000];

            m_DataSize = (ulong)m_FileInfo.Length;
            for (int i = 0; (ulong)i < m_DataSize; i++)
            {
                m_ReadBuffer[i] = binRead.ReadByte();
            }
        }

        public bool Compress()
        {
            try
            {
                WriteHeader();
                do
                {
                    WriteIteration();
                } while (m_ReadPos < (int)(m_DataSize - 16));
                WriteEndcap();
                WriteFile();
                return true;
            }
            catch (Exception comex)
            {
                MessageBox.Show(comex.Message);
                return false;
            }
        }

        private void WriteHeader()
        {
            m_WriteBuffer[0] = 0;
            m_WriteBuffer[1] = 0;
            m_WritePos += 2;
            for (int i = 8; i > 0; i--)
            {
                m_WriteBuffer[m_WritePos] = m_ReadBuffer[m_ReadPos];
                m_ReadPos++;
                m_WritePos++;
            }
        }

        private void WriteIteration()
        {
            m_WriteBuffer[m_WritePos] = 0;
            m_WritePos++;
            m_WriteBuffer[m_WritePos] = 0;
            m_WritePos++;
            for (int i = 16; i > 0; i--)
            {
                m_WriteBuffer[m_WritePos] = m_ReadBuffer[m_ReadPos];
                m_ReadPos++;
                m_WritePos++;
            }
        }

        private void WriteEndcap()
        {
            int bytesToAdd = 16 - ((int)m_DataSize - (m_ReadPos));
            m_WriteBuffer[m_WritePos] = 00;             // don't forget to read bytes into the buffer for little endian
            m_WritePos++;
            m_WriteBuffer[m_WritePos] = 00;            
            m_WritePos++;
            for (int i = m_ReadPos; i < (int)m_DataSize; i++)
            {
                m_WriteBuffer[m_WritePos] = m_ReadBuffer[m_ReadPos];
                m_ReadPos++;
                m_WritePos++;
            }
            for (int j = bytesToAdd; j > 0; j--)            // fill in padding so that we make sure there are exactly 16 bites between the two 00 bytes written above and the 0003 integer we'll write below
            {
                m_WriteBuffer[m_WritePos] = 0;
                m_WritePos++;
            }
            m_WriteBuffer[m_WritePos] = 03;
            m_WritePos++;
            m_WriteBuffer[m_WritePos] = 00;
            m_WritePos++;
            m_WriteBuffer[m_WritePos] = 00;     // write at least one full ushort worth of zeros
            m_WritePos++;
            m_WriteBuffer[m_WritePos] = 00;
            m_WritePos++;
        }

        private void WriteFile()
        {
            m_oFileStream.Position = 0;
            binWrite.Write((ushort)m_WritePos);
            for (int q = 0; q < m_WritePos; q++)
            {
                binWrite.Write(m_WriteBuffer[q]);
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
        }
    }

}
