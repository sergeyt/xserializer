# XSerializer

[![Build Status](https://drone.io/github.com/sergeyt/xserializer/status.png)](https://drone.io/github.com/sergeyt/xserializer/latest)
[![Build Status](https://travis-ci.org/sergeyt/xserializer.png)](https://travis-ci.org/sergeyt/xserializer)

Fast and easy schema-oriented serialization engine for .NET.

## Features

* Multiple output formats XML, JSON, JsonML. Easy to implement custom writer/reader.
* High performance with streaming serialization
* Declarative definition of schema

## Sample Usage

### Example Schema Definition

```C#
var schema =
	Scope.New("http://test.com")
	// custom simple types seriablizable to string
	.Type(s => Length.Parse(s), x => x.IsValid ? x.ToString() : "")
	.Type(s => ExpressionInfo.Parse(s), x => x.ToString())
	.Enum(DataElementOutput.Auto);

schema.Elem<Report>()
	.Attr(x => x.Name)
	.Elem(x => x.Width)
	.Elem(x => x.Body);

schema.Elem<Body>()
	.Elem(x => x.Height)
	.Elem(x => x.ReportItems);

var item = schema.Elem<ReportItem>()
	.Attr(x => x.Name)
	.Elem(x => x.DataElementName)
	.Elem(x => x.DataElementOutput);

item.Sub<TextBox>()
	.Elem(x => x.Value);

item.Sub<Rectangle>()
	.Elem(x => x.ReportItems);
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
