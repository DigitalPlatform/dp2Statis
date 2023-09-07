using System.Xml;
using System.Linq;

using DigitalPlatform.Xml;
using Newtonsoft.Json.Linq;

namespace UnitTestXml
{
    public class UnitTestXmlDocumentExtension
    {
        [Theory]
        [InlineData("<root />",
            "barcode", "0000001")]
        [InlineData("<root><barcode>0000001</barcode></root>",
            "barcode", "0000002")]
        [InlineData("<root />",
            "barcode", null)]
        [InlineData("<root><barcode>0000001</barcode></root>",
            "barcode", null)]
        [InlineData("<root />",
            "barcode", "")]
        [InlineData("<root><barcode>0000001</barcode></root>",
            "barcode", "")]
        [InlineData("<root><barcode>0000001</barcode><barcode>0000002</barcode></root>",
            "barcode", "0000002")]  // 原有的第二个元素不会被修改
        public void Test_SetElementText(string xml, string path, string value)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);

            // 原有的文本内容
            var nodes = dom.DocumentElement?.SelectNodes(path);
            var old_texts = nodes.Cast<XmlElement>().Select(o => o.InnerText).ToList();

            dom.SetElementText(path, value);

            // 核对修改后的第一个元素
            var node = dom.DocumentElement?.SelectSingleNode(path) as XmlElement;
            if (value == null)
                Assert.Null(node);
            else
                Assert.NotNull(node);
            Assert.Equal(value, node?.InnerText);

            // 比较第一个元素以后的其它元素的文本，相对以前应没有变化
            if (old_texts.Count > 1)
            {
                var new_texts = dom.DocumentElement?.SelectNodes(path)
                    .Cast<XmlElement>()
                    .Select(o => o.InnerText)
                    .ToList();
                Assert.Equal(old_texts.Count, new_texts?.Count);
                for(int i = 1;i<old_texts.Count;i++)
                {
                    Assert.Equal(old_texts[i], new_texts?[i]);
                }
            }
        }

        [Theory]
        [InlineData("<root />",
            "barcode")]
        [InlineData("<root><barcode>0000001</barcode></root>",
            "barcode")]
        [InlineData("<root><barcode>0000001</barcode><barcode>0000002</barcode></root>",
            "barcode")]  // 原有的第二个元素不会被修改
        public void Test_ensureElement(string xml, 
            string path)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);

            // 原有的文本内容
            var nodes = dom.DocumentElement?.SelectNodes(path);
            var old_texts = nodes.Cast<XmlElement>().Select(o => o.InnerText).ToList();

            var element = dom.EnsureElement(path);
            Assert.NotNull(element);
            if (old_texts.Count > 0)
            {
            // 核对修改后的第一个元素
                Assert.Equal(old_texts[0], element.InnerText);
            }
            else
            {
                Assert.Equal("", element.InnerText);
            }

            // 比较第一个元素以后的其它元素的文本，相对以前应没有变化
            if (old_texts.Count > 1)
            {
                var new_texts = dom.DocumentElement?.SelectNodes(path)
                    .Cast<XmlElement>()
                    .Select(o => o.InnerText)
                    .ToList();
                Assert.Equal(old_texts.Count, new_texts?.Count);
                for (int i = 1; i < old_texts.Count; i++)
                {
                    Assert.Equal(old_texts[i], new_texts?[i]);
                }
            }
        }
    }
}