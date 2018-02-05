using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    class Parser
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
        private ushort m_CurrentOffset;
        private int m_TotalStrings;
        private int j = 0;

        public Parser()
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
            m_TotalStrings = 0;
            m_Offset = new int[700];
            j = 0;
        }

        /// <summary>
        /// a method to test whether this particular file has been parsed by the script parser or not
        /// </summary>
        /// <returns>false means it has not been parsed</returns>
        public bool TryParse()
        {
            m_FileStream.Position = 0;
            byte ByteTest1 = binRead.ReadByte();
            byte ByteTest2 = binRead.ReadByte();
            byte ByteTest3 = binRead.ReadByte();
            if (ByteTest2 != 0x30 || ByteTest3 != 0x2D)
            {
                if ((ByteTest1 < 127 && ByteTest1 > 31) && (ByteTest2 < 127 && ByteTest2 > 31) && (ByteTest3 < 127 && ByteTest3 > 31))
                    return true;
                else
                    return false;
            }
            else
                return true;
        }

        public void CloseOut()
        {
            m_FileStream.Close();
            m_oFileStream.Close();
        }

        public bool SYParse()               // not at all done very smart
        {
            m_FileStream.Position = 0;
            j = 0;
            ushort Terminator = (ushort)binRead.ReadInt16();    // collect the stopping point the offset parse
            ushort nextOffset;
            m_FileStream.Position = 0;                         // reset stream to 0 for the read
            while(((j + 1) * 2) <= (int)Terminator)                // break   // make sure we read all the way up to and including the last entry
            {
                m_CurrentOffset = (ushort)binRead.ReadInt16();
                if(m_FileStream.Position < (long)Terminator)
                    nextOffset = (ushort)binRead.ReadInt16();               // get the next offset so we just read till the next one begins
                else
                {
                    nextOffset = (ushort)m_DataSize;
                }
                m_FileStream.Position -= 2;
                m_ReadPos = m_CurrentOffset;
                // entering stringread loop
                for (int i = 0; nextOffset != m_ReadPos; i++)    
                {
                    m_CurrentString[i] = m_ReadBuffer[m_ReadPos];
                    m_ReadPos++;
                }
                m_Offset[j] = (int)m_CurrentOffset;        // now stores where each string should have started in the initial file                                // stores the offset for the start of each string
                // string write loop
                string lengthAppended = j.ToString() + "\t\t\t" + m_Offset[j].ToString() + "\n\r___________________________________________\n\r\n\r";
                binWrite.Write(lengthAppended);
                ushort stringLength = (ushort)(nextOffset - m_CurrentOffset);
                for (int l = 0; l <= stringLength; l++)
                {
                    binWrite.Write(m_CurrentString[l]);
                }
                binWrite.Write("\n\r\n\r");
                j++;
            }
            m_TotalStrings = j;
            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
            return true;
        }

        private byte[,] m_StringBuffer;

        public bool SYRePack()
        {
            // initialize important components
            m_StringBuffer = new byte[500, 500];
            m_TotalStrings = 0;
            int offsetSize = 0;
            int[] sizeOffset = new int[500];
            m_Offset = new int[500];
            int l = 0;
            // will be one big loop to pull segments and load into organized array
            for (int i = 0; m_ReadPos < (int)m_DataSize; i++)
            {
                m_ReadPos++;                // skip this weird byte that is a bug in the .ToString() method     // not really necessary
                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)      // read through to first return (count and offset)
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    m_ReadPos++;
                }

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // skip through the return and newline chars
                {
                    m_ReadPos++;
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                }

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)      // read through the ____ underline segment
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    m_ReadPos++;
                }

                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // read through the newline and return chars
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    m_ReadPos++;
                }

                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)      // finally, read the string into the multi array
                {
                    m_StringBuffer[i, l] = m_ReadBuffer[m_ReadPos];
                    m_ReadPos++;
                    l++;
                }

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // read through the newline and return chars
                {
                    m_ReadPos++;
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                }

                m_StringBuffer[i, l] = 0;                                                    // parse routine somehow writes 2 extra bytes at ends of string; we eliminate them here (cause I can't find the problem above)
                l--;
                m_StringBuffer[i, l] = 0;
                l--;
                sizeOffset[i] = l;                                                          // this will store the size of each string in bytes while m_offset will store the offset in the file of the beginning of each string.
                offsetSize = l + offsetSize;
                if (i == 0)
                    m_Offset[i] = sizeOffset[i];
                else
                    m_Offset[i] = offsetSize;
                                                               // record the size of the previous string read in
                l = 0;
                m_TotalStrings++;                                                               // update total strings
            }

            // now we write the organized data
            for(int i = 0; i < m_TotalStrings; i++)                                            // write all the offsets first
            {
                binWrite.Write((ushort)((m_Offset[i] - sizeOffset[i]) + (m_TotalStrings * 2)));         // the proper offset is found by adding the size of the header to the size of the strings (in bytes) preceeding it, but not including the size of the string itself
            }

            for (int i = 0; i <= m_TotalStrings; i++)                                            // now write the strings
            {
                for (int k = 0; k < sizeOffset[i]; k++)
                {
                    binWrite.Write(m_StringBuffer[i, k]);
                }
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();

            return true;
        }

        public bool Parse()
        {
            m_FileStream.Position = 0;
            j = 0;
            ushort Terminator = (ushort)binRead.ReadInt16();    // collect the stopping point the offset parse
            ushort lowestTerminator;
            while (m_FileStream.Position < Terminator)
            {
                lowestTerminator = (ushort)binRead.ReadInt16();
                if (lowestTerminator < Terminator)
                    Terminator = lowestTerminator;
            }                                                   // gather the lowest termination point and read to there
            ushort nextOffset;
            ushort[] headerArray = new ushort[Terminator / 2];
            m_FileStream.Position = 0;                         // reset stream to 0 for the read
            for(int i = 0; m_FileStream.Position < (int)Terminator; i++)                // read the header into an array for sorting
            {
                headerArray[i] = (ushort)binRead.ReadInt16();
            }
            ushort[] sortedHeaderArray = new ushort[Terminator / 2];// now copy the header into a new array--note that a straight assignment provides a ref to the same memory space, not a new allocation
            headerArray.CopyTo(sortedHeaderArray, 0);                           
            Array.Sort(sortedHeaderArray);                               // now we sort the header
            while (j < sortedHeaderArray.Length)                     // break   // make sure we read all the way up to and including the last entry
            {
                m_CurrentOffset = sortedHeaderArray[j];
                if (j < sortedHeaderArray.Length - 1)                       // j must be smaller, not smaller or equal
                    nextOffset = sortedHeaderArray[j + 1];             // get the next offset so we just read till the next one begins
                else
                    nextOffset = (ushort)m_DataSize;
                m_ReadPos = m_CurrentOffset;
                // entering stringread loop
                int k = 0;
                for (int i = 0; nextOffset > m_ReadPos; i++)
                {
                    m_CurrentString[i] = m_ReadBuffer[m_ReadPos];
                    m_ReadPos++;
                    k = i;
                }
                m_CurrentString[k + 1] = 0xFF;                          // place a weird set of bytes to indicate a string end
                m_CurrentString[k + 2] = 0xFF;

                // string write loop
                string lengthAppended = j.ToString() + "----" + sortedHeaderArray[j].ToString() + "\t\t\t" + headerArray[j].ToString() + "\n\r___________________________________________\n\r\n\r"; // we write the 
                binWrite.Write(lengthAppended);
                ushort stringLength = (ushort)((nextOffset - m_CurrentOffset) + 2);
                for (int l = 0; l <= stringLength; l++)
                {
                    binWrite.Write(m_CurrentString[l]);
                }
                binWrite.Write("\n\r\n\r");
                j++;
            }
            m_TotalStrings = j;
            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
            return true;
        }

        public bool RePack()
        {
            // initialize important components
            m_StringBuffer = new byte[500, 1500];
            m_TotalStrings = 0;
            int offsetSize = 0;
            int[] sizeOffset = new int[500];
            m_Offset = new int[500];
            byte[] currentOffset = new byte[5];
            ushort[] headerArray = new ushort[1000];            // this is a ushort array to hold the offset order that was in the original file
            int l = 0;
            // will be one big loop to pull segments and load into organized array
            for (int i = 0; m_ReadPos < (int)m_DataSize; i++)
            {
                //m_ReadPos++;                // skip this weird byte that is a bug in the .ToString() method     // not really necessary
                if (m_ReadPos >= (int)m_DataSize)
                    break;
                while (m_ReadBuffer[m_ReadPos] != 0x09)                                         // read until the first tab
                {
                    m_ReadPos++;
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                }
                if (m_ReadPos >= (int)m_DataSize)
                    break;
                while (m_ReadBuffer[m_ReadPos] == 0x09)                                         // read until the first tab
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    m_ReadPos++;
                }
                for(int u = 0; u < currentOffset.Length; u++)                                   // the memory used by this array is not cleaned by itself---if any bytes remain from a previous loop, and the current loop is shorter than the last, than those bytes from last loop will corrupt the string here; so we clean out the memory
                    currentOffset[u] = 0;
                int q = 0;
                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)      // now we read to gather the offset int
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    currentOffset[q] = m_ReadBuffer[m_ReadPos];
                    q++;
                    m_ReadPos++;
                }

                string s = "";
                s = ASCIIEncoding.ASCII.GetString(currentOffset);                        // now we parse to get the real int of the header
                headerArray[i] = ushort.Parse(s);

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // skip through the return and newline chars
                {
                    m_ReadPos++;
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                }

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)      // read through the ____ underline segment
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    m_ReadPos++;
                }

                int z = 0;
                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // read through the newline and return chars
                {
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                    if (z > 4)                                                                  // break if the actual string is not present to make sure we don't overtake the next return carriage read
                        break;
                    m_ReadPos++;
                    z++;
                }

                if (m_ReadPos >= (int)m_DataSize)
                    break;

                while (m_ReadBuffer[m_ReadPos] != 0xFF || m_ReadBuffer[m_ReadPos + 1] != 0xFF)      // finally, read the string into the multi array
                {
                    m_StringBuffer[i, l] = m_ReadBuffer[m_ReadPos];
                    m_ReadPos++;
                    l++;
                }
                if (l < 6)
                    l = 0;
                m_ReadPos += 2;                 // this is to skip over the 0xFF0xFF bytes indicating the string end (see parse)

                if (m_ReadPos >= (int)m_DataSize)
                    break;
                while (m_ReadBuffer[m_ReadPos] != 0x0D && m_ReadBuffer[m_ReadPos] != 0x0A)
                {
                    m_ReadPos++;
                }
                while (m_ReadBuffer[m_ReadPos] == 0x0D || m_ReadBuffer[m_ReadPos] == 0x0A)      // read through the newline and return chars
                {
                    m_ReadPos++;
                    if (m_ReadPos >= (int)m_DataSize)
                        break;
                }
                //l -= 1;
                sizeOffset[i] = l;                                                          // this will store the size of each string in bytes while m_offset will store the offset in the file of the beginning of each string.
                offsetSize = l + offsetSize;
                if (i == 0)
                    m_Offset[i] = sizeOffset[i];
                else
                    m_Offset[i] = offsetSize;
                // record the size of the previous string read in
                l = 0;
                m_TotalStrings++;                                                               // update total strings
            }
            ushort[] sortedHeaderArray = new ushort[1000];
            // now we write the organized data
            // we must reorganize the header data to make sure it writes according to the original order
            headerArray.CopyTo(sortedHeaderArray, 0);
            Array.Sort(sortedHeaderArray, 0, m_TotalStrings);
            int[] maskArray = new int[m_TotalStrings];                                          // an array to hold the mask for the proper array order
            for (int g = 0; g < m_TotalStrings; g++)
            {
                for (int h = 0; h < m_TotalStrings; h++)
                {
                    if (sortedHeaderArray[g] == headerArray[h])
                    {
                        maskArray[g] = h;                                                       // we fill the mask with the location of the real position a given item should be, as was in the original header; we then apply this later to the array produced for this file
                        headerArray[h] = 65535;                                                 // make sure we fill the original header with something that is impossible to emerge in the real header
                        break;
                    }
                }
            }
            ushort[] sortedRealHeader = new ushort[m_TotalStrings];
            for (int i = 0; i < m_TotalStrings; i++)                                            // write all the offsets into the unsorted array
            {
                sortedRealHeader[i] = (ushort)((m_Offset[i] - sizeOffset[i]) + (m_TotalStrings * 2));         // the proper offset is found by adding the size of the header to the size of the strings (in bytes) preceeding it, but not including the size of the string itself
            }

            // finally, unsort the array with the mask and write that as the file header
            for (int i = 0; i < m_TotalStrings; i++)
            {
                for (int k = 0; k < m_TotalStrings; k++)
                {
                    if (maskArray[k] == i)
                    {
                        binWrite.Write(sortedRealHeader[k]);
                        maskArray[k] = 65535;
                    }
                }
            }

            for (int i = 0; i <= m_TotalStrings; i++)                                            // now write the strings
            {
                for (int k = 0; k < sizeOffset[i]; k++)
                {
                    binWrite.Write(m_StringBuffer[i, k]);
                }
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();

            return true;
        }
    }
}
