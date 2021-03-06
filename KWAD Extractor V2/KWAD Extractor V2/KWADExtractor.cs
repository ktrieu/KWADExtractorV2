﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWAD_Extractor_V2
{
    class KWADExtractor
    {
        string extractDir;

        public Dictionary<string, List<VirtualFile>> allFiles = new Dictionary<string, List<VirtualFile>>();

        Dictionary<string, KWADLoader> loaders = new Dictionary<string, KWADLoader>();

        public KWADExtractor(string KWADDir, string extractDir)
        {
            this.extractDir = extractDir;
            Directory.CreateDirectory(extractDir);
            string[] files = Directory.GetFiles(KWADDir);
            foreach (string file in files)
            {
                extract(file, new KWADLoader(file));
            }
        }

        private void extract(string name, KWADLoader loader)
        {
            allFiles.Add(name, loader.files);
            Console.WriteLine("Extracting " + name);
            Console.WriteLine(loader.resourceCount + " files found");
            List<VirtualFile> files = loader.files;
            Parallel.ForEach(files, file =>
                {
                    string path = Path.Combine(extractDir, file.alias);
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                    using (FileStream stream = Util.getFileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        loader.extractRange(file).WriteTo(stream);
                        stream.Flush();
                    }
                }
            );
        }
    }
}
