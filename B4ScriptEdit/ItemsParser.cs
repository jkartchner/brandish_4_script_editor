/*
 *  A class to take the itemlist file and recalibrate it once you've translated.
 * The translated file can be done with a hex editor to avoid changing any binary
 * 
 * 
 * 
 * 
 * 
 * ****************************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace B4ScriptEdit
{
    class ItemsParser
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

        /// <summary>
        /// = ref to read buff
        /// </summary>
        private int m_ReadPos;

        public bool m_isOverriden = false;

        BinaryReader binRead;
        BinaryWriter binWrite;

        private byte[] m_CurrentString;
        private int[] m_Offset;            // this byte array corresponds exactly to the strings as they are read
        private ushort m_FirstOffset;
        private ushort m_CurrentOffset;
        private ushort m_ModelOffset;
        private ushort m_FinalOffset;

        public ItemsParser()
        {

        }

        /// <summary>
        /// Opens the file to parse and readies the routine to save it as a text file
        /// </summary>
        /// <param name="inFilePath">the file to parse to straight text</param>
        /// <param name="outFilePath">the path and name of the straight text file to produce</param>
        public void Initialize(string inFilePath, string outFilePath)
        {
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
            ParseItems();
        }

        private void ParseItems()
        {
            m_FileStream.Position = 0;
            while (m_FileStream.Position < 29)
            {
                binWrite.Write(binRead.ReadByte());
            }
            binWrite.Write(binRead.ReadInt16());
            m_FileStream.Position -= 2;
            int backward = m_ReadBuffer.Length - 2;
            while (m_ReadBuffer[backward] != 0x00)
                backward--;
            m_FinalOffset = (ushort)backward;
            m_CurrentOffset = (ushort)binRead.ReadInt16();
            ushort firstOffset = m_CurrentOffset;
            m_FirstOffset = firstOffset;
            FindSporadicNode();

            int i = 0;
            ushort temporaryOffset = 0;
            while (m_ReadPos < firstOffset)             // why did I use insane logic for this? It just kept growing more and more crazy...
            {
                while (m_ReadPos < firstOffset)         // it's easier to understand if you see that I just made the conditions for change checking (and breaking) in the middle of the routines, rather than at the end 
                {
                    
                    while (m_ReadBuffer[m_CurrentOffset + i] != 0x00)
                        i++;
                    while ((m_ReadBuffer[m_CurrentOffset + i] == 0x00 || m_ReadBuffer[m_CurrentOffset + i] == 0x20))
                        i++;
                    for (int l = 0; l < 29; l++)
                        binWrite.Write(binRead.ReadByte());

                    temporaryOffset = (ushort)binRead.ReadInt16();
                    if (temporaryOffset == m_ModelOffset)
                        break;
                    binWrite.Write((ushort)(m_CurrentOffset + i));
                    m_CurrentOffset += (ushort)(i);
                    i = 0;
                    m_ReadPos = (int)m_FileStream.Position;
                }

                if (temporaryOffset == m_ModelOffset)
                {
                    binWrite.Write(m_FinalOffset);
                }
                while (temporaryOffset == m_ModelOffset) 
                {
                    for (int l = 0; l < 29; l++)
                        binWrite.Write(binRead.ReadByte());
                    temporaryOffset = (ushort)binRead.ReadInt16();
                    if (temporaryOffset != m_ModelOffset)
                        break;
                    binWrite.Write(m_FinalOffset);
                }
                if (temporaryOffset != m_ModelOffset && m_FileStream.Position < firstOffset)
                {
                    binWrite.Write((ushort)(m_CurrentOffset + i));
                    m_CurrentOffset += (ushort)(i);
                    i = 0;
                    m_ReadPos = (int)m_FileStream.Position;
                }
            }
            while(m_FileStream.Position < m_FileStream.Length)
                binWrite.Write(binRead.ReadByte());

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
        }

        private void FindSporadicNode()
        {
            m_FileStream.Position += 29;
            List<ushort> array = new List<ushort>();
            while (m_FileStream.Position < m_FirstOffset)
            {
                array.Add((ushort)binRead.ReadInt16());
                m_FileStream.Position += 29;
            }
            array.Sort();
            m_ModelOffset = array[array.Count - 1];
            m_FileStream.Position = 31;
        }
    }
}
