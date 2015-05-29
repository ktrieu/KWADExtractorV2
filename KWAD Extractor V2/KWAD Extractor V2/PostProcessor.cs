using ManagedSquish;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class PostProcessor
    {

        string fileDir;

        private int sigOffset = 12;
        private int srfCompressionBoolOffset = 20;

        Encoding encoding = Encoding.UTF8;

        public PostProcessor(string fileDir)
        {
            this.fileDir = fileDir;
            Console.WriteLine("Processing KLEISRF files");
            //Process KLEISRFs, which confusingly have the extension .tex. These need to be processed first because the .pngs depend on them
            Parallel.ForEach(Directory.EnumerateFiles(fileDir, "*.tex", SearchOption.AllDirectories), path =>
                {
                    FileStream file = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
                    processSrf(file);
                });
            Console.WriteLine("Processing KLEITEX files");
            //Then process the .pngs
            Parallel.ForEach(Directory.EnumerateFiles(fileDir, "*.png", SearchOption.AllDirectories), path =>
                {
                    FileStream file = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
                    processTex(file);
                });
            Console.WriteLine("Processing all other files");
            //Process everything without those extensions
            Parallel.ForEach(Directory.EnumerateFiles(fileDir, "*", SearchOption.AllDirectories), path =>
                {
                    FileStream file = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
                    processFile(file);
                });

        }

        private void processFile(FileStream file)
        {
            byte[] header = new byte[8];
            file.Read(header, 0, 8);
            string headerStr = encoding.GetString(header);
            switch (headerStr)
            {
                case "KLEIBLOB":
                    processBlob(file);
                    break;
                default:
                    break;
            }

        }

        private void processBlob(FileStream file)
        {
            byte[] temp = new byte[file.Length];
            file.Read(temp, 0, (int)file.Length);
            file.Close();
            string writePath = file.Name.Replace("extracted", "processed");
            FileStream replaced = File.Open(writePath, FileMode.OpenOrCreate, FileAccess.Write);
            replaced.Write(temp, sigOffset, temp.Length - sigOffset);
            
        }

        private void processSrf(FileStream file)
        {
            file.Seek(srfCompressionBoolOffset, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(file);
            bool compressed = Convert.ToBoolean(reader.ReadInt32());
            int mipCount = reader.ReadInt32();
            int mipDataSize = reader.ReadInt32();
            for (int i = 0; i < mipCount; i++)
            {
                int size = reader.ReadInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32(); //We're throwing these away for now, but they're being read so we can use them later and to advance the reader
                int dataSize = reader.ReadInt32();
                byte[] data = new byte[dataSize];
                data = reader.ReadBytes(dataSize);
                using (MemoryStream dataStream = new MemoryStream(data, 2, data.Length - 2)) //doesn't include the first two bytes of the array, because of zlibs header
                using (MemoryStream tex = new MemoryStream()) 
                using (DeflateStream defStream = new DeflateStream(dataStream, CompressionMode.Decompress))
                {
                    defStream.CopyTo(tex);
                    string writePath = file.Name.Replace("extracted", "processed").Replace(".tex", ".png");
                    if (compressed)
                    {
                        saveImage(writePath, width, height, Squish.DecompressImage(tex.ToArray(), width, height, SquishFlags.Dxt5));
                    }
                    else
                    {
                        saveImage(writePath, width, height, tex.ToArray());
                    }
                }
            }
        }

        private unsafe void saveImage(string path, int width, int height, byte[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            fixed (byte* dataPtr = data)
            {
                Bitmap bmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, new IntPtr(dataPtr));
                using (FileStream fStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    bmp.Save(fStream, ImageFormat.Png);
                }
            }
        }

        private void processTex(FileStream file)
        {

        }

        private void getAnimFilePath(string path)
        {

        }
    }
}
