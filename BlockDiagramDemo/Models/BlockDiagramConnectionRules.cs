namespace BlockDiagramDemo.Models;

public static class BlockDiagramConnectionRules
{
    private static readonly DiagramConnectorSide[] AllConnectorSides =
    [
        DiagramConnectorSide.Top,
        DiagramConnectorSide.Right,
        DiagramConnectorSide.Bottom,
        DiagramConnectorSide.Left
    ];

    private static readonly DiagramConnectorSide[] InputConnectorSides = [DiagramConnectorSide.Right];
    private static readonly DiagramConnectorSide[] OutputConnectorSides = [DiagramConnectorSide.Left];

    public static IReadOnlyList<DiagramConnectorSide> GetVisibleConnectorSides(DiagramBlock block)
    {
        return block.Role switch
        {
            DiagramBlockRole.Input => InputConnectorSides,
            DiagramBlockRole.Output => OutputConnectorSides,
            _ => AllConnectorSides
        };
    }

    public static DiagramConnectionValidation ValidateConnectionStart(
        DiagramBlock sourceBlock,
        DiagramConnectorSide sourceSide)
    {
        if (!GetVisibleConnectorSides(sourceBlock).Contains(sourceSide))
        {
            return DiagramConnectionValidation.Invalid("Connector is not available for this block role.");
        }

        if (sourceBlock.Role == DiagramBlockRole.Output)
        {
            return DiagramConnectionValidation.Invalid("Output blocks cannot start connections.");
        }

        return DiagramConnectionValidation.Valid;
    }

    public static DiagramConnectionValidation ValidateConnection(
        BlockDiagramModel model,
        DiagramBlock sourceBlock,
        DiagramConnectorSide sourceSide,
        DiagramBlock targetBlock,
        DiagramConnectorSide targetSide)
    {
        var startValidation = ValidateConnectionStart(sourceBlock, sourceSide);
        if (!startValidation.IsValid)
        {
            return startValidation;
        }

        if (!GetVisibleConnectorSides(targetBlock).Contains(targetSide))
        {
            return DiagramConnectionValidation.Invalid("Connector is not available for this block role.");
        }

        if (sourceBlock.Id == targetBlock.Id)
        {
            return DiagramConnectionValidation.Invalid("Cannot connect a block to itself.");
        }

        if (!CanConnectRoles(sourceBlock.Role, targetBlock.Role))
        {
            return DiagramConnectionValidation.Invalid(
                "Invalid route. Allowed: Input -> Function, Function -> Function, Function -> Output.");
        }

        if (CreatesCycle(model, sourceBlock.Id, targetBlock.Id))
        {
            return DiagramConnectionValidation.Invalid("Connection would create a cycle.");
        }

        if (model.Links.Any(link =>
                link.SourceBlockId == sourceBlock.Id &&
                link.TargetBlockId == targetBlock.Id &&
                link.SourceSide == sourceSide &&
                link.TargetSide == targetSide))
        {
            return DiagramConnectionValidation.Invalid("Connection already exists between those connector points.");
        }

        return DiagramConnectionValidation.Valid;
    }

    private static bool CanConnectRoles(DiagramBlockRole sourceRole, DiagramBlockRole targetRole)
    {
        return (sourceRole, targetRole) switch
        {
            (DiagramBlockRole.Input, DiagramBlockRole.Function) => true,
            (DiagramBlockRole.Function, DiagramBlockRole.Function) => true,
            (DiagramBlockRole.Function, DiagramBlockRole.Output) => true,
            _ => false
        };
    }

    private static bool CreatesCycle(
        BlockDiagramModel model,
        string sourceBlockId,
        string targetBlockId)
    {
        var stack = new Stack<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        stack.Push(targetBlockId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == sourceBlockId)
            {
                return true;
            }

            foreach (var next in model.Links
                         .Where(link => link.SourceBlockId == current)
                         .Select(link => link.TargetBlockId))
            {
                stack.Push(next);
            }
        }

        return false;
    }
}

public readonly record struct DiagramConnectionValidation(
    bool IsValid,
    string Reason)
{
    public static DiagramConnectionValidation Valid { get; } = new(true, string.Empty);

    public static DiagramConnectionValidation Invalid(string reason)
    {
        return new DiagramConnectionValidation(false, reason);
    }
}
