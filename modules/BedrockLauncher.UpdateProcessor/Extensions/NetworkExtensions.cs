using System;
using System.Linq;
using System.Xml.Linq;
using System.Linq;

namespace BedrockLauncher.UpdateProcessor.Extensions
{
    public static class NetworkExtensions
    {
        public static XElement CreateElement(XName name) => new XElement(name);
        public static XElement CreateElement(XName name, string value) => new XElement(name, value);

        public static XAttribute CreateAttribute(XName name, string value) => new XAttribute(name, value);

        public static XAttribute first_attribute(XNode element, string name)
            => (element as XElement)?.Attributes().FirstOrDefault(x => x.Name == name);

        /// <summary>
        /// Returns the next sibling element with the given name, or the immediate next element node
        /// when <paramref name="name"/> is not specified (null).
        /// </summary>
        /// <remarks>
        /// Fix: the original implementation ignored the <paramref name="name"/> parameter entirely
        /// and always returned the first following XElement regardless of its name.
        /// </remarks>
        public static XElement next_sibling(XElement element, XName name)
        {
            var next = element?.NextNode;
            while (next != null)
            {
                if (next is XElement el)
                {
                    if (name == null || el.Name == name)
                        return el;
                }
                next = next.NextNode;
            }
            return null;
        }

        public static XElement first_node(XElement element, XName name)
        {
            var nodes = element?.DescendantsAndSelf();
            return nodes?.FirstOrDefault(x => x.Name == name);
        }

        public static XElement first_node_or_throw(XElement element, XName name)
        {
            try
            {
                var nodes = element.DescendantsAndSelf();
                return nodes.First(x => x.Name == name);
            }
            catch (Exception ex)
            {
                throw new Exception($"first_node_or_throw: element '{name}' not found", ex);
            }
        }

        public static void Save(string fileName, string content)
        {
            XDocument xml = XDocument.Parse(content);
            xml.Save(fileName);
        }
    }
}
