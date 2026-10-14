(function () {
  'use strict';
  var VERSION = '0.1.0';
  var script = document.currentScript;
  var HIDE = 'uitk-proto-hidden';
  var reduced = /[?&]reduced=1/.test(location.search) || (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  var overlayOn = !/[?&]flow=off/.test(location.search) && !(script && script.getAttribute('data-flow') === 'false');
  var DEFAULT_MS = 220;
  var TRANSITIONS = ['fade', 'slide-left', 'slide-right', 'slide-up', 'scale', 'none'];

  var pages = {};
  var order = [];
  var modals = {};
  var tabs = [];
  var state = { current: null, history: [], modalStack: [], busy: false, log: [] };
  var flow = null;

  function $all(sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); }
  function ms(el, fallback) { var v = parseInt(el && el.getAttribute('data-transition-ms'), 10); return isFinite(v) ? v : fallback; }
  function nameOf(el) { return el.getAttribute('name') || el.id || ''; }
  function isHarness(el) { return !!(el.closest && el.closest('[data-uitk-harness]')); }

  function interactiveIn(root) {
    var list = $all('button, input, select, textarea, [data-uxml="Button"], [tabindex]', root).filter(function (e) {
      return !isHarness(e) && !e.closest('.' + HIDE);
    });
    return list;
  }

  function firstFocusable(root) {
    var f = root.querySelector('[data-focus]');
    if (f) return f;
    var list = $all('button, input, select, textarea', root).filter(function (e) { return !isHarness(e); });
    return list[0] || null;
  }

  function injectStyle() {
    var st = document.createElement('style');
    st.setAttribute('data-uitk-harness', '');
    st.textContent = '.' + HIDE + '{display:none !important}' +
      '#uitk-flow{position:fixed;left:8px;bottom:8px;z-index:2147483646;width:340px;max-height:60vh;overflow:auto;background:#10161cf2;color:#e8eef2;font:12px/1.4 Consolas,monospace;border:1px solid #3a4a58;border-radius:6px;box-shadow:0 6px 24px #000a}' +
      '#uitk-flow,#uitk-flow .item{display:block;flex-shrink:0}#uitk-flow span,#uitk-flow b,#uitk-flow button,#uitk-flow a{display:inline-block;margin:0}#uitk-flow .item,#uitk-flow h4{display:block}#uitk-flow header{flex-direction:row;display:flex;align-items:center;gap:8px;padding:6px 8px;border-bottom:1px solid #2a3846;position:sticky;top:0;background:#10161c}' +
      '#uitk-flow h4{margin:8px 8px 2px;font-size:11px;text-transform:uppercase;color:#8a98a4;font-weight:bold}' +
      '#uitk-flow .item{padding:2px 8px}#uitk-flow .cur{color:#8ff0d4;font-weight:bold}#uitk-flow .start::after{content:" (start)";color:#8a98a4}' +
      '#uitk-flow button{background:#243442;color:#e8eef2;border:1px solid #3a4a58;border-radius:3px;padding:1px 6px;cursor:pointer;font:inherit}' +
      '#uitk-flow .edge{color:#9fd0ff}#uitk-flow .bad{color:#ff7b7b}';
    document.head.appendChild(st);
  }

  function collect() {
    $all('[data-page]').forEach(function (el) {
      var id = el.getAttribute('data-page');
      pages[id] = { id: id, el: el, start: el.hasAttribute('data-start'), transition: el.getAttribute('data-transition') || null, ms: ms(el, null), focus: nameOf(firstFocusable(el) || el) };
      order.push(id);
    });
    $all('[data-modal]').forEach(function (el) {
      var id = el.getAttribute('data-modal');
      var owner = el.closest('[data-page]');
      modals[id] = { id: id, el: el, owner: owner ? owner.getAttribute('data-page') : null, closeTriggers: $all('[data-close]', el).map(nameOf) };
    });
    $all('[data-tabs]').forEach(function (el) {
      var group = el.getAttribute('data-tabs');
      var page = el.closest('[data-page]');
      var list = $all('[data-tab]', el).map(function (t) { return { trigger: t, tab: nameOf(t), panelName: t.getAttribute('data-tab'), isDefault: t.hasAttribute('data-default') }; });
      list.forEach(function (t) { t.panel = document.querySelector('[name="' + t.panelName + '"], #' + CSS.escape(t.panelName)); });
      var def = list.filter(function (t) { return t.isDefault; })[0] || list[0];
      tabs.push({ group: group, page: page ? page.getAttribute('data-page') : null, tabs: list, def: def, el: el });
    });
  }

  function startPage() {
    for (var i = 0; i < order.length; i++) if (pages[order[i]].start) return order[i];
    return order[0] || null;
  }

  function hideAll() {
    order.forEach(function (id) { pages[id].el.classList.add(HIDE); });
    Object.keys(modals).forEach(function (id) { modals[id].el.classList.add(HIDE); });
    tabs.forEach(function (g) { selectTab(g, g.def, true); });
  }

  function selectTab(g, t, silent) {
    g.tabs.forEach(function (x) {
      var on = x === t;
      x.trigger.classList.toggle('is-selected', on);
      if (x.panel) x.panel.classList.toggle(HIDE, !on);
    });
    if (!silent) log('tab ' + g.group + ' -> ' + t.tab);
  }

  function log(msg) {
    state.log.push(msg);
    if (state.log.length > 30) state.log.shift();
    render();
    document.dispatchEvent(new CustomEvent('uitk:flow', { detail: { message: msg, state: snapshot() } }));
  }

  function snapshot() {
    return { current: state.current, history: state.history.slice(), modals: state.modalStack.slice() };
  }

  function dur(d) { return reduced ? 0 : (d == null ? DEFAULT_MS : d); }

  function transitionStyle(kind, phase, forward) {
    var dir = forward ? 1 : -1;
    var map = {
      'fade': phase === 'in' ? ['opacity:0', 'opacity:1'] : ['opacity:1', 'opacity:0'],
      'slide-left': phase === 'in' ? ['transform:translateX(' + (100 * dir) + '%)', 'transform:translateX(0)'] : ['transform:translateX(0)', 'transform:translateX(' + (-100 * dir) + '%)'],
      'slide-right': phase === 'in' ? ['transform:translateX(' + (-100 * dir) + '%)', 'transform:translateX(0)'] : ['transform:translateX(0)', 'transform:translateX(' + (100 * dir) + '%)'],
      'slide-up': phase === 'in' ? ['transform:translateY(' + (100 * dir) + '%)', 'transform:translateY(0)'] : ['transform:translateY(0)', 'transform:translateY(' + (-100 * dir) + '%)'],
      'scale': phase === 'in' ? ['opacity:0;transform:scale(.92)', 'opacity:1;transform:scale(1)'] : ['opacity:1;transform:scale(1)', 'opacity:0;transform:scale(1.04)']
    };
    return map[kind] || null;
  }

  function animate(el, kind, phase, duration, forward, done) {
    var pair = kind && kind !== 'none' && duration > 0 ? transitionStyle(kind, phase, forward) : null;
    if (!pair) { done(); return; }
    var prev = el.getAttribute('style') || '';
    el.style.transition = 'none';
    el.setAttribute('style', prev + ';' + pair[0]);
    void el.offsetWidth;
    el.style.transition = 'opacity ' + duration + 'ms ease-out, transform ' + duration + 'ms ease-out';
    el.setAttribute('style', el.getAttribute('style') + ';' + pair[1]);
    setTimeout(function () {
      if (prev) el.setAttribute('style', prev); else el.removeAttribute('style');
      done();
    }, duration + 20);
  }

  function showPage(id, kind, duration, forward, done) {
    var p = pages[id];
    p.el.classList.remove(HIDE);
    animate(p.el, kind, 'in', duration, forward, function () {
      var f = firstFocusable(p.el);
      if (f && f.focus) try { f.focus({ preventScroll: true }); } catch (e) { f.focus(); }
      if (done) done();
    });
  }

  function hidePage(id, kind, duration, forward, done) {
    var p = pages[id];
    animate(p.el, kind, 'out', duration, forward, function () { p.el.classList.add(HIDE); if (done) done(); });
  }

  function navigate(to, opts) {
    opts = opts || {};
    if (!pages[to]) { log('navigate: unknown page ' + to); return false; }
    if (state.busy || to === state.current) return false;
    closeAllModals();
    var from = state.current;
    var mode = opts.mode || 'push';
    var kind = opts.transition || pages[to].transition || 'fade';
    var d = dur(opts.ms != null ? opts.ms : (pages[to].ms != null ? pages[to].ms : null));
    state.busy = true;
    if (mode === 'show') state.history = []; else if (from) state.history.push(from);
    state.current = to;
    if (from) hidePage(from, kind, d, true);
    showPage(to, kind, d, true, function () { state.busy = false; });
    log((mode === 'push' ? 'push ' : 'show ') + (from || '-') + ' -> ' + to + ' [' + kind + ' ' + d + 'ms]');
    return true;
  }

  function closeAllModals() {
    while (state.modalStack.length) closeModal();
  }

  function openModal(id) {
    var m = modals[id];
    if (!m) { log('openModal: unknown modal ' + id); return false; }
    if (state.modalStack.indexOf(id) >= 0) return false;
    state.modalStack.push(id);
    m.el.classList.remove(HIDE);
    animate(m.el, 'fade', 'in', dur(150), true, function () {
      var f = firstFocusable(m.el);
      if (f && f.focus) try { f.focus({ preventScroll: true }); } catch (e) { f.focus(); }
    });
    log('openModal ' + id);
    return true;
  }

  function closeModal() {
    var id = state.modalStack.pop();
    if (!id) return false;
    var m = modals[id];
    animate(m.el, 'fade', 'out', dur(150), false, function () { m.el.classList.add(HIDE); });
    log('closeModal ' + id);
    return true;
  }

  function back() {
    if (state.busy) return false;
    if (state.modalStack.length) return closeModal();
    var prev = state.history.pop();
    if (!prev) { log('back: nothing to go back to'); return false; }
    var from = state.current;
    var kind = pages[from].transition || 'fade';
    var d = dur(pages[from].ms);
    state.busy = true;
    state.current = prev;
    hidePage(from, kind, d, false);
    showPage(prev, kind, d, false, function () { state.busy = false; });
    log('back ' + from + ' -> ' + prev);
    return true;
  }

  function wire() {
    document.addEventListener('click', function (ev) {
      var t = ev.target.closest ? ev.target.closest('[data-nav], [data-back], [data-open-modal], [data-close], [data-tab]') : null;
      if (!t || isHarness(t)) return;
      if (t.hasAttribute('data-nav')) {
        var tr = t.getAttribute('data-transition');
        if (tr && TRANSITIONS.indexOf(tr) < 0) { log('unknown transition ' + tr); tr = null; }
        navigate(t.getAttribute('data-nav'), { mode: t.getAttribute('data-nav-mode') || 'push', transition: tr, ms: ms(t, null) });
      } else if (t.hasAttribute('data-back')) {
        back();
      } else if (t.hasAttribute('data-open-modal')) {
        openModal(t.getAttribute('data-open-modal'));
      } else if (t.hasAttribute('data-close')) {
        closeModal();
      } else if (t.hasAttribute('data-tab')) {
        tabs.forEach(function (g) {
          g.tabs.forEach(function (x) { if (x.trigger === t || x.trigger.contains(t)) selectTab(g, x); });
        });
      }
    });
    document.addEventListener('keydown', function (ev) {
      if (ev.key === 'Escape') back();
      if ((ev.key === 'f' || ev.key === 'F') && ev.altKey) { overlayOn = !overlayOn; render(); }
    });
  }

  function edgesModel() {
    var edges = [];
    $all('[data-nav], [data-back], [data-open-modal]').forEach(function (t) {
      if (isHarness(t)) return;
      var page = t.closest('[data-page]');
      var from = page ? page.getAttribute('data-page') : null;
      if (t.hasAttribute('data-nav')) {
        var e = { from: from, trigger: nameOf(t), action: 'navigate', to: t.getAttribute('data-nav'), mode: t.getAttribute('data-nav-mode') || 'push' };
        if (t.getAttribute('data-transition')) e.transition = t.getAttribute('data-transition');
        if (t.getAttribute('data-transition-ms')) e.ms = parseInt(t.getAttribute('data-transition-ms'), 10);
        edges.push(e);
      } else if (t.hasAttribute('data-back')) {
        edges.push({ from: from, trigger: nameOf(t), action: 'back' });
      } else {
        edges.push({ from: from, trigger: nameOf(t), action: 'openModal', to: t.getAttribute('data-open-modal') });
      }
    });
    return edges;
  }

  function navModel() {
    var model = { schema: 1, document: document.title || 'Page', start: startPage(), pages: [], edges: edgesModel(), modals: [], tabs: [] };
    order.forEach(function (id) {
      var p = pages[id];
      var o = { id: id, element: nameOf(p.el), focus: p.focus };
      if (p.transition || p.ms != null) { o.transition = {}; if (p.transition) { o.transition.in = p.transition; o.transition.out = p.transition; } if (p.ms != null) o.transition.ms = p.ms; }
      model.pages.push(o);
    });
    Object.keys(modals).forEach(function (id) { var m = modals[id]; model.modals.push({ id: id, element: nameOf(m.el), ownerPage: m.owner, closeTriggers: m.closeTriggers }); });
    tabs.forEach(function (g) { model.tabs.push({ group: g.group, page: g.page, default: g.def ? g.def.tab : null, tabs: g.tabs.map(function (t) { return { tab: t.tab, panel: t.panelName }; }) }); });
    return model;
  }

  function validate() {
    var problems = [];
    var starts = order.filter(function (id) { return pages[id].start; });
    if (order.length && starts.length === 0) problems.push('P001 no data-start page');
    if (starts.length > 1) problems.push('P002 more than one data-start page');
    edgesModel().forEach(function (e) {
      if (e.action === 'navigate' && !pages[e.to]) problems.push('P003 data-nav to unknown page "' + e.to + '" (' + e.trigger + ')');
      if (e.action === 'openModal' && !modals[e.to]) problems.push('P003 data-open-modal to unknown modal "' + e.to + '" (' + e.trigger + ')');
      if (e.transition && TRANSITIONS.indexOf(e.transition) < 0) problems.push('P009 unknown transition "' + e.transition + '"');
    });
    order.forEach(function (id) { var t = pages[id].transition; if (t && TRANSITIONS.indexOf(t) < 0) problems.push('P009 unknown transition "' + t + '" on page ' + id); });
    return problems;
  }

  function render() {
    if (!overlayOn) { if (flow) flow.style.display = 'none'; return; }
    if (!flow) {
      flow = document.createElement('div');
      flow.id = 'uitk-flow';
      flow.setAttribute('data-uitk-harness', '');
      document.documentElement.appendChild(flow);
      flow.addEventListener('click', function (ev) {
        var go = ev.target.getAttribute && ev.target.getAttribute('data-go');
        if (go) navigate(go, { mode: 'show', transition: 'fade' });
        if (ev.target.getAttribute && ev.target.getAttribute('data-act') === 'back') back();
      });
    }
    flow.style.display = '';
    var h = '<header><b>FLOW</b><span>' + (state.current || '-') + '</span><span style="flex:1"></span><button data-act="back">back</button></header>';
    h += '<h4>Pages</h4>';
    order.forEach(function (id) {
      h += '<div class="item' + (pages[id].start ? ' start' : '') + (id === state.current ? ' cur' : '') + '"><a data-go="' + id + '" style="cursor:pointer">' + id + '</a></div>';
    });
    h += '<h4>History</h4><div class="item">' + (state.history.length ? state.history.join(' &gt; ') : '(empty)') + '</div>';
    h += '<h4>Modals</h4><div class="item">' + (state.modalStack.length ? state.modalStack.join(', ') : '(none)') + '</div>';
    h += '<h4>Edges</h4>';
    edgesModel().forEach(function (e) {
      h += '<div class="item edge">' + (e.from || '*') + ' --' + e.action + (e.to ? ' ' + e.to : '') + (e.transition ? ' [' + e.transition + ']' : '') + '</div>';
    });
    var problems = validate();
    if (problems.length) { h += '<h4>Problems</h4>'; problems.forEach(function (p) { h += '<div class="item bad">' + p + '</div>'; }); }
    h += '<h4>Log</h4>';
    state.log.slice(-6).forEach(function (m) { h += '<div class="item">' + m + '</div>'; });
    flow.innerHTML = h;
  }

  function init() {
    injectStyle();
    collect();
    hideAll();
    wire();
    var s = startPage();
    if (s) { state.current = s; pages[s].el.classList.remove(HIDE); var f = firstFocusable(pages[s].el); if (f && f.focus) try { f.focus({ preventScroll: true }); } catch (e) {} }
    render();
    window.uitkProto = {
      version: VERSION,
      navigate: navigate,
      back: back,
      openModal: openModal,
      closeModal: closeModal,
      selectTab: function (group, tab) { tabs.forEach(function (g) { if (g.group === group) g.tabs.forEach(function (t) { if (t.tab === tab) selectTab(g, t); }); }); },
      state: snapshot,
      navModel: navModel,
      validate: validate,
      pages: function () { return order.slice(); }
    };
    document.dispatchEvent(new CustomEvent('uitk:ready'));
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
