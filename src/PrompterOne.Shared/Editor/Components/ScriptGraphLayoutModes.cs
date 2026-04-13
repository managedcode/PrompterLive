namespace PrompterOne.Shared.Components.Editor;

public static class ScriptGraphLayoutModes
{
    public const string Story = "story";
    public const string Compact = "compact";
    public const string Knowledge = "knowledge";
    public const string Structure = "structure";
    public const string Delivery = "delivery";
    public const string Relationship = "relationship";
    public const string Radial = "radial";
    public const string Force = "force";
    public const string Dagre = "dagre";
    public const string Grid = "grid";
    public const string Concentric = "concentric";
    public const string Circular = "circular";
    public const string Mds = "mds";
    public const string Fruchterman = "fruchterman";
    public const string ForceAtlas2 = "force-atlas2";
    public const string D3Force = "d3-force";
    public const string AntvDagre = "antv-dagre";
    public const string Indented = "indented";

    private static readonly string[] AllModes =
    [
        Story, Compact, Knowledge, Structure,
        Delivery, Relationship, Radial, Force,
        Dagre, Grid, Concentric, Circular,
        Mds, Fruchterman, ForceAtlas2, D3Force,
        AntvDagre, Indented
    ];

    public static string Normalize(string? value) =>
        Array.Exists(AllModes, m => string.Equals(m, value, StringComparison.Ordinal))
            ? value!
            : Knowledge;
}
