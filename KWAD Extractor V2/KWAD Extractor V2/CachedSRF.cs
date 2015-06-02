using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KWAD_Extractor_V2
{
    class CachedSRF
    {
        public float[] transform;
        public String texPath;
        public String alias;

        public CachedSRF(float[] transform, string texPath, string alias)
        {
            this.texPath = texPath;
            this.transform = transform;
            this.alias = alias;
        }
    }
}
