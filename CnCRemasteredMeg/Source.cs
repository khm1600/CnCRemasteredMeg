using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CnCRemasteredMeg
{
    public static class Extractor
    {
        public static void UnpackMegFile(string path, string outputDirectory)
        {
            var names = new List<FileNameTableRecord>();
            var fileinfos = new List<FileTableRecord>();

            using (var s = File.OpenRead(path))
            {
                var magic = ReadUInt32(s);
                if (magic == uint.MaxValue)
                {
                    // file ver 3 unencrypted
                    var magic2 = ReadUInt32(s);
                    if (magic2 != 0x3F7D70A4u)
                    {
                        throw new Exception("Unsupported file.");
                    }
                    var header = new Format3Header();
                    header.dataStart = ReadUInt32(s);
                    header.fileNameCount = ReadUInt32(s);
                    header.fileCount = ReadUInt32(s);
                    header.fileNameTableSize = ReadUInt32(s);

                    // read name table
                    for (int k = 0; k < header.fileNameCount; k++)
                    {
                        var length = ReadUInt16(s);
                        var sb = new StringBuilder(length);
                        for (int i = 0; i < length; i++)
                        {
                            var ch = s.ReadByte();
                            _ = sb.Append((char)ch);
                        }
                        names.Add(new FileNameTableRecord { fileName = sb.ToString(), length = length });
                    }

                    // read file info table
                    for (int k = 0; k < header.fileCount; k++)
                    {
                        _ = ReadUInt16(s);
                        var crc = ReadUInt32(s);
                        var index = ReadUInt32(s);
                        var size = ReadUInt32(s);
                        var beginPosition = ReadUInt32(s);
                        var nameIndex = ReadUInt16(s);
                        fileinfos.Add(new FileTableRecord
                        {
                            beginPosition = beginPosition,
                            crc = crc,
                            index = index,
                            nameIndex = nameIndex,
                            size = size
                        });
                    }
                }
                else
                {
                    throw new Exception("Unsupported file.");
                }
            }

            foreach (var item in fileinfos)
            {
                var name = names[(int)item.nameIndex].fileName;
                OutputFileContent(path, outputDirectory, item, name);
            }
        }

        static uint ReadUInt32(Stream s)
        {
            uint result = 0;

            for (int i = 0; i < 4; i++)
            {
                var b = s.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                result |= (uint)(b & 0xFF) << (i * 8);
            }

            return result;
        }

        static ushort ReadUInt16(Stream s)
        {
            ushort result = 0;

            for (int i = 0; i < 2; i++)
            {
                var b = s.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException();
                }
                result |= (ushort)((b & 0xFF) << (i * 8));
            }

            return result;
        }

        static void OutputFileContent(string path, string targetDirectory, FileTableRecord fileInfo, string name)
        {
            using var s = File.OpenRead(path);
            var str = targetDirectory.Trim('\\') + '\\' + name;
            _ = Directory.CreateDirectory(str);
            Directory.Delete(str);
            using var target = File.Create(str);

            _ = s.Seek(fileInfo.beginPosition, SeekOrigin.Begin);

            for (int i = 0; i < fileInfo.size; i++)
            {
                target.WriteByte((byte)s.ReadByte());
            }
        }
    }
    struct Format3Header
    {
        public uint dataStart;
        public uint fileNameCount;
        public uint fileCount;
        public uint fileNameTableSize;
    }

    class FileNameTableRecord
    {
        public ushort length;
        public string fileName;
    }

    class FileTableRecord
    {
        public uint crc;
        public uint index;
        /// <summary>
        /// in bytes
        /// </summary>
        public uint size;
        /// <summary>
        /// in bytes from the start of the file
        /// </summary>
        public uint beginPosition;
        public uint nameIndex;
    }
}
