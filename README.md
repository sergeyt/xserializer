[![Build Status](https://drone.io/github.com/sergeyt/xserializer/status.png)](https://drone.io/github.com/sergeyt/xserializer/latest)
[![Build Status](https://travis-ci.org/sergeyt/xserializer.png)](https://travis-ci.org/sergeyt/xserializer)
[![Build status](https://ci.appveyor.com/api/projects/status/07totjnew0ubcxq0)](https://ci.appveyor.com/project/sergeyt/xserializer)
[![NuGet Version](http://img.shields.io/nuget/v/TsvBits.XSerializer.svg?style=flat)](https://www.nuget.org/packages/TsvBits.XSerializer/)
[![NuGet Downloads](http://img.shields.io/nuget/dt/TsvBits.XSerializer.svg?style=flat)](https://www.nuget.org/packages/TsvBits.XSerializer/)

# XSerializer

Simple schema-oriented serialization engine for .NET

## Features

* Multiple output formats XML, JSON, JsonML. Easy to implement custom writer/reader.
* High performance with streaming serialization
* Fluent API to declare serialization schema

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

var item = schema.Element<ReportItem>()
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
// init report instance
var report = new Report();
// ...

// get xml/json string using schema defined earlier
var xml = schema.ToXmlString(report);
var json = schema.ToString(report, Format.Json);

// load state from xml string
schema.ReadXmlString(xml, report);
```
