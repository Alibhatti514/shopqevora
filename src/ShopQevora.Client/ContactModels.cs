using System.ComponentModel.DataAnnotations;
namespace ShopQevora.Contracts;

// Shared validation contract; editable contact data only, never account/order ownership.
public sealed class ContactRequest : IValidatableObject
{
    [Required(ErrorMessage="Enter your full name."), StringLength(120, MinimumLength=2)]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage="Enter your email address."), EmailAddress(ErrorMessage="Enter a valid email address."), StringLength(254)]
    public string Email { get; set; } = "";
    [StringLength(32), RegularExpression(@"^\+[0-9][0-9 ()-]{6,30}$", ErrorMessage="Use international format, including + and the country code, or leave phone blank.")]
    public string? Phone { get; set; }
    [Required, StringLength(32)] public string Category { get; set; } = "general";
    [StringLength(64, ErrorMessage="Keep the order reference within 64 characters.")]
    public string? OrderReference { get; set; }
    [Required(ErrorMessage="Enter your message."), StringLength(4000, MinimumLength=10, ErrorMessage="Use between 10 and 4,000 characters for your message.")]
    public string Message { get; set; } = "";
    [Required, StringLength(32, MinimumLength=32)] public string SubmissionKey { get; set; } = "";
    [Required, StringLength(1024)] public string FormToken { get; set; } = "";
    [StringLength(200)] public string? Website { get; set; }
    public static IReadOnlyDictionary<string,string> Categories { get; } = new Dictionary<string,string>
    {
        ["general"]="General question", ["order"]="Demo order question", ["product"]="Product & collection",
        ["account"]="Account & website", ["feedback"]="Feedback & suggestions"
    };
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Categories.ContainsKey(Category ?? "")) yield return new("Choose an inquiry category.", [nameof(Category)]);
        foreach (var (field,value) in new[]{(nameof(FullName),FullName),(nameof(Email),Email),(nameof(OrderReference),OrderReference),(nameof(Message),Message)})
            if(value is not null && value.Any(c=>c is '<' or '>' or '\0')) yield return new("Use plain text without HTML markup.", [field]);
    }
    public void Normalize()
    {
        FullName=FullName?.Trim()??""; Email=Email?.Trim()??""; Message=Message?.Trim()??"";
        Phone=string.IsNullOrWhiteSpace(Phone)?null:Phone.Trim(); OrderReference=string.IsNullOrWhiteSpace(OrderReference)?null:OrderReference.Trim();
    }
}
public sealed record ContactChallenge(string SubmissionKey, string FormToken);
public sealed record ContactReceipt(string Reference, DateTime CreatedUtc);
