using BlockDiagramDemo.Models;
using FluentAssertions;

namespace BlockDiagramDemo.Tests;

public sealed class BlockDiagramRouterTests
{
    [Theory]
    [InlineData(DiagramConnectorSide.Top, 80, 32)]
    [InlineData(DiagramConnectorSide.Right, 128, 64)]
    [InlineData(DiagramConnectorSide.Bottom, 80, 80)]
    [InlineData(DiagramConnectorSide.Left, 16, 64)]
    public void GetConnectorPoint_returns_snapped_edge_midpoints(
        DiagramConnectorSide side,
        double expectedX,
        double expectedY)
    {
        var block = new DiagramBlock
        {
            X = 16,
            Y = 32,
            Width = 112,
            Height = 48
        };

        BlockDiagramRouter.GetConnectorPoint(block, side).Should().Be((expectedX, expectedY));
    }

    [Theory]
    [InlineData(DiagramConnectorSide.Top, 40, 24)]
    [InlineData(DiagramConnectorSide.Right, 56, 40)]
    [InlineData(DiagramConnectorSide.Bottom, 40, 56)]
    [InlineData(DiagramConnectorSide.Left, 24, 40)]
    public void OffsetFromSide_moves_points_outward_from_connector_side(
        DiagramConnectorSide side,
        double expectedX,
        double expectedY)
    {
        var point = (X: 40d, Y: 40d);

        BlockDiagramRouter.OffsetFromSide(point, side, 16).Should().Be((expectedX, expectedY));
    }

    [Fact]
    public void SimplifyPolyline_removes_redundant_collinear_points()
    {
        var points = new List<(double X, double Y)>
        {
            (0, 0),
            (16, 0),
            (32, 0),
            (32, 16),
            (32, 32),
            (48, 32)
        };

        BlockDiagramRouter.SimplifyPolyline(points)
            .Should()
            .Equal((0, 0), (32, 0), (32, 32), (48, 32));
    }

    [Fact]
    public void BuildPathData_formats_svg_move_and_line_segments()
    {
        var pathData = BlockDiagramRouter.BuildPathData([(0, 0), (16, 0), (16, 32)]);

        pathData.Should().Be("M 0 0 L 16 0 L 16 32");
    }

    [Fact]
    public void TryGetLinkPath_returns_false_when_source_or_target_is_missing()
    {
        var model = new BlockDiagramModel
        {
            Blocks = [new DiagramBlock { Id = "source" }]
        };
        var link = new DiagramLink
        {
            SourceBlockId = "source",
            TargetBlockId = "missing"
        };

        BlockDiagramRouter.TryGetLinkPath(model, link, out var pathData).Should().BeFalse();
        pathData.Should().BeEmpty();
    }

    [Fact]
    public void TryGetLinkPath_routes_unobstructed_links_directly_between_connector_points()
    {
        var model = CreateModel(
            new DiagramBlock
            {
                Id = "input",
                Role = DiagramBlockRole.Input,
                X = 16,
                Y = 64,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function",
                Role = DiagramBlockRole.Function,
                X = 176,
                Y = 64,
                Width = 112,
                Height = 48
            });
        var link = new DiagramLink
        {
            SourceBlockId = "input",
            SourceSide = DiagramConnectorSide.Right,
            TargetBlockId = "function",
            TargetSide = DiagramConnectorSide.Left
        };

        BlockDiagramRouter.TryGetLinkPath(model, link, out var pathData).Should().BeTrue();

        pathData.Should().Be("M 128 96 L 176 96");
    }

    [Fact]
    public void BuildRoutedPolyline_routes_around_block_obstacles()
    {
        var model = CreateModel(
            new DiagramBlock { Id = "source", Role = DiagramBlockRole.Function, X = 0, Y = 64, Width = 64, Height = 64 },
            new DiagramBlock { Id = "blocker", Role = DiagramBlockRole.Function, X = 96, Y = 64, Width = 64, Height = 64 },
            new DiagramBlock { Id = "target", Role = DiagramBlockRole.Function, X = 192, Y = 64, Width = 64, Height = 64 });

        var route = BlockDiagramRouter.BuildRoutedPolyline(
            model,
            "source",
            "target",
            (64, 96),
            (80, 96),
            (176, 96),
            (192, 96));

        route.Should().StartWith((64, 96));
        route.Should().EndWith((192, 96));
        route.Should().Contain(point => Math.Abs(point.Y - 96) > 0.001);
    }

    [Fact]
    public void TryGetConnectionPreviewPath_starts_at_source_connector_and_snaps_current_point()
    {
        var model = CreateModel(new DiagramBlock
        {
            Id = "source",
            Role = DiagramBlockRole.Function,
            X = 16,
            Y = 64,
            Width = 112,
            Height = 48
        });

        BlockDiagramRouter.TryGetConnectionPreviewPath(
            model,
            "source",
            DiagramConnectorSide.Right,
            246,
            140,
            out var pathData).Should().BeTrue();

        pathData.Should().StartWith("M 128 96");
        pathData.Should().EndWith("240 144");
    }

    private static BlockDiagramModel CreateModel(params DiagramBlock[] blocks)
    {
        return new BlockDiagramModel { Blocks = blocks.ToList() };
    }
}
