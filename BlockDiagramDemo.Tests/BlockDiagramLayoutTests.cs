using BlockDiagramDemo.Models;
using FluentAssertions;

namespace BlockDiagramDemo.Tests;

public sealed class BlockDiagramLayoutTests
{
    [Fact]
    public void GetLaneLayout_uses_minimum_canvas_width()
    {
        var layout = BlockDiagramLayout.GetLaneLayout(320);

        layout.InputStart.Should().Be(0);
        layout.FunctionStart.Should().Be(BlockDiagramLayout.InputLaneWidth);
        layout.OutputStart.Should().Be(368);
        layout.OutputEnd.Should().Be(BlockDiagramLayout.MinimumCanvasWidth);
        layout.FunctionWidth.Should().Be(BlockDiagramLayout.MinimumFunctionLaneWidth);
    }

    [Fact]
    public void GetLaneLayout_keeps_input_and_output_fixed_while_middle_expands()
    {
        var layout = BlockDiagramLayout.GetLaneLayout(800);

        layout.FunctionStart.Should().Be(BlockDiagramLayout.InputLaneWidth);
        layout.OutputStart.Should().Be(656);
        layout.OutputEnd.Should().Be(800);
        layout.FunctionWidth.Should().Be(512);
    }

    [Theory]
    [InlineData(DiagramBlockRole.Input, 12, 132)]
    [InlineData(DiagramBlockRole.Function, 156, 644)]
    [InlineData(DiagramBlockRole.Output, 668, 788)]
    public void GetLaneHorizontalBounds_maps_roles_to_padded_lanes(
        DiagramBlockRole role,
        double expectedMinX,
        double expectedMaxX)
    {
        var bounds = BlockDiagramLayout.GetLaneHorizontalBounds(role, 800);

        bounds.MinX.Should().Be(expectedMinX);
        bounds.MaxX.Should().Be(expectedMaxX);
    }

    [Theory]
    [InlineData(23, 16)]
    [InlineData(24, 32)]
    [InlineData(40, 48)]
    public void SnapToGrid_rounds_to_the_nearest_grid_line(
        double value,
        double expected)
    {
        BlockDiagramLayout.SnapToGrid(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(38, 64, 64)]
    [InlineData(73, 64, 80)]
    [InlineData(96, 64, 96)]
    public void SnapSizeToGrid_snaps_sizes_and_preserves_minimums(
        double value,
        double minimum,
        double expected)
    {
        BlockDiagramLayout.SnapSizeToGrid(value, minimum).Should().Be(expected);
    }

    [Fact]
    public void ConstrainBlockToLane_snaps_size_and_keeps_block_inside_role_lane()
    {
        var block = new DiagramBlock
        {
            Role = DiagramBlockRole.Function,
            X = 900,
            Y = 12,
            Width = 119,
            Height = 51
        };

        BlockDiagramLayout.ConstrainBlockToLane(block, 800);

        block.X.Should().Be(528);
        block.Y.Should().Be(BlockDiagramLayout.MinimumBlockOffset);
        block.Width.Should().Be(112);
        block.Height.Should().Be(BlockDiagramLayout.MinimumBlockHeight);

        var bounds = BlockDiagramLayout.GetLaneHorizontalBounds(block.Role, 800);
        block.X.Should().BeGreaterThanOrEqualTo(bounds.MinX);
        (block.X + block.Width).Should().BeLessThanOrEqualTo(bounds.MaxX);
    }

    [Fact]
    public void ConstrainBlocksToLanes_mutates_every_block_in_the_model()
    {
        var model = new BlockDiagramModel
        {
            Blocks =
            [
                new DiagramBlock { Role = DiagramBlockRole.Input, X = 400, Y = 0, Width = 112, Height = 48 },
                new DiagramBlock { Role = DiagramBlockRole.Output, X = 0, Y = 0, Width = 112, Height = 48 }
            ]
        };

        BlockDiagramLayout.ConstrainBlocksToLanes(model, 800);

        model.Blocks[0].X.Should().Be(16);
        model.Blocks[0].Y.Should().Be(BlockDiagramLayout.MinimumBlockOffset);
        model.Blocks[1].X.Should().Be(672);
        model.Blocks[1].Y.Should().Be(BlockDiagramLayout.MinimumBlockOffset);
    }

    [Theory]
    [InlineData(DiagramBlockRole.Input, "Input 1", 16, 80)]
    [InlineData(DiagramBlockRole.Function, "Function 1", 160, 80)]
    [InlineData(DiagramBlockRole.Output, "Output 1", 672, 80)]
    public void CreateBlock_places_first_block_in_the_matching_lane(
        DiagramBlockRole role,
        string expectedLabel,
        double expectedX,
        double expectedY)
    {
        var block = BlockDiagramLayout.CreateBlock(role, new BlockDiagramModel(), 800);

        block.Id.Should().StartWith($"{role.ToString().ToLowerInvariant()}_");
        block.Label.Should().Be(expectedLabel);
        block.Role.Should().Be(role);
        block.X.Should().Be(expectedX);
        block.Y.Should().Be(expectedY);
        block.Width.Should().Be(BlockDiagramLayout.DefaultBlockWidth);
        block.Height.Should().Be(BlockDiagramLayout.DefaultBlockHeight);
    }

    [Fact]
    public void CreateBlock_places_function_blocks_in_two_columns_then_next_row()
    {
        var model = new BlockDiagramModel
        {
            Blocks =
            [
                new DiagramBlock { Role = DiagramBlockRole.Function },
                new DiagramBlock { Role = DiagramBlockRole.Function }
            ]
        };

        var block = BlockDiagramLayout.CreateBlock(DiagramBlockRole.Function, model, 800);

        block.Label.Should().Be("Function 3");
        block.X.Should().Be(160);
        block.Y.Should().Be(224);
    }

    [Fact]
    public void CreateBlock_places_second_input_below_existing_input()
    {
        var model = new BlockDiagramModel
        {
            Blocks = [new DiagramBlock { Role = DiagramBlockRole.Input }]
        };

        var block = BlockDiagramLayout.CreateBlock(DiagramBlockRole.Input, model, 800);

        block.Label.Should().Be("Input 2");
        block.X.Should().Be(16);
        block.Y.Should().Be(224);
    }
}
