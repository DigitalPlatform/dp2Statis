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
            "barcode", "0000002")]  // ԭ�еĵڶ���Ԫ�ز��ᱻ�޸�
        public void Test_SetElementText(string xml, string path, string value)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);

            // ԭ�е��ı�����
            var nodes = dom.DocumentElement?.SelectNodes(path);
            var old_texts = nodes.Cast<XmlElement>().Select(o => o.InnerText).ToList();

            dom.SetElementText(path, value);

            // �˶��޸ĺ�ĵ�һ��Ԫ��
            var node = dom.DocumentElement?.SelectSingleNode(path) as XmlElement;
            if (value == null)
                Assert.Null(node);
            else
                Assert.NotNull(node);
            Assert.Equal(value, node?.InnerText);

            // �Ƚϵ�һ��Ԫ���Ժ������Ԫ�ص��ı��������ǰӦû�б仯
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
            "barcode")]  // ԭ�еĵڶ���Ԫ�ز��ᱻ�޸�
        public void Test_ensureElement(string xml, 
            string path)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xml);

            // ԭ�е��ı�����
            var nodes = dom.DocumentElement?.SelectNodes(path);
            var old_texts = nodes.Cast<XmlElement>().Select(o => o.InnerText).ToList();

            var element = dom.EnsureElement(path);
            Assert.NotNull(element);
            if (old_texts.Count > 0)
            {
            // �˶��޸ĺ�ĵ�һ��Ԫ��
                Assert.Equal(old_texts[0], element.InnerText);
            }
            else
            {
                Assert.Equal("", element.InnerText);
            }

            // �Ƚϵ�һ��Ԫ���Ժ������Ԫ�ص��ı��������ǰӦû�б仯
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