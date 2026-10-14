(function () {
  'use strict';
  var VERSION = '0.1.0';
  var script = document.currentScript;
  var cfg = {
    minFont: num(script && script.getAttribute('data-min-font'), 13),
    minTouch: num(script && script.getAttribute('data-min-touch'), 40),
    auto: !(script && script.getAttribute('data-auto') === 'false'),
    overlay: !/[?&]uitk=off/.test(location.search) && !(script && script.getAttribute('data-overlay') === 'false'),
    debounce: num(script && script.getAttribute('data-debounce'), 350)
  };
  function num(v, d) { var n = parseFloat(v); return isFinite(n) ? n : d; }

  var USS = new Set(('align-content align-items align-self all animation-play-state aspect-ratio backdrop-filter background-color background-image background-position background-position-x background-position-y background-repeat background-size border-bottom-color border-bottom-left-radius border-bottom-right-radius border-bottom-width border-color border-left-color border-left-width border-radius border-right-color border-right-width border-top-color border-top-left-radius border-top-right-radius border-top-width border-width bottom color cursor display filter flex flex-basis flex-direction flex-grow flex-shrink flex-wrap font-size height justify-content left letter-spacing margin margin-bottom margin-left margin-right margin-top max-height max-width min-height min-width opacity overflow padding padding-bottom padding-left padding-right padding-top position right rotate scale text-overflow text-shadow top transform-origin transition transition-delay transition-duration transition-property transition-timing-function translate visibility white-space width word-spacing').split(' '));
  var TRANSLATED = new Set(('border border-top border-right border-bottom border-left border-style border-top-style border-right-style border-bottom-style border-left-style background font-weight font-style font-family text-align transform inset flex-flow box-sizing animation animation-name animation-duration animation-delay animation-iteration-count animation-direction animation-fill-mode animation-timing-function content').split(' '));
  var LAYOUT = new Set(('width height min-width min-height max-width max-height margin margin-top margin-right margin-bottom margin-left padding padding-top padding-right padding-bottom padding-left left top right bottom flex flex-basis flex-grow flex-shrink border-width font-size letter-spacing word-spacing aspect-ratio').split(' '));
  var DISCRETE = new Set(('align-content align-items align-self background-image cursor flex-direction flex-wrap justify-content overflow position text-overflow visibility white-space display').split(' '));
  var EASING = new Set(('ease ease-in ease-out ease-in-out linear').split(' '));
  var ADVICE = {
    'line-height': 'No USS equivalent: control the height with height/min-height and padding.',
    'text-transform': 'Store the text in the required case in the string table.',
    'text-decoration': 'No USS equivalent: use a border-bottom or a rich-text tag in the string.',
    'pointer-events': 'Use data-picking="ignore" on the element (picking-mode="Ignore").',
    'outline': 'Use a border.',
    'float': 'Use flex-direction: row and justify-content.',
    'user-select': 'Not available.',
    'object-fit': 'Use -unity-background-scale-mode.',
    'clip-path': 'Use overflow: hidden with border-radius.',
    'mix-blend-mode': 'Not available.',
    'word-break': 'No USS equivalent.',
    'overflow-wrap': 'No USS equivalent.',
    'vertical-align': 'Use -unity-text-align.',
    'order': 'Reorder the elements in the markup.'
  };
  var RULES = {
    C001: ['ConvertTime', 'warn', 'display: grid is flattened into nested flex rows by the converter (static templates only)', 'Prefer nested flex rows; auto-fill, auto-fit and minmax() are rejected.'],
    C002: ['ConvertTime', 'warn', 'gap becomes margins on the children (USS has no gap in 6000.6)', 'Use margins on the children yourself to see the exact result.'],
    C003: ['ConvertTime', 'warn', ':not(), :is(), :where() are resolved statically into generated classes', 'Or toggle an explicit class from code.'],
    C004: ['ConvertTime', 'warn', 'structural pseudo-class is resolved statically into a generated class', 'Or add an explicit class (first, last).'],
    C005: ['ConvertTime', 'warn', 'pseudo-element becomes a real child element (containers only)', 'Or add a real child element yourself.'],
    C006: ['ConvertTime', 'warn', 'math function: constants fold, mixed % and px are rejected', 'Use fixed values, % or flex sizing.'],
    C007: ['Runtime', 'warn', 'at-rule: @media on width/height/orientation and @keyframes need a runtime component; others are rejected', 'Use classes toggled from code or the animation spec.'],
    C008: ['ConvertTime', 'warn', 'vh, vw, em, rem resolve to px at conversion time; fr and others are rejected', 'Use px or %.'],
    C009: ['Reject', 'error', 'position: sticky and static are not supported (fixed moves to the root)', 'Use relative or absolute.'],
    C010: ['Native', 'warn', 'filter / backdrop-filter exist in 6000.6 (backdrop only on Screen Space Overlay panels)', 'Check the result in Unity.'],
    C011: ['Native', 'warn', 'gradient: linear is native (assembly-only evidence), radial and conic are baked to a texture', 'Check the result in Unity.'],
    C012: ['Reject', 'error', 'background shorthand or several backgrounds', 'Use background-color and one background-image.'],
    C013: ['Native', 'warn', 'box-shadow is rewritten to filter: drop-shadow (inset is rejected)', 'Check the result in Unity.'],
    C014: ['Reject', 'error', 'not a USS property', ''],
    C015: ['ConvertTime', 'warn', 'sibling combinator or attribute selector is resolved statically', 'Or use classes.'],
    C016: ['Reject', 'error', 'type selector (tag name) does not map to UI Toolkit', 'Select by class or name.'],
    C017: ['Reject', 'error', '!important does not exist in USS', 'Raise specificity instead.'],
    C018: ['Native', 'warn', 'inherit/unset exist in the assembly but the manual only documents initial', 'Write the explicit value.'],
    C019: ['Reject', 'error', 'display must be flex or none', 'Every element is already a flex container.'],
    C020: ['ConvertTime', 'warn', 'overflow auto/scroll becomes a ScrollView', 'Or use data-uxml="ScrollView".'],
    C021: ['ConvertTime', 'warn', 'z-index reorders siblings (same parent only)', 'Reorder the markup yourself.'],
    C022: ['ConvertTime', 'warn', 'transform must be translate, rotate, scale in that order, each once', 'Use translate/rotate/scale properties.'],
    C023: ['Reject', 'error', 'value not valid for this USS property', ''],
    C025: ['Reject', 'error', 'box-sizing must be border-box', 'Remove box-sizing.'],
    C035: ['Reject', 'error', 'unsupported pseudo-class (USS: hover, active, inactive, focus, disabled, enabled, checked, root)', 'Toggle a class from code.'],
    R001: ['Native', 'warn', 'font size below the project minimum', ''],
    R002: ['Native', 'warn', 'touch target below the project minimum', ''],
    R003: ['Native', 'warn', 'nowrap text without ellipsis clipping', 'Add overflow: hidden and text-overflow: ellipsis.'],
    R006: ['Native', 'warn', 'transition on a property that does not animate', ''],
    R007: ['Reject', 'error', 'unsupported easing (use a keyword: ease, ease-in, ease-out, ease-in-out, linear or a Unity easing)', ''],
    R008: ['Native', 'warn', 'transition on a layout property', 'Animate translate, scale, rotate or opacity.'],
    L001: ['Reject', 'error', 'visible text without data-loc', 'Add data-loc="key".'],
    N001: ['Reject', 'error', 'duplicate element name', ''],
    N003: ['Native', 'warn', 'interactive element without a name', ''],
    A002: ['Reject', 'error', 'animated property is not an animatable USS property', 'Animate opacity, colours, translate, scale or rotate.'],
    A003: ['Reject', 'error', 'layout property animated in @keyframes', 'Animate translate or scale instead.'],
    X001: ['Native', 'warn', 'text is wider or taller than its box (clipped or overlapping)', 'Widen the box, allow wrapping or add ellipsis.'],
    X002: ['Native', 'warn', 'siblings overlap', 'Fix the layout; in UI Toolkit overflowing text paints over neighbours.']
  };

  var issues = [];
  var lastVerdict = 'PASS';
  var panel = null;
  var timer = null;
  var observer = null;

  function isHarness(node) {
    while (node && node.nodeType === 1) {
      if (node.hasAttribute('data-uitk-harness')) return true;
      node = node.parentElement;
    }
    return false;
  }

  function add(rule, message, target, extra) {
    var r = RULES[rule] || ['Native', 'warn', rule, ''];
    issues.push({
      rule: rule,
      tier: (extra && extra.tier) || r[0],
      severity: (extra && extra.severity) || r[1],
      message: message || r[2],
      suggestion: (extra && extra.suggestion) || r[3],
      element: target && target.nodeType === 1 ? target : null,
      where: (extra && extra.where) || ''
    });
  }

  function splitTop(text, sep) {
    var out = [], depth = 0, quote = null, cur = '';
    for (var i = 0; i < text.length; i++) {
      var ch = text[i];
      if (quote) { cur += ch; if (ch === quote) quote = null; continue; }
      if (ch === '"' || ch === "'") { quote = ch; cur += ch; continue; }
      if (ch === '(') depth++;
      if (ch === ')') depth--;
      if (ch === sep && depth === 0) { out.push(cur); cur = ''; continue; }
      cur += ch;
    }
    if (cur.trim()) out.push(cur);
    return out;
  }

  function declsOf(rule) {
    var text = rule.style ? rule.style.cssText : '';
    var out = [];
    splitTop(text, ';').forEach(function (part) {
      var i = part.indexOf(':');
      if (i < 0) return;
      out.push([part.slice(0, i).trim().toLowerCase(), part.slice(i + 1).trim()]);
    });
    return out;
  }

  function checkSelector(sel, where) {
    if (/::/.test(sel) || /:(before|after|first-line|first-letter)\b/.test(sel)) add('C005', null, null, { where: where + ' ' + sel });
    if (/:(not|is|where|matches)\(/.test(sel)) add('C003', null, null, { where: where + ' ' + sel });
    if (/:has\(/.test(sel)) add('C003', ':has() is rejected', null, { tier: 'Reject', severity: 'error', where: where + ' ' + sel });
    if (/:(first-child|last-child|only-child|nth-child|nth-last-child|nth-of-type|nth-last-of-type|first-of-type|last-of-type|only-of-type|empty)\b/.test(sel)) add('C004', null, null, { where: where + ' ' + sel });
    if (/\[[^\]]+\]/.test(sel) || /(^|[^\\])[+~](?![^()]*\))/.test(sel.replace(/\([^)]*\)/g, ''))) add('C015', null, null, { where: where + ' ' + sel });
    var pseudos = sel.replace(/\([^)]*\)/g, '').match(/:[a-z-]+/g) || [];
    pseudos.forEach(function (p) {
      var name = p.slice(1);
      if (/^(hover|active|inactive|focus|disabled|enabled|checked|root|not|is|where|has|matches|first-child|last-child|only-child|nth-child|nth-last-child|nth-of-type|nth-last-of-type|first-of-type|last-of-type|only-of-type|empty|before|after|first-line|first-letter)$/.test(name)) return;
      add('C035', 'pseudo-class :' + name + ' is not supported', null, { where: where + ' ' + sel });
    });
    var bare = sel.replace(/\([^)]*\)/g, '').replace(/\[[^\]]*\]/g, '').replace(/::?[a-z-]+/g, '');
    var tokens = bare.split(/[\s>+~,]+/).filter(Boolean);
    tokens.forEach(function (t) {
      if (/^[a-z][a-z0-9-]*$/i.test(t) && t !== '*') add('C016', 'type selector "' + t + '"', null, { where: where + ' ' + sel });
    });
  }

  function checkDecl(name, value, where) {
    var low = value.toLowerCase();
    var w = { where: where };
    if (/!important/.test(low)) add('C017', null, null, w);
    if (/(^|[^a-z-])(calc|min|max|clamp)\(/.test(low)) add('C006', null, null, w);
    if (/[0-9.](vh|vw|vmin|vmax)\b/.test(low)) add('C008', 'viewport unit in ' + name + ': ' + value, null, w);
    if (/[0-9.](em|rem|ch|pt)\b/.test(low) && name !== 'content') add('C008', 'relative unit in ' + name + ': ' + value, null, w);
    if (/[0-9.]fr\b/.test(low) && !/^grid/.test(name)) add('C008', 'fr unit in ' + name, null, { tier: 'Reject', severity: 'error', where: where });
    if (/^(inherit|unset)$/.test(low)) add('C018', null, null, w);
    if (/^revert/.test(low)) add('C018', 'revert is not supported', null, { tier: 'Reject', severity: 'error', where: where });
    if (name === 'display') {
      if (/grid/.test(low)) add('C001', null, null, w);
      else if (!/^(flex|none)$/.test(low)) add('C019', 'display: ' + value, null, w);
    } else if (/^grid/.test(name)) {
      add('C001', null, null, w);
    } else if (/(^|-)gap$/.test(name)) {
      add('C002', null, null, w);
    } else if (name === 'position') {
      if (low === 'fixed') add('C009', 'position: fixed moves the element to the root', null, { tier: 'ConvertTime', severity: 'warn', where: where });
      else if (low !== 'relative' && low !== 'absolute') add('C009', 'position: ' + value, null, w);
    } else if (name === 'z-index') {
      add('C021', null, null, w);
    } else if (name === 'box-shadow') {
      add('C013', /inset/.test(low) ? 'inset box-shadow is rejected' : null, null, /inset/.test(low) ? { tier: 'Reject', severity: 'error', where: where } : w);
    } else if (name === 'filter' || name === 'backdrop-filter') {
      add('C010', null, null, w);
    } else if (name === 'transform') {
      var fns = (low.match(/[a-z0-9]+\(/g) || []).map(function (s) { return s.slice(0, -1); });
      var order = { translate: 0, translatex: 0, translatey: 0, rotate: 1, rotatez: 1, scale: 2, scalex: 2, scaley: 2 };
      var bad = fns.some(function (f) { return !(f in order); });
      var sorted = fns.every(function (f, i) { return i === 0 || order[f] > order[fns[i - 1]]; });
      if (fns.length > 1 || bad) add('C022', null, null, (bad || !sorted) ? { tier: 'Reject', severity: 'error', where: where } : w);
    } else if (name === 'overflow' || name === 'overflow-x' || name === 'overflow-y') {
      if (/auto|scroll/.test(low)) add('C020', null, null, w);
      else if (!/^(visible|hidden)$/.test(low)) add('C023', name + ': ' + value, null, w);
    } else if (name === 'box-sizing') {
      if (low !== 'border-box') add('C025', null, null, w);
    } else if (name === 'background-image' || name === 'background') {
      if (/gradient\(/.test(low)) {
        if (/^linear-/.test(low.replace(/^.*?(?=linear-gradient|radial-gradient|conic-gradient|repeating)/, ''))) add('C011', null, null, w);
        else add('C011', 'radial/conic gradient is baked to a texture', null, { tier: 'ConvertTime', severity: 'warn', where: where });
      }
      if (name === 'background' && !/^(none|transparent|#[0-9a-f]{3,8}|rgba?\([^)]*\)|[a-z]+)$/.test(low) && !/gradient\(/.test(low)) add('C012', null, null, w);
      if (splitTop(value, ',').length > 1 && !/gradient\(/.test(low)) add('C012', 'several backgrounds', null, w);
    } else if (/^(transition|transition-timing-function)$/.test(name)) {
      if (/cubic-bezier\(|steps\(/.test(low)) add('R007', null, null, w);
      else if (name === 'transition-timing-function') {
        splitTop(low, ',').forEach(function (e) { e = e.trim(); if (e && !EASING.has(e) && !/^ease-(in|out|in-out)-(sine|cubic|circ|elastic|back|bounce)$/.test(e)) add('R007', 'easing ' + e, null, w); });
      }
      if (name === 'transition') {
        splitTop(low, ',').forEach(function (part) {
          var toks = splitTop(part.trim(), ' ').map(function (x) { return x.trim(); }).filter(Boolean);
          var p = 'all';
          for (var t = 0; t < toks.length; t++) {
            if (/^[0-9.]+m?s$/.test(toks[t]) || EASING.has(toks[t]) || /^(cubic-bezier|steps)\(/.test(toks[t]) || /^ease-/.test(toks[t])) continue;
            p = toks[t];
            break;
          }
          if (!p || p === 'all' || p === 'none') return;
          if (p === 'transform') return;
          if (!USS.has(p)) add('C023', 'transition property ' + p + ' is not a USS property', null, w);
          else if (DISCRETE.has(p)) add('R006', 'transition on ' + p, null, w);
          else if (LAYOUT.has(p)) add('R008', 'transition on ' + p, null, w);
        });
      }
    } else if (!USS.has(name) && !TRANSLATED.has(name) && name.indexOf('--') !== 0 && name.indexOf('-unity-') !== 0 && !/^(animation-)/.test(name)) {
      if (/^(margin|padding|border|font|text|background|outline|overflow|flex|align|justify|grid|transition|animation)/.test(name) && USS.has(name.replace(/-(left|right|top|bottom)$/, '')) ) return;
      add('C014', '"' + name + '" is not a USS property', null, { where: where, suggestion: ADVICE[name] || 'Check the spelling or use a property of the USS reference.' });
    }
    if (name === 'font-size') {
      var px = parseFloat(low);
      if (/px$/.test(low) && px < cfg.minFont) add('R001', 'font-size ' + value + ' is below ' + cfg.minFont + 'px', null, w);
    }
  }

  function walkRules(list, where, inKeyframes) {
    for (var i = 0; i < list.length; i++) {
      var r = list[i];
      var t = r.constructor && r.constructor.name;
      if (r.type === 1) {
        splitTop(r.selectorText, ',').forEach(function (s) { checkSelector(s.trim(), where); });
        declsOf(r).forEach(function (d) { checkDecl(d[0], d[1], where + ' ' + r.selectorText); });
      } else if (r.type === 7 || t === 'CSSKeyframesRule') {
        add('C007', '@keyframes ' + r.name + ' runs through the animator component', null, { where: where });
        var kf = r.cssRules || [];
        for (var k = 0; k < kf.length; k++) {
          declsOf(kf[k]).forEach(function (d) {
            if (LAYOUT.has(d[0])) add('A003', 'layout property ' + d[0] + ' in @keyframes ' + r.name, null, { tier: 'Reject', severity: 'error', where: where });
            else if (!USS.has(d[0]) && d[0].indexOf('--') !== 0 && !TRANSLATED.has(d[0])) add('A002', 'animated property ' + d[0] + ' in @keyframes ' + r.name + ' is not animatable', null, { where: where });
          });
        }
      } else if (r.type === 4 || t === 'CSSMediaRule') {
        add('C007', '@media ' + (r.conditionText || r.media.mediaText), null, { where: where });
        if (r.cssRules) walkRules(r.cssRules, where + ' @media', false);
      } else if (r.type === 3 || t === 'CSSImportRule') {
        add('C007', '@import', null, { tier: 'Reject', severity: 'error', where: where });
      } else if (r.type === 5 || t === 'CSSFontFaceRule' || t === 'CSSSupportsRule' || t === 'CSSLayerBlockRule' || t === 'CSSContainerRule') {
        add('C007', '@' + (t || 'rule').replace(/^CSS|Rule$/g, '').toLowerCase() + ' is not supported', null, { tier: 'Reject', severity: 'error', where: where });
      }
    }
  }

  function checkStyles() {
    var sheets = document.styleSheets;
    for (var i = 0; i < sheets.length; i++) {
      var sheet = sheets[i];
      var owner = sheet.ownerNode;
      if (owner && (owner.hasAttribute('data-uitk-harness') || owner.hasAttribute('data-uss'))) continue;
      var rules;
      try { rules = sheet.cssRules; } catch (e) { continue; }
      var where = owner && owner.tagName === 'LINK' ? owner.getAttribute('href') : '<style>';
      walkRules(rules, where, false);
    }
  }

  var INTERACTIVE = 'button, input, select, textarea, [data-uxml="Button"], [data-uxml="Toggle"], [data-uxml="Slider"], [data-uxml="DropdownField"], [data-uxml="Foldout"]';

  function describe(el) {
    var s = el.tagName.toLowerCase();
    if (el.getAttribute('name') || el.id) s += ' name="' + (el.getAttribute('name') || el.id) + '"';
    else if (el.className && typeof el.className === 'string') s += ' class="' + el.className.trim().slice(0, 40) + '"';
    return '<' + s + '>';
  }

  function ownText(el) {
    var t = '';
    for (var n = el.firstChild; n; n = n.nextSibling) if (n.nodeType === 3) t += n.nodeValue;
    return t.replace(/\s+/g, ' ').trim();
  }

  function scaleOf() {
    var b = document.body;
    var w = b.getBoundingClientRect().width;
    return b.offsetWidth ? w / b.offsetWidth : 1;
  }

  function checkDom() {
    var scale = scaleOf() || 1;
    var all = document.body.querySelectorAll('*');
    var names = {};
    for (var i = 0; i < all.length; i++) {
      var el = all[i];
      if (isHarness(el) || /^(script|style|link|meta|option|br)$/i.test(el.tagName)) continue;
      var cs = getComputedStyle(el);
      if (cs.display === 'none' || !el.getClientRects().length) continue;
      var name = el.getAttribute('name') || el.id;
      if (name) {
        if (names[name]) { add('N001', 'duplicate name "' + name + '"', el); add('N001', 'first use of "' + name + '"', names[name]); }
        else names[name] = el;
      }
      var text = ownText(el);
      if (text && /[A-Za-zÀ-ɏЀ-ӿ]/.test(text) && !el.hasAttribute('data-loc') && el.tagName !== 'TEXTAREA' && !el.closest('select')) {
        add('L001', 'text "' + text.slice(0, 40) + '" has no data-loc', el);
      }
      if (text) {
        var fs = parseFloat(cs.fontSize);
        if (fs < cfg.minFont) add('R001', 'computed font-size ' + fs + 'px is below ' + cfg.minFont + 'px', el);
        var range = document.createRange();
        range.selectNodeContents(el);
        var rr = range.getBoundingClientRect();
        var er = el.getBoundingClientRect();
        if (rr.width > 0 && (rr.right > er.right + 1.5 * scale || rr.bottom > er.bottom + 1.5 * scale)) {
          var clipped = cs.overflowX !== 'visible';
          add('X001', (clipped ? 'text is clipped by ' : 'text overflows ') + describe(el) + ' (' + Math.round(rr.width / scale) + 'x' + Math.round(rr.height / scale) + ' in ' + Math.round(er.width / scale) + 'x' + Math.round(er.height / scale) + ')', el);
        }
        if (cs.whiteSpace === 'nowrap' && !(cs.textOverflow === 'ellipsis' && cs.overflowX === 'hidden') && el.scrollWidth > el.clientWidth + 1) {
          add('R003', null, el);
        }
      }
    }
    var inter = document.body.querySelectorAll(INTERACTIVE);
    for (var j = 0; j < inter.length; j++) {
      var e2 = inter[j];
      if (isHarness(e2) || getComputedStyle(e2).display === 'none' || !e2.getClientRects().length) continue;
      var r2 = e2.getBoundingClientRect();
      var w = r2.width / scale, h = r2.height / scale;
      if (!(e2.getAttribute('name') || e2.id)) add('N003', null, e2);
      if (h < cfg.minTouch || w < cfg.minTouch) add('R002', Math.round(w) + 'x' + Math.round(h) + 'px is below ' + cfg.minTouch + 'px', e2);
    }
    var parents = document.body.querySelectorAll('*');
    for (var p = 0; p < parents.length; p++) {
      var par = parents[p];
      if (isHarness(par) || getComputedStyle(par).display === 'none' || !par.getClientRects().length) continue;
      var kids = [];
      for (var c = par.firstElementChild; c; c = c.nextElementSibling) {
        var kcs = getComputedStyle(c);
        if (kcs.display === 'none' || kcs.position === 'absolute' || kcs.position === 'fixed' || isHarness(c) || /^(script|style)$/i.test(c.tagName)) continue;
        var kr = c.getBoundingClientRect();
        if (kr.width < 1 || kr.height < 1) continue;
        kids.push([c, kr]);
      }
      for (var a = 0; a < kids.length; a++) {
        for (var b = a + 1; b < kids.length; b++) {
          var A = kids[a][1], B = kids[b][1];
          var ox = Math.min(A.right, B.right) - Math.max(A.left, B.left);
          var oy = Math.min(A.bottom, B.bottom) - Math.max(A.top, B.top);
          if (ox > 2 * scale && oy > 2 * scale) {
            add('X002', describe(kids[a][0]) + ' overlaps ' + describe(kids[b][0]) + ' by ' + Math.round(ox / scale) + 'x' + Math.round(oy / scale) + 'px', kids[a][0]);
            add('X002', 'overlapped by ' + describe(kids[a][0]), kids[b][0]);
          }
        }
      }
    }
  }

  function verdictOf() {
    var err = 0, warn = 0;
    issues.forEach(function (i) { if (i.severity === 'error') err++; else if (i.severity === 'warn') warn++; });
    return err ? 'FAIL' : (warn ? 'PASS WITH WARNINGS' : 'PASS');
  }

  function clearMarks() {
    var marked = document.querySelectorAll('.uitk-v-error, .uitk-v-warn');
    for (var i = 0; i < marked.length; i++) marked[i].classList.remove('uitk-v-error', 'uitk-v-warn');
  }

  function mark() {
    issues.forEach(function (i) {
      if (!i.element) return;
      if (i.severity === 'error') { i.element.classList.remove('uitk-v-warn'); i.element.classList.add('uitk-v-error'); }
      else if (!i.element.classList.contains('uitk-v-error')) i.element.classList.add('uitk-v-warn');
    });
  }

  function injectStyle() {
    if (document.getElementById('uitk-v-style')) return;
    var st = document.createElement('style');
    st.id = 'uitk-v-style';
    st.setAttribute('data-uitk-harness', '');
    st.textContent = '.uitk-v-error{outline:2px solid #ff3b3b !important;outline-offset:-2px}.uitk-v-warn{outline:2px dashed #ffb020 !important;outline-offset:-2px}' +
      '.uitk-v-flash{animation:uitk-v-flash 1s ease-out}@keyframes uitk-v-flash{0%{box-shadow:0 0 0 6px #4aa8ff}100%{box-shadow:0 0 0 0 transparent}}' +
      '#uitk-v-panel{position:fixed;top:8px;right:8px;z-index:2147483647;width:420px;max-height:70vh;overflow:auto;background:#10161cf2;color:#e8eef2;font:12px/1.35 Consolas,monospace;border:1px solid #3a4a58;border-radius:6px;box-shadow:0 6px 24px #000a}' +
      '#uitk-v-panel,#uitk-v-panel .row{display:block;flex-shrink:0}#uitk-v-panel span,#uitk-v-panel b,#uitk-v-panel button{display:inline-block;margin:0}#uitk-v-panel .row,#uitk-v-panel .where,#uitk-v-panel .sug{display:block}#uitk-v-panel header{flex-direction:row;align-items:center}#uitk-v-panel header>span{display:inline}#uitk-v-panel *{box-sizing:border-box}#uitk-v-panel header{display:flex;gap:8px;align-items:center;padding:6px 8px;border-bottom:1px solid #2a3846;position:sticky;top:0;background:#10161c}' +
      '#uitk-v-panel .v{font-weight:bold;padding:1px 6px;border-radius:3px}#uitk-v-panel .v.PASS{background:#1f6f43}#uitk-v-panel .v.FAIL{background:#9b2c2c}#uitk-v-panel .v.W{background:#8a6414}' +
      '#uitk-v-panel .row{padding:5px 8px;border-bottom:1px solid #1d2832;cursor:pointer}#uitk-v-panel .row:hover{background:#1a2530}' +
      '#uitk-v-panel .id{display:inline-block;min-width:38px;font-weight:bold}#uitk-v-panel .tier{opacity:.75;margin-left:6px}#uitk-v-panel .sug{color:#9fd0ff}#uitk-v-panel .where{color:#8a98a4}' +
      '#uitk-v-panel .e{color:#ff7b7b}#uitk-v-panel .w{color:#ffc65c}#uitk-v-panel button{background:#243442;color:#e8eef2;border:1px solid #3a4a58;border-radius:3px;padding:1px 6px;cursor:pointer}';
    document.head.appendChild(st);
  }

  function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }

  function render() {
    if (!cfg.overlay) return;
    injectStyle();
    if (!panel) {
      panel = document.createElement('div');
      panel.id = 'uitk-v-panel';
      panel.setAttribute('data-uitk-harness', '');
      document.documentElement.appendChild(panel);
    }
    var v = verdictOf();
    var counts = { error: 0, warn: 0 };
    issues.forEach(function (i) { counts[i.severity === 'error' ? 'error' : 'warn']++; });
    var collapsed = panel.getAttribute('data-collapsed') === '1';
    var html = '<header><span class="v ' + (v === 'PASS' ? 'PASS' : v === 'FAIL' ? 'FAIL' : 'W') + '">' + v + '</span><span>' + counts.error + ' errors, ' + counts.warn + ' warnings</span><span style="flex:1"></span><button data-act="rerun">re-run</button><button data-act="toggle">' + (collapsed ? 'show' : 'hide') + '</button></header>';
    if (!collapsed) {
      var sorted = issues.slice().sort(function (a, b) { return (a.severity === 'error' ? 0 : 1) - (b.severity === 'error' ? 0 : 1) || a.rule.localeCompare(b.rule); });
      sorted.forEach(function (i, idx) {
        html += '<div class="row" data-idx="' + issues.indexOf(i) + '"><span class="id ' + (i.severity === 'error' ? 'e' : 'w') + '">' + i.rule + '</span>' + esc(i.message) + '<span class="tier">[' + i.tier + ']</span>' +
          (i.where ? '<div class="where">' + esc(i.where) + '</div>' : '') + (i.suggestion ? '<div class="sug">rewrite: ' + esc(i.suggestion) + '</div>' : '') + '</div>';
      });
      if (!issues.length) html += '<div class="row">No findings.</div>';
    }
    panel.innerHTML = html;
  }

  function run() {
    if (observer) observer.disconnect();
    issues = [];
    clearMarks();
    try { checkStyles(); } catch (e) { console.warn('uitk-validate: styles', e); }
    try { checkDom(); } catch (e) { console.warn('uitk-validate: dom', e); }
    var seen = {};
    issues = issues.filter(function (i) {
      var key = i.rule + '|' + i.message + '|' + i.where + '|' + (i.element ? describe(i.element) : '');
      if (seen[key]) return false;
      seen[key] = 1;
      return true;
    });
    lastVerdict = verdictOf();
    mark();
    render();
    api.issues = issues;
    api.verdict = lastVerdict;
    document.dispatchEvent(new CustomEvent('uitk:validated', { detail: { verdict: lastVerdict, count: issues.length } }));
    watch();
    return api.report();
  }

  function watch() {
    if (!window.MutationObserver) return;
    observer = new MutationObserver(function (muts) {
      for (var i = 0; i < muts.length; i++) {
        var t = muts[i].target;
        if (t.nodeType === 1 && isHarness(t)) continue;
        if (muts[i].attributeName === 'class' && t.nodeType === 1 && /uitk-v-/.test(t.className)) continue;
        schedule();
        return;
      }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true, attributes: true, characterData: true });
  }

  function schedule() {
    clearTimeout(timer);
    timer = setTimeout(run, cfg.debounce);
  }

  document.addEventListener('click', function (ev) {
    var t = ev.target;
    if (!panel || !panel.contains(t)) return;
    var act = t.getAttribute && t.getAttribute('data-act');
    if (act === 'rerun') { run(); return; }
    if (act === 'toggle') { panel.setAttribute('data-collapsed', panel.getAttribute('data-collapsed') === '1' ? '0' : '1'); render(); return; }
    var row = t.closest && t.closest('.row');
    if (row && row.getAttribute('data-idx') != null) {
      var issue = issues[parseInt(row.getAttribute('data-idx'), 10)];
      if (issue && issue.element) {
        issue.element.scrollIntoView({ block: 'center', behavior: 'smooth' });
        issue.element.classList.remove('uitk-v-flash');
        void issue.element.offsetWidth;
        issue.element.classList.add('uitk-v-flash');
      }
    }
  }, true);

  var api = {
    version: VERSION,
    run: run,
    issues: issues,
    verdict: lastVerdict,
    rules: RULES,
    report: function () {
      return {
        tool: 'uitk-validate.js',
        version: VERSION,
        verdict: verdictOf(),
        findings: issues.map(function (i) {
          return { rule: i.rule, severity: i.severity === 'error' ? 'Error' : 'Warning', tier: i.tier, message: i.message, where: i.where, suggestion: i.suggestion, element: i.element ? describe(i.element) : null };
        })
      };
    }
  };
  window.uitkValidate = api;

  function start() { if (cfg.auto) run(); }
  if (document.readyState === 'complete') setTimeout(start, 50);
  else window.addEventListener('load', function () { setTimeout(start, 50); });
})();
