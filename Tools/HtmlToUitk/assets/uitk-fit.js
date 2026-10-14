(function () {
  'use strict';
  if (/[?&]fit=0/.test(location.search)) return;
  var DW = 1920, DH = 1080;
  var script = document.currentScript;
  if (script && script.getAttribute('data-width')) DW = parseInt(script.getAttribute('data-width'), 10);
  if (script && script.getAttribute('data-height')) DH = parseInt(script.getAttribute('data-height'), 10);
  function apply() {
    var s = Math.min(window.innerWidth / DW, window.innerHeight / DH);
    document.documentElement.style.overflow = 'hidden';
    document.documentElement.style.width = window.innerWidth + 'px';
    document.documentElement.style.height = window.innerHeight + 'px';
    document.body.style.width = DW + 'px';
    document.body.style.height = DH + 'px';
    document.body.style.transformOrigin = '0 0';
    document.body.style.transform = 'scale(' + s + ')';
    window.uitkScale = s;
  }
  window.addEventListener('resize', apply);
  if (document.body) apply(); else document.addEventListener('DOMContentLoaded', apply);
})();
