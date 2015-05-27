using System;
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
                    FileStream stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
                    loader.extractRange(file).CopyTo(stream);
                    stream.Flush();
                }
            );
        }
    }
}
