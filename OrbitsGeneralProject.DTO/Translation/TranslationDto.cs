using System.Text.Json.Serialization;

namespace Orbits.GeneralProject.DTO.Translation
{


    public class TranslationInputDto
    {
        public int EntityId { get; set; }
        public TranslationHeaderInputDto TranslationHeader { get; set; }

    }
    public class TranslationHeaderInputDto
    {
        public int Id { get; set; }
        [JsonIgnore]
        public int TableNameId { get; set; }
        [JsonIgnore]
        public string? EntityName { get; set; }
        public string? FieldName { get; set; }
        public List<TranslationDetailInputDto>? Details { get; set; }

    }

    public class TranslationDetailInputDto //: BaseDto
    {
        public int Id { get; set; }

        public int TranslationHeaderId { get; set; }
        public int LanguageId { get; set; }
        public string? Translation { get; set; }
        [JsonIgnore]
        public string? FieldName { get; set; }

    }

    internal class TranslationHeaderDto
    {
        public int Id { get; set; }
        public int TableNameId { get; set; }

        public List<TranslationDetailDto>? TranslationDetails { get; set; }
    }
    internal class TranslationDetailDto
    {
        public int Id { get; set; }
        public int TranslationId { get; set; }
        [JsonIgnore]
        public int LanguageId { get; set; }
        public string Translation { get; set; } = null!;
        public string? FieldName { get; set; }
    }

}
