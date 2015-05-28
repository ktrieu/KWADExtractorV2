using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                case "KLEISRF1":
                    processSrf(file);
                    break;
                case "KLEITEX1":
                    processTex(file);
                    break;
            }

        }

        private void processBlob(FileStream file)
        {
            byte[] temp = new byte[file.Length];
            file.Read(temp, 0, (int)file.Length);
            file.Close();
            FileStream replaced = File.Open(file.Name, FileMode.Open, FileAccess.Write);
            replaced.Write(temp, sigOffset, temp.Length - sigOffset);
            
        }

        private void processSrf(FileStream file)
        {
            file.Seek(srfCompressionBoolOffset, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(file);
            bool compression = Convert.ToBoolean(reader.ReadInt32());
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
                MemoryStream dataStream = new MemoryStream(data, 2, data.Length - 2); //doesn't include the first two bytes of the array, because of zlibs header
                DeflateStream defStream = new DeflateStream(dataStream, CompressionMode.Decompress); 
            }
        }

        private void processTex(FileStream file)
        {

        }
    }
}
