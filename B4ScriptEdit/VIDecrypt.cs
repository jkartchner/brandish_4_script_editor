using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace B4ScriptEdit
{
    /// <summary>
    /// A class which runs the reversed decompression algorithm from Brandish 4 on a given file. Contains limited methods to create bitmap and text files from the decompressed product. Other file types may be created if the decompressed material is properly identified as a given file type.
    /// </summary>
    class VIDecrypt
    {
        public int m_width = 280;
        public int m_height = 120;
        public int m_ArraySize;

        private FileStream m_FileStream;
        private FileInfo m_FileInfo;
        private FileInfo m_oFileInfo;
        private FileStream m_oFileStream;
        /// <summary>
        /// = bytes to read
        /// </summary>
        private ulong m_DataSize;           // full size of file
        private ulong m_ImageSize;          // size of current image file being decompressed
        private byte[] m_ReadBuffer;
        private byte[] m_WriteBuffer;
        private ushort m_OddByte;             // = *****84
        private ushort m_OddHolder;           // = eax after CheckVariables
        private int m_Pos;                  // = ref to read buff
        private int m_WritePos = 0;         // = ref to write buff
        private int m_Count;                // = *****86
        private int m_BytesRead;            // = *****88
        private int m_TotalBytesWritten = 0;

        private ushort m_FileType;

        private int m_esi = 0;              // = common holder for esi register
        private uint m_palettelocation;
        //private uint m_calcedpalette;

        private bool m_Continue = false;
        private bool m_FirstImage = true;
        private bool m_custompalette = false;
        public bool m_isOverriden = false;

        BinaryReader binRead;
        BinaryWriter binWrite;

        public VIDecrypt()
        {

        }

        /// <summary>
        /// Opens the file to decompress and dumps it all into a byte buffer for analysis and decompression.
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

            m_ImageSize = binRead.ReadUInt16();        // this read is particularly important: the read buffer will not contain the first two bytes of the file now
            m_ReadBuffer = new byte[m_FileInfo.Length];
            m_WriteBuffer = new byte[2000000];
            m_DataSize = (ulong)m_FileInfo.Length;
            for (int i = 0; (ulong)i < m_DataSize - 2; i++)
            {
                m_ReadBuffer[i] = binRead.ReadByte();
            }
        }

        public void WriteFile()
        {
            for (int q = 0; q < m_WritePos; q++)
            {
                binWrite.Write(m_WriteBuffer[q]);
            }

            m_FileStream.Flush();
            m_oFileStream.Flush();
            binRead.Close();
            binWrite.Close();
        }

        public void WriteBMP()
        {
            if(m_FileType == m_height)
            {
                m_height = 480;
                m_width = 640;
                WriteBMPHeader();
                    for (int q = m_WritePos; q > 6; q--)
                    {
                        binWrite.Write(m_WriteBuffer[q]);
                    }
                    m_FileStream.Flush();
                    m_oFileStream.Flush();
                    binRead.Close();
                    binWrite.Close();
                    MessageBox.Show("This doesn't look like an image file according to game specs.");
            }
            else
            {
                    m_custompalette = true;
                    WriteBMPHeader();
                    m_custompalette = false;
                    for (int q = (int)(m_WritePos - m_palettelocation - 6); q > 6; q--)
                    {
                        binWrite.Write(m_WriteBuffer[q]);
                    }
                    m_FileStream.Flush();
                    m_oFileStream.Flush();
                    binRead.Close();
                    binWrite.Close();
            }        
        }
        
        public bool Decrypt()
        {
            m_esi = 0;
            //m_Pos += 2;
            while(true)
            {
                // it seems that this main loop in the decompression algorithm is 
                // meant to cut through header information in the compressed file in order to 
                // (a) get header info and (b) identify what needs to start being decompressed.
                // This means that this part in the compression routine needs to create header 
                // information where possible and skip to the actual compression
                if (!m_FirstImage)
                {
                    m_Pos += 2;         // skips the first two bytes only if they're not at the beginning of the file (i.e., at image breaks)
                }                       // this means that the first two bytes are signals for something important in the file--but they are not file size as noted below
                m_FirstImage = false;

                if (m_Pos >= m_FileInfo.Length)
                    break;

                if (m_ReadBuffer[m_Pos] == 0)   // if the first byte (byte 0 in file) is equal to zero, then we start the decompression---this means 0 is a flag for a start of an image that needs decompression
                {
                    PrepDecrypt();             // jump to getting the header of the image that needs decompression
                }
                if (m_Pos >= m_FileInfo.Length)
                    break;
                m_Pos++;
                m_FileStream.Position = m_Pos;
                ushort newdatalength = (ushort)binRead.ReadInt16(); // image size is part of the image header, but not the file header
                // m_Pos++;
                /*try
                {
                    if (m_ReadBuffer[m_Pos] == 0)
                        break;
                }
                catch
                {
                    break;
                }*/
            }
            m_ArraySize = m_WritePos;                               // cleanup by dumping all

            int isimage = BitConverter.ToInt16(m_WriteBuffer, 0);   // determines image size if available
            int imwidth = BitConverter.ToInt16(m_WriteBuffer, 2);
            int imheight = BitConverter.ToInt16(m_WriteBuffer, 4);
            m_height = (int)imheight;
            m_width = (int)imwidth;
            m_FileType = (ushort)isimage;



            return true;
        }

        private void PrepDecrypt()
        {
            m_OddByte = m_ReadBuffer[m_Pos + 1];                    // the second byte in the file (or the first after an image header); oddbyte is important for the compression---see below
            m_Pos += 2;
            m_Count = 8;                                            // number of times to iterate through compression
            m_BytesRead += 4;
            RunDecrypt();
        }
        private bool RunDecrypt()
        {
            try
            {
                m_Continue = false;
                while (true)       //(ulong)m_Pos < m_DataSize) // 004b3edb
                {
                    while (true)                 // the decompression here runs until the oddbyte from the header prep is not even; thus the oddbyte is calced by the number of repeatable bytes that need to be suppressed
                    {
                        CheckVariables();           // notice that this is usually run right before a write cycle--probably means this is an analyzer to determine if something is compressed or not
                        if (m_OddHolder == 0)    // check if the byte is even
                        {
                            m_WriteBuffer[m_WritePos] = m_ReadBuffer[m_Pos];   // write!
                            m_Pos++;
                            m_WritePos++;
                            m_TotalBytesWritten++;
                            m_BytesRead++;
                        }
                        else
                            break;
                    }
                    CheckVariables();        // 004b3f1e                // key for compression is right here---if you want to compress again, you need to hit it here right to setup for the exit below in the else
                    if (m_OddHolder == 0)    // check if the byte is even
                    {
                        m_esi = (int)m_ReadBuffer[m_Pos];
                        m_Pos++;
                        m_BytesRead++;
                    }
                    else                     // 004b3f45
                    {
                        int oddsresult = CalcOdds(5);
                        oddsresult *= 256;
                        oddsresult += m_ReadBuffer[m_Pos];
                        m_Pos++;
                        if (oddsresult == 0)
                            return true;     // jmp 004b40ba
                        else if (oddsresult == 1) // 004b3f76
                        {
                            int y = 0;
                            CheckVariables();
                            if (m_OddHolder == 0) // 004b3f82
                            {
                                y = CalcOdds(4);
                                m_BytesRead += y + 15;   // lea ecx, [eax + F]
                                // have to duplicate code here...probably
                                // jmp 004b3fc0

                                LoopWrite1(y);      // ******fc0
                                m_Continue = true;
                                break;              // jmp *****edb
                            }
                            else                // 004b3f98
                            {
                                y = CalcOdds(4);
                                y *= 256;       // 004b3fa6
                                y += m_ReadBuffer[m_Pos];
                                m_Pos++;
                                m_esi = y + 16; // lea edx, [eax + 10];
                                m_BytesRead += m_esi;

                                LoopWrite1(y);          // ******fc0
                                m_Continue = true;      // jmp 004b3edb
                                break;
                            }
                        }
                        else
                        {
                            m_esi = oddsresult;
                        }
                    }
                    // 4004/6  if junp calls for 4004, esi should be loaded
                    int eax = 0;
                    CheckVariables();
                    if (m_OddHolder == 0)
                    {
                        CheckVariables();       // 004b4017
                        if (m_OddHolder == 0)
                        {
                            CheckVariables();     // 004b4028
                            if (m_OddHolder == 0)
                            {
                                CheckVariables(); // 004b4039
                                if (m_OddHolder == 0)
                                {
                                    CheckVariables();  // 004b404a
                                    if (m_OddHolder == 0)
                                    {
                                        eax = (short)m_ReadBuffer[m_Pos];
                                        m_Pos++;
                                        eax += 14;
                                        m_BytesRead++;
                                    }
                                    else
                                    {
                                        // 004b4071
                                        eax = CalcOdds(3);
                                        eax += 6;
                                    }
                                }
                                else
                                {
                                    eax = 5;
                                    // jmp 004b407e
                                }
                            }
                            else
                            {
                                eax = 4;
                                // jmp 004b407e
                            }
                        }
                        else
                        {
                            eax = 3;
                            // jmp 004b407e
                        }
                    }
                    else
                    {
                        eax = 2;
                        // jmp 004b407e
                    }
                    // 004b407e
                    m_esi = m_esi & 0x0000ffff;
                    ushort si = (ushort)m_esi;
                    ushort ecx = (ushort)(m_WritePos - si);
                    if (eax >= 65535)
                    {
                        m_TotalBytesWritten += eax;
                        m_Continue = true;
                        break;

                    }
                    si = (ushort)eax;
                    byte bl = 0;
                    while (si != 0)
                    {
                        bl = m_WriteBuffer[ecx];
                        m_WriteBuffer[m_WritePos] = bl;
                        ecx++;
                        m_WritePos++;
                        si--;
                    }
                    m_TotalBytesWritten += eax;

                    // test all this
                    // and eax, 0FFFF
                    // jbe 004b40b2?
                    // je 004b4017
                    // mov eax, 2
                    // jmp 004b07

                }
                if (m_Continue)
                {
                    RunDecrypt();
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return true;
            }
        }

        private void CheckVariables()
        {
            if (m_Count == 0)
            {
                m_FileStream.Position = m_Pos + 2;              // remember that the read buffer has 2 less bytes in total, so we compensate here by adding 2 when dealing with a filestream that is not offset like m_pos
                m_OddByte = binRead.ReadUInt16();
                m_Count = 16;
                m_Pos += 2;
                m_BytesRead += 2;
                // m_OddByte = BitConverter.ToUInt16(m_ReadBuffer, m_Pos);
            }
            m_OddHolder = m_OddByte;
            if(m_OddHolder % 2 == 0)
                m_OddHolder = 0;
            else
                m_OddHolder = 1;
            m_OddByte = (ushort)(m_OddByte / 2);
            m_Count--;
        }
         
        private int CalcOdds(int iter)
        {
            if(iter == 0)
                return 0;
            int j = 0;
            int k = 0;
            for(int w = iter; w > 0; w--)
            {
                CheckVariables();
                j = k + k;
                m_OddHolder += (byte)j;
                k = m_OddHolder;
            }
            return k;
        }

        private void LoopWrite1(int y)
        {
            short x = (short)y;
            byte dl = m_ReadBuffer[m_Pos];
            m_Pos++;
            int ecx = x + 14;
            do
            {
                m_WriteBuffer[m_WritePos] = dl;
                m_WritePos++;
                ecx--;
            } while (ecx != 0);
            x += 14;
            m_TotalBytesWritten += x;
        }

        private void WriteBMPHeader()
        {
            WriteHeader();
            WriteDIBHead();
            WriteColorPalette();
        }

        private void WriteHeader()
        {
            byte[] magicnumber = new byte[2];
            magicnumber[0] = 0x42;
            magicnumber[1] = 0x4d;
            uint filesize;
            uint dataoffset;
            if (m_custompalette)
            {
                ushort PaletteSize = (ushort)(m_FileType + 2);
                PaletteSize *= 4;

                //uint icalcedpalette = (uint)(m_WritePos - (m_width * m_height) - 6);
                //uint holder = icalcedpalette / 3;
                //uint calcedpalette = icalcedpalette + holder;
                filesize = (uint)(54 + m_WritePos + PaletteSize);
                dataoffset = (uint)(54 + PaletteSize);
            }
            else
            {
                filesize = (uint)m_WritePos + 1078;
                dataoffset = 1078;
            }
            ushort reservedbyte = 0x0000;
            // end data section

            binWrite.Write(magicnumber[0]);
            binWrite.Write(magicnumber[1]);
            binWrite.Write(filesize);
            binWrite.Write(reservedbyte);
            binWrite.Write(reservedbyte);
            binWrite.Write(dataoffset);
        }

        private void WriteDIBHead()
        {
            uint DIBsize = 40;
            int width = m_width;
            int height = m_height;
            ushort colorplanes = 1;
            ushort bitsperpixel = 8;
            uint compressionmethod = 0;
            uint rawimagesize = 00000000;
            int horizresolution = 0;
            int vertresolution = 0;
            int nocolors = 0;
            int noimportcolors = 0;
            // end data section

            binWrite.Write(DIBsize);
            binWrite.Write(width);
            binWrite.Write(height);
            binWrite.Write(colorplanes);
            binWrite.Write(bitsperpixel);
            binWrite.Write(compressionmethod);
            binWrite.Write(rawimagesize);
            binWrite.Write(horizresolution);
            binWrite.Write(vertresolution);
            binWrite.Write(nocolors);
            binWrite.Write(noimportcolors);
        }

        private void WriteColorPalette()
        {
            if (m_custompalette)
            {
                /*long writepos = (long)m_WritePos;
                long width = (long)m_width;
                long height = (long)m_height;
                long icalcedpalette = (writepos - (width * height) - 6);
                icalcedpalette &= 0x000fffff;*/

                ushort PaletteSize = (ushort)(m_FileType + 2);
                ushort DecompPalSize = (ushort)(PaletteSize * 3);
                m_palettelocation = (uint)DecompPalSize + 6;
                int index = (int)(m_WritePos - DecompPalSize + 6);

                /*long holder = icalcedpalette / 3;
                long calcedpalette = icalcedpalette + holder;

                index &= (int)0x000fffff;
                m_calcedpalette = (uint)icalcedpalette;*/

                byte alpha = 0;
                for (int i = 0; i < PaletteSize; i++)
                {
                    for (int q = 0; q < 4; q++)
                    {
                        if (q == 3)
                            binWrite.Write(alpha);
                        else
                            binWrite.Write(m_WriteBuffer[index + q]); // write the inversed palette (rgb)*/
                    }
                    index += 3;
                }
            }
            else
            {
                FileInfo fi = new FileInfo(Application.StartupPath + @"\\256Palette.rgb");
                FileStream fs = fi.OpenRead();
                BinaryReader br = new BinaryReader(fs);
                for (int i = 0; i < 1024; i++)
                {
                    binWrite.Write(br.ReadByte());
                }
                br.Close();
            }
        }

    }
}
