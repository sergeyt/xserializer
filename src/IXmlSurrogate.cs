using System.Xml;

namespace TsvBits.Serialization
{
	/// <summary>
	/// Provides a way to implement custom XML serialization.
	/// </summary>
	public interface IXmlSurrogate
	{
		/// <summary>
		/// Reads object properties.
		/// </summary>
		/// <param name="reader">The XML reader.</param>
		/// <param name="instance">The object to initialize.</param>
		void Read(XmlReader reader, object instance);

		/// <summary>
		/// Writes given object using specified XML writer.
		/// </summary>
		/// <param name="writer">The XML writer.</param>
		/// <param name="instance">The object to write.</param>
		void Write(XmlWriter writer, object instance);
	}
}
