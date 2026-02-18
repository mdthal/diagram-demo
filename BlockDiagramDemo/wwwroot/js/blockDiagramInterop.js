(() => {
  window.blockDiagramInterop = {
    getWidth(element) {
      if (!element) {
        return 0;
      }

      return Number(element.clientWidth || 0);
    }
  };
})();
