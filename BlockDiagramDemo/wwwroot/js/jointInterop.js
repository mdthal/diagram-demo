(() => {
  const instances = new Map();

  const PAPER_HEIGHT = 600;
  const GRID_SIZE = 16;
  const CANVAS_MIN_WIDTH = 960;
  const LANE_OUTER_PADDING = 24;
  const LANE_INNER_PADDING = 14;
  const INPUT_LANE_WIDTH = 176;
  const OUTPUT_LANE_WIDTH = 176;
  const MIN_ACTION_LANE_WIDTH = 320;
  const BLOCK_WIDTH = 128;
  const BLOCK_HEIGHT = 56;
  const BLOCK_ROW_STEP = 88;
  const ACTION_COLUMNS = 2;

  const SAMPLE_BLOCKS = [
    { id: "input_flour", label: "Flour", role: "input", x: 48, y: 80 },
    { id: "input_water", label: "Water", role: "input", x: 48, y: 208 },
    { id: "action_meter_flour", label: "Meter", role: "action", x: 224, y: 80 },
    { id: "action_meter_water", label: "Meter", role: "action", x: 224, y: 208 },
    { id: "action_mix", label: "Mix", role: "action", x: 360, y: 144 },
    { id: "action_divide", label: "Divide", role: "action", x: 488, y: 144 },
    { id: "action_bake", label: "Bake", role: "action", x: 600, y: 144 },
    { id: "output_accept", label: "Tortillas", role: "output", x: 792, y: 104 }
  ];

  const SAMPLE_LINKS = [
    { sourceId: "input_flour", targetId: "action_meter_flour" },
    { sourceId: "input_water", targetId: "action_meter_water" },
    { sourceId: "action_meter_flour", targetId: "action_mix" },
    { sourceId: "action_meter_water", targetId: "action_mix" },
    { sourceId: "action_mix", targetId: "action_divide" },
    { sourceId: "action_divide", targetId: "action_bake" },
    { sourceId: "action_bake", targetId: "output_accept" }
  ];

  const ROLE_STYLE = {
    input: { fill: "#e8faee", stroke: "#2f855a" },
    action: { fill: "#eaf2ff", stroke: "#3b6ea8" },
    output: { fill: "#fff2e3", stroke: "#b56b21" }
  };

  function normalizeRole(role) {
    const value = (role || "").toString().toLowerCase();
    if (value === "input" || value === "action" || value === "output") {
      return value;
    }

    return "action";
  }

  function titleCaseRole(role) {
    const safeRole = normalizeRole(role);
    return safeRole.charAt(0).toUpperCase() + safeRole.slice(1);
  }

  function getRoleStyle(role) {
    return ROLE_STYLE[normalizeRole(role)] || ROLE_STYLE.action;
  }

  function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
  }

  function getCellRole(cell) {
    if (!cell || cell.isLink()) {
      return "action";
    }

    return normalizeRole(cell.get("blockRole"));
  }

  function getPaperWidth(instance) {
    const width = Number(instance.paper.options.width || 0);
    if (Number.isFinite(width) && width > 0) {
      return width;
    }

    return Math.max(CANVAS_MIN_WIDTH, Math.floor(instance.paper.el.clientWidth || 0));
  }

  function getPaperHeight(instance) {
    const height = Number(instance.paper.options.height || 0);
    return Number.isFinite(height) && height > 0 ? height : PAPER_HEIGHT;
  }

  function resolveLaneLayout(width) {
    const safeWidth = Math.max(CANVAS_MIN_WIDTH, Math.floor(width));
    const inputStart = LANE_OUTER_PADDING;
    const actionStart = inputStart + INPUT_LANE_WIDTH;
    const outputEnd = safeWidth - LANE_OUTER_PADDING;
    const minOutputStart = actionStart + MIN_ACTION_LANE_WIDTH;
    const outputStart = Math.max(minOutputStart, outputEnd - OUTPUT_LANE_WIDTH);

    return {
      input: {
        start: inputStart,
        end: actionStart,
        minX: inputStart + LANE_INNER_PADDING,
        maxX: actionStart - LANE_INNER_PADDING
      },
      action: {
        start: actionStart,
        end: outputStart,
        minX: actionStart + LANE_INNER_PADDING,
        maxX: outputStart - LANE_INNER_PADDING
      },
      output: {
        start: outputStart,
        end: outputStart + OUTPUT_LANE_WIDTH,
        minX: outputStart + LANE_INNER_PADDING,
        maxX: outputStart + OUTPUT_LANE_WIDTH - LANE_INNER_PADDING
      }
    };
  }

  function getRoleLane(layout, role) {
    return layout[normalizeRole(role)] || layout.action;
  }

  function createEntity(joint, graph, label, role, x, y, id) {
    const safeRole = normalizeRole(role);
    const style = getRoleStyle(safeRole);
    const shape = new joint.shapes.standard.Rectangle();
    if (id) {
      shape.set("id", id);
    }
    shape.position(x, y);
    shape.resize(BLOCK_WIDTH, BLOCK_HEIGHT);
    shape.set("blockRole", safeRole);
    shape.attr({
      body: {
        fill: style.fill,
        stroke: style.stroke,
        strokeWidth: 2,
        rx: 10,
        ry: 10
      },
      label: {
        text: label,
        fill: "#0f172a",
        fontSize: 12,
        fontWeight: "600"
      }
    });
    shape.addTo(graph);
    return shape;
  }

  function createLink(joint, graph, source, target) {
    const link = new joint.shapes.standard.Link();
    link.source(source);
    link.target(target);
    link.attr({
      line: {
        stroke: "#334155",
        strokeWidth: 2,
        targetMarker: {
          type: "path",
          d: "M 10 -5 0 0 10 5 z"
        }
      }
    });
    link.addTo(graph);
    return link;
  }

  function loadSample(instance) {
    const { graph } = instance;
    graph.clear();
    const elementsById = new Map();

    SAMPLE_BLOCKS.forEach((block) => {
      const element = createEntity(
        instance.joint,
        graph,
        block.label,
        block.role,
        block.x,
        block.y,
        block.id
      );
      elementsById.set(block.id, element);
    });

    SAMPLE_LINKS.forEach((link) => {
      const source = elementsById.get(link.sourceId);
      const target = elementsById.get(link.targetId);
      if (!source || !target) {
        return;
      }

      createLink(instance.joint, graph, source, target);
    });

    clampAllBlocksToLanes(instance);
  }

  function createPaper(joint, element, graph, onResize) {
    const resolveWidth = () => {
      const rect = element.getBoundingClientRect();
      return Math.max(CANVAS_MIN_WIDTH, Math.floor(rect.width || element.clientWidth || 0));
    };

    const paper = new joint.dia.Paper({
      el: element,
      model: graph,
      cellViewNamespace: joint.shapes,
      width: resolveWidth(),
      height: PAPER_HEIGHT,
      gridSize: GRID_SIZE,
      drawGrid: { name: "mesh" },
      background: { color: "#f8fafc" },
      defaultConnector: { name: "rounded" },
      defaultRouter: { name: "manhattan" }
    });

    const resize = () => {
      paper.setDimensions(resolveWidth(), PAPER_HEIGHT);
      if (onResize) {
        onResize();
      }
    };

    const observer = new ResizeObserver((entries) => {
      if (entries[0]) {
        resize();
      }
    });

    requestAnimationFrame(resize);
    const resizeTimeoutId = window.setTimeout(resize, 80);
    window.addEventListener("resize", resize);

    observer.observe(element);
    return { paper, observer, resize, resizeTimeoutId };
  }

  function clampElementToLane(instance, element) {
    if (!element || element.isLink()) {
      return false;
    }

    const role = getCellRole(element);
    const layout = resolveLaneLayout(getPaperWidth(instance));
    const lane = getRoleLane(layout, role);
    const current = element.position();
    const minX = lane.minX;
    const maxX = Math.max(minX, lane.maxX - BLOCK_WIDTH);
    const minY = LANE_OUTER_PADDING;
    const maxY = Math.max(minY, getPaperHeight(instance) - BLOCK_HEIGHT - LANE_OUTER_PADDING);
    const nextX = clamp(current.x, minX, maxX);
    const nextY = clamp(current.y, minY, maxY);
    const size = element.size();
    const shouldResize = size.width !== BLOCK_WIDTH || size.height !== BLOCK_HEIGHT;
    const shouldMove = Math.abs(current.x - nextX) > 0.1 || Math.abs(current.y - nextY) > 0.1;

    if (shouldResize) {
      element.resize(BLOCK_WIDTH, BLOCK_HEIGHT, { laneClamp: true });
    }

    if (shouldMove) {
      element.position(nextX, nextY, { laneClamp: true });
    }

    return shouldResize || shouldMove;
  }

  function clampAllBlocksToLanes(instance) {
    let changed = false;
    instance.graph.getElements().forEach((element) => {
      if (clampElementToLane(instance, element)) {
        changed = true;
      }
    });

    return changed;
  }

  function resetCellStyle(cell) {
    if (!cell) {
      return;
    }

    if (cell.isLink()) {
      cell.attr("line/stroke", "#334155");
      cell.attr("line/strokeWidth", 2);
      return;
    }

    const role = getCellRole(cell);
    const style = getRoleStyle(role);
    cell.attr("body/stroke", style.stroke);
    cell.attr("body/strokeWidth", 2);
  }

  function applySelectedStyle(cell) {
    if (!cell) {
      return;
    }

    if (cell.isLink()) {
      cell.attr("line/stroke", "#0284c7");
      cell.attr("line/strokeWidth", 3);
      return;
    }

    cell.attr("body/stroke", "#0284c7");
    cell.attr("body/strokeWidth", 3);
  }

  function selectCell(instance, cellId) {
    if (instance.selectedId === cellId) {
      return;
    }

    const previous = instance.selectedId ? instance.graph.getCell(instance.selectedId) : null;
    resetCellStyle(previous);

    instance.selectedId = cellId || null;
    const next = cellId ? instance.graph.getCell(cellId) : null;
    applySelectedStyle(next);
  }

  function getCellLabel(cell) {
    if (!cell) {
      return "";
    }

    if (cell.isLink()) {
      return cell.label(0)?.attrs?.text?.text ?? "";
    }

    return cell.attr("label/text") || "";
  }

  function setCellLabel(cell, labelText) {
    if (!cell) {
      return;
    }

    if (cell.isLink()) {
      cell.labels([
        {
          attrs: {
            text: {
              text: labelText,
              fontSize: 12,
              fill: "#334155"
            }
          }
        }
      ]);
      return;
    }

    cell.attr("label/text", labelText);
  }

  function emitGraphChanged(instance) {
    if (!instance.dotnetRef) {
      return;
    }

    const json = JSON.stringify(instance.graph.toJSON(), null, 2);
    instance.dotnetRef.invokeMethodAsync("OnGraphChanged", json);
  }

  function queueGraphChanged(instance) {
    if (!instance.dotnetRef) {
      return;
    }

    window.clearTimeout(instance.changeTimeoutId);
    instance.changeTimeoutId = window.setTimeout(() => emitGraphChanged(instance), 120);
  }

  function allowsRoleTransition(sourceRole, targetRole) {
    if (sourceRole === "input" && targetRole === "action") {
      return true;
    }

    if (sourceRole === "action" && (targetRole === "action" || targetRole === "output")) {
      return true;
    }

    return false;
  }

  function isForwardConnection(source, target) {
    const sourceCenter = source.getBBox().center();
    const targetCenter = target.getBBox().center();
    return targetCenter.x > sourceCenter.x + 4;
  }

  function hasExistingConnection(instance, sourceId, targetId) {
    return instance.graph.getLinks().some((link) => {
      const source = link.get("source")?.id;
      const target = link.get("target")?.id;
      return source === sourceId && target === targetId;
    });
  }

  function createsCycle(instance, sourceId, targetId) {
    const visited = new Set();
    const stack = [targetId];

    while (stack.length > 0) {
      const current = stack.pop();
      if (visited.has(current)) {
        continue;
      }

      visited.add(current);
      if (current === sourceId) {
        return true;
      }

      instance.graph.getLinks().forEach((link) => {
        const source = link.get("source")?.id;
        const target = link.get("target")?.id;
        if (source === current && target) {
          stack.push(target);
        }
      });
    }

    return false;
  }

  function validateConnection(instance, source, target) {
    if (!source || !target) {
      return { ok: false, message: "Invalid selection." };
    }

    if (source.id === target.id) {
      return { ok: false, message: "Cannot connect a block to itself." };
    }

    const sourceRole = getCellRole(source);
    const targetRole = getCellRole(target);

    if (!allowsRoleTransition(sourceRole, targetRole)) {
      return { ok: false, message: "Allowed routes: Input -> Action, Action -> Action, Action -> Output." };
    }

    if (!isForwardConnection(source, target)) {
      return { ok: false, message: "Connections must move left to right." };
    }

    if (hasExistingConnection(instance, source.id, target.id)) {
      return { ok: false, message: "Connection already exists between those blocks." };
    }

    if (createsCycle(instance, source.id, target.id)) {
      return { ok: false, message: "Connection would create a cycle." };
    }

    return { ok: true, message: "" };
  }

  function getNextBlockPosition(instance, role) {
    const safeRole = normalizeRole(role);
    const count = instance.graph
      .getElements()
      .filter((element) => getCellRole(element) === safeRole)
      .length;

    const columns = safeRole === "action" ? ACTION_COLUMNS : 1;
    const row = Math.floor(count / columns);
    const column = count % columns;
    const layout = resolveLaneLayout(getPaperWidth(instance));
    const lane = getRoleLane(layout, safeRole);
    const xCandidate = lane.minX + (column * (BLOCK_WIDTH + GRID_SIZE));
    const x = clamp(xCandidate, lane.minX, Math.max(lane.minX, lane.maxX - BLOCK_WIDTH));
    const y = clamp(
      60 + (row * BLOCK_ROW_STEP),
      LANE_OUTER_PADDING,
      getPaperHeight(instance) - BLOCK_HEIGHT - LANE_OUTER_PADDING
    );

    return { x, y };
  }

  function defaultLabelForRole(instance, role) {
    const safeRole = normalizeRole(role);
    const count = instance.graph
      .getElements()
      .filter((element) => getCellRole(element) === safeRole)
      .length + 1;

    return `${titleCaseRole(safeRole)} ${count}`;
  }

  function addRoleBlock(instance, role, label) {
    const safeRole = normalizeRole(role);
    const safeLabel = (label || "").trim() || defaultLabelForRole(instance, safeRole);
    const { x, y } = getNextBlockPosition(instance, safeRole);
    const block = createEntity(instance.joint, instance.graph, safeLabel, safeRole, x, y);
    clampElementToLane(instance, block);
    selectCell(instance, block.id);
    emitGraphChanged(instance);
    return `Added ${safeRole} block "${safeLabel}"`;
  }

  function bindInteractions(instance) {
    const { paper, graph, joint } = instance;

    paper.on("blank:pointerdown", () => {
      selectCell(instance, null);
    });

    paper.on("element:pointerclick", (elementView) => {
      const element = elementView.model;

      if (instance.mode === "connect") {
        if (!instance.connectSourceId) {
          const sourceRole = getCellRole(element);
          if (sourceRole === "output") {
            window.alert("Output blocks cannot start connections.");
            return;
          }

          instance.connectSourceId = element.id;
          selectCell(instance, element.id);
          return;
        }

        if (instance.connectSourceId !== element.id) {
          const source = graph.getCell(instance.connectSourceId);
          const result = validateConnection(instance, source, element);
          if (!result.ok) {
            window.alert(result.message);
            selectCell(instance, source?.id || null);
            return;
          }

          createLink(joint, graph, source, element);
          instance.connectSourceId = null;
          instance.mode = "select";
          selectCell(instance, element.id);
          return;
        }
      }

      selectCell(instance, element.id);
    });

    paper.on("link:pointerclick", (linkView) => {
      selectCell(instance, linkView.model.id);
    });

    paper.on("element:pointerdblclick", (elementView) => {
      const current = getCellLabel(elementView.model);
      const next = window.prompt("Rename block", current);
      if (next === null) {
        return;
      }

      setCellLabel(elementView.model, next.trim() || "Block");
    });

    paper.on("link:pointerdblclick", (linkView) => {
      const current = getCellLabel(linkView.model);
      const next = window.prompt("Rename connection", current);
      if (next === null) {
        return;
      }

      setCellLabel(linkView.model, next.trim());
    });

    graph.on("change:position", (cell, _position, opt) => {
      if (cell && !cell.isLink() && !opt?.laneClamp) {
        clampElementToLane(instance, cell);
      }

      queueGraphChanged(instance);
    });

    graph.on("add", (cell) => {
      if (cell && !cell.isLink()) {
        clampElementToLane(instance, cell);
      }

      queueGraphChanged(instance);
    });

    graph.on("remove change:size change:attrs change:source change:target", () => {
      queueGraphChanged(instance);
    });
  }

  function createInstance(joint, element, exampleKey, dotnetRef) {
    const graph = new joint.dia.Graph({}, { cellNamespace: joint.shapes });
    let instance = null;

    const { paper, observer, resize, resizeTimeoutId } = createPaper(joint, element, graph, () => {
      if (!instance) {
        return;
      }

      const changed = clampAllBlocksToLanes(instance);
      if (changed) {
        queueGraphChanged(instance);
      }
    });

    instance = {
      joint,
      graph,
      paper,
      observer,
      resize,
      resizeTimeoutId,
      dotnetRef: dotnetRef || null,
      selectedId: null,
      mode: "select",
      connectSourceId: null,
      changeTimeoutId: null
    };

    bindInteractions(instance);
    loadSample(instance);
    emitGraphChanged(instance);
    return instance;
  }

  function disposeInstance(elementId) {
    const instance = instances.get(elementId);
    if (!instance) {
      return;
    }

    window.removeEventListener("resize", instance.resize);
    window.clearTimeout(instance.resizeTimeoutId);
    window.clearTimeout(instance.changeTimeoutId);
    instance.observer.disconnect();
    instance.paper.remove();
    instance.graph.clear();
    instances.delete(elementId);
  }

  function getInstance(elementId) {
    const instance = instances.get(elementId);
    if (!instance) {
      throw new Error(`JointJS instance not found: ${elementId}`);
    }

    return instance;
  }

  window.jointInterop = {
    initialize(elementId, exampleKey, dotnetRef) {
      const element = document.getElementById(elementId);
      if (!element) {
        throw new Error(`JointJS target not found: ${elementId}`);
      }

      if (!window.joint) {
        throw new Error("JointJS failed to initialize.");
      }

      disposeInstance(elementId);
      const instance = createInstance(window.joint, element, exampleKey, dotnetRef);
      instances.set(elementId, instance);
    },

    loadExample(elementId, exampleKey) {
      const instance = getInstance(elementId);
      selectCell(instance, null);
      instance.mode = "select";
      instance.connectSourceId = null;
      loadSample(instance);
      emitGraphChanged(instance);
    },

    addBlock(elementId, label) {
      const instance = getInstance(elementId);
      return addRoleBlock(instance, "action", label);
    },

    addRoleBlock(elementId, role, label) {
      const instance = getInstance(elementId);
      return addRoleBlock(instance, role, label);
    },

    beginConnectMode(elementId) {
      const instance = getInstance(elementId);
      instance.mode = "connect";
      instance.connectSourceId = null;
      return "Connect mode: click source then target. Allowed: Input -> Action, Action -> Action, Action -> Output.";
    },

    cancelMode(elementId) {
      const instance = getInstance(elementId);
      instance.mode = "select";
      instance.connectSourceId = null;
      return "Mode reset to selection.";
    },

    deleteSelected(elementId) {
      const instance = getInstance(elementId);
      if (!instance.selectedId) {
        return false;
      }

      const cell = instance.graph.getCell(instance.selectedId);
      if (!cell) {
        instance.selectedId = null;
        return false;
      }

      cell.remove();
      instance.selectedId = null;
      emitGraphChanged(instance);
      return true;
    },

    renameSelected(elementId, newLabel) {
      const instance = getInstance(elementId);
      if (!instance.selectedId) {
        return false;
      }

      const cell = instance.graph.getCell(instance.selectedId);
      if (!cell) {
        return false;
      }

      let label = newLabel;
      if (label === null || label === undefined || label.trim().length === 0) {
        const current = getCellLabel(cell);
        const response = window.prompt(cell.isLink() ? "Rename connection" : "Rename block", current);
        if (response === null) {
          return false;
        }
        label = response;
      }

      setCellLabel(cell, label.trim() || (cell.isLink() ? "" : "Block"));
      emitGraphChanged(instance);
      return true;
    },

    getJson(elementId) {
      const instance = getInstance(elementId);
      return JSON.stringify(instance.graph.toJSON(), null, 2);
    },

    dispose(elementId) {
      disposeInstance(elementId);
    }
  };
})();
