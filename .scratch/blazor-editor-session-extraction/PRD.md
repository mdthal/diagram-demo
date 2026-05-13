# Blazor Editor Session Extraction

Status: ready-for-agent

## Problem Statement

The Blazor block diagram editor has already started moving deterministic logic out of the Razor component. Layout, connection rules, and routing are now separate testable modules. The next large concentration of non-UI behavior is editor command and history logic.

The editor component still directly owns selection state, undo and redo stacks, command mutations, stale-selection reconciliation, and status-message decisions for common operations such as adding blocks, deleting selections, renaming blocks, resetting the sample, undoing, and redoing. This makes the component harder to reason about and makes future features like multi-select, alignment, and distribution riskier because those features will need to coordinate selection, history, and model mutation.

The user wants to continue logic extraction before adding new behavior. The next step is to extract a behavior-preserving editor session module that owns editor commands and emits command results/messages while the Razor component remains responsible for UI events and display.

## Solution

Create a `BlockDiagramEditorSession` module that owns the active editing session around a `BlockDiagramModel`. It should manage the current model, selected block/link ids, undo/redo history, and basic editor commands. The Razor component should call this session for command behavior, receive a simple command result, and decide how to display the result.

The extraction should preserve current behavior. It should not introduce multi-select, alignment, distribution, zoom, toolbar redesign, persistence, or immutable model updates.

The session should emit command messages as data. The component may continue storing the current status text and rendering it in the existing status area.

## User Stories

1. As a block diagram author, I want adding a block to continue working the same way, so that extraction does not change my workflow.
2. As a block diagram author, I want newly added blocks to become selected, so that I can immediately inspect or edit them.
3. As a block diagram author, I want adding a block to clear selected connections, so that commands apply to the new block.
4. As a block diagram author, I want selecting a block to continue clearing selected connections, so that selection remains unambiguous.
5. As a block diagram author, I want selecting a connection to continue clearing selected blocks, so that delete and inspector behavior remain predictable.
6. As a block diagram author, I want deleting a selected connection to remove only that connection, so that I can correct wiring mistakes without changing blocks.
7. As a block diagram author, I want deleting a selected block to remove its connected links, so that the diagram cannot contain broken connections.
8. As a block diagram author, I want deleting with no selection to be a no-op, so that accidental delete commands do not change the diagram.
9. As a block diagram author, I want renaming the selected block to update its label, so that inspector editing continues to work.
10. As a block diagram author, I want renaming with no selected block to be a no-op, so that stale UI events do not mutate the model.
11. As a block diagram author, I want resetting the sample to replace the model and clear selection, so that reset remains predictable.
12. As a block diagram author, I want undo to restore the previous model and selection, so that I can recover from mistakes.
13. As a block diagram author, I want redo to restore the undone model and selection, so that I can reapply changes after checking them.
14. As a block diagram author, I want redo history to clear when I make a new change after undo, so that history follows normal editor expectations.
15. As a block diagram author, I want drag and resize undo behavior to remain one undo step per completed operation, so that extraction does not make undo noisy.
16. As a block diagram author, I want status text to continue explaining command outcomes, so that I get lightweight feedback after actions.
17. As a maintainer, I want selection and history behavior outside the Razor component, so that command logic can be tested without rendering UI.
18. As a maintainer, I want command methods to return structured results, so that UI display remains separate from command behavior.
19. As a maintainer, I want no-op commands to avoid pushing undo history, so that undo stacks stay meaningful.
20. As a maintainer, I want stale selected ids reconciled when the model changes, so that the editor does not hold invalid selection state.
21. As a maintainer, I want the Razor component to keep mouse-coordinate math for now, so that this extraction stays focused.
22. As a future implementation agent, I want a clean command surface before adding alignment, so that alignment can become another editor command instead of new Razor component logic.

## Implementation Decisions

- Add a module named `BlockDiagramEditorSession`.
- Add a result type named `DiagramCommandResult` or equivalent.
- `DiagramCommandResult` should include at least whether the command changed the diagram and an optional message emitted by the session.
- The session should own the active `BlockDiagramModel`.
- The session should own `SelectedBlockId` and `SelectedLinkId`.
- The session should own undo and redo stacks.
- The session should expose `CanUndo` and `CanRedo`.
- The session should expose the selected block and selected link, or enough selection state for the component to keep rendering the inspector.
- The session should support selecting a block by id.
- The session should support selecting a link by id.
- The session should support clearing or reconciling stale selections when the current model no longer contains the selected item.
- The session should support adding a block by role and canvas width, using `BlockDiagramLayout` for placement.
- The session should support deleting the current selection.
- The session should support resetting to the sample diagram.
- The session should support renaming the selected block.
- The session should support undo and redo.
- The session should preserve model/selection snapshots for history.
- Drag and resize live pointer math should remain in the Razor component in this tranche.
- The session may expose snapshot capture and history-push methods so drag and resize can continue creating one undo step at operation end.
- The component should continue owning connection drag transient state for now.
- The component should continue owning DOM measurement, JS interop, mouse event handling, CSS class decisions, inspector markup, toolbar markup, and status rendering.
- Status messages should be emitted by session command results, while the component decides whether and where to display them.
- Existing user-facing messages should be preserved unless minor wording changes are needed to make command results consistent.
- Current mutation-based model behavior should be preserved. Do not convert the model to immutable updates in this tranche.
- No changes are required to layout, connection rules, or routing modules except where needed to call them from the session.
- The implementation sequence should be:
  1. Add `BlockDiagramEditorSession` and command result/snapshot types.
  2. Move selection, add, delete, reset, rename, undo, redo, stale-selection reconciliation, and history stack behavior into the session.
  3. Update `BlockDiagramEditor.razor` to call session methods and assign `_status` from command results.
  4. Keep drag/resize coordinate math in the component, using session snapshot/history hooks if needed.
  5. Add unit tests for session command behavior.
  6. Run the full test project.

## Testing Decisions

- Add `BlockDiagramEditorSessionTests`.
- Use xUnit and FluentAssertions, consistent with the existing test project.
- Tests should exercise command behavior through the public session interface.
- Tests should not assert private stack implementation details.
- Test that adding a block mutates the model, selects the new block, clears link selection, emits a message, and is undoable.
- Test that deleting a selected link removes that link, clears link selection, emits a message, and is undoable.
- Test that deleting a selected block removes connected links, clears selection, emits the correct message, and is undoable.
- Test that deleting with no selection is a no-op and does not create undo history.
- Test that resetting the sample replaces the model, clears selection, emits a message, and is undoable.
- Test that renaming a selected block changes the label, emits a message, and is undoable.
- Test that renaming to the existing label is a no-op and does not create undo history.
- Test that undo restores both model contents and selected ids.
- Test that redo restores both model contents and selected ids.
- Test that redo history clears after a new change following undo.
- Test that stale selected block/link ids are cleared when reconciled against the current model.
- If drag/resize snapshot hooks are exposed, test that pushing a snapshot makes the final mutation undoable as a single step.

## Out of Scope

- Multi-select is out of scope.
- Alignment commands are out of scope.
- Distribution commands are out of scope.
- Zoom and view-state changes are out of scope.
- Toolbar redesign is out of scope.
- Inspector redesign is out of scope.
- Drag and resize pointer math extraction is out of scope.
- Connection drag state extraction is out of scope.
- Connection rule changes are out of scope.
- Routing changes are out of scope.
- Persistence, import, export, save/load, and backend storage are out of scope.
- Replacing mutable model behavior with immutable updates is out of scope.

## Further Notes

- This tranche prepares the editor for alignment work. Once command behavior and history live in a session module, alignment and distribution can be implemented as new session commands.
- The component should become thinner but will still be a real UI coordinator after this work. It will continue to own rendering, CSS classes, JS interop, and pointer event translation.
- If command result needs grow, prefer adding structured fields gradually rather than making the session aware of UI rendering.
