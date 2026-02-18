namespace BlockDiagramDemo.Models;

public enum DiagramConnectorSide
{
    Top,
    Right,
    Bottom,
    Left
}

public enum DiagramBlockRole
{
    Input,
    Action,
    Output
}

public sealed class BlockDiagramModel
{
    public List<DiagramBlock> Blocks { get; set; } = [];
    public List<DiagramLink> Links { get; set; } = [];

    public BlockDiagramModel Clone()
    {
        return new BlockDiagramModel
        {
            Blocks = Blocks.Select(static block => block.Clone()).ToList(),
            Links = Links.Select(static link => link.Clone()).ToList()
        };
    }
}

public sealed class DiagramBlock
{
    public string Id { get; set; } = $"block_{Guid.NewGuid():N}";
    public string Label { get; set; } = "Block";
    public DiagramBlockRole Role { get; set; } = DiagramBlockRole.Action;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 192;
    public double Height { get; set; } = 96;

    public DiagramBlock Clone()
    {
        return new DiagramBlock
        {
            Id = Id,
            Label = Label,
            Role = Role,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height
        };
    }
}

public sealed class DiagramLink
{
    public string Id { get; set; } = $"link_{Guid.NewGuid():N}";
    public string SourceBlockId { get; set; } = string.Empty;
    public string TargetBlockId { get; set; } = string.Empty;
    public DiagramConnectorSide SourceSide { get; set; } = DiagramConnectorSide.Right;
    public DiagramConnectorSide TargetSide { get; set; } = DiagramConnectorSide.Left;
    public string? Label { get; set; }

    public DiagramLink Clone()
    {
        return new DiagramLink
        {
            Id = Id,
            SourceBlockId = SourceBlockId,
            TargetBlockId = TargetBlockId,
            SourceSide = SourceSide,
            TargetSide = TargetSide,
            Label = Label
        };
    }
}
