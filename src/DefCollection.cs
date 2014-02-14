using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace TsvBits.Serialization
{
	internal sealed class DefCollection<T> : IDefCollection<T> where T : IDef
	{
		public static readonly IDefCollection<T> Empty = new EmptyImpl();

		private readonly IList<T> _list = new List<T>();
		private readonly IDictionary<XName, T> _index = new Dictionary<XName, T>();

		public IEnumerator<T> GetEnumerator()
		{
			return _list.GetEnumerator();
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
				return _index.TryGetValue(name, out def) ? def : default(T);
			}
		}

		public void Alias(XName name, T def)
		{
			_index[name] = def;
		}

		public void Add(XName name, T def)
		{
			// TODO override existing
			_index[name] = def;
			_list.Add(def);
		}

		public void AddRange(DefCollection<T> collection)
		{
			foreach (var p in collection._index)
			{
				_index[p.Key] = p.Value;
			}
			foreach (var p in collection._list)
			{
				_list.Add(p);
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