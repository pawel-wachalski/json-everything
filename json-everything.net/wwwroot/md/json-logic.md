#  Overview {#logic}

[JsonLogic](https://jsonlogic.com) is a mechanism that can be used to apply logical transformations to JSON values and that is also itself expressed in JSON.

# The syntax {#logic-syntax}

JsonLogic is expressed using single-keyed objects called _rules_.  The key is the operator and the value is (usually) an array containing the parameters for the operation.  Here are a few examples:

- Less than: `{"<" : [1, 2]}`
- Merging arrays: `{"merge":[ [1,2], [3,4] ]}`
- Value detection: `{"in":[ "Ringo", ["John", "Paul", "George", "Ringo"] ]}`

***NOTE** For rules that only have one parameter, that parameter can be expressed directly instead of in an array.  This shorthand is provided as a syntactic sugar.*

While explicitly listing rules is all well and good, the real power of this comes from the ability to reference input data using the `var` operator.  This operator
takes a dot-delimited path that is evaluated on the input object, and the `var` rule is replaced by the resolved value.

So if we want to ensure a value in the input data is less than 2, we could use `{"<": [{"var": "foo.bar"}, 2]}`.  This checks the input value for a `foo` then a `bar` property, returns that value, and compares it against 2.

There are many operators that work on different data types, ranging from string and array manipulation to arithmetic to boolean logic.  The full list is [on their website](https://jsonlogic.com/operations.html), and their docs are pretty good, so I won't repeat the list here.

# In code {#logic-in-code}

The library defines an object model for rules, starting with the `Rule` base class.  This type is fully serializeable, so if you have rules in a text format, just deserialize them to get a `Rule` instance.

```c#
var rule = JsonSerializer.Deserialize<Rule>("{\"<\" : [1, 2]}");
```

Once you have a rule instance, you can apply it using the `.Apply()` method, which takes a `JsonNode?` for the data.  (JSON null and .Net null are unified for this model.)  Sometimes, you may not have a data instance; rather you just want the rule to run.  In these cases you can call `.Apply()` passing a `null` or by using the `.Apply()` extension method which takes no parameters.

```c#
var data = JsonNode.Parse("{\"foo\": \"bar\"}");
var result = rule.Apply(data);
```

In addition to reading and deserializing rules, you can define them inline using the `JsonLogic` static class.  This class defines methods for all of the built-in rules.

Creating the "less than" rule with a variable lookup from above:

```c#
var rule = JsonLogic.LessThan(JsonLogic.Variable("foo.bar"), 2);
```

The `2` here is actually implicitly cast to a `LiteralRule` which is a stand-in for discrete JSON elements.  It can hold any JSON value, and there are implicit casts for numeric, string\*, and boolean types, as well as `JsonElement`.  For arrays and objects, you can either build nodes inline

```c#
new JsonArray { 1, false, "string" };
```

or via `JsonNode.Parse()`.

\* _JSON null literals need to either be cast to `string`, use `JsonNull.Node` from Json.More.Net, or use the provided `LiteralRule.Null`.  All of these result in the same semantic value._

# Gotchas for .Net developers {#logic-gotchas}

In developing this library, I found that many of the operations don't align with similar operations in .Net.  Instead they tend to mimic the behavior of Javascript.  In this section, I'll try to list some of the more significant ones.

## `==` vs `===` {#logic-equality}

`===` defines a "strict" equality.  This is the equality we're all familiar with in .Net.

`==` defines a "loose" equality that can appropriately convert values to like types before performing the comparison.  For example, `"1" == 1` returns true because `"1"` can be converted to a number, and that number strictly equals the number.  More on type conversions later.

The first check is whether they are they same type.  If so, it just applies strict equality.  For the other cases, the following table gives a view of what equals what.  The different cases are evaluated in a top-down sequence.

|a|b|result|
|:-:|:-:|-|
|`null`|anything|`false`|
|anything|`null`|`false`|
|object|anything|`false`|
|anything|object|`false`|
|number|array|convert the array to comma-delimited string and apply loose equality\*\*|
|array|number|convert the array to comma-delimited string and apply loose equality\*\*|
|number|anything|attempt to convert to number and apply strict equality|
|anything|number|attempt to convert to number and apply strict equality|
|array|string|convert the array to comma-delimited string and apply strict equality|
|string|array|convert the array to comma-delimited string and apply strict equality|

That _should_ cover everything, but in case something's missed, it'll just return `false`.

\*\* _These cases effectively mean that the array must have a single element that is loosely equal to the number, though perhaps something like `[1,234]` might pass.  Again, the equality is **very** loose._

## Type conversion {#logic-conversions}

Some operations operate on specific types: sometimes strings, sometimes numbers.  To ensure maximum support, an attempt will be made to convert values to the type that the operation prefers.  If the value cannot be converted, a `JsonLogicException` will be thrown.

Arithmetic operations, like `+` and `-`, and some other operations, like `min` and `max`, will attempt to convert the values to a number.

String operations will attempt to convert to... yeah, strings.

Because `+` supports both numbers (addition) and strings (concatenation); it will try both.

Objects are never converted.

## Automatic array flattening {#logic-array-flattening}

Nested arrays are flattened before being operated upon.  As an example of this, `[["a"]]` is flattened to `["a"]` and `["a",["b"]]` is flattened to `["a","b"]`. 

That's it.  Not much to it; just be aware that it happens.

# Creating new operators {#logic-new-operators}

JSON Logic also supports [adding custom operations](https://jsonlogic.com/add_operation.html).

In C#, your operators will need to derive from the `Rule` abstract class.  There is only a single method to implement, `Apply()`, and you'll need to add an `Operator` attribute.  The logic in the rule doesn't need to be complex, but there are a couple things to be aware of:

- The arguments for your rule must correspond to the parameters of the constructor.
- You're working with `JsonNode`s, so you'll need to detect compatible value types.  There are a few extension methods that you can use, like `.Numberify()`, that try to "fuzzy-cast" to an appropriate value.
- If you encounter invalid input, throw a `JsonLogicException` with an appropriate message.

`Apply()` takes two parameters, both of which are data for variables to act upon.

- `data` represents the external data.
- `contextData` represents data that's passed to it by other rules.

Several rules (`all`, `none`, and `some`) can pass data to their children.  `var` will prioritize `contextData` when attempting to resolve the path.  If `contextData` is (JSON) null or doesn't have data at the indicated path, the path will be resolved against `data`.

It's definitely recommended to go through the [code for the built-in ruleset](https://github.com/gregsdennis/json-everything/tree/master/JsonLogic/Rules) for examples.

Once your rule is defined, it needs to be registered using the `RuleRegistry.Register<T>()` method.  This will allow the rule to be automatically deserialized.

# Overriding existing operators {#logic-overriding}

While this library allows you to inherit from, and therefore override, the default behavior of a `Rule`, you need to be aware of the implications.

The rules in this library implement the Json Logic Specification.  If you override this behavior, then you are no longer implementing that specification, and you lose interoperability with other implementations.  If you want custom behavior _and_ have this custom behavior common across implementations, you'll need to also override the behavior in _every_ implementation and application you use.