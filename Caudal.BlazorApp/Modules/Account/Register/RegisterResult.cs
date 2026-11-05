namespace Caudal.BlazorApp.Modules.Account.Register;

public sealed record RegisterResult(string? Message, IReadOnlyDictionary<RegisterField, string> FieldErrors)
{
    public bool Succeeded => Message is null && FieldErrors.Count == 0;

    public static RegisterResult Success { get; } = new(null, new Dictionary<RegisterField, string>());
}
