using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSDFile
{
    [Serializable]
    public class PsdInvalidException : Exception
    {
        public PsdInvalidException()
        {
        }

        public PsdInvalidException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class RleException : Exception
    {
        public RleException()
        {
        }

        public RleException(string message) : base(message)
        {
        }
    }
}
