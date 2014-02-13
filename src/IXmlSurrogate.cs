using System.Xml;

namespace TsvBits.Serialization
{
	public interface IXmlSurrogate
	{
		void Read(XmlReader reader, object instance);
		void Write(XmlWriter writer, object instance);
	}
}
