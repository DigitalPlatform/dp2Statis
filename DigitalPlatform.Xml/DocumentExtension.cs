using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DigitalPlatform.Xml
{
    // 针对 System.Xml.XmlDocument 进行扩展的一些函数
    // TODO:
    // 1)
    // e1=text1/@attr1=value1/@attr2=value/e2=text2
    // 如何 escape 特殊字符?
    // 2) 链式调用创建元素和属性
    public static class DocumentExtension
    {
        // parameters:
        //      value   打算写入的值。
        //              如果为 null，表示要删除这个 element(如果 element 已经存在)
        public static void SetElementText(this XmlDocument dom,
    string path,
    string? value)
        {
            if (dom.DocumentElement == null)
                throw new ArgumentException("_dom.DocumentElement 不应为 null");

            var element = dom.DocumentElement.SelectSingleNode(path) as XmlElement;
            if (element == null)
            {
                if (value == null)
                    return;
                // 逐级创建元素
                element = CreateElementByPath(dom.DocumentElement, path);
            }

            if (value == null)
            {
                if (element.ParentNode != null)
                    element.ParentNode.RemoveChild(element);
                return;
            }

            element.InnerText = value;
        }

        public static XmlElement EnsureElement(this XmlDocument dom,
string path)
        {
            if (dom.DocumentElement == null)
                throw new ArgumentException("_dom.DocumentElement 不应为 null");

            var element = dom.DocumentElement.SelectSingleNode(path) as XmlElement;
            if (element == null)
            {
                // 逐级创建元素
                element = CreateElementByPath(dom.DocumentElement, path);
            }

            return element;
        }

        // 根据指定的 XPath 找到元素，并找到指定的属性值
        public static string? GetElementAttribute(this XmlDocument dom,
string path,
string attr_name,
string? default_value = null)
        {
            if (dom.DocumentElement == null)
                throw new ArgumentException("_dom.DocumentElement 不应为 null");

            if (dom.DocumentElement.SelectSingleNode(path) is not XmlElement element)
                return default_value;

            return element.GetAttribute(attr_name);
        }

        // 根据指定的 XPath 确保创建元素，并设置指定的属性值
        // parameters:
        //      attr_value  要设置的属性值。如果为 null，表示要删除此属性节点

        public static void SetElementAttribute(this XmlDocument dom,
string path,
string attr_name,
string? attr_value)
        {
            if (dom.DocumentElement == null)
                throw new ArgumentException("_dom.DocumentElement 不应为 null");

            var element = dom.DocumentElement.SelectSingleNode(path) as XmlElement;
            if (element == null)
            {
                // 逐级创建元素
                element = CreateElementByPath(dom.DocumentElement, path);
            }

            if (attr_value == null)
            {
                element.RemoveAttributeNode(attr_name, null);
                return;
            }
            element.SetAttribute(attr_name, attr_value);
        }

        // 根据名称数组逐级创建节点
        // parameters:
        //      start    根节点
        //      names   节点名称数组
        // return:
        //      返回新创建的XmlNode节点
        public static XmlElement CreateElement(
            XmlElement start,
            string[] names)
        {
            if (start == null)
                throw new ArgumentException($"{nameof(start)} 参数值不允许为 null");

            var dom = start.OwnerDocument ?? throw new Exception("start.OwnerDocument 为 null");
            if (names == null)
                throw new ArgumentException($"{nameof(names)} 参数值不允许为 null");

            if (names.Length == 0)
                throw new ArgumentException($"{nameof(names)} 参数值长度不允许为 0");

            int i = 0;
            if (names[0] == "")
                i = 1;

            XmlElement current_element = start;
            for (; i < names.Length; i++)
            {
                string strOneName = names[i];
                if (strOneName == "")
                    throw new Exception($"第 {i} 级的名称不应为空");

                XmlElement? temp = null;
                temp = current_element.SelectSingleNode(strOneName) as XmlElement;
                if (temp == null)
                {
                    char firstChar = strOneName[0];
                    if (firstChar == '@')
                    {
                        throw new Exception($"不允许使用 @");
#if REMOVED
                        if (i != names.Length - 1)
                            throw new Exception("只有最后一级才允许使用 @ 符号");

                        string strAttrName = strOneName.Substring(1);
                        if (strAttrName == "")
                            throw new Exception($"第 {i} 级的属性名称不应为空");
                        current_element.SetAttribute(strAttrName, "");
                        Debug.Assert(current_element.GetAttributeNode(strAttrName) != null);
                        /*
                        {
                            temp = nodeCurrent.SelectSingleNode("@" + strAttrName);
                            if (temp == null)
                                throw new Exception("已经创建了'" + strAttrName + "'属性，不可能找不到。");
                        }
                        */
                        temp = current_element;
#endif
                    }
                    else
                    {
                        temp = dom.CreateElement(strOneName);
                        current_element.AppendChild(temp);
                    }
                }
                current_element = temp;
            }

            return current_element;
        }

        public static XmlElement CreateElementByPath(XmlElement start,
    string path)
        {
            if (start.SelectSingleNode(path) is XmlElement exist)
                return exist;

            string[] names = path.Split(new Char[] { '/' });
            return CreateElement(start, names);
        }
    }
}
