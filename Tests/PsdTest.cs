using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PSDFile;
using XmpCore;

namespace PsdTest
{
    [TestFixture]
    public class PsdTest
    {
        [TestCase]
        public void TestLayer()
        {
            var resPath = Path.Combine(Environment.CurrentDirectory, @"..\..\Res");
            var path = Path.Combine(resPath, "test.psd");
            PsdFile psd = new PsdFile(path, new LoadContext());
            foreach (var image in psd.ImageResources)
            {
                var info = image;
                if (info is XmpRawResource xmp)
                {
                    var t = xmp.XmpInfo;
                    var x = XmpMetaFactory.ParseFromString(t);
                    foreach (var property in x.Properties)
                    {
                        var l = $"Path={property.Path} Namespace={property.Namespace} Value={property.Value}";
                    }
                }
            }
        }

        [TestCase]
        public void TestMake()
        {
            var resPath = Path.Combine(Environment.CurrentDirectory, @"..\..\Res");
            var path = Path.Combine(resPath, "test.xmp");
            PsdFile psd = new PsdFile
            {
                Width = 600,
                Height = 600,
                Resolution = new ResolutionInfo
                {
                    HeightDisplayUnit = ResolutionInfo.Unit.Centimeters,
                    WidthDisplayUnit = ResolutionInfo.Unit.Centimeters,
                    HResDisplayUnit = ResolutionInfo.ResUnit.PxPerInch,
                    VResDisplayUnit = ResolutionInfo.ResUnit.PxPerInch,
                    HDpi = new UFixed16_16(0, 350),
                    VDpi = new UFixed16_16(0, 350)
                },
                ImageCompression = ImageCompression.Rle
            };

            psd.ImageResources.Add(new XmpRawResource("") { XmpInfo = File.ReadAllText(path) });
            psd.Save("xmp.psd", Encoding.UTF8);
        }
    }
}