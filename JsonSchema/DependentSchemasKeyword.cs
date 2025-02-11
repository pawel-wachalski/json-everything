﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Json.Schema;

/// <summary>
/// Handles `dependentSchemas`.
/// </summary>
[SchemaPriority(10)]
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[JsonConverter(typeof(DependentSchemasKeywordJsonConverter))]
public class DependentSchemasKeyword : IJsonSchemaKeyword, IKeyedSchemaCollector, IEquatable<DependentSchemasKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "dependentSchemas";

	/// <summary>
	/// The collection of "schema"-type dependencies.
	/// </summary>
	public IReadOnlyDictionary<string, JsonSchema> Schemas { get; }

	/// <summary>
	/// Creates a new <see cref="DependentSchemasKeyword"/>.
	/// </summary>
	/// <param name="values">The collection of "schema"-type dependencies.</param>
	public DependentSchemasKeyword(IReadOnlyDictionary<string, JsonSchema> values)
	{
		Schemas = values ?? throw new ArgumentNullException(nameof(values));
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.Object)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		var obj = (JsonObject)context.LocalInstance!;
		if (!obj.VerifyJsonObject()) return;

		var overallResult = true;
		var evaluatedProperties = new List<string>();
		foreach (var property in Schemas)
		{
			context.Options.LogIndentLevel++;
			context.Log(() => $"Evaluating property '{property.Key}'.");
			var schema = property.Value;
			var name = property.Key;
			if (!obj.TryGetPropertyValue(name, out _))
			{
				context.Log(() => $"Property '{property.Key}' does not exist. Skipping.");
				continue;
			}

			context.Push(context.EvaluationPath.Combine(name), schema);
			context.Evaluate();
			overallResult &= context.LocalResult.IsValid;
			if (!overallResult && context.ApplyOptimizations) break;

			if (context.LocalResult.IsValid)
				evaluatedProperties.Add(name);
			context.Log(() => $"Property '{property.Key}' {context.LocalResult.IsValid.GetValidityString()}.");
			context.Options.LogIndentLevel--;
			context.Pop();
		}

		if (!overallResult)
			context.LocalResult.Fail(Name, ErrorMessages.DependentSchemas, ("failed", evaluatedProperties));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(DependentSchemasKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		if (Schemas.Count != other.Schemas.Count) return false;
		var byKey = Schemas.Join(other.Schemas,
				td => td.Key,
				od => od.Key,
				(td, od) => new { ThisDef = td.Value, OtherDef = od.Value })
			.ToArray();
		if (byKey.Length != Schemas.Count) return false;

		return byKey.All(g => Equals(g.ThisDef, g.OtherDef));
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as DependentSchemasKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schemas.GetStringDictionaryHashCode();
	}
}

internal class DependentSchemasKeywordJsonConverter : JsonConverter<DependentSchemasKeyword>
{
	public override DependentSchemasKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected object");

		var schema = JsonSerializer.Deserialize<Dictionary<string, JsonSchema>>(ref reader, options)!;
		return new DependentSchemasKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, DependentSchemasKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(DependentSchemasKeyword.Name);
		writer.WriteStartObject();
		foreach (var kvp in value.Schemas)
		{
			writer.WritePropertyName(kvp.Key);
			JsonSerializer.Serialize(writer, kvp.Value, options);
		}
		writer.WriteEndObject();
	}
}

public static partial class ErrorMessages
{
	private static string? _dependentSchemas;

	/// <summary>
	/// Gets or sets the error message for <see cref="DependentSchemasKeyword"/>.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[value]] - the value in the schema
	/// </remarks>
	public static string DependentSchemas
	{
		get => _dependentSchemas ?? Get();
		set => _dependentSchemas = value;
	}
}