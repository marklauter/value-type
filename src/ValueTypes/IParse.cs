using Results;

namespace ValueTypes;

/// <summary>
/// The fallible lift from text into the domain: a Kleisli arrow <c>string → Result&lt;TSelf&gt;</c> in the validation applicative. <see cref="Parse"/> is total
/// on untrusted text: every rejection is a value, never an exception. Nothing downstream re-validates, so a lifted value is trusted everywhere. Implemented by
/// <see cref="IValueType{TSelf, TValue}"/> wrappers, which inherit it and factor it through <see cref="IValueType{TSelf, TValue}.Checked"/> so the text and
/// primitive entry points cannot disagree, and by composite value records whose canonical form spans multiple parts, where each part is lifted and the errors
/// accumulated applicatively. <c>ToString()</c> renders the canonical text form and <see cref="Parse"/> is its retraction: the pair obeys the round-trip law
/// <c>Parse(x.ToString()) == Success(x)</c>.
/// </summary>
/// <typeparam name="TSelf">The implementing type. Self-referential (CRTP) so the static abstract member resolves through the type parameter at every call site.
/// No struct constraint. Sealed records implement this alongside value-type wrappers.</typeparam>
public interface IParse<TSelf>
    where TSelf : IParse<TSelf>
{
    /// <summary>
    /// Lifts <paramref name="s"/> into the domain. Whether <see langword="null"/> is valid input is a business rule
    /// of the implementing type: a type for which null is invalid checks and returns a validation failure. A type for which null is valid does not check.
    /// Reflection-based callers can deliver null at runtime regardless of the parameter's non-nullable annotation.
    /// </summary>
    /// <param name="s">The untrusted input string.</param>
    /// <returns>
    /// <see cref="Result{TSelf}.Success"/> wrapping the lifted value when <paramref name="s"/> is well-formed and satisfies every validation rule; otherwise
    /// <see cref="Result{TSelf}.Failure"/> carrying one or more validation <see cref="Error"/>s.
    /// </returns>
    static abstract Result<TSelf> Parse(string s);
}
