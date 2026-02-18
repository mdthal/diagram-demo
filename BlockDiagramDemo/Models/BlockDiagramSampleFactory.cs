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
                X = 48,
                Y = 80,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "input_water",
                Label = "Water",
                Role = DiagramBlockRole.Input,
                X = 48,
                Y = 208,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "action_meter_flour",
                Label = "Meter",
                Role = DiagramBlockRole.Action,
                X = 224,
                Y = 80,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "action_meter_water",
                Label = "Meter",
                Role = DiagramBlockRole.Action,
                X = 224,
                Y = 208,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "action_mix",
                Label = "Mix",
                Role = DiagramBlockRole.Action,
                X = 384,
                Y = 144,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "action_divide",
                Label = "Divide",
                Role = DiagramBlockRole.Action,
                X = 488,
                Y = 144,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "action_bake",
                Label = "Bake",
                Role = DiagramBlockRole.Action,
                X = 600,
                Y = 144,
                Width = 128,
                Height = 56
            },
            new DiagramBlock
            {
                Id = "output_accept",
                Label = "Tortillas",
                Role = DiagramBlockRole.Output,
                X = 792,
                Y = 104,
                Width = 128,
                Height = 56
            }
        ]);

        model.Links.AddRange(
        [
            new DiagramLink
            {
                Id = "link_input_flour_meter",
                SourceBlockId = "input_flour",
                TargetBlockId = "action_meter_flour",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_input_water_meter",
                SourceBlockId = "input_water",
                TargetBlockId = "action_meter_water",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_meter_flour_mix",
                SourceBlockId = "action_meter_flour",
                TargetBlockId = "action_mix",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_meter_water_mix",
                SourceBlockId = "action_meter_water",
                TargetBlockId = "action_mix",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_mix_divide",
                SourceBlockId = "action_mix",
                TargetBlockId = "action_divide",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_divide_bake",
                SourceBlockId = "action_divide",
                TargetBlockId = "action_bake",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            },
            new DiagramLink
            {
                Id = "link_bake_accept",
                SourceBlockId = "action_bake",
                TargetBlockId = "output_accept",
                SourceSide = DiagramConnectorSide.Right,
                TargetSide = DiagramConnectorSide.Left
            }
        ]);

        return model;
    }
}
