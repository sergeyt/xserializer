using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	internal sealed class DefCollection<T> : IDefCollection<T> where T : IDef
	{
		public static readonly IDefCollection<T> Empty = new EmptyImpl();

		private readonly IDictionary<XName, T> _store = new Dictionary<XName, T>();

		public IEnumerator<T> GetEnumerator()
		{
			return _store.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public T this[XName name]
		{
			get
			{
				T def;
				return _store.TryGetValue(name, out def) ? def : default(T);
			}
		}

		public void Add(XName name, T def)
		{
			_store[name] = def;
		}

		public void AddRange(DefCollection<T> collection)
		{
			foreach (var p in collection._store)
			{
				Add(p.Key, p.Value);
			}
		}

		private sealed class EmptyImpl : IDefCollection<T>
		{
			public IEnumerator<T> GetEnumerator()
			{
				yield break;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public T this[XName name]
			{
				get { return default(T); }
			}
		}
	}
}