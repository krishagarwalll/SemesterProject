mergeInto(LibraryManager.library, {
  HTGH_SetFullscreen: function (enabled) {
    var canvas = Module["canvas"];
    if (!canvas) return;

    if (enabled) {
      var request = canvas.requestFullscreen ||
        canvas.webkitRequestFullscreen ||
        canvas.mozRequestFullScreen ||
        canvas.msRequestFullscreen;
      if (request) {
        var result = request.call(canvas);
        if (result && result.catch) result.catch(function () {});
      }
      return;
    }

    var exit = document.exitFullscreen ||
      document.webkitExitFullscreen ||
      document.mozCancelFullScreen ||
      document.msExitFullscreen;
    if (exit) {
      var result = exit.call(document);
      if (result && result.catch) result.catch(function () {});
    }
  },

  HTGH_IsFullscreen: function () {
    return document.fullscreenElement ||
      document.webkitFullscreenElement ||
      document.mozFullScreenElement ||
      document.msFullscreenElement ? 1 : 0;
  }
});
