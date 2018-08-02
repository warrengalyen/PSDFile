using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSDFile.Compression
{
    internal class RawImage : ImageData
    {
        private byte[] data;

        protected override bool AltersWrittenData => false;

        public RawImage(byte[] data, Size size, int bitDepth) : base(size, bitDepth)
        {
            this.data = data;
        }

        internal override void Read(byte[] buffer)
        {
            Array.Copy(data, buffer, data.Length);
        }

        public override byte[] ReadCompressed()
        {
            return data;
        }

        internal override void WriteInternal(byte[] array)
        {
            data = array;
        }
    }
}
