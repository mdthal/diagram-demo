namespace BlockDiagramDemo.Models;

public static class BlockDiagramRouter
{
    private static readonly (int Dx, int Dy, CardinalDirection Direction)[] CardinalMoves =
    [
        (0, -1, CardinalDirection.Up),
        (1, 0, CardinalDirection.Right),
        (0, 1, CardinalDirection.Down),
        (-1, 0, CardinalDirection.Left)
    ];

    public static bool TryGetLinkPath(
        BlockDiagramModel model,
        DiagramLink link,
        out string pathData)
    {
        pathData = string.Empty;

        var source = TryGetBlock(model, link.SourceBlockId);
        var target = TryGetBlock(model, link.TargetBlockId);

        if (source is null || target is null)
        {
            return false;
        }

        var start = GetConnectorPoint(source, link.SourceSide);
        var end = GetConnectorPoint(target, link.TargetSide);
        var sourceExit = OffsetFromSide(start, link.SourceSide, BlockDiagramLayout.GridSize);
        var targetEntry = OffsetFromSide(end, link.TargetSide, BlockDiagramLayout.GridSize);

        var route = BuildRoutedPolyline(
            model,
            source.Id,
            target.Id,
            start,
            sourceExit,
            targetEntry,
            end);

        pathData = BuildPathData(route);
        return true;
    }

    public static bool TryGetConnectionPreviewPath(
        BlockDiagramModel model,
        string sourceBlockId,
        DiagramConnectorSide sourceSide,
        double currentX,
        double currentY,
        out string pathData)
    {
        pathData = string.Empty;

        var source = TryGetBlock(model, sourceBlockId);
        if (source is null)
        {
            return false;
        }

        var start = GetConnectorPoint(source, sourceSide);
        var sourceExit = OffsetFromSide(start, sourceSide, BlockDiagramLayout.GridSize);
        var current = (
            X: BlockDiagramLayout.SnapToGrid(currentX),
            Y: BlockDiagramLayout.SnapToGrid(currentY));
        var preview = BuildPreviewPolyline(model, sourceExit, current);
        preview.Insert(0, start);
        preview = SimplifyPolyline(preview);
        pathData = BuildPathData(preview);

        return true;
    }

    public static List<(double X, double Y)> BuildRoutedPolyline(
        BlockDiagramModel model,
        string sourceBlockId,
        string targetBlockId,
        (double X, double Y) start,
        (double X, double Y) sourceExit,
        (double X, double Y) targetEntry,
        (double X, double Y) end)
    {
        var route = new List<(double X, double Y)> { start, sourceExit };

        if (TryFindGridPath(model, sourceExit, targetEntry, sourceBlockId, targetBlockId, out var middlePath))
        {
            foreach (var point in middlePath.Skip(1).Take(Math.Max(0, middlePath.Count - 2)))
            {
                route.Add((BlockDiagramLayout.FromGrid(point.X), BlockDiagramLayout.FromGrid(point.Y)));
            }
        }
        else
        {
            var fallback = BuildPreviewPolyline(model, sourceExit, targetEntry);
            foreach (var point in fallback.Skip(1).Take(Math.Max(0, fallback.Count - 2)))
            {
                route.Add(point);
            }
        }

        route.Add(targetEntry);
        route.Add(end);
        return SimplifyPolyline(route);
    }

    public static List<(double X, double Y)> BuildPreviewPolyline(
        BlockDiagramModel model,
        (double X, double Y) start,
        (double X, double Y) end)
    {
        if (TryFindGridPath(model, start, end, null, null, out var path))
        {
            return path.Select(point => (BlockDiagramLayout.FromGrid(point.X), BlockDiagramLayout.FromGrid(point.Y))).ToList();
        }

        var points = new List<(double X, double Y)> { start };

        if (Math.Abs(start.X - end.X) >= Math.Abs(start.Y - end.Y))
        {
            var midX = BlockDiagramLayout.SnapToGrid((start.X + end.X) / 2);
            points.Add((midX, start.Y));
            points.Add((midX, end.Y));
        }
        else
        {
            var midY = BlockDiagramLayout.SnapToGrid((start.Y + end.Y) / 2);
            points.Add((start.X, midY));
            points.Add((end.X, midY));
        }

        points.Add(end);
        return SimplifyPolyline(points);
    }

    public static List<(double X, double Y)> SimplifyPolyline(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count <= 2)
        {
            return points.ToList();
        }

        var simplified = new List<(double X, double Y)> { points[0] };

        for (var i = 1; i < points.Count - 1; i++)
        {
            var previous = simplified[^1];
            var current = points[i];
            var next = points[i + 1];
            var sameX = Math.Abs(previous.X - current.X) < 0.001 && Math.Abs(current.X - next.X) < 0.001;
            var sameY = Math.Abs(previous.Y - current.Y) < 0.001 && Math.Abs(current.Y - next.Y) < 0.001;

            if (!sameX && !sameY)
            {
                simplified.Add(current);
            }
        }

        simplified.Add(points[^1]);
        return simplified;
    }

    public static string BuildPathData(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count == 0)
        {
            return string.Empty;
        }

        var segments = points
            .Select((point, index) =>
                index == 0
                    ? FormattableString.Invariant($"M {point.X:0.###} {point.Y:0.###}")
                    : FormattableString.Invariant($"L {point.X:0.###} {point.Y:0.###}"));

        return string.Join(' ', segments);
    }

    public static (double X, double Y) OffsetFromSide(
        (double X, double Y) point,
        DiagramConnectorSide side,
        double distance)
    {
        return side switch
        {
            DiagramConnectorSide.Top => (point.X, point.Y - distance),
            DiagramConnectorSide.Right => (point.X + distance, point.Y),
            DiagramConnectorSide.Bottom => (point.X, point.Y + distance),
            DiagramConnectorSide.Left => (point.X - distance, point.Y),
            _ => point
        };
    }

    public static (double X, double Y) GetConnectorPoint(
        DiagramBlock block,
        DiagramConnectorSide side)
    {
        var x = block.X + (block.Width / 2);
        var y = block.Y + (block.Height / 2);

        switch (side)
        {
            case DiagramConnectorSide.Top:
                y = block.Y;
                break;
            case DiagramConnectorSide.Right:
                x = block.X + block.Width;
                break;
            case DiagramConnectorSide.Bottom:
                y = block.Y + block.Height;
                break;
            case DiagramConnectorSide.Left:
                x = block.X;
                break;
        }

        return (BlockDiagramLayout.SnapToGrid(x), BlockDiagramLayout.SnapToGrid(y));
    }

    private static bool TryFindGridPath(
        BlockDiagramModel model,
        (double X, double Y) start,
        (double X, double Y) end,
        string? sourceBlockId,
        string? targetBlockId,
        out List<GridPoint> path)
    {
        path = [];

        var startPoint = new GridPoint(BlockDiagramLayout.ToGrid(start.X), BlockDiagramLayout.ToGrid(start.Y));
        var endPoint = new GridPoint(BlockDiagramLayout.ToGrid(end.X), BlockDiagramLayout.ToGrid(end.Y));

        if (startPoint == endPoint)
        {
            path.Add(startPoint);
            return true;
        }

        var blocked = BuildBlockedGridSet(model, startPoint, endPoint, sourceBlockId, targetBlockId, out var minX, out var maxX, out var minY, out var maxY);
        var startState = new GridState(startPoint.X, startPoint.Y, CardinalDirection.None);
        var queue = new PriorityQueue<GridState, double>();
        var gScore = new Dictionary<GridState, double> { [startState] = 0 };
        var cameFrom = new Dictionary<GridState, GridState>();

        queue.Enqueue(startState, Heuristic(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y));

        while (queue.TryDequeue(out var current, out _))
        {
            if (current.X == endPoint.X && current.Y == endPoint.Y)
            {
                path = ReconstructGridPath(current, cameFrom);
                return true;
            }

            if (!gScore.TryGetValue(current, out var currentScore))
            {
                continue;
            }

            foreach (var (dx, dy, direction) in CardinalMoves)
            {
                var nx = current.X + dx;
                var ny = current.Y + dy;

                if (nx < minX || nx > maxX || ny < minY || ny > maxY)
                {
                    continue;
                }

                var neighborPoint = new GridPoint(nx, ny);
                if (blocked.Contains(neighborPoint) && neighborPoint != endPoint)
                {
                    continue;
                }

                var turnPenalty = current.Direction == CardinalDirection.None || current.Direction == direction ? 0 : 0.35;
                var proximityPenalty = GetProximityPenalty(neighborPoint, blocked);
                var tentative = currentScore + 1 + turnPenalty + proximityPenalty;

                var neighbor = new GridState(nx, ny, direction);
                if (gScore.TryGetValue(neighbor, out var known) && tentative >= known)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentative;
                var priority = tentative + Heuristic(nx, ny, endPoint.X, endPoint.Y);
                queue.Enqueue(neighbor, priority);
            }
        }

        return false;
    }

    private static HashSet<GridPoint> BuildBlockedGridSet(
        BlockDiagramModel model,
        GridPoint start,
        GridPoint end,
        string? sourceBlockId,
        string? targetBlockId,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        var blocked = new HashSet<GridPoint>();

        var allMinX = new List<int> { start.X, end.X };
        var allMaxX = new List<int> { start.X, end.X };
        var allMinY = new List<int> { start.Y, end.Y };
        var allMaxY = new List<int> { start.Y, end.Y };

        foreach (var block in model.Blocks)
        {
            var blockMinX = BlockDiagramLayout.ToGrid(block.X);
            var blockMaxX = BlockDiagramLayout.ToGrid(block.X + block.Width);
            var blockMinY = BlockDiagramLayout.ToGrid(block.Y);
            var blockMaxY = BlockDiagramLayout.ToGrid(block.Y + block.Height);

            allMinX.Add(blockMinX);
            allMaxX.Add(blockMaxX);
            allMinY.Add(blockMinY);
            allMaxY.Add(blockMaxY);

            for (var x = blockMinX; x <= blockMaxX; x++)
            {
                for (var y = blockMinY; y <= blockMaxY; y++)
                {
                    blocked.Add(new GridPoint(x, y));
                }
            }
        }

        var padding = 8;
        minX = allMinX.Min() - padding;
        maxX = allMaxX.Max() + padding;
        minY = allMinY.Min() - padding;
        maxY = allMaxY.Max() + padding;

        blocked.Remove(start);
        blocked.Remove(end);

        if (!string.IsNullOrWhiteSpace(sourceBlockId))
        {
            RemoveConnectorStubNeighborhood(model, sourceBlockId, start, blocked);
        }

        if (!string.IsNullOrWhiteSpace(targetBlockId))
        {
            RemoveConnectorStubNeighborhood(model, targetBlockId, end, blocked);
        }

        return blocked;
    }

    private static void RemoveConnectorStubNeighborhood(
        BlockDiagramModel model,
        string blockId,
        GridPoint point,
        HashSet<GridPoint> blocked)
    {
        if (TryGetBlock(model, blockId) is null)
        {
            return;
        }

        blocked.Remove(point);

        foreach (var (dx, dy, _) in CardinalMoves)
        {
            blocked.Remove(new GridPoint(point.X + dx, point.Y + dy));
        }
    }

    private static DiagramBlock? TryGetBlock(BlockDiagramModel model, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return model.Blocks.FirstOrDefault(block => block.Id == id);
    }

    private static double GetProximityPenalty(GridPoint point, HashSet<GridPoint> blocked)
    {
        var adjacentBlocked = 0;

        foreach (var (dx, dy, _) in CardinalMoves)
        {
            if (blocked.Contains(new GridPoint(point.X + dx, point.Y + dy)))
            {
                adjacentBlocked++;
            }
        }

        return adjacentBlocked * 0.12;
    }

    private static List<GridPoint> ReconstructGridPath(
        GridState endState,
        Dictionary<GridState, GridState> cameFrom)
    {
        var route = new List<GridPoint> { new(endState.X, endState.Y) };
        var cursor = endState;

        while (cameFrom.TryGetValue(cursor, out var previous))
        {
            route.Add(new GridPoint(previous.X, previous.Y));
            cursor = previous;
        }

        route.Reverse();
        return route;
    }

    private static double Heuristic(int x, int y, int targetX, int targetY)
    {
        return Math.Abs(targetX - x) + Math.Abs(targetY - y);
    }

    private enum CardinalDirection
    {
        None = -1,
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    private readonly record struct GridPoint(int X, int Y);

    private readonly record struct GridState(int X, int Y, CardinalDirection Direction);
}
