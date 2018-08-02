using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSDFile
{
    public class LoadContext
    {
        public Encoding Encoding { get; set; }

        public LoadContext()
        {
            Encoding = Encoding.Default;
        }

        public virtual void OnLoadLayersHeader(PsdFile psdFile)
        {
        }

        public virtual void OnLoadLayerHeader(Layer layer)
        {
        }
    }
}
