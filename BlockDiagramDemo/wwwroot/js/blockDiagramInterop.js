(() => {
  const observers = new WeakMap();
  const deleteShortcuts = new WeakMap();

  const isEditableTarget = (target) => {
    if (!target) {
      return false;
    }

    const editable = target.closest?.("input, textarea, select, [contenteditable='true']");
    return Boolean(editable);
  };

  const getWidth = (element) => {
    if (!element) {
      return 0;
    }

    const rectWidth = element.getBoundingClientRect?.().width || 0;
    return Number(rectWidth || element.clientWidth || 0);
  };

  const notifyWidthChanged = (element, dotNetReference) => {
    const width = getWidth(element);

    if (width > 0) {
      dotNetReference.invokeMethodAsync("HandleCanvasResize", width);
    }
  };

  window.blockDiagramInterop = {
    getWidth(element) {
      return getWidth(element);
    },

    observeCanvas(element, dotNetReference) {
      this.unobserveCanvas(element);

      if (!element || !dotNetReference) {
        return;
      }

      if (!window.ResizeObserver) {
        notifyWidthChanged(element, dotNetReference);
        return;
      }

      const entry = {
        animationFrame: 0,
        observer: new ResizeObserver(() => {
          cancelAnimationFrame(entry.animationFrame);
          entry.animationFrame = requestAnimationFrame(() => {
            notifyWidthChanged(element, dotNetReference);
          });
        })
      };

      observers.set(element, entry);
      entry.observer.observe(element);
      notifyWidthChanged(element, dotNetReference);
    },

    unobserveCanvas(element) {
      const entry = observers.get(element);

      if (!entry) {
        return;
      }

      cancelAnimationFrame(entry.animationFrame);
      entry.observer.disconnect();
      observers.delete(element);
    },

    registerDeleteShortcut(element, dotNetReference) {
      this.unregisterDeleteShortcut(element);

      if (!element || !dotNetReference) {
        return;
      }

      const handleKeyDown = (event) => {
        if (event.defaultPrevented || isEditableTarget(event.target)) {
          return;
        }

        if (event.key !== "Delete" && event.key !== "Backspace") {
          return;
        }

        event.preventDefault();
        dotNetReference.invokeMethodAsync("HandleDeleteShortcut");
      };

      document.addEventListener("keydown", handleKeyDown);
      deleteShortcuts.set(element, handleKeyDown);
    },

    unregisterDeleteShortcut(element) {
      const handleKeyDown = deleteShortcuts.get(element);

      if (!handleKeyDown) {
        return;
      }

      document.removeEventListener("keydown", handleKeyDown);
      deleteShortcuts.delete(element);
    }
  };
})();
