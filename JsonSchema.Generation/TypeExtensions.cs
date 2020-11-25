﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Provides informative methods for types.
	/// </summary>
	public static class TypeExtensions
	{
		/// <summary>
		/// Determines whether the type is considered an integer.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>true if it represents an integer; false otherwise.</returns>
		public static bool IsInteger(this Type type)
		{
			return type == typeof(byte) ||
			       type == typeof(short) ||
			       type == typeof(ushort) ||
			       type == typeof(int) ||
			       type == typeof(uint) ||
			       type == typeof(long) ||
			       type == typeof(ulong);
		}

		/// <summary>
		/// Determines whether the type is a non-integer floating point number.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>true if it represents a floating-point number; false otherwise.</returns>
		public static bool IsFloatingPoint(this Type type)
		{
			return type == typeof(float) ||
			       type == typeof(double) ||
			       type == typeof(decimal);
		}

		/// <summary>
		/// Determines whether the type is a number.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>true if it represents a number; false otherwise.</returns>
		public static bool IsNumber(this Type type)
		{
			return type.IsInteger() || type.IsFloatingPoint();
		}

		/// <summary>
		/// Determines whether the type is a simple, one-dimensional, non-keyed collection.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>true if the type represents an array; false otherwise.</returns>
		public static bool IsArray(this Type type)
		{
			return type.IsArray ||
			       type == typeof(Array) ||
			       typeof(IEnumerable).IsAssignableFrom(type);
		}

		internal static int GetAttributeSetHashCode(this IEnumerable<Attribute> items)
		{
			unchecked
			{
				int hashCode = 0;
				foreach (var item in items)
				{
					hashCode = (hashCode * 397) ^ item.GetHashCode();
				}
				return hashCode;
			}
		}
	}
}
