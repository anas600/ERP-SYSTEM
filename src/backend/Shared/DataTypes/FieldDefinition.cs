using System.Text.Json.Serialization;

namespace ERPSystem.Shared.DataTypes;

/// <summary>
/// Column metadata for a JSON DataType definition.
/// Example JSON:
///   { "name": "code", "type": "varchar(20)", "nullable": false, "primary_key": false }
/// </summary>
public sealed class FieldDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;  // e.g. "uuid", "varchar(20)", "timestamptz", "numeric(18,4)"

    [JsonPropertyName("nullable")]
    public bool Nullable { get; set; } = true;

    [JsonPropertyName("primary_key")]
    public bool PrimaryKey { get; set; }

    [JsonPropertyName("default")]
    public string? Default { get; set; }  // raw SQL expression, e.g. "now()", "'LYD'", "false"

    /// <summary>
    /// Sprint 32 (DEC-112): if true, the migrator forces double-quoted SQL identifier for
    /// this column (e.g., `"from"`, `"to"`). Required for SQL reserved words that would
    /// otherwise cause a syntax error in CREATE TABLE / ALTER TABLE.
    /// </summary>
    [JsonPropertyName("quoted")]
    public bool Quoted { get; set; }  // DEC-112: escape hatch for SQL reserved words

    [JsonPropertyName("foreign_key")]
    public ForeignKeyDefinition? ForeignKey { get; set; }
}
