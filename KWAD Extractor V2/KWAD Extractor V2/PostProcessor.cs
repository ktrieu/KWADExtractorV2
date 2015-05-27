using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class PostProcessor
    {

        string fileDir;

        private int blobSigOffset = 12;

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
            replaced.Write(temp, blobSigOffset, temp.Length - blobSigOffset);
            
        }

        private void processSrf(FileStream file)
        {

        }

        private void processTex(FileStream file)
        {

        }
    }
}
