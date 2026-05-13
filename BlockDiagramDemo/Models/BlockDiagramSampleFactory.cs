namespace BlockDiagramDemo.Models;

public static class BlockDiagramSampleFactory
{
    public static BlockDiagramModel CreateDefault()
    {
        var model = new BlockDiagramModel();

        model.Blocks.AddRange(
        [
            new DiagramBlock
            {
                Id = "input_flour",
                Label = "Flour",
                Role = DiagramBlockRole.Input,
                X = 16,
                Y = 64,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "input_water",
                Label = "Water",
                Role = DiagramBlockRole.Input,
                X = 16,
                Y = 176,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function_meter_flour",
                Label = "Meter",
                Role = DiagramBlockRole.Function,
                X = 176,
                Y = 64,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function_meter_water",
                Label = "Meter",
                Role = DiagramBlockRole.Function,
                X = 176,
                Y = 176,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function_mix",
                Label = "Mix",
                Role = DiagramBlockRole.Function,
                X = 288,
                Y = 128,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function_divide",
                Label = "Divide",
                Role = DiagramBlockRole.Function,
                X = 400,
                Y = 176,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "function_bake",
                Label = "Bake",
                Role = DiagramBlockRole.Function,
                X = 512,
                Y = 240,
                Width = 112,
                Height = 48
            },
            new DiagramBlock
            {
                Id = "output_accept",
                Label = "Tortillas",
                Role = DiagramBlockRole.Output,
                X = 672,
                Y = 240,
                Width = 112,
                Height = 48
            }
        ]);

        model.Links.AddRange(
        [
            new DiagramLink
            {
                Id = "link_input_flour_meter",
                SourceBlockId = "input_flour",
                TargetBlockId = "function_meter_flour",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_input_water_meter",
                SourceBlockId = "input_water",
                TargetBlockId = "function_meter_water",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_meter_flour_mix",
                SourceBlockId = "function_meter_flour",
                TargetBlockId = "function_mix",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_meter_water_mix",
                SourceBlockId = "function_meter_water",
                TargetBlockId = "function_mix",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_mix_divide",
                SourceBlockId = "function_mix",
                TargetBlockId = "function_divide",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_divide_bake",
                SourceBlockId = "function_divide",
                TargetBlockId = "function_bake",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_bake_accept",
                SourceBlockId = "function_bake",
                TargetBlockId = "output_accept",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            }
        ]);

        return model;
    }
}
