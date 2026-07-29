[![.NET Tests](https://github.com/marklauter/value-type/actions/workflows/dotnet.tests.yml/badge.svg)](https://github.com/marklauter/value-type/actions/workflows/dotnet.tests.yml)
[![.NET Publish](https://github.com/marklauter/value-type/actions/workflows/dotnet.publish.yml/badge.svg)](https://github.com/marklauter/value-type/actions/workflows/dotnet.publish.yml)
[![NuGet](https://img.shields.io/nuget/v/MSL.ValueTypes?logo=nuget)](https://www.nuget.org/packages/MSL.ValueTypes/)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0/)

![MSL Armory](https://raw.githubusercontent.com/marklauter/value-type/main/images/msl.armory.small.png "MSL Armory")

# ValueTypes

*Another weapon from the MSL Armory*

Contracts for strongly-typed wrappers that embed a primitive in your domain, so a bare `string` is no longer assignment-compatible with a checked value.

```bash
dotnet add package MSL.ValueTypes
```

ValueTypes targets .NET 10 and builds on [MSL.Results](https://github.com/marklauter/result): `Parse` returns a `Result<TSelf>`, so a rejection is a value rather than an exception.

## Parse, don't validate

A `string` that arrived from a query string and a `string` you've already checked are the same type to the compiler. Nothing stops you from handing the unchecked one to code that assumes otherwise. Wrapping the primitive closes that gap. The wrapper's constructor is private, so the only ways in are `Parse`, `Checked`, and `Unchecked`: two that validate, and one that puts the caller's claim in writing at the call site.

`IValueType<TSelf, TValue>` gives a wrapper three arrows in and one out:

- `Parse` — the fallible lift from text, `string → Result<TSelf>`. Inherited from `IParse<TSelf>`.
- `Checked` — the fallible embedding of a primitive, `TValue → Result<TSelf>`. Validation lives here. `Parse` factors through it: convert the text to a `TValue`, then defer. When `TValue` is `string` that conversion is the identity, so `Parse` is a one-line delegation.
- `Unchecked` — the total embedding, `TValue → TSelf`. Pure assignment: no validation, no normalization. Lawful only on the valid subset the caller vouches for, so misuse is the caller's defect.
- `Value` — the projection back to the primitive, `TSelf → TValue`.

`Checked` and `Unchecked` are a pair over where the primitive came from. Reach for `Checked` at the edge, where the value is untrusted: an `int` off a database row, a `Guid` from another service, or the body of `Parse`, which is the edge by definition. Reach for `Unchecked` when you author the value yourself: a constant, a static seed, a test fixture.

The contract is self-referential (CRTP), so the static abstract members resolve through the type parameter at every call site. `TSelf` is constrained to `struct`. Wrappers also get `IComparable<TSelf>`, `IEquatable<TSelf>`, and `IComparisonOperators<TSelf, TSelf, bool>`, so they sort and compare like the primitive they carry.

## A wrapper

```csharp
public readonly record struct Slug : IValueType<Slug, string>
{
    private readonly string value;

    public string Value => value;

    private Slug(string value) => this.value = value;

    public static Slug Unchecked(string value) => new(value);

    public static Result<Slug> Checked(string value) =>
        !string.IsNullOrEmpty(value) && value.All(c => char.IsAsciiLetterLower(c) || c == '-')
            ? Result.Success(new Slug(value))
            : Result.Failure<Slug>(Error.Validation(
                ErrorCode.Unchecked("slug.invalid"),
                ErrorMessage.Unchecked($"'{value}' is not a lowercase hyphenated slug.")));

    public static Result<Slug> Parse(string s) => Checked(s);

    public override string ToString() => value;

    public int CompareTo(Slug other) => string.CompareOrdinal(value, other.value);

    public static bool operator <(Slug left, Slug right) => left.CompareTo(right) < 0;
    public static bool operator <=(Slug left, Slug right) => left.CompareTo(right) <= 0;
    public static bool operator >(Slug left, Slug right) => left.CompareTo(right) > 0;
    public static bool operator >=(Slug left, Slug right) => left.CompareTo(right) >= 0;
}
```

`Parse` is total over untrusted text: every rejection comes back as a value. `ToString()` renders the canonical text form, and parsing that text recovers the value it came from: `Parse(x.ToString()) == Success(x)`.

Whether `null` is valid input is a business rule of the implementing type. Reflection-based callers deliver null at runtime regardless of the parameter's non-nullable annotation. A type for which null is invalid checks for it in `Checked`.

Because `Parse` returns a `Result`, independent lifts compose and report every error at once instead of stopping at the first. Curry the constructor and apply once per part:

```csharp
Result<Article> article = Result.Apply(
    Result.Apply(
        Result.Success((Slug s) => (Title t) => new Article(s, t)),
        Slug.Parse(slugInput)),
    Title.Parse(titleInput));
```

## Binding from a route or query string

ASP.NET Core's parameter binder discovers the BCL `bool`-plus-`out` `TryParse` on the concrete type. That's a transport concern, so it isn't part of `IValueType`. Types that cross the boundary opt in with `ITryParse<TSelf>` and delegate:

```csharp
public readonly record struct Slug : IValueType<Slug, string>, ITryParse<Slug>
{
    public static bool TryParse(string s, out Slug parsed) => ValueParser.TryParse(s, out parsed);

    // ...
}
```

`ValueParser.TryParse` is the canonical body: it projects `Parse` into the `bool`-plus-`out` shape and discards the accumulated errors. Declare `TryParse` on your own type rather than inheriting it, because reflection-based binders won't find an inherited member.

Types that never bind from a route carry none of this.

## Composite values

`IParse<TSelf>` has no `struct` constraint. A sealed record whose canonical form spans several parts implements it directly: lift each part, accumulate the errors, and keep `ToString()` as the canonical text form.

## API

| Member | What it does |
| --- | --- |
| `IValueType<TSelf, TValue>` | The wrapper contract. Constrains `TSelf` to `struct`. Inherits `IParse`, `IComparable`, `IEquatable`, `IComparisonOperators`. |
| `IValueType.Checked(value)` | `static abstract Result<TSelf> Checked(TValue)`. Validates a primitive the caller already holds. Where the validation rules live; `Parse` defers to it. |
| `IValueType.Unchecked(value)` | Total embedding of a trusted, already-canonical primitive. No validation. |
| `IValueType.Value` | Projection back to the wrapped primitive. |
| `IParse<TSelf>` | `static abstract Result<TSelf> Parse(string)`. No `struct` constraint, so composite records implement it too. |
| `ITryParse<TSelf>` | Extends `IParse`. Adds `static abstract bool TryParse(string, out TSelf)` for reflection-based binders. |
| `ValueParser.TryParse<TSelf>` | The canonical `TryParse` body. Projects `Parse` to `bool`-plus-`out`, discarding errors. |

---
[Repository](https://github.com/marklauter/value-type) · [NuGet](https://www.nuget.org/packages/MSL.ValueTypes/) · [MIT License](https://github.com/marklauter/value-type/blob/main/LICENSE) · [Report an issue](https://github.com/marklauter/value-type/issues)
