namespace TsvBits.Serialization
{
	public enum Format
	{
		Xml,
#if FULL
		Json,
		JsonML,
		Bson,
		Yaml,
		Soap,
#endif
	};
}