using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PSDFile
{
    /// <summary>
    /// The names of the alpha channels.
    /// </summary>
    public class UnicodeAlphaNames : ImageResource
    {

        public override ResourceID ID => ResourceID.UnicodeAlphaNames;

        private List<string> channelNames = new List<string>();
        public List<string> ChannelNames => channelNames;

        public UnicodeAlphaNames() : base(String.Empty)
        {
        }

        public UnicodeAlphaNames(PsdBinaryReader reader, string name, int resourceDataLength)
            : base(name)
        {
            var endPosition = reader.BaseStream.Position + resourceDataLength;

            while (reader.BaseStream.Position < endPosition)
            {
                var channelName = reader.ReadUnicodeString();

                // Photoshop writes out a null terminator for Unicode alpha names.
                // There is no null terminator on other Unicode strings in PSD files.
                if (channelName.EndsWith("\0"))
                {
                    channelName = channelName.Substring(0, channelName.Length - 1);
                }
                ChannelNames.Add(channelName);
            }
        }
        protected override void WriteData(PsdBinaryWriter writer)
        {
            foreach (var channelName in ChannelNames)
            {
                // We must add a null terminator because Photoshop always strips the
                // last character of a Unicode alpha name, even if it is not null.
                writer.WriteUnicodeString(channelName + "\0");
            }
        }
    }
}
