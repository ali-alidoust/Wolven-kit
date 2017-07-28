﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WolvenKit.Cache
{
 /*
    FileTable + "null terminated"
    0x00
    Int32 NULL;
    for(int i = 0;i < filecount;i++)
    {
	    0xC Null byte
	    Int32 Offset;
	    Int32 Size;
	    Int32 Unk1;
	    Int32 NULL;
	    In64 Unk2;
	    0x00
	    Int64 Unk3;
	    0x00
	    Int64 Unk4;
	    Int64 Unk5;
	    Int32 Comtype?
	    Int32 NULL;
	    Int32 Unk6;	
    }   
*/
    public class CollisionCache
    {
        public byte[] IdString = { (byte)'C', (byte)'C', (byte)'3', (byte)'W' };
        public uint Version;
        public uint Unknown1;
        public uint Unknown2;
        public uint FileNameTableEndOffset;
        public uint NumberOfFiles;
        public uint FileNameTableStartOffset;
        public List<string> FileNames;

        public CollisionCache(string Filename)
        {
            this.Read(new BinaryReader(new FileStream(Filename, FileMode.Open)));
        }

        public void Read(BinaryReader br)
        {
            if (!br.ReadBytes(4).SequenceEqual(IdString))
                throw new Exception("Invalid file!");
            this.Version = br.ReadUInt32();
            this.Unknown1 = br.ReadUInt32();
            this.Unknown2 = br.ReadUInt32();
            this.FileNameTableEndOffset = br.ReadUInt32();
            this.NumberOfFiles = br.ReadUInt32();
            this.FileNameTableStartOffset = br.ReadUInt32();
            Console.WriteLine($"IDString: " + new string(Encoding.ASCII.GetChars(IdString)));
            Console.WriteLine("Version: " + Version);
            Console.WriteLine("Unknown1: " + Unknown1);
            Console.WriteLine("Unknown2: " + Unknown2);
            Console.WriteLine("Filename table start: " + FileNameTableStartOffset);
            Console.WriteLine("Filename table end: " + FileNameTableEndOffset);
            Console.WriteLine("Filecount: " + NumberOfFiles);
            Console.WriteLine();
            FileNames = new List<string>();
            br.BaseStream.Seek(this.FileNameTableStartOffset, SeekOrigin.Begin);
            for (int i = 0; i < this.NumberOfFiles; i++)
            {
                this.FileNames.Add(br.ReadCR2WString());
            }
            foreach (var fileName in FileNames)
            {
                br.ReadUInt64();
                br.ReadUInt64();
                Console.WriteLine(fileName);
                Console.WriteLine("\tOffset: " + br.ReadUInt32());
                Console.WriteLine("\tSize: " + br.ReadUInt32());
                Console.WriteLine("\tUnknown 1:" + br.ReadUInt64());
                Console.WriteLine("\tUnknown 2:" + br.ReadUInt64());
                Console.WriteLine("\tUnknown 3:" + br.ReadUInt64());
                Console.WriteLine("\tUnknown 4:" + br.ReadUInt64());
                Console.WriteLine("\tUnknown 5:" + br.ReadUInt64());
                Console.WriteLine("\tCompression type:" + br.ReadUInt64());
            }
        }
    }




    public class ShaderCache
    {
        public byte[] IdString = { (byte)'R', (byte)'D', (byte)'H', (byte)'S' };
        public uint Version;
        public Int64 FileTableOffset1;
        public Int64 FileTableOffset2;
        public Int64 Unk1;

        public List<Tuple<byte[],Int32>> Files = new List<Tuple<byte[], int>>();

        public ShaderCache(string filename)
        {
            this.Read(new BinaryReader(new FileStream(filename, FileMode.Open)));
        }

        public void Read(BinaryReader br)
        {
            br.BaseStream.Seek(-32, SeekOrigin.End);
            FileTableOffset2 = br.ReadInt64();
            Unk1 = br.ReadInt64();
            FileTableOffset1 = br.ReadInt64();
            if (!br.ReadBytes(4).SequenceEqual(IdString))
                throw new Exception("Invalid file!");
            this.Version = br.ReadUInt32();
            var len = 0;
            br.BaseStream.Seek(FileTableOffset1, SeekOrigin.Begin);
            len = br.ReadInt32();
            do
            {
                var info = br.ReadBytes(len);
                var hash = br.ReadInt32();
                br.BaseStream.Seek(0x30, SeekOrigin.Current);
                Files.Add(new Tuple<byte[], int>(info,hash));
                len = br.ReadInt32();
            } while ((len + 0x30 + 4 + br.BaseStream.Position) < br.BaseStream.Length);
        }


    }
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Collision.cache reader test";
            var of = new OpenFileDialog();
            of.Filter = "Cache files | *.cache";
            of.Title = "Please select a witcher 3 cache file";
            if (of.ShowDialog() == DialogResult.OK)
            {
                switch (Cache.GetCacheTypeOfFile(of.FileName))
                {
                    case Cache.Cachetype.Collision:
                    {
                        var cc = new CollisionCache(of.FileName);
                        break;
                    }
                    case Cache.Cachetype.Shader:
                    {
                        var sc = new ShaderCache(of.FileName);
                        Console.WriteLine("IDString: " + new string(Encoding.ASCII.GetChars(sc.IdString)));
                        Console.WriteLine("Version: " + sc.Version);
                        Console.WriteLine("FileTableOffset1: " + sc.FileTableOffset1);
                        Console.WriteLine("FileTableOffset2: " + sc.FileTableOffset2);
                        Console.WriteLine("UNK: " + sc.Unk1);
                        Console.WriteLine("Filecount: " + sc.Files.Count);
                        Console.WriteLine("Version: " + sc.Version);
                        break;
                    }
                    default:
                        break;
                }
            }
            Console.ReadLine();
        }
    }

    public static class BREXT
    {
        public static string ReadCR2WString(this BinaryReader file, int len = 0)
        {
            string str = null;
            if (len > 0)
            {
                str = Encoding.Default.GetString(file.ReadBytes(len));
            }
            else
            {
                var sb = new StringBuilder();
                while (true)
                {
                    var c = (char)file.ReadByte();
                    if (c == 0)
                        break;
                    sb.Append(c);
                }
                str = sb.ToString();
            }
            return str;
        }
    }
}