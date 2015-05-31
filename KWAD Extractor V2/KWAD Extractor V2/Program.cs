using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KWAD_Extractor_V2
{
    class Program
    {

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            KWADExtractor extractor = new KWADExtractor("KWAD/", "extracted/");
            watch.Stop();
            Console.WriteLine("Extracted in: " + watch.ElapsedMilliseconds + "ms");
            Console.WriteLine("Beginning file post-processing...");
            watch.Start();
            PostProcessor processor = new PostProcessor(extractor.allFiles);
            watch.Stop();
            Console.WriteLine("Post-processing complete in {0}ms", watch.ElapsedMilliseconds);
            Console.WriteLine("Post-processing complete. Press any key to continue");
            Console.ReadKey();
        }
    }
}
