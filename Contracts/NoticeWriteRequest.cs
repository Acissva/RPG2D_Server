using RPG2D_Server.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RPG2D_Server.Contracts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class NoticeWriteRequest : IValidatableObject
    {
        public string? Title { get; set; }
        public string? Content { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in NoticeContentRules.GetErrors(Title, Content))
            {
                yield return new ValidationResult(error.Value[0], new[] { error.Key });
            }
        }
    }
}
