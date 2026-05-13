namespace BlockDiagramDemo.Models;

public sealed class BlockDiagramEditorSession
{
    private readonly Stack<DiagramHistorySnapshot> _undoStack = new();
    private readonly Stack<DiagramHistorySnapshot> _redoStack = new();

    public BlockDiagramEditorSession()
        : this(new BlockDiagramModel())
    {
    }

    public BlockDiagramEditorSession(BlockDiagramModel model)
    {
        Model = model;
        ReconcileSelection();
    }

    public BlockDiagramModel Model { get; private set; }

    public string? SelectedBlockId { get; private set; }

    public string? SelectedLinkId { get; private set; }

    public DiagramBlock? SelectedBlock => TryGetBlock(SelectedBlockId);

    public DiagramLink? SelectedLink => TryGetLink(SelectedLinkId);

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void SetModel(BlockDiagramModel model)
    {
        Model = model;
        ReconcileSelection();
    }

    public DiagramCommandResult SelectBlock(string blockId)
    {
        var block = TryGetBlock(blockId);
        if (block is null)
        {
            SelectedBlockId = null;
            return DiagramCommandResult.NoChange("Selection no longer exists.");
        }

        SelectedBlockId = block.Id;
        SelectedLinkId = null;
        return DiagramCommandResult.NoChange($"Selected {block.Label}.");
    }

    public DiagramCommandResult SelectLink(string linkId)
    {
        if (TryGetLink(linkId) is null)
        {
            SelectedLinkId = null;
            return DiagramCommandResult.NoChange("Selection no longer exists.");
        }

        SelectedLinkId = linkId;
        SelectedBlockId = null;
        return DiagramCommandResult.NoChange("Selected connection.");
    }

    public DiagramCommandResult AddBlock(DiagramBlockRole role, double canvasWidth)
    {
        RecordHistoryBeforeChange();

        var block = BlockDiagramLayout.CreateBlock(role, Model, canvasWidth);
        Model.Blocks.Add(block);
        SelectedBlockId = block.Id;
        SelectedLinkId = null;

        return DiagramCommandResult.Changed(
            $"Added {role.ToString().ToLowerInvariant()} block \"{block.Label}\".");
    }

    public DiagramCommandResult AddConnection(DiagramLink link)
    {
        RecordHistoryBeforeChange();

        Model.Links.Add(link);
        SelectedLinkId = link.Id;
        SelectedBlockId = null;

        return DiagramCommandResult.Changed("Connection added.");
    }

    public DiagramCommandResult DeleteSelection()
    {
        if (SelectedLinkId is not null)
        {
            if (TryGetLink(SelectedLinkId) is null)
            {
                SelectedLinkId = null;
                return DiagramCommandResult.NoChange("Selection no longer exists.");
            }

            RecordHistoryBeforeChange();

            var removedLinkCount = Model.Links.RemoveAll(link => link.Id == SelectedLinkId);
            SelectedLinkId = null;

            return removedLinkCount > 0
                ? DiagramCommandResult.Changed("Deleted selected connection.")
                : DiagramCommandResult.NoChange();
        }

        if (SelectedBlockId is null)
        {
            return DiagramCommandResult.NoChange();
        }

        if (TryGetBlock(SelectedBlockId) is null)
        {
            SelectedBlockId = null;
            return DiagramCommandResult.NoChange("Selection no longer exists.");
        }

        RecordHistoryBeforeChange();

        var selectedBlockId = SelectedBlockId;
        var removedCount = Model.Links.RemoveAll(
            link => link.SourceBlockId == selectedBlockId || link.TargetBlockId == selectedBlockId);

        var removedBlock = Model.Blocks.RemoveAll(block => block.Id == selectedBlockId) > 0;
        SelectedBlockId = null;
        SelectedLinkId = null;

        if (!removedBlock)
        {
            return DiagramCommandResult.NoChange("Selection no longer exists.");
        }

        var message = removedCount == 0
            ? "Deleted selected block."
            : $"Deleted selected block and {removedCount} link(s).";

        return DiagramCommandResult.Changed(message);
    }

    public DiagramCommandResult ResetSample()
    {
        RecordHistoryBeforeChange();

        Model = BlockDiagramSampleFactory.CreateDefault();
        SelectedBlockId = null;
        SelectedLinkId = null;

        return DiagramCommandResult.Changed("Diagram reset to sample.");
    }

    public DiagramCommandResult RenameSelectedBlock(string label)
    {
        var block = SelectedBlock;
        if (block is null)
        {
            return DiagramCommandResult.NoChange();
        }

        if (string.Equals(block.Label, label, StringComparison.Ordinal))
        {
            return DiagramCommandResult.NoChange();
        }

        RecordHistoryBeforeChange();
        block.Label = label;

        return DiagramCommandResult.Changed($"Updated label for {block.Id}.");
    }

    public DiagramCommandResult Undo(double canvasWidth)
    {
        if (!CanUndo)
        {
            return DiagramCommandResult.NoChange();
        }

        _redoStack.Push(CaptureHistorySnapshot());
        RestoreHistorySnapshot(_undoStack.Pop(), canvasWidth);

        return DiagramCommandResult.Changed("Undid last change.");
    }

    public DiagramCommandResult Redo(double canvasWidth)
    {
        if (!CanRedo)
        {
            return DiagramCommandResult.NoChange();
        }

        _undoStack.Push(CaptureHistorySnapshot());
        RestoreHistorySnapshot(_redoStack.Pop(), canvasWidth);

        return DiagramCommandResult.Changed("Redid last change.");
    }

    public DiagramHistorySnapshot CaptureHistorySnapshot()
    {
        return new DiagramHistorySnapshot(Model.Clone(), SelectedBlockId, SelectedLinkId);
    }

    public void PushHistorySnapshot(DiagramHistorySnapshot snapshot)
    {
        _undoStack.Push(snapshot);
        _redoStack.Clear();
    }

    public void ConstrainBlocksToLanes(double canvasWidth)
    {
        BlockDiagramLayout.ConstrainBlocksToLanes(Model, canvasWidth);
    }

    public DiagramCommandResult ReconcileSelection()
    {
        var changed = false;

        if (TryGetBlock(SelectedBlockId) is null && SelectedBlockId is not null)
        {
            SelectedBlockId = null;
            changed = true;
        }

        if (TryGetLink(SelectedLinkId) is null && SelectedLinkId is not null)
        {
            SelectedLinkId = null;
            changed = true;
        }

        return changed
            ? DiagramCommandResult.NoChange("Selection no longer exists.")
            : DiagramCommandResult.NoChange();
    }

    public DiagramBlock? TryGetBlock(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Model.Blocks.FirstOrDefault(block => block.Id == id);
    }

    public DiagramLink? TryGetLink(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Model.Links.FirstOrDefault(link => link.Id == id);
    }

    private void RecordHistoryBeforeChange()
    {
        PushHistorySnapshot(CaptureHistorySnapshot());
    }

    private void RestoreHistorySnapshot(DiagramHistorySnapshot snapshot, double canvasWidth)
    {
        Model = snapshot.Model.Clone();
        SelectedBlockId = snapshot.SelectedBlockId;
        SelectedLinkId = snapshot.SelectedLinkId;
        BlockDiagramLayout.ConstrainBlocksToLanes(Model, canvasWidth);
        ReconcileSelection();
    }
}

public sealed record DiagramCommandResult(
    bool ChangedDiagram,
    string? Message = null)
{
    public static DiagramCommandResult Changed(string? message = null)
    {
        return new DiagramCommandResult(true, message);
    }

    public static DiagramCommandResult NoChange(string? message = null)
    {
        return new DiagramCommandResult(false, message);
    }
}

public sealed record DiagramHistorySnapshot(
    BlockDiagramModel Model,
    string? SelectedBlockId,
    string? SelectedLinkId);
