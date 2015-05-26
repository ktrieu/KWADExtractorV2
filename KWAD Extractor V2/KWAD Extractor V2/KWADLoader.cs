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

        List<VirtualFile> files;

        public KWADLoader(string filename)
        {
            Console.WriteLine("Loading " + filename);
            fileBytes = File.ReadAllBytes(filename);
            readHeader();
            readMetaData();
        }

        private void readMetaData()
        {
            int resourceCount = BitConverter.ToInt32(fileBytes, resourceCountOffset);
            Console.WriteLine(resourceCount);
        }

        private void readHeader() 
        {
            BinaryFormatter formatter = new BinaryFormatter();
            string header = "KLEIPKG2";
            string fileHeader = UTF8Encoding.GetEncoding("UTF-8").GetString(fileBytes.Take(8).ToArray());
            if (fileHeader != header)
            {
                Console.WriteLine("Invalid header found. File may be corrupt.");
            }
        }


    }
}
