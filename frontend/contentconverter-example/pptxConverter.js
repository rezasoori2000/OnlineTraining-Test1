/**
 * pptxConverter.js
 * ──────────────────────────────────────────────────────────────────────────────
 * Framework-agnostic PPTX → HTML conversion service.
 *
 * Wraps the patched PPTXjs library (pptxjs.js) which must be loaded as a
 * global script BEFORE this module is used  — PPTXjs exposes itself as a
 * jQuery plugin and requires jQuery + JSZip v2 globals.
 *
 * Required globals (load via <script> tags or equivalent):
 *   • jQuery (>=1.12) → window.$
 *   • JSZip v2        → window.JSZip  (jszip.js + companions in /public/)
 *   • PPTXjs          → window.$.fn.pptxToHtml  (pptxjs.js in /public/)
 *
 * ──────────────────────────────────────────────────────────────────────────────
 */

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Throw a clear error when jQuery / PPTXjs globals are missing.
 * @throws {Error}
 */
function assertGlobals() {
  if (typeof window === 'undefined') {
    throw new Error('pptxConverter must run in a browser environment.');
  }
  if (typeof window.$ === 'undefined' || typeof window.$.fn?.pptxToHtml !== 'function') {
    throw new Error(
      'PPTXjs globals are not loaded. ' +
      'Add the following <script> tags to your HTML before using pptxConverter:\n' +
      '  <script src="/jszip.js"></script>\n' +
      '  <script src="/jszip-deflate.js"></script>\n' +
      '  <script src="/jszip-inflate.js"></script>\n' +
      '  <script src="/jszip-load.js"></script>\n' +
      '  <script src="https://code.jquery.com/jquery-3.7.1.min.js"></script>\n' +
      '  <script src="/pptxjs.js"></script>'
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Public API
// ─────────────────────────────────────────────────────────────────────────────

/**
 * @typedef {Object} PptxResult
 * @property {HTMLElement} container
 *   The hidden container element populated with rendered slide divs.
 *   Append it to the DOM or read its innerHTML.
 * @property {number} slideCount  Number of rendered slides.
 * @property {number} slideWidth  Computed slide width in px.
 * @property {number} slideHeight Computed slide height in px.
 */

/**
 * Convert a PPTX file to HTML slide divs using PPTXjs.
 *
 * The function creates a temporary off-screen container, renders all slides
 * into it, then resolves with that container so the caller decides how to
 * display it (append to DOM, read innerHTML, etc.).
 *
 * @param {File | string} source
 *   Either a browser File object or a URL string pointing to the .pptx file.
 * @param {object}  [options]
 * @param {boolean} [options.themeProcess=true]
 *   Process theme colors and fonts (recommended: true).
 * @param {boolean} [options.mediaProcess=true]
 *   Process video / audio nodes.
 * @param {string}  [options.slidesScale='100%']
 *   CSS scale applied to the slide wrapper (e.g. '75%').
 * @param {number}  [options.timeoutMs=15000]
 *   Maximum time to wait for PPTXjs afterRender callback (ms).
 * @returns {Promise<PptxResult>}
 *
 * @example — React
 * import { convertPptx } from 'content-converter/pptx';
 *
 * const containerRef = useRef(null);
 *
 * async function load(file) {
 *   const { container } = await convertPptx(file);
 *   containerRef.current.innerHTML = '';
 *   containerRef.current.appendChild(container);
 * }
 *
 * @example — Vanilla JS
 * import { convertPptx } from 'content-converter/pptx';
 *
 * document.getElementById('input').addEventListener('change', async (e) => {
 *   const { container, slideCount } = await convertPptx(e.target.files[0]);
 *   console.log(`Rendered ${slideCount} slides`);
 *   document.getElementById('viewer').appendChild(container);
 * });
 */
export async function convertPptx(source, options = {}) {
  assertGlobals();

  const {
    themeProcess  = true,
    mediaProcess  = true,
    slidesScale   = '100%',
    timeoutMs     = 15000,
  } = options;

  // Create an off-screen container that PPTXjs can render into.
  // PPTXjs calls $result.attr("id") and builds selectors like $("#<id> .slide"),
  // so the element MUST have a unique id or those selectors silently fail and
  // afterRender is never called.
  const containerId = 'pptx-cc-' + Date.now() + '-' + Math.random().toString(36).slice(2);
  const container = document.createElement('div');
  container.id = containerId;
  container.style.cssText = 'position:absolute;left:-9999px;top:-9999px;pointer-events:none;';
  document.body.appendChild(container);

  const $ = window.$;
  const $container = $(container);

  let objectUrl = null;

  const pptxOptions = {
    themeProcess,
    mediaProcess,
    slidesScale,
    slideMode: false,
    keyBoardShortCut: false,
  };

  if (source instanceof File) {
    objectUrl = URL.createObjectURL(source);
    pptxOptions.pptxFileUrl = objectUrl;
  } else {
    pptxOptions.pptxFileUrl = source; // URL string
  }

  return new Promise((resolve, reject) => {
    let settled = false;

    const cleanup = () => {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };

    const settle = (fn) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      window.removeEventListener('error', onGlobalError);
      cleanup();
      fn();
    };

    // Intercept PPTXjs internal errors (fired asynchronously via window.onerror)
    const onGlobalError = (event) => {
      const src = event.filename || (event.error?.stack ?? '');
      if (src.includes('pptxjs')) {
        event.preventDefault();
        // Treat as partial success — slides may still have rendered
        const slides = container.querySelectorAll('.slide');
        settle(() =>
          resolve({
            container,
            slideCount: slides.length,
            slideWidth:  slides[0]?.offsetWidth  ?? 0,
            slideHeight: slides[0]?.offsetHeight ?? 0,
          })
        );
        return true;
      }
    };
    window.addEventListener('error', onGlobalError);

    // Safety timeout
    const timer = setTimeout(() => {
      settle(() => {
        document.body.removeChild(container);
        reject(new Error(`PPTXjs did not finish within ${timeoutMs} ms.`));
      });
    }, timeoutMs);

    try {
      $container.pptxToHtml({
        ...pptxOptions,
        afterRender: () => {
          const slides = container.querySelectorAll('.slide');
          // Detach from the hidden position — caller will re-attach wherever needed
          if (container.parentNode) document.body.removeChild(container);
          settle(() =>
            resolve({
              container,
              slideCount: slides.length,
              slideWidth:  slides[0]?.offsetWidth  ?? 0,
              slideHeight: slides[0]?.offsetHeight ?? 0,
            })
          );
        },
      });
    } catch (err) {
      settle(() => {
        if (container.parentNode) document.body.removeChild(container);
        reject(err);
      });
    }
  });
}

/**
 * Convenience wrapper: convert and return a plain HTML string.
 * Useful for server-side hydration or storing the result.
 *
 * @param {File | string} source  File or URL.
 * @param {object}        [options]  Same as convertPptx options.
 * @returns {Promise<string>}  Inner HTML of the rendered slide container.
 *
 * @example
 * import { convertPptxToHtmlString } from 'content-converter/pptx';
 *
 * const html = await convertPptxToHtmlString(file);
 * document.getElementById('viewer').innerHTML = html;
 */
export async function convertPptxToHtmlString(source, options = {}) {
  const { container } = await convertPptx(source, options);
  return container.innerHTML;
}
