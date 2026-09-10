mergeInto(LibraryManager.library, {
  ReportState: function(ptr) {
    window.dispatchEvent(new CustomEvent('unity-state', { detail: JSON.parse(UTF8ToString(ptr)) }));
  }
});
