/**
 * Makes JSON fields in Swagger UI clickable: click any key or value to copy it.
 * Works for request bodies, response bodies, and example values.
 */
(function () {
  'use strict';

  function getCharIndexAtClick(preElement, clientX, clientY) {
    var doc = preElement.ownerDocument;
    var range = null;
    if (doc.caretRangeFromPoint) range = doc.caretRangeFromPoint(clientX, clientY);
    else if (doc.caretPositionFromPoint) {
      var pos = doc.caretPositionFromPoint(clientX, clientY);
      if (pos) range = { startContainer: pos.offsetNode, startOffset: pos.offset };
    }
    if (!range || !preElement.contains(range.startContainer)) return -1;
    var walker = doc.createTreeWalker(preElement, NodeFilter.SHOW_TEXT, null, false);
    var idx = 0;
    var node;
    while ((node = walker.nextNode())) {
      var len = (node.textContent || '').length;
      if (node === range.startContainer) return idx + (range.startOffset || 0);
      idx += len;
    }
    return -1;
  }

  /** Returns the JSON token at the given character index. For double-quoted strings, returns the full string including quotes (so spaces etc. are preserved). */
  function getTokenAt(text, charIndex) {
    if (charIndex < 0 || charIndex >= text.length) return null;

    function isSpace(ch) { return ch === ' ' || ch === '\t' || ch === '\n' || ch === '\r'; }

    // First: check if we're inside a double-quoted string (JSON keys and string values).
    // Walk backwards to find an unescaped opening ".
    var i = charIndex;
    while (i >= 0) {
      if (text[i] === '"' && (i === 0 || text[i - 1] !== '\\')) {
        var startQuote = i;
        var j = i + 1;
        while (j < text.length) {
          if (text[j] === '\\') { j += 2; continue; }
          if (text[j] === '"') {
            var endQuote = j;
            if (charIndex >= startQuote && charIndex <= endQuote)
              return { value: text.slice(startQuote, endQuote + 1), start: startQuote, end: endQuote + 1 };
            break;
          }
          j++;
        }
        break;
      }
      i--;
    }

    // Not inside a double-quoted string: find token at position (skip spaces backwards first).
    i = charIndex;
    while (i >= 0 && isSpace(text[i])) i--;
    if (i < 0) return null;
    var start = i;
    var end = charIndex;
    if (text[i] === "'") {
      var q = "'";
      start = i;
      i++;
      while (i < text.length && text[i] !== q) {
        if (text[i] === '\\') i++;
        i++;
      }
      end = i;
      return { value: text.slice(start, end + 1), start: start, end: end + 1 };
    }
    if (/[-\d.eE]/.test(text[i]) || (text[i] === '-' && i + 1 < text.length && /[\d]/.test(text[i + 1]))) {
      while (start > 0 && /[-0-9.eE]/.test(text[start - 1])) start--;
      while (end < text.length && /[-0-9.eE]/.test(text[end])) end++;
      return { value: text.slice(start, end), start: start, end: end };
    }
    if (/[a-zA-Z]/.test(text[i])) {
      while (start > 0 && /[a-zA-Z]/.test(text[start - 1])) start--;
      while (end < text.length && /[a-zA-Z]/.test(text[end])) end++;
      return { value: text.slice(start, end), start: start, end: end };
    }
    return null;
  }

  function copyToClipboard(text) {
    if (!navigator.clipboard || !navigator.clipboard.writeText) {
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      try { document.execCommand('copy'); } catch (e) {}
      document.body.removeChild(ta);
      return true;
    }
    return navigator.clipboard.writeText(text).then(function () { return true; }, function () { return false; });
  }

  function showCopiedFeedback(el) {
    var msg = document.createElement('span');
    msg.textContent = 'Copied!';
    msg.style.cssText = 'position:fixed;bottom:16px;right:16px;background:#333;color:#fff;padding:6px 12px;border-radius:4px;font-size:12px;z-index:99999;pointer-events:none;';
    document.body.appendChild(msg);
    setTimeout(function () { msg.remove(); }, 1200);
  }

  function isJsonLikePre(el) {
    if (!el || el.tagName !== 'PRE') return false;
    var text = (el.textContent || '').trim();
    if (text.length < 2) return false;
    var first = text.charAt(0);
    return first === '{' || first === '[' || (first === '"' && text.indexOf('"', 1) > 0);
  }

  function findPreAncestor(node) {
    while (node && node !== document.body) {
      if (node.tagName === 'PRE' && isJsonLikePre(node)) return node;
      node = node.parentElement;
    }
    return null;
  }

  document.addEventListener('click', function (ev) {
    var pre = findPreAncestor(ev.target);
    if (!pre) return;
    var text = pre.textContent || '';
    var idx = getCharIndexAtClick(pre, ev.clientX, ev.clientY);
    if (idx < 0) return;
    var token = getTokenAt(text, idx);
    if (!token || !token.value) return;
    ev.preventDefault();
    ev.stopPropagation();
    var toCopy = token.value;
    if (toCopy.charAt(0) === '"' && toCopy.length >= 2 && toCopy.charAt(toCopy.length - 1) === '"') {
      try { toCopy = JSON.parse(toCopy); } catch (e) { toCopy = toCopy.slice(1, -1); }
    } else if (toCopy.charAt(0) === "'" && toCopy.length >= 2) {
      toCopy = toCopy.slice(1, -1);
    }
    copyToClipboard(toCopy).then(function (ok) {
      if (ok) showCopiedFeedback(pre);
    });
  }, true);

  document.addEventListener('DOMContentLoaded', function () {
    document.body.style.setProperty('cursor', 'default');
    var style = document.createElement('style');
    style.textContent = '.swagger-ui pre.microlight, .swagger-ui .opblock-body pre, .swagger-ui .response-body pre, .swagger-ui .body-param__content pre, .swagger-ui pre.highlight-code { cursor: pointer; user-select: none; } .swagger-ui pre.microlight:hover, .swagger-ui .opblock-body pre:hover, .swagger-ui .response-body pre:hover, .swagger-ui .body-param__content pre:hover { outline: 1px solid rgba(0,0,0,0.1); outline-offset: 2px; }';
    (document.head || document.documentElement).appendChild(style);
  });
})();
