using System;
using System.Text;
using XmpCore;
using XmpCore.Options;

namespace PSDFile
{
    public class XmpResource : ImageResource
    {
        public IXmpMeta XmpMeta { get; set; }
        public string XmpMetaString
        {
            get => XmpMetaFactory.SerializeToString(XmpMeta, new SerializeOptions());
            set => XmpMeta = XmpMetaFactory.ParseFromString(value, new ParseOptions());
        }

        public XmpResource(string name) : base(name)
        {
        }

        public XmpResource(PsdBinaryReader br, string name, int numBytes) : base(name)
        {
            try
            {
                XmpMeta = XmpMetaFactory.ParseFromString(Encoding.UTF8.GetString(br.ReadBytes(numBytes)));
            }
            catch (Exception e)
            {
                Util.DebugMessage(br.BaseStream,
                    $"Load, Error, XmpResource, ParseXmpMetaData");
            }
        }

        public override ResourceID ID => ResourceID.XmpMetadata;

        protected override void WriteData(PsdBinaryWriter writer)
        {
            if (XmpMeta != null)
            {
                writer.Write(XmpMetaFactory.SerializeToBuffer(XmpMeta, new SerializeOptions()));
            }
            else
            {
                writer.Write(Encoding.UTF8.GetBytes(""));
            }
        }
    }
}
