(() => {
  const sessions = new Map();
  let listenerRegistered = false;

  function post(iframeId, message) {
    const iframe = document.getElementById(iframeId);
    if (!iframe || !iframe.contentWindow) {
      throw new Error(`draw.io iframe not found: ${iframeId}`);
    }

    iframe.contentWindow.postMessage(JSON.stringify(message), "*");
  }

  function onMessage(event) {
    let data = event.data;
    if (!data) {
      return;
    }

    if (typeof data === "string") {
      try {
        data = JSON.parse(data);
      } catch {
        return;
      }
    }

    if (!data || typeof data !== "object") {
      return;
    }

    for (const [iframeId, session] of sessions.entries()) {
      const iframe = document.getElementById(iframeId);
      if (!iframe || iframe.contentWindow !== event.source) {
        continue;
      }

      handleEvent(iframeId, session, data);
      break;
    }
  }

  function handleEvent(iframeId, session, data) {
    switch (data.event) {
      case "init":
        post(iframeId, {
          action: "load",
          autosave: 1,
          xml: session.xml
        });
        break;
      case "autosave":
        if (typeof data.xml === "string") {
          session.xml = data.xml;
          session.dotnet.invokeMethodAsync("OnAutosave", data.xml);
        }
        break;
      case "save":
        if (typeof data.xml === "string") {
          session.xml = data.xml;
          session.dotnet.invokeMethodAsync("OnSave", data.xml);
        }
        post(iframeId, { action: "status", message: { key: "allChangesSaved", modified: false } });
        break;
      case "export":
        if (typeof data.data === "string") {
          session.dotnet.invokeMethodAsync("OnExport", data.data);
        }
        break;
      case "exit":
        session.dotnet.invokeMethodAsync("OnExit");
        break;
      default:
        break;
    }
  }

  window.drawioInterop = {
    initialize(iframeId, dotnetRef, initialXml) {
      if (!listenerRegistered) {
        window.addEventListener("message", onMessage);
        listenerRegistered = true;
      }

      sessions.set(iframeId, {
        dotnet: dotnetRef,
        xml: initialXml
      });
    },

    setXml(iframeId, xml) {
      const session = sessions.get(iframeId);
      if (!session) {
        throw new Error(`draw.io session not found: ${iframeId}`);
      }

      session.xml = xml;
      post(iframeId, {
        action: "load",
        autosave: 1,
        xml
      });
    },

    requestSave(iframeId) {
      post(iframeId, { action: "save", exit: false });
    },

    requestExportPng(iframeId) {
      post(iframeId, {
        action: "export",
        format: "png",
        spin: "Exporting..."
      });
    },

    dispose(iframeId) {
      sessions.delete(iframeId);
    }
  };
})();
