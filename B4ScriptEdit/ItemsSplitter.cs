using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace B4ScriptEdit
{
    class ItemsSplitter
    {
                // enter variables here
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

        public bool m_isOverriden = false;

        BinaryReader binRead;
        BinaryWriter binWrite;

        private byte[] m_CurrentString;
        private int[] m_Offset;            // this byte array corresponds exactly to the strings as they are read
        private ushort m_DataOffset;

        public ItemsSplitter()
        {

        }

        /// <summary>
        /// Opens the file to parse and readies the routine to save it as a text file
        /// </summary>
        /// <param name="inFilePath">the file to parse to straight text</param>
        /// <param name="outFilePath">the path and name of the straight text file to produce</param>
        public void Initialize(string inFilePath, string outFilePath)
        {
            if(File.Exists(outFilePath))
                File.Delete(outFilePath);
            m_FileInfo = new FileInfo(inFilePath);
            m_FileStream = m_FileInfo.OpenRead();
            m_oFileInfo = new FileInfo(outFilePath);
            m_oFileStream = m_oFileInfo.OpenWrite();
            binWrite = new BinaryWriter(m_oFileStream);
            binRead = new BinaryReader(m_FileStream);

            m_ReadBuffer = new byte[m_FileInfo.Length];
            m_WriteBuffer = new byte[200000];

            m_DataSize = (ulong)m_FileInfo.Length;
            for (int i = 0; (ulong)i < m_DataSize; i++)
            {
                m_ReadBuffer[i] = binRead.ReadByte();
            }
            m_CurrentString = new byte[10000];
            m_Offset = new int[700];
        }

        public void Split()
        {
            m_FileStream.Position = 29;
            m_DataOffset = (ushort)binRead.ReadInt16();
            m_FileStream.Position = m_DataOffset;
            byte b = 0xFF;
            int count = 0;
            while (m_FileStream.Position < m_FileStream.Length)
            {
                binWrite.Write(count.ToString() + "-------------------------------" + "\n\r\n\r");
                while (b != 0x00)
                {
                    b = binRead.ReadByte();
                    if (m_FileStream.Position >= m_FileStream.Length)
                        break;
                    binWrite.Write(b);
                }
                while (b == 0x00 && m_FileStream.Position < m_FileStream.Length)
                {
                    b = binRead.ReadByte();
                    if (b != 0x00)
                    {
                        m_FileStream.Position -= 1;
                        break;
                    }
                    binWrite.Write(b);
                }
                binWrite.Write("\n\r\n\r");
                count++;
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
        }

        public void Join(string origFileName)
        {
            FileInfo origInfo = new FileInfo(origFileName);
            FileStream origStream = origInfo.OpenRead();
            BinaryReader origRead = new BinaryReader(origStream);
            origStream.Position = 29;
            m_DataOffset = (ushort)origRead.ReadInt16();
            origStream.Position = 0;
            while (origStream.Position < m_DataOffset)
                binWrite.Write(origRead.ReadByte());
            byte b = 0xFF;
            byte c = 0x00;
            m_FileStream.Position = 0;
            while (m_FileStream.Position < m_FileStream.Length)
            {
                while (b != 0x0A && b != 0x0D)
                {
                    while (b != 0x0A && b != 0x0D)
                        b = binRead.ReadByte();
                    b = binRead.ReadByte();
                }
                while (b == 0x0A || b == 0x0D)
                    b = binRead.ReadByte();
                m_FileStream.Position -= 1;
                while (b != 0x0A && b != 0x0D)
                {
                    b = binRead.ReadByte();
                    binWrite.Write(b);
                }
                m_oFileStream.Position -= 2;
                binWrite.Write(c);
                binWrite.Write(c);
                m_oFileStream.Position -= 2;
                while ((b == 0x0A || b == 0x0D) && m_FileStream.Position < m_FileStream.Length)
                    b = binRead.ReadByte();
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            origStream.Flush();
            binRead.Close();
            binWrite.Close();
            origRead.Close();
        }

    }
}
