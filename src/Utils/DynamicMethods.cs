using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace TsvBits.Serialization.Utils
{
	/// <summary>
	/// Compiles dynamic methods.
	/// </summary>
	internal static class DynamicMethods
	{
		private static readonly IDictionary<Type, Func<object,object>> UnboxNullableCache = new Dictionary<Type, Func<object, object>>();

		public static Func<object, object> UnboxNullable(Type type)
		{
			Func<object, object> func;
			if (UnboxNullableCache.TryGetValue(type, out func))
				return func;

			var thisArg = Expression.Parameter(type, "target");
			var value = Expression.Property(thisArg, "Value");
			var result = Expression.Convert(value, typeof(object));
			func = Expression.Lambda<Func<object, object>>(result, thisArg).Compile();

			UnboxNullableCache.Add(type, func);

			return func;
		}

		public static Action<object, object> Adder(object target, object item, Type elementType)
		{
			var type = target.GetType();
			var itemType = item != null ? item.GetType() : elementType;
			var method = type.GetMethod("Add", new[] {itemType});
			itemType = method.GetParameters()[0].ParameterType;

			var thisArg = Expression.Parameter(typeof(object), "target");
			var itemArg = Expression.Parameter(typeof(object), "value");
			var call = Expression.Call(Expression.Convert(thisArg, type), method, Expression.Convert(itemArg, itemType));
			return Expression.Lambda<Action<object, object>>(call, thisArg, itemArg).Compile();
		}

		public static Action<T, TValue> Setter<T, TValue>(Expression<Func<T, TValue>> expression)
		{
			var me = (MemberExpression)expression.Body;
			var pi = me.Member as PropertyInfo;
			if (pi != null)
			{
				// TODO: handle collections

				var setMethod = pi.GetSetMethod();
				if (setMethod == null) return null;

				var target = Expression.Parameter(typeof(T), "target");
				var value = Expression.Parameter(typeof(TValue), "value");

				var setter = Expression.Call(target, setMethod, value);
				return Expression.Lambda<Action<T, TValue>>(setter, target, value).Compile();
			}

			var fi = me.Member as FieldInfo;
			if (fi != null)
			{
				if (fi.IsInitOnly) return null;

				return FieldSetter<T, TValue>(fi);
			}

			throw new NotSupportedException();
		}

		private static Action<T, TValue> FieldSetter<T, TValue>(FieldInfo field)
		{
			var d = new DynamicMethod("<dynamic>_" + typeof(T).Name + "_set_field_" + field.Name,
			                          typeof(void), new[] {typeof(T), typeof(TValue)}, typeof(T), true);

			var il = d.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, field);
			il.Emit(OpCodes.Ret);

			return (Action<T, TValue>)d.CreateDelegate(typeof(Action<T, TValue>));
		}
	}
}
