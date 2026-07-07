using System.Text.Json.Serialization;

namespace ERPSystem.Shared.DataTypes;

/// <summary>
/// JSON-driven entity definition. Loaded from `data-types/*.json` at startup.
/// Allows additive schema changes (new columns) without writing FluentMigrator
/// classes for every change.
///
/// DEC-079 PoC: 1 entity (companies) to prove the pattern works.
/// </summary>
public sealed class DataType
{
    /// <summary>Display name (PascalCase). E.g. "Company", "Vendor".</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Database table name (snake_case, plural). E.g. "companies".</summary>
    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;

    /// <summary>Schema version (semver). Used for tracking future migrations.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>Owning module (for grouping). E.g. "Companies", "Finance".</summary>
    [JsonPropertyName("module")]
    public string Module { get; set; } = string.Empty;

    /// <summary>Columns in this table (ordered).</summary>
    [JsonPropertyName("fields")]
    public List<FieldDefinition> Fields { get; set; } = new();

    /// <summary>Indexes on this table.</summary>
    [JsonPropertyName("indexes")]
    public List<IndexDefinition> Indexes { get; set; } = new();
}