(() => {
  let mermaidLoadPromise;

  function initialize(mermaid) {
    mermaid.initialize({
      startOnLoad: false,
      theme: "default",
      securityLevel: "loose"
    });
    window.__mermaid = mermaid;
    return mermaid;
  }

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const existing = document.querySelector(`script[src="${src}"]`);
      if (existing) {
        if (window.mermaid) {
          resolve();
          return;
        }
        existing.addEventListener("load", () => resolve(), { once: true });
        existing.addEventListener("error", () => reject(new Error(`Failed to load script: ${src}`)), { once: true });
        return;
      }

      const script = document.createElement("script");
      script.src = src;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error(`Failed to load script: ${src}`));
      document.head.appendChild(script);
    });
  }

  async function getMermaid() {
    if (window.__mermaid) {
      return window.__mermaid;
    }

    if (!mermaidLoadPromise) {
      mermaidLoadPromise = (async () => {
        try {
          const module = await import("https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs");
          return initialize(module.default ?? module);
        } catch {
          await loadScript("https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js");
          if (!window.mermaid) {
            throw new Error("Mermaid failed to initialize.");
          }
          return initialize(window.mermaid);
        }
      })();
    }

    return mermaidLoadPromise;
  }

  window.mermaidInterop = {
    async render(elementId, definition) {
      const target = document.getElementById(elementId);
      if (!target) {
        throw new Error(`Mermaid target not found: ${elementId}`);
      }

      const mermaid = await getMermaid();
      const diagramId = `m_${crypto.randomUUID().replace(/-/g, "")}`;
      const { svg } = await mermaid.render(diagramId, definition);
      target.innerHTML = svg;
    }
  };
})();
