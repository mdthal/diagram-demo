using BlockDiagramDemo.Models;
using FluentAssertions;

namespace BlockDiagramDemo.Tests;

public sealed class BlockDiagramConnectionRulesTests
{
    [Fact]
    public void GetVisibleConnectorSides_limits_inputs_and_outputs_to_flow_edges()
    {
        BlockDiagramConnectionRules.GetVisibleConnectorSides(new DiagramBlock { Role = DiagramBlockRole.Input })
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(DiagramConnectorSide.Right);

        BlockDiagramConnectionRules.GetVisibleConnectorSides(new DiagramBlock { Role = DiagramBlockRole.Output })
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be(DiagramConnectorSide.Left);
    }

    [Fact]
    public void GetVisibleConnectorSides_allows_functions_to_connect_from_any_side()
    {
        BlockDiagramConnectionRules.GetVisibleConnectorSides(new DiagramBlock { Role = DiagramBlockRole.Function })
            .Should()
            .BeEquivalentTo(new[]
            {
                DiagramConnectorSide.Top,
                DiagramConnectorSide.Right,
                DiagramConnectorSide.Bottom,
                DiagramConnectorSide.Left
            });
    }

    [Fact]
    public void ValidateConnectionStart_rejects_output_blocks()
    {
        var validation = BlockDiagramConnectionRules.ValidateConnectionStart(
            new DiagramBlock { Role = DiagramBlockRole.Output },
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Output blocks cannot start connections.");
    }

    [Theory]
    [InlineData(DiagramBlockRole.Input, DiagramConnectorSide.Right, DiagramBlockRole.Function, DiagramConnectorSide.Left)]
    [InlineData(DiagramBlockRole.Function, DiagramConnectorSide.Right, DiagramBlockRole.Function, DiagramConnectorSide.Left)]
    [InlineData(DiagramBlockRole.Function, DiagramConnectorSide.Right, DiagramBlockRole.Output, DiagramConnectorSide.Left)]
    public void ValidateConnection_accepts_allowed_routes(
        DiagramBlockRole sourceRole,
        DiagramConnectorSide sourceSide,
        DiagramBlockRole targetRole,
        DiagramConnectorSide targetSide)
    {
        var model = CreateModel(
            new DiagramBlock { Id = "source", Role = sourceRole },
            new DiagramBlock { Id = "target", Role = targetRole });

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            model.Blocks[0],
            sourceSide,
            model.Blocks[1],
            targetSide);

        validation.IsValid.Should().BeTrue();
        validation.Reason.Should().BeEmpty();
    }

    [Fact]
    public void ValidateConnection_rejects_unavailable_connector_sides()
    {
        var model = CreateModel(
            new DiagramBlock { Id = "input", Role = DiagramBlockRole.Input },
            new DiagramBlock { Id = "function", Role = DiagramBlockRole.Function });

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            model.Blocks[0],
            DiagramConnectorSide.Left,
            model.Blocks[1],
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Connector is not available for this block role.");
    }

    [Fact]
    public void ValidateConnection_rejects_invalid_role_pairs()
    {
        var model = CreateModel(
            new DiagramBlock { Id = "input", Role = DiagramBlockRole.Input },
            new DiagramBlock { Id = "output", Role = DiagramBlockRole.Output });

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            model.Blocks[0],
            DiagramConnectorSide.Right,
            model.Blocks[1],
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Invalid route. Allowed: Input -> Function, Function -> Function, Function -> Output.");
    }

    [Fact]
    public void ValidateConnection_rejects_self_connections()
    {
        var block = new DiagramBlock { Id = "function", Role = DiagramBlockRole.Function };
        var model = CreateModel(block);

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            block,
            DiagramConnectorSide.Right,
            block,
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Cannot connect a block to itself.");
    }

    [Fact]
    public void ValidateConnection_rejects_cycles()
    {
        var model = CreateModel(
            new DiagramBlock { Id = "first", Role = DiagramBlockRole.Function },
            new DiagramBlock { Id = "second", Role = DiagramBlockRole.Function });
        model.Links.Add(new DiagramLink
        {
            SourceBlockId = "first",
            TargetBlockId = "second",
            SourceSide = DiagramConnectorSide.Right,
            TargetSide = DiagramConnectorSide.Left
        });

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            model.Blocks[1],
            DiagramConnectorSide.Right,
            model.Blocks[0],
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Connection would create a cycle.");
    }

    [Fact]
    public void ValidateConnection_rejects_duplicate_connector_pairs()
    {
        var model = CreateModel(
            new DiagramBlock { Id = "input", Role = DiagramBlockRole.Input },
            new DiagramBlock { Id = "function", Role = DiagramBlockRole.Function });
        model.Links.Add(new DiagramLink
        {
            SourceBlockId = "input",
            TargetBlockId = "function",
            SourceSide = DiagramConnectorSide.Right,
            TargetSide = DiagramConnectorSide.Left
        });

        var validation = BlockDiagramConnectionRules.ValidateConnection(
            model,
            model.Blocks[0],
            DiagramConnectorSide.Right,
            model.Blocks[1],
            DiagramConnectorSide.Left);

        validation.IsValid.Should().BeFalse();
        validation.Reason.Should().Be("Connection already exists between those connector points.");
    }

    private static BlockDiagramModel CreateModel(params DiagramBlock[] blocks)
    {
        return new BlockDiagramModel { Blocks = blocks.ToList() };
    }
}
