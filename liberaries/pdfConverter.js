/**
 * pdfConverter.js
 * ──────────────────────────────────────────────────────────────────────────────
 * Framework-agnostic PDF conversion service.
 *
 * Two strategies:
 *   • 'pdfjs'      – Renders each page to a <canvas> entirely in the browser.
 *                    No server required.  Returns an array of PdfPage objects.
 *   • 'pdf2htmlex' – Posts the file to a backend endpoint that runs pdf2htmlEX
 *                    and returns a fully self-contained HTML string.
 *                    Requires the C# API (PptxToHtml.Api) to be running.
 * ──────────────────────────────────────────────────────────────────────────────
 */

import * as pdfjs from 'pdfjs-dist';

// Point the worker at the CDN copy that matches the installed library version.
// Override this BEFORE calling any convert* function if you host the worker
// yourself:
//   import { setPdfWorkerSrc } from 'content-converter/pdf';
//   setPdfWorkerSrc('/workers/pdf.worker.min.mjs');
pdfjs.GlobalWorkerOptions.workerSrc =
  `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;

/**
 * Override the PDF.js worker URL.
 * Call this once at application startup if you self-host the worker.
 *
 * @param {string} url  Absolute or relative URL to pdf.worker.min.mjs
 */
export function setPdfWorkerSrc(url) {
  pdfjs.GlobalWorkerOptions.workerSrc = url;
}

// ─────────────────────────────────────────────────────────────────────────────
// Strategy 1 — PDF.js (browser-only)
// ─────────────────────────────────────────────────────────────────────────────

/**
 * @typedef {Object} PdfPage
 * @property {number} pageNum   1-based page index
 * @property {string} dataUrl   PNG data-URL of the rendered page
 * @property {number} width     Canvas width in CSS pixels
 * @property {number} height    Canvas height in CSS pixels
 * @property {HTMLCanvasElement} canvas  The live canvas element (detached from DOM)
 */

/**
 * Render every page of a PDF to canvas and return data-URLs.
 *
 * @param {File | ArrayBuffer | Uint8Array} source
 *   A browser File object, an ArrayBuffer, or a Uint8Array containing the PDF.
 * @param {object}  [options]
 * @param {number}  [options.scale=1.5]
 *   Render scale factor (1 = 72 DPI screen, 1.5 = 108 DPI, 2 = 144 DPI).
 * @param {(current: number, total: number) => void} [options.onProgress]
 *   Called after each page is rendered.
 * @returns {Promise<PdfPage[]>}
 *
 * @example
 * import { convertWithPdfJs } from 'content-converter/pdf';
 *
 * const pages = await convertWithPdfJs(fileInput.files[0], { scale: 2 });
 * pages.forEach(p => {
 *   const img = document.createElement('img');
 *   img.src = p.dataUrl;
 *   document.body.appendChild(img);
 * });
 */
export async function convertWithPdfJs(source, options = {}) {
  const { scale = 1.5, onProgress } = options;

  let data;
  if (source instanceof File) {
    data = await source.arrayBuffer();
  } else {
    data = source; // ArrayBuffer or Uint8Array
  }

  const loadingTask = pdfjs.getDocument({ data });
  const pdf = await loadingTask.promise;

  const pages = [];
  for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
    const page = await pdf.getPage(pageNum);
    const viewport = page.getViewport({ scale });

    const canvas = document.createElement('canvas');
    canvas.width = viewport.width;
    canvas.height = viewport.height;

    const ctx = canvas.getContext('2d');
    await page.render({ canvasContext: ctx, viewport }).promise;

    pages.push({
      pageNum,
      dataUrl: canvas.toDataURL('image/png'),
      width: viewport.width,
      height: viewport.height,
      canvas,
    });

    onProgress?.(pageNum, pdf.numPages);
  }

  return pages;
}

// ─────────────────────────────────────────────────────────────────────────────
// Strategy 2 — pdf2htmlEX via backend API
// ─────────────────────────────────────────────────────────────────────────────

/**
 * @typedef {Object} Pdf2HtmlExResult
 * @property {string} html      Full self-contained HTML string
 * @property {string} blobUrl   Object URL pointing to the HTML blob.
 *                              Call URL.revokeObjectURL(blobUrl) when done.
 */

/**
 * Send a PDF to the backend server that runs pdf2htmlEX and return the
 * resulting HTML string + a blob URL ready to use in an <iframe>.
 *
 * @param {File}   file              The PDF file to convert.
 * @param {object} [options]
 * @param {string} [options.apiBase='http://localhost:5005']
 *   Base URL of the PptxToHtml.Api backend.
 * @param {string} [options.endpoint='/api/convert-pdf']
 *   Endpoint path on the backend.
 * @returns {Promise<Pdf2HtmlExResult>}
 *
 * @example
 * import { convertWithPdf2HtmlEx } from 'content-converter/pdf';
 *
 * const { blobUrl } = await convertWithPdf2HtmlEx(file, {
 *   apiBase: 'https://my-api.example.com',
 * });
 * iframeEl.src = blobUrl;
 * // When the component unmounts:
 * // URL.revokeObjectURL(blobUrl);
 */
export async function convertWithPdf2HtmlEx(file, options = {}) {
  const {
    apiBase   = 'http://localhost:5005',
    endpoint  = '/api/convert-pdf',
  } = options;

  const formData = new FormData();
  formData.append('file', file);

  let response;
  try {
    response = await fetch(`${apiBase}${endpoint}`, {
      method: 'POST',
      body: formData,
    });
  } catch {
    throw new Error(
      `Cannot reach backend at ${apiBase}. ` +
      'Ensure the C# API is running: cd PptxToHtml.Api && dotnet run'
    );
  }

  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`Server error ${response.status}: ${detail}`);
  }

  const html = await response.text();
  const blob = new Blob([html], { type: 'text/html' });
  const blobUrl = URL.createObjectURL(blob);

  return { html, blobUrl };
}
