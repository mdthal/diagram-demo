# Blazor Editor Foundation

Status: ready-for-agent

## Problem Statement

The project began as a comparison between multiple block diagramming approaches. The chosen direction is now the Blazor-first block diagram editor, but the repository and app shell still carry comparison-era code and structure. The old JointJS route, component, JavaScript interop, styles, navigation entry, and globally loaded CDN assets remain in the app even though they are no longer part of the intended product direction.

The Blazor editor also contains deterministic canvas layout behavior inside the editor implementation itself. Grid snapping, section geometry, role-to-section bounds, block constraints, and default block placement are mixed together with rendering, mouse handling, inspector state, link routing, selection, and history. This is workable for a prototype, but it will make upcoming improvements like alignment, zoom, and section behavior harder to implement safely.

The user wants a clean starting point before changing behavior: remove comparison/demo shell code, make the Blazor editor launch directly as the full-screen home experience, extract the current layout rules into a small testable module, and begin the project's test suite.

## Solution

Create a cleanup and layout-foundation tranche that preserves current Blazor editor behavior while simplifying the app around it.

This tranche will:

- Fully remove the old JointJS comparison implementation from source and app loading.
- Remove the navigation/sidebar demo shell.
- Launch the Blazor block diagram editor directly on the home page.
- Use a minimal layout that gives the editor the whole screen.
- Keep Bootstrap for now because the current editor still depends on Bootstrap classes.
- Extract deterministic canvas layout behavior into a public `BlockDiagramLayout` module.
- Add an xUnit test project with FluentAssertions.
- Add focused unit tests for current layout rules.

This tranche should not introduce alignment, zoom, multi-select, toolbar redesign, connection-rule refactoring, link-routing refactoring, Bootstrap removal, or the future `Action` to `Function` rename.

## User Stories

1. As a block diagram author, I want the app to open directly into the Blazor editor, so that I can start diagramming without navigating through a demo shell.
2. As a block diagram author, I want the editor to use the full screen, so that canvas work has as much space as possible.
3. As a block diagram author, I want the old JointJS option removed, so that the app clearly reflects the chosen Blazor-first direction.
4. As a block diagram author, I want the Blazor editor to behave the same after cleanup, so that refactoring does not disrupt my current workflow.
5. As a block diagram author, I want existing block placement behavior preserved, so that sample diagrams and added blocks still appear where expected.
6. As a block diagram author, I want blocks to continue snapping to the grid, so that diagrams remain tidy.
7. As a block diagram author, I want block sizes to continue snapping to the grid, so that resized blocks stay visually aligned.
8. As a block diagram author, I want blocks to remain constrained to their current role sections, so that inputs, process/action blocks, and outputs stay structurally organized.
9. As a block diagram author, I want the input and output sections to remain stable widths, so that those sections keep their current visual role.
10. As a block diagram author, I want the middle process/action section to continue using extra horizontal room, so that the main workflow area expands when space is available.
11. As a block diagram author, I want the editor to remain usable at narrow widths, so that the canvas still has a minimum working size.
12. As a block diagram author, I want the inspector opening or closing to preserve valid block layout, so that the diagram remains coherent when available canvas width changes.
13. As a block diagram author, I want browser resizing to preserve valid block layout, so that the diagram remains coherent when the viewport changes.
14. As a maintainer, I want the old JointJS source removed, so that future changes are not split between a chosen editor and a discarded comparison implementation.
15. As a maintainer, I want unused global JointJS assets removed, so that the app shell only loads dependencies used by the chosen editor.
16. As a maintainer, I want the navigation/sidebar shell removed, so that the application structure matches the single-editor product shape.
17. As a maintainer, I want the page host to be thin, so that model creation and editor rendering are easy to understand.
18. As a maintainer, I want canvas layout logic extracted from the editor, so that upcoming behavior work has a stable foundation.
19. As a maintainer, I want `BlockDiagramLayout` to own the layout constants for now, so that layout rules and tests live together.
20. As a maintainer, I want `BlockDiagramLayout` to be public, so that tests can use it directly and it can be moved to a future logic library with minimal friction.
21. As a maintainer, I want layout methods to preserve the current mutation style, so that this tranche does not force a broader model-update rewrite.
22. As a maintainer, I want the layout module interface to remain intentional, so that a future immutable editor-action model is still possible.
23. As a maintainer, I want a test project introduced now, so that future alignment, zoom, and section behavior work can build on executable expectations.
24. As a maintainer, I want tests written as behavioral rules rather than snapshots of every incidental number, so that future layout changes are easy to adjust intentionally.
25. As a future implementation agent, I want this tranche to be behavior-preserving, so that cleanup and extraction can be verified without also evaluating new product behavior.

## Implementation Decisions

- The Blazor-first editor is the only active diagramming implementation after this tranche.
- The JointJS comparison implementation will be fully removed from source rather than archived in the running app.
- The JointJS route, JointJS canvas component, JointJS page styles, JointJS JavaScript interop, navigation entry, global JointJS CSS include, and global JointJS JavaScript include will be removed.
- The app will launch the Blazor editor directly from the home route.
- The demo sidebar/navigation shell will be removed.
- The default layout will remain as a minimal full-screen body wrapper, because Blazor routing already has a layout concept and future app-level concerns may need a place to live.
- The home page will remain a thin host that creates the current sample model and renders the editor.
- The demo-style page heading will be removed so the editor can own the first screen.
- Minimal CSS will be added or adjusted so the app, layout, page host, and editor can occupy the full viewport.
- Bootstrap will remain for now because the editor currently uses Bootstrap buttons, grid, cards, forms, and utility classes.
- The existing Blazor editor behavior should be preserved, except for the intentional removal of JointJS navigation and assets.
- The extracted layout module will be named `BlockDiagramLayout`.
- Supporting public value types may be introduced for lane layout and lane bounds.
- `BlockDiagramLayout` will own deterministic layout rules: grid size, minimum block offset, minimum block size, default block size, minimum canvas width, input/output lane widths, minimum middle-lane width, and lane padding.
- `BlockDiagramLayout` will own grid snapping, size snapping, lane geometry, role-to-lane bounds, block constraint behavior, all-block constraint behavior, and default block creation/placement.
- `BlockDiagramLayout` will not own Blazor rendering, DOM measurement, JavaScript interop, pointer events, drag state, resize state, selection state, undo/redo state, inspector state, connection validation, or link routing.
- The layout module may mutate existing block instances to match the current model style.
- The layout module should expose intentional operations rather than leaking editor event details.
- The current internal `Action` role name will be preserved during this tranche, even though the future domain name should become `Function`.
- Connection rules and link routing will remain in the editor for now and should be addressed in a later focused pass.
- No persistence, import/export, or backend contract changes are part of this tranche.
- The implementation sequence should be:
  1. Remove JointJS route, component, styles, interop script, nav entry, and global CDN assets.
  2. Simplify the app shell to a full-screen editor host with minimal layout.
  3. Extract `BlockDiagramLayout` and supporting value types.
  4. Update the editor to use `BlockDiagramLayout` while preserving current behavior.
  5. Add `BlockDiagramDemo.Tests` with xUnit and FluentAssertions.
  6. Add layout tests for current section, grid, bounds, constraint, and default-placement behavior.
  7. Run build and tests.

## Testing Decisions

- Add a test project for this tranche.
- Use xUnit as the test framework.
- Use FluentAssertions for readable assertions.
- Do not use Bogus for this tranche because layout tests should use carefully chosen examples.
- Do not use NSubstitute for this tranche because the extracted layout module should not require mocks.
- The test project can directly reference the current Blazor app project for now.
- Tests should target the public `BlockDiagramLayout` module directly.
- Tests should verify externally observable layout behavior, not private helper implementation details.
- Tests should be easy to adjust when future section behavior intentionally changes.
- Layout tests should cover minimum canvas width behavior.
- Layout tests should cover input and output sections retaining fixed widths.
- Layout tests should cover the middle process/action section expanding with available canvas width.
- Layout tests should cover role-to-lane horizontal bounds.
- Layout tests should cover position snapping to the grid.
- Layout tests should cover size snapping to the grid and minimum dimensions.
- Layout tests should cover constraining a block inside its role lane.
- Layout tests should cover constraining all blocks in a model.
- Layout tests should cover default placement for input blocks.
- Layout tests should cover default placement for process/action blocks.
- Layout tests should cover default placement for output blocks.
- Build verification should remain a baseline check after the implementation.

## Out of Scope

- Alignment commands are out of scope.
- Multi-select is out of scope.
- Zoom controls and view transforms are out of scope.
- Toolbar redesign is out of scope.
- Inspector redesign is out of scope.
- Connection validation extraction is out of scope.
- Link routing extraction is out of scope.
- Link routing behavior changes are out of scope.
- Renaming the current `Action` role to `Function` is out of scope.
- Removing Bootstrap is out of scope.
- Persistence, export, import, save/load workflows, and backend storage are out of scope.
- Collaborative editing is out of scope.
- Freeform drawing tools, custom shapes, image import, or rich diagram decoration are out of scope.
- Broader application integration work is out of scope.

## Further Notes

- The future domain name for the current process/action concept should be `Function`. This should be handled later in one coordinated rename across model names, visible labels, CSS role classes, sample data, tests, and any documentation.
- Bootstrap should eventually be removed, but doing that now would turn this tranche into a styling rewrite.
- The eventual larger-app shape may include a logic class library, a Razor class library, and a test project. For now, the most straightforward approach is to keep the extracted module in the app project and structure it so it can be moved later.
- The old PRD was intentionally removed because it was too broad for a `ready-for-agent` implementation tranche.
