namespace BlockDiagramDemo.Models;

public static class BlockDiagramLayout
{
    public const double GridSize = 16;
    public const double MinimumBlockOffset = 48;
    public const double MinimumBlockWidth = 64;
    public const double MinimumBlockHeight = 48;
    public const double DefaultBlockWidth = 112;
    public const double DefaultBlockHeight = 48;
    public const double MinimumCanvasWidth = 512;
    public const double MinimumActionLaneWidth = 224;
    public const double InputLaneWidth = 144;
    public const double OutputLaneWidth = 144;
    public const double LanePadding = 12;

    public static DiagramLaneLayout GetLaneLayout(double canvasWidth)
    {
        canvasWidth = Math.Max(MinimumCanvasWidth, canvasWidth);
        var inputStart = 0;
        var actionStart = inputStart + InputLaneWidth;
        var outputEnd = canvasWidth;
        var outputStart = outputEnd - OutputLaneWidth;
        var minimumOutputStart = actionStart + MinimumActionLaneWidth;

        if (outputStart < minimumOutputStart)
        {
            outputStart = minimumOutputStart;
            outputEnd = outputStart + OutputLaneWidth;
        }

        var actionWidth = outputStart - actionStart;
        return new DiagramLaneLayout(inputStart, actionStart, outputStart, outputEnd, actionWidth);
    }

    public static DiagramLaneHorizontalBounds GetLaneHorizontalBounds(
        DiagramBlockRole role,
        double canvasWidth)
    {
        var layout = GetLaneLayout(canvasWidth);

        return role switch
        {
            DiagramBlockRole.Input => new DiagramLaneHorizontalBounds(
                layout.InputStart + LanePadding,
                layout.ActionStart - LanePadding),
            DiagramBlockRole.Action => new DiagramLaneHorizontalBounds(
                layout.ActionStart + LanePadding,
                layout.OutputStart - LanePadding),
            DiagramBlockRole.Output => new DiagramLaneHorizontalBounds(
                layout.OutputStart + LanePadding,
                layout.OutputEnd - LanePadding),
            _ => new DiagramLaneHorizontalBounds(
                layout.InputStart + LanePadding,
                layout.OutputEnd - LanePadding)
        };
    }

    public static DiagramBlock CreateBlock(
        DiagramBlockRole role,
        BlockDiagramModel model,
        double canvasWidth)
    {
        var existingInRole = model.Blocks.Count(block => block.Role == role);
        var ordinal = existingInRole + 1;
        var columns = role == DiagramBlockRole.Action ? 2 : 1;
        var row = existingInRole / columns;
        var column = existingInRole % columns;
        var bounds = GetLaneHorizontalBounds(role, canvasWidth);
        var x = bounds.MinX + (column * (DefaultBlockWidth + GridSize));
        var maxX = Math.Max(bounds.MinX, bounds.MaxX - DefaultBlockWidth);
        x = Math.Min(x, maxX);

        var block = new DiagramBlock
        {
            Id = $"{role.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}",
            Label = $"{role} {ordinal}",
            Role = role,
            X = SnapToGrid(x),
            Y = SnapToGrid(72 + (row * 144)),
            Width = DefaultBlockWidth,
            Height = DefaultBlockHeight
        };

        ConstrainBlockToLane(block, canvasWidth);
        return block;
    }

    public static void ConstrainBlocksToLanes(
        BlockDiagramModel model,
        double canvasWidth)
    {
        foreach (var block in model.Blocks)
        {
            ConstrainBlockToLane(block, canvasWidth);
        }
    }

    public static void ConstrainBlockToLane(
        DiagramBlock block,
        double canvasWidth)
    {
        var bounds = GetLaneHorizontalBounds(block.Role, canvasWidth);
        var laneWidth = Math.Max(MinimumBlockWidth, bounds.MaxX - bounds.MinX);
        var maxWidth = Math.Max(MinimumBlockWidth, SnapToGrid(laneWidth));
        block.Width = Math.Clamp(SnapSizeToGrid(block.Width, MinimumBlockWidth), MinimumBlockWidth, maxWidth);
        block.Height = SnapSizeToGrid(block.Height, MinimumBlockHeight);

        var minX = bounds.MinX;
        var maxX = Math.Max(minX, bounds.MaxX - block.Width);
        block.X = SnapToGrid(Math.Clamp(block.X, minX, maxX));
        block.Y = SnapToGrid(Math.Max(MinimumBlockOffset, block.Y));
    }

    public static double SnapToGrid(double value)
    {
        return Math.Round(value / GridSize, MidpointRounding.AwayFromZero) * GridSize;
    }

    public static int ToGrid(double value)
    {
        return (int)Math.Round(value / GridSize, MidpointRounding.AwayFromZero);
    }

    public static double FromGrid(int value)
    {
        return value * GridSize;
    }

    public static double SnapSizeToGrid(
        double value,
        double minimum)
    {
        var snapped = SnapToGrid(value);
        return Math.Max(minimum, snapped);
    }
}

public readonly record struct DiagramLaneLayout(
    double InputStart,
    double ActionStart,
    double OutputStart,
    double OutputEnd,
    double ActionWidth);

public readonly record struct DiagramLaneHorizontalBounds(
    double MinX,
    double MaxX);
