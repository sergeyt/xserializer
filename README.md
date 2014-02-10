# XSerializer

[![Build Status](https://drone.io/github.com/sergeyt/xserializer/status.png)](https://drone.io/github.com/sergeyt/xserializer/latest)
[![Build Status](https://travis-ci.org/sergeyt/xserializer.png)](https://travis-ci.org/sergeyt/xserializer)

Fast and easy schema-oriented serialization engine for .NET.

## Features

* Multiple output formats XML, JSON, JsonML. Easy to implement custom writer/reader.
* High performance with streaming serialization
* Declarative definition of schema

## Examples

### Example Schema Definition

```C#
var schema =
	Scope.New("http://test.com")
	// custom simple types seriablizable to string
	.Type(s => Length.Parse(s), x => x.IsValid ? x.ToString() : "")
	.Type(s => ExpressionInfo.Parse(s), x => x.ToString())
	.Enum(DataElementOutput.Auto);

schema.Element<Report>()
	.Attributes()
	.Add(x => x.Name)
	.End()
	.Elements()
	.Add(x => x.Width)
	.Add(x => x.Body)
	.End();

schema.Element<Body>()
	.Elements()
	.Add(x => x.Height)
	.Add(x => x.ReportItems)
	.End();

var item = schema.Elem<ReportItem>()
	.Attributes()
	.Add(x => x.Name)
	.End()
	.Elements()
	.Add(x => x.DataElementName)
	.Add(x => x.DataElementOutput)
	.End();

item.Sub<TextBox>()
	.Elements()
	.Add(x => x.Value)
	.End();

item.Sub<Rectangle>()
	.Elements()
	.Add(x => x.ReportItems)
	.End();
```

### Serialization Example

```c#
// schema is defined earlier
var serializer = XSerializer.New(schema);
var report = new Report();
// init report instance
var xml = serializer.ToXmlString(report);
var json = serializer.ToString(report, Format.Json);
```
