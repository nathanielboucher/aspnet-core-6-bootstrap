# Bootstrapped ASP.NET Core 6 Web API Solution

This repository contains a bootstrapped ASP.NET Core 6 Web API solution.

## Changes to make after cloning

Be sure to make the following changes once you clone the repository:

1. Set the `<UserSecretsId>` property in `/source/Api/Company.Product.WebApi.Api.csproj` to a unique GUID.
2. Set the `<UserSecretsId>` property in `/source/Api/Company.Product.WebApi.ScheduledTasks.csproj` to a unique GUID.
3. Rename any occurrences of `Company` or `Product` in all file contents and filenames.
4. Carefully look through `/Directory.Build.props` for property values to change. Some values to change are:
    - `<RepositoryRootUrl>`
    - `<VersionPrefix>`
    - `<VersionSuffix>`
    - `<Company>`
    - `<Product>`
    - `<Authors>`
    - `<Copyright>`

## Understand the code

The solution is opinionated about several things. It's important to understand the purpose of each class before using the solution.

## Before running the API application

You must do a few things before successfully running the API application:

- Run the `create-network.ps1` script (see [Scripts](#scripts))
- Run the `run-postgresql.ps1` script (see [Scripts](#scripts))
- Run the `run-seq.ps1` script (see [Scripts](#scripts))
- Run the `add-postgresql-connection-string.ps1` script (see [Scripts](#scripts))
    - Be sure to run the script twice: once with `-Project Api` and once with `-Project ScheduledTasks`

## Notable features

### Custom fluent result API

Microsoft inexplicably does not implement `IActionResult` for every HTTP status code. Additionally, developers sometimes want to implement "response body envelopes" where all response bodies follow a certain JSON schema. I implemented response body envelopes with the `StandardJsonResult` and `StandardJsonResult<TData>` classes. A fluent API makes it easy to compose these response body envelopes. See the [`HealthController`](source/Api/Controllers/Health/HealthController.cs) class for an example. The full set of helper methods can be found in [Result.cs](source/Api/Results/Result.cs).

### Swashbuckle customizations

Swashbuckle has some annoying behavior that I was intent on fixing.

Swashbuckle does not correctly honor non-null reference types when the [nullable reference type](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references) feature is enabled. The [NullableSchemaFilter](source/Api/Swashbuckle/NullableSchemaFilter.cs) class corrects this behavior.

Swashbuckle does not render generic model type names correctly. The [TitleFilter](source/Api/Swashbuckle/TitleFilter.cs) class corrects this behavior.

I wrote a couple of attributes that wrap common response types to reduce boilerplate.

### Scheduled tasks app

The Scheduled Tasks console application can be used to execute jobs at certain times.

### Seq

Seq is an excellent log sink. Both the `Api` and `ScheduledTasks` projects are configured to use Seq as a Serilog sink.

## Frameworks and libraries

| Framework or Library | Purpose |
| -------------------- | ------- |
| [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) | [Object-relational mapping](https://en.wikipedia.org/wiki/Object%E2%80%93relational_mapping)
| [Fluent Assertions](https://fluentassertions.com/) | Test assertion |
| [Fluent Validation](https://fluentvalidation.net/) | Model validation |
| [Moq](https://github.com/moq/moq) | Mocking |
| [NodaTime](https://nodatime.org/) | Date/time |
| [Serilog](https://serilog.net/) (console sink) | Logging |
| [Swashbuckle](https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-6.0&tabs=visual-studio) | [OpenAPI](https://swagger.io/) documentation |
| [TerraFX](https://github.com/terrafx/terrafx) | Utilities |
| [xUnit](https://xunit.net/) | Test harness |

## Scripts

| Script | Purpose |
| ------ | ------- |
| `/scripts/dev/docker/create-network.ps1` | Creates a Docker network |
| `/scripts/dev/docker/run-postgresql.ps1` | Runs a PostgreSQL Docker container |
| `/scripts/dev/docker/run-seq.ps1` | Runs a Seq SQL Docker container |
| `/scripts/dev/secrets/add-postgresql-connection-string.ps1` | Adds a PostgreSQL connection string using `dotnet user-secrets` |
| `/scripts/dev/security/dotnet-devcerts.ps1` | .NET HTTPS development certificate management |

## Versioning

The application is versioned using a tweaked [versioning scheme from .NET Arcade](https://github.com/dotnet/arcade/blob/main/Documentation/CorePackages/Versioning.md).

## Debugging

The solution supports debugging in either a console application or Docker.
