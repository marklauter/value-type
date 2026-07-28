# ValueTypes

Contracts for strongly-typed wrappers that embed a primitive in your domain, so a bare `string` is no longer assignment-compatible with a checked value.

```
dotnet add package MSL.ValueTypes
```

Builds on [MSL.Results](https://github.com/marklauter/result) — `Parse` returns a `Result<TSelf>`, so a rejection is a value rather than an exception.

## Parse, don't validate

A `string` that reached you from a query string and a `string` you've checked are the same type to the compiler, so nothing stops you from passing the unchecked one to code that assumes otherwise. Wrapping the primitive fixes that: the wrapper can only be built by parsing, so holding one is proof the value was checked.

`IValue<TSelf, TValue>` gives a wrapper two arrows in and one out:

- `Parse` — the fallible lift, `string → Result<TSelf>`. Inherited from `IParse<TSelf>`. All validation lives here and only here.
- `Unchecked` — the total embedding, `TValue → TSelf`. Pure assignment, no validation, no normalization. Lawful only on the valid subset the caller vouches for, so misuse is the caller's defect.
- `Value` — the projection back to the primitive, `TSelf → TValue`.

The contract is self-referential (CRTP), so the static abstract members resolve through the type parameter at every call site and `TSelf` is constrained to `struct`. Wrappers also get `IComparable<TSelf>`, `IEquatable<TSelf>`, and `IComparisonOperators<TSelf, TSelf, bool>`, so they sort and compare like the primitive they carry.

## A wrapper

```csharp
public readonly record struct Slug : IValue<Slug, string>
{
    private readonly string value;

    public string Value => value;

    private Slug(string value) => this.value = value;

    public static Slug Unchecked(string value) => new(value);

    public static Result<Slug> Parse(string s) =>
        !string.IsNullOrEmpty(s) && s.All(c => char.IsAsciiLetterLower(c) || c == '-')
            ? Result.Success(new Slug(s))
            : Result.Failure<Slug>(Error.Validation("slug.invalid", $"'{s}' is not a lowercase hyphenated slug."));

    public override string ToString() => value;

    public int CompareTo(Slug other) => string.CompareOrdinal(value, other.value);

    public static bool operator <(Slug left, Slug right) => left.CompareTo(right) < 0;
    public static bool operator <=(Slug left, Slug right) => left.CompareTo(right) <= 0;
    public static bool operator >(Slug left, Slug right) => left.CompareTo(right) > 0;
    public static bool operator >=(Slug left, Slug right) => left.CompareTo(right) >= 0;
}
```

`Parse` is total over untrusted text — every rejection comes back as a value, never an exception — and it's the retraction of `ToString()`, so the pair obeys the round-trip law `Parse ∘ ToString = id`.

Because `Parse` returns a `Result`, several independent lifts compose and accumulate every error at once rather than stopping at the first:

```csharp
Result<Article> article = Result.Apply(
    Result.Apply(
        Result.Success((Slug s) => (Title t) => new Article(s, t)),
        Slug.Parse(slugInput)),
    Title.Parse(titleInput));
```

## Binding from a route or query string

ASP.NET Core's parameter binder discovers the BCL `bool`-plus-`out` `TryParse` on the concrete type. That's a transport concern, so it isn't part of `IValue`. Types that cross the boundary opt in with `ITryParse<TSelf>` instead, and delegate:

```csharp
public readonly record struct Slug : IValue<Slug, string>, ITryParse<Slug>
{
    public static bool TryParse(string s, out Slug parsed) => ValueParser.TryParse(s, out parsed);

    // ...
}
```

`ValueParser.TryParse` is the canonical body: it projects `Parse` into the `bool`-plus-`out` shape and discards the accumulated errors. Declare `TryParse` on your own type rather than inheriting it — reflection-based binders won't find an inherited member.

Types that never bind from a route don't carry any of this.

## Composite values

`IParse<TSelf>` has no `struct` constraint, so a sealed record whose canonical form spans several parts implements it directly: lift each part, accumulate the errors, and keep `ToString()` as the retraction.

## License

MIT. See [LICENSE](LICENSE).
