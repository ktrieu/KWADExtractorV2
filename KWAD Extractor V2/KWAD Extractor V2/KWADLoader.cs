using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class KWADLoader
    {

        byte[] fileBytes;

        private int resourceCountOffset = 16;
        private int resourceInfoOffset = 20;
        private int resourceInfoSize = 16;

        Encoding encoding = UTF8Encoding.UTF8;

        public List<VirtualFile> files;
        public int resourceCount;

        public KWADLoader(string filename)
        {
            fileBytes = File.ReadAllBytes(filename);
            readHeader();
            readMetaData();
        }

        public byte[] extractRange(int start, int length)
        {
            return fileBytes.Skip(start).Take(length).ToArray();
        }

        private void readMetaData()
        {
            //Get count of resources
            resourceCount = BitConverter.ToInt32(fileBytes, resourceCountOffset);
            //Init the stored file array to the same amount
            files = new List<VirtualFile>(resourceCount);
            MemoryStream memStream = new MemoryStream(fileBytes); //create a stream to deal with the rest of the file
            memStream.Seek(resourceInfoOffset, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(memStream);
            for (int i = 0; i < resourceCount; i++)
            {
                VirtualFile file = new VirtualFile();
                int slabIndex = reader.ReadInt32(); //For now, we throw this away, but its being kept for later
                file.size = reader.ReadInt32();
                file.offset = reader.ReadInt32();
                file.type = encoding.GetString(reader.ReadBytes(4));
                files.Add(file);
            }
            int aliasCount = reader.ReadInt32();
            for (int i = 0; i < aliasCount; i++)
            {
                int aliasLen = reader.ReadInt32();
                int paddedLen = aliasLen + (4 - aliasLen % 4) % 4; //taken straight from the Klei specification
                byte[] aliasBytes = reader.ReadBytes(paddedLen);
                string alias = encoding.GetString(aliasBytes).Substring(0, aliasLen);
                int aliasIndex = reader.ReadInt32();
                files[aliasIndex].alias = alias;
            }
        }

        private void readHeader() 
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string header = "KLEIPKG2";
            string fileHeader = encoding.GetString(fileBytes.Take(8).ToArray());
            if (fileHeader != header)
            {
                Console.WriteLine("Invalid header found. File may be corrupt.");
            }
        }


    }
}
