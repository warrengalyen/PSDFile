using System.Text;

namespace PSDFile
{
    public class XmpRawResource : ImageResource
    {
        public string XmpInfo { get; set; }
        public XmpRawResource(string name) : base(name)
        {
        }

        public XmpRawResource(PsdBinaryReader br, string name, int numBytes) : base(name)
        {
            XmpInfo = Encoding.UTF8.GetString(br.ReadBytes(numBytes));
        }

        public override ResourceID ID => ResourceID.XmpMetadata;

        protected override void WriteData(PsdBinaryWriter writer)
        {
            writer.Write(Encoding.UTF8.GetBytes(XmpInfo));
        }
    }
}