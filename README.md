<img src="Hardcoded.NET.png" alt="Hardcoded.NET Logo" width="100px" style="margin-right: 10px;" align="left" />

### `Hardcoded.NET`

[![Build and test](https://github.com/WaveHDMI/Hardcoded.NET/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/WaveHDMI/Hardcoded.NET/actions/workflows/build-and-test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Turn "hardcoding" into a performance feature.

Ironic, but accurate. **Hardcoded.NET** is a C# Source Generator that leverages Roslyn to convert SQL files and resources into mapped string constants at compile time. It gives you the maintainability of external files with the raw performance of `const string` literals.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [How It Works](#how-it-works)
- [Examples](#examples)
  - [Basic Query](#basic-query)
  - [Mock Variables with `@start`](#mock-variables-with-start)
  - [String formatting with numeric parameters](#string-formatting-with-numeric-parameters)

## Features
- 🚀 **Zero runtime overhead**: Generates `internal const string` fields at compile time.
- 🛠️ **Seamless IDE Integration**: Write proper `.sql` files with full syntax highlighting and IntelliSense, but consume them as constants in C#.
- 📦 **No dependencies**: Works completely as a development dependency. It leaves no footprint in your final assemblies.
- 🪄 **Auto-inclusion**: Automatically recognizes `.sql` files in your project, no need to manually tag them as `AdditionalFiles`.

## Installation

You can install the package via NuGet Package Manager or the .NET CLI:

```bash
dotnet add package Hardcoded.NET
```

## How It Works

Hardcoded.NET scans your project for `.sql` files that start with the `-- @hardcoded` directive. It parses these files, looking for specific class and query annotations to generate a partial class containing your SQL statements as constants.

### Directives

| Directive | Description |
|-----------|-------------|
| `-- @hardcoded` | Must be at the very top of the file to signify this file should be processed. |
| `-- @namespace [Namespace]` | Defines the namespace for the generated C# class. |
| `-- @class [ClassName]` | Defines the name of the generated static partial class. |
| `-- @query [QueryName]` | Defines the name of the constant for the following SQL query. |
| `-- @start` | Optional. Tells the parser to ignore everything above it (useful for declaring mock SQL variables that you don't want in your final C# string). |

## Examples

### Basic Query

Create a file with the `.sql` extension in your project, for example `Queries.sql`:

```sql
-- @hardcoded
-- @namespace MyProject.Data
    
-- @class UserQueries
-- @query GetUserById
-- Retrieves a user from the database by their ID
SELECT [Id], [Username], [Email]
FROM [dbo].[Users]
WHERE [Id] = @Id
```

At compile time, Hardcoded.NET generates the following code:

```csharp
namespace MyProject.Data;

internal static partial class UserQueries
{
    /// <summary>
    /// Retrieves a user from the database by their ID
    /// </summary>
    internal const string GetUserById = @"SELECT [Id], [Username], [Email]
FROM [dbo].[Users]
WHERE [Id] = @Id";
}
```

Now you can use this constant efficiently and securely with libraries like Dapper or EF Core:

```csharp
using var connection = new SqlConnection("...");
var user = await connection.QueryFirstOrDefaultAsync<User>(UserQueries.GetUserById, new { Id = 123 });
```

### Mock Variables with `@start`

Sometimes you want to test your SQL scripts directly in SSMS or Azure Data Studio without modifying them later for C#. You can declare local SQL variables at the top of your query for testing, and use `-- @start` to exclude them from the generated C# string.

```sql
-- @hardcoded
-- @namespace MyProject.Data
    
-- @class ProductQueries
-- @query GetActiveProducts
-- Retrieves products from the database by active status and category ID
DECLARE @IsActive BIT = 1;
DECLARE @CategoryId INT = 5;

-- @start

SELECT *
FROM [dbo].[Products]
WHERE [IsActive] = @IsActive AND [CategoryId] = @CategoryId
```

The generated C# string will start exactly from the `-- @start` marker:

```csharp
namespace MyProject.Data;

internal static partial class ProductQueries
{
    /// <summary>
    /// Retrieves products from the database by active status and category ID
    /// </summary>
    internal const string GetActiveProducts = @"SELECT *
FROM [dbo].[Products]
WHERE [IsActive] = @IsActive AND [CategoryId] = @CategoryId";
}
```

### String formatting with numeric parameters

For scenarios requiring dynamic logic within C#, you can use numeric parameters (e.g., `@0`, `@1`). These are automatically converted into curly-bracket placeholders (`{0}`, `{1}`) in the generated C# string, allowing you to use `string.Format` to inject values at runtime.

```sql
-- @hardcoded
-- @namespace MyProject.Data
    
-- @class ProductQueries
-- @query GetActiveProducts
SELECT *
FROM [dbo].[Products]
ORDER BY @0
```

Generated code:

```csharp
namespace MyProject.Data;

internal static partial class ProductQueries
{
    /// <summary>
    /// Query from Queries.sql
    /// </summary>
    internal const string GetActiveProducts = @"SELECT *
FROM [dbo].[Products]
ORDER BY {0}";
}
```
