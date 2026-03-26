# Skill: Add a Configuration Section to RapiAgent

Use this skill when the user asks to add a new configuration section to RapiAgent
(e.g., "add an X config section", "support configuring Y", "add options for Z").

## Overview

RapiAgent uses a two-layer configuration system:

- **`*Options`** — raw .NET configuration classes (deserialized from JSON / ENV). Live in `RapiAgent/Config/Options/`.
- **`*Config`** — validated, immutable objects used inside the application. Live in `RapiAgent/Config/`.

Configuration is loaded at startup. If validation fails, the agent throws and exits immediately.

## Checklist

Work through these steps in order. Build after every significant change to catch errors early.

### 1. Create Options class(es) in `RapiAgent/Config/Options/`

For each JSON section level, create a separate file and class.

**Naming:** `<Section>[SubSection]Options.cs`  
**Rules:**
- All properties must be nullable (`string?`, `int?`, nested class `?`) — they come from JSON deserialization.
- No constructor needed (default parameterless constructor is fine).
- No validation logic here.

```csharp
// Config/Options/ExampleOptions.cs
namespace RapiAgent.Config.Options;

public class ExampleSubOptions
{
    public string? Value { get; set; }
}

public class ExampleOptions
{
    public ExampleSubOptions? Sub { get; set; }
}
```

### 2. Create Config class(es) in `RapiAgent/Config/`

Mirror the Options structure, but with validated, non-nullable properties.

**Naming:** `<Section>[SubSection]Config.cs`  
**Rules:**
- Private constructor only — no public constructor.
- All properties are non-nullable (validation happens in `Convert`).
- Static `Convert` method accepts the corresponding `*Options` and returns the Config.
- Throw `InvalidOperationException` (with a descriptive message) for any invalid or missing required value.
- Optional sections: return `null` from `Convert` if the options object itself is `null` and the section is truly optional.

```csharp
// Config/ExampleConfig.cs
using RapiAgent.Config.Options;

namespace RapiAgent.Config;

public class ExampleSubConfig
{
    public string Value { get; }

    private ExampleSubConfig(string value)
    {
        Value = value;
    }

    public static ExampleSubConfig Convert(ExampleSubOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Value))
            throw new InvalidOperationException("Example.Sub.Value is required and must not be empty.");

        return new ExampleSubConfig(options.Value);
    }
}

public class ExampleConfig
{
    public ExampleSubConfig Sub { get; }

    private ExampleConfig(ExampleSubConfig sub)
    {
        Sub = sub;
    }

    public static ExampleConfig Convert(ExampleOptions options)
    {
        var sub = ExampleSubConfig.Convert(options.Sub ?? new ExampleSubOptions());
        return new ExampleConfig(sub);
    }
}
```

### 3. Add a property to `RapiOptions`

Open `RapiAgent/Config/Options/RapiOptions.cs` and add a nullable property for the new section:

```csharp
public ExampleOptions? Example { get; set; }
```

### 4. Add a property and wire up `Convert` in `RapiConfig`

Open `RapiAgent/Config/RapiConfig.cs`:

1. Add a property for the new Config (nullable if the section is optional, non-nullable if required).
2. In the private constructor, assign it.
3. In `Convert`, call the section's `Convert` method and pass the result to the constructor.

```csharp
// Property
public ExampleConfig? Example { get; }

// Constructor
private RapiConfig(ExampleConfig? example)
{
    Example = example;
}

// In Convert:
var example = options.Example != null
    ? ExampleConfig.Convert(options.Example)
    : null;
return new RapiConfig(example);
```

### 5. Build and verify

```bash
dotnet build Rapi.sln
```

The build must produce **0 warnings and 0 errors**. Fix everything before considering the task done.

## JSON shape

The section name in the JSON file must match the property name on `RapiOptions` exactly (case-insensitive by default in .NET configuration):

```json
{
  "Example": {
    "Sub": {
      "Value": "hello"
    }
  }
}
```

Equivalent environment variable (double-underscore as separator):

```
EXAMPLE__SUB__VALUE=hello
```

## Notes

- Do **not** inject `IOptions<T>` inside the application. Inject `RapiConfig` directly (it is registered as a singleton).
- Do **not** add a `Version="..."` attribute to any `<PackageReference>` — use `Directory.Packages.props` for all version declarations.
- `Startup.cs` is kept for test infrastructure — mirror any DI registrations there if new services depend on `RapiConfig`.
