using BlockDiagramDemo.Models;
using FluentAssertions;

namespace BlockDiagramDemo.Tests;

public sealed class BlockDiagramEditorSessionTests
{
    private const double CanvasWidth = 800;

    [Fact]
    public void AddBlock_mutates_model_selects_block_clears_link_selection_emits_message_and_is_undoable()
    {
        var model = CreateModelWithLinkedBlocks();
        var session = new BlockDiagramEditorSession(model);
        session.SelectLink("link");

        var result = session.AddBlock(DiagramBlockRole.Output, CanvasWidth);

        result.ChangedDiagram.Should().BeTrue();
        result.Message.Should().StartWith("Added output block");
        session.Model.Blocks.Should().HaveCount(3);
        session.SelectedBlockId.Should().Be(session.Model.Blocks[^1].Id);
        session.SelectedLinkId.Should().BeNull();
        session.CanUndo.Should().BeTrue();

        session.Undo(CanvasWidth);

        session.Model.Blocks.Should().HaveCount(2);
        session.SelectedBlockId.Should().BeNull();
        session.SelectedLinkId.Should().Be("link");
    }

    [Fact]
    public void DeleteSelection_removes_selected_link_clears_selection_emits_message_and_is_undoable()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectLink("link");

        var result = session.DeleteSelection();

        result.Should().Be(DiagramCommandResult.Changed("Deleted selected connection."));
        session.Model.Links.Should().BeEmpty();
        session.SelectedLinkId.Should().BeNull();

        session.Undo(CanvasWidth);

        session.Model.Links.Should().ContainSingle(link => link.Id == "link");
        session.SelectedLinkId.Should().Be("link");
    }

    [Fact]
    public void DeleteSelection_removes_selected_block_and_connected_links_clears_selection_and_is_undoable()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");

        var result = session.DeleteSelection();

        result.Should().Be(DiagramCommandResult.Changed("Deleted selected block and 1 link(s)."));
        session.Model.Blocks.Should().ContainSingle(block => block.Id == "target");
        session.Model.Links.Should().BeEmpty();
        session.SelectedBlockId.Should().BeNull();

        session.Undo(CanvasWidth);

        session.Model.Blocks.Should().HaveCount(2);
        session.Model.Links.Should().ContainSingle(link => link.Id == "link");
        session.SelectedBlockId.Should().Be("source");
    }

    [Fact]
    public void DeleteSelection_without_selection_is_noop_and_does_not_create_undo_history()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());

        var result = session.DeleteSelection();

        result.Should().Be(DiagramCommandResult.NoChange());
        session.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Selecting_block_and_link_keep_selection_unambiguous()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());

        session.SelectLink("link");
        session.SelectedLinkId.Should().Be("link");
        session.SelectedBlockId.Should().BeNull();

        session.SelectBlock("source");
        session.SelectedBlockId.Should().Be("source");
        session.SelectedLinkId.Should().BeNull();
    }

    [Fact]
    public void ResetSample_replaces_model_clears_selection_emits_message_and_is_undoable()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");

        var result = session.ResetSample();

        result.Should().Be(DiagramCommandResult.Changed("Diagram reset to sample."));
        session.Model.Blocks.Should().Contain(block => block.Id == "input_flour");
        session.SelectedBlockId.Should().BeNull();
        session.SelectedLinkId.Should().BeNull();

        session.Undo(CanvasWidth);

        session.Model.Blocks.Should().Contain(block => block.Id == "source");
        session.SelectedBlockId.Should().Be("source");
    }

    [Fact]
    public void RenameSelectedBlock_changes_label_emits_message_and_is_undoable()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");

        var result = session.RenameSelectedBlock("Renamed");

        result.Should().Be(DiagramCommandResult.Changed("Updated label for source."));
        session.SelectedBlock!.Label.Should().Be("Renamed");

        session.Undo(CanvasWidth);

        session.SelectedBlock!.Label.Should().Be("Source");
        session.SelectedBlockId.Should().Be("source");
    }

    [Fact]
    public void RenameSelectedBlock_to_existing_label_is_noop_and_does_not_create_undo_history()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");

        var result = session.RenameSelectedBlock("Source");

        result.Should().Be(DiagramCommandResult.NoChange());
        session.CanUndo.Should().BeFalse();
    }

    [Fact]
    public void Undo_and_redo_restore_model_contents_and_selected_ids()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");
        session.RenameSelectedBlock("Renamed");

        session.Undo(CanvasWidth);

        session.SelectedBlockId.Should().Be("source");
        session.SelectedBlock!.Label.Should().Be("Source");
        session.CanRedo.Should().BeTrue();

        session.Redo(CanvasWidth);

        session.SelectedBlockId.Should().Be("source");
        session.SelectedBlock!.Label.Should().Be("Renamed");
    }

    [Fact]
    public void Redo_history_clears_after_new_change_following_undo()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");
        session.RenameSelectedBlock("First");
        session.Undo(CanvasWidth);

        session.RenameSelectedBlock("Second");

        session.CanRedo.Should().BeFalse();
        session.SelectedBlock!.Label.Should().Be("Second");
    }

    [Fact]
    public void ReconcileSelection_clears_stale_selected_block_and_link_ids()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");
        session.Model.Blocks.Clear();

        var blockResult = session.ReconcileSelection();

        blockResult.Message.Should().Be("Selection no longer exists.");
        session.SelectedBlockId.Should().BeNull();

        session.SetModel(CreateModelWithLinkedBlocks());
        session.SelectLink("link");
        session.Model.Links.Clear();

        var linkResult = session.ReconcileSelection();

        linkResult.Message.Should().Be("Selection no longer exists.");
        session.SelectedLinkId.Should().BeNull();
    }

    [Fact]
    public void Pushing_snapshot_makes_final_mutation_undoable_as_single_step()
    {
        var session = new BlockDiagramEditorSession(CreateModelWithLinkedBlocks());
        session.SelectBlock("source");
        var snapshot = session.CaptureHistorySnapshot();

        session.SelectedBlock!.X += 64;
        session.SelectedBlock.Y += 32;
        session.PushHistorySnapshot(snapshot);

        session.Undo(CanvasWidth);

        session.SelectedBlockId.Should().Be("source");
        session.SelectedBlock!.X.Should().Be(16);
        session.SelectedBlock.Y.Should().Be(64);
    }

    private static BlockDiagramModel CreateModelWithLinkedBlocks()
    {
        return new BlockDiagramModel
        {
            Blocks =
            [
                new DiagramBlock
                {
                    Id = "source",
                    Label = "Source",
                    Role = DiagramBlockRole.Input,
                    X = 16,
                    Y = 64,
                    Width = 112,
                    Height = 48
                },
                new DiagramBlock
                {
                    Id = "target",
                    Label = "Target",
                    Role = DiagramBlockRole.Function,
                    X = 176,
                    Y = 64,
                    Width = 112,
                    Height = 48
                }
            ],
            Links =
            [
                new DiagramLink
                {
                    Id = "link",
                    SourceBlockId = "source",
                    TargetBlockId = "target",
                    SourceSide = DiagramConnectorSide.Right,
                    TargetSide = DiagramConnectorSide.Left
                }
            ]
        };
    }
}
