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
            KWADExtractor extractor = new KWADExtractor("KWAD/", "extracted/");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            extractor.extract();
            timer.Stop();
            Console.WriteLine("Extraction complete. Press any key to exit...");
            Console.WriteLine("Extracted in: " + timer.ElapsedMilliseconds + "ms");
            Console.ReadKey();
        }
    }
}
