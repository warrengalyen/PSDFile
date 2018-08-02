using System;

namespace PSDFile
{
    public class LayerUnicodeName : LayerInfo
    {
        public override string Signature => "8BIM";

        public override string Key => "luni";

        public string Name { get; set; }

        public LayerUnicodeName(string name)
        {
            Name = name;
        }

        public LayerUnicodeName(PsdBinaryReader reader)
        {
            Name = reader.ReadUnicodeString();
        }

        protected override void WriteData(PsdBinaryWriter writer)
        {
            var startPosition = writer.BaseStream.Position;

            writer.WriteUnicodeString(Name);
            writer.WritePadding(startPosition, 4);
        }
    }
}
