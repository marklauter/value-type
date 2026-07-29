using Results;

namespace ValueTypes.Tests;

// Reference IValueType implementor for exercising ValueParser. A valid TestValue is one or more lowercase ASCII letters.
public readonly record struct TestValue
    : IValueType<TestValue, string>
    , ITryParse<TestValue>
{
    private readonly string value;

    public string Value => value;

    public static TestValue Unchecked(string value) => new(value);

    public static Result<TestValue> Checked(string value) =>
        !string.IsNullOrEmpty(value) && value.All(char.IsAsciiLetterLower)
            ? Result.Success(new TestValue(value))
            : Result.Failure<TestValue>(Error.Validation(
                ErrorCode.Unchecked("test.invalid"),
                ErrorMessage.Unchecked($"'{value}' is not one or more lowercase ASCII letters")));

    // TValue is string, so the text-to-primitive leg is the identity and Parse is the delegation the IValueType docs prescribe.
    public static Result<TestValue> Parse(string s) => Checked(s);

    // The canonical one-line delegation prescribed by the ITryParse<TSelf> docs.
    public static bool TryParse(string s, out TestValue parsed) =>
        ValueParser.TryParse(s, out parsed);

    private TestValue(string value) => this.value = value;

    public override string ToString() => value;

    public int CompareTo(TestValue other) => string.CompareOrdinal(value, other.value);

    public static bool operator <(TestValue left, TestValue right) => left.CompareTo(right) < 0;

    public static bool operator <=(TestValue left, TestValue right) => left.CompareTo(right) <= 0;

    public static bool operator >(TestValue left, TestValue right) => left.CompareTo(right) > 0;

    public static bool operator >=(TestValue left, TestValue right) => left.CompareTo(right) >= 0;
}
