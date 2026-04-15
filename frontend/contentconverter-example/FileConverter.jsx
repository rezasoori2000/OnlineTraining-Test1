import { useState, useRef, useCallback, useEffect } from 'react';
import * as pdfjs from 'pdfjs-dist';

pdfjs.GlobalWorkerOptions.workerSrc =
  `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`;

const RENDER_SCALE = 1.5;

// ─── PDF conversion ───────────────────────────────────────────────────────────

async function convertPdf(source) {
  const loadingTask = pdfjs.getDocument(
    source instanceof File
      ? { data: await source.arrayBuffer() }
      : { url: source }
  );
  const pdf = await loadingTask.promise;
  const pages = [];

  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i);
    const viewport = page.getViewport({ scale: RENDER_SCALE });
    const canvas = document.createElement('canvas');
    canvas.width = viewport.width;
    canvas.height = viewport.height;
    await page.render({ canvasContext: canvas.getContext('2d'), viewport }).promise;
    pages.push({
      pageNum: i,
      dataUrl: canvas.toDataURL('image/png'),
      width: viewport.width,
      height: viewport.height,
    });
  }

  return pages;
}

// ─── PPTX library loading ─────────────────────────────────────────────────────

let pptxLibsLoaded = false;

function loadScript(src) {
  return new Promise((resolve, reject) => {
    if (document.querySelector(`script[src="${src}"]`)) { resolve(); return; }
    const s = document.createElement('script');
    s.src = src;
    s.onload = () => resolve();
    s.onerror = () => reject(new Error(`Failed to load: ${src}`));
    document.head.appendChild(s);
  });
}

function loadCss(href) {
  if (document.querySelector(`link[href="${href}"]`)) return;
  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = href;
  document.head.appendChild(link);
}

async function ensurePptxLibs() {
  if (pptxLibsLoaded) return;

  // CSS required for correct slide rendering
  loadCss('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/css/pptxjs.css');
  loadCss('https://cdnjs.cloudflare.com/ajax/libs/nvd3/1.8.6/nv.d3.min.css');

  // jQuery 1.11.3 — PPTXjs was built for jQuery 1.x, does NOT work with 3.x
  await loadScript('https://code.jquery.com/jquery-1.11.3.min.js');

  // JSZip v2 (local copies)
  await loadScript('/libs/jszip.js');
  await loadScript('/libs/jszip-inflate.js');
  await loadScript('/libs/jszip-deflate.js');
  await loadScript('/libs/jszip-load.js');

  // Real filereader.js from CDN (PPTXjs needs this to read File objects)
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/filereader.js@master/filereader.js');

  // D3 + NVD3 for chart slides
  await loadScript('https://cdnjs.cloudflare.com/ajax/libs/d3/3.5.17/d3.min.js');
  await loadScript('https://cdnjs.cloudflare.com/ajax/libs/nvd3/1.8.6/nv.d3.min.js');

  // Dingbat font helper
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/js/dingbat.js');

  // PPTXjs core (local)
  await loadScript('/libs/pptxjs.js');

  // Slideshow plugin
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/js/divs2slides.js');

  pptxLibsLoaded = true;
}

// ─── PPTX conversion ──────────────────────────────────────────────────────────

function convertPptx(file) {
  return ensurePptxLibs().then(() => new Promise((resolve, reject) => {
    if (!window.$) { reject(new Error('jQuery failed to load.')); return; }
    // pptxjs calls file_name.split('.') internally — it MUST receive a string URL
    const blobUrl = URL.createObjectURL(file);
    const containerId = 'pptx-fc-' + Date.now() + '-' + Math.random().toString(36).slice(2);
    const container = document.createElement('div');
    container.id = containerId;
    Object.assign(container.style, {
      position: 'absolute',
      left: '-9999px',
      top: '-9999px',
      pointerEvents: 'none',
      width: '1280px',
    });
    document.body.appendChild(container);

    let settled = false;
    const cleanup = () => {
      if (container.parentNode) document.body.removeChild(container);
      window.removeEventListener('error', onWindowError);
      clearTimeout(timer);
      URL.revokeObjectURL(blobUrl);
    };

    const onWindowError = (evt) => {
      if (!settled && evt.message && evt.message.includes('pptx')) {
        settled = true;
        cleanup();
        reject(new Error(evt.message));
      }
    };
    window.addEventListener('error', onWindowError);

    const timer = setTimeout(() => {
      if (!settled) {
        settled = true;
        // partial success — return whatever rendered
        const clone = container.cloneNode(true);
        cleanup();
        resolve(clone);
      }
    }, 20000);

    try {
      window.$(container).pptxToHtml({
        pptxFileUrl: blobUrl,
        fileInputId: null,
        slidesScale: '100%',
        slideMode: false,
        keyBoardShortCut: false,
        afterRender: () => {
          if (!settled) {
            settled = true;
            const clone = container.cloneNode(true);
            cleanup();
            resolve(clone);
          }
        },
      });
    } catch (err) {
      settled = true;
      cleanup();
      reject(err);
    }
  }));
}

// ─── Styles ───────────────────────────────────────────────────────────────────

const S = {
  wrapper: {
    fontFamily: 'system-ui, sans-serif',
    maxWidth: '960px',
    margin: '0 auto',
    padding: '24px 16px',
  },
  dropZone: (dragging) => ({
    border: `2px dashed ${dragging ? '#0066cc' : '#aaa'}`,
    borderRadius: '12px',
    padding: '40px 24px',
    textAlign: 'center',
    cursor: 'pointer',
    background: dragging ? '#e8f0fe' : '#fafafa',
    transition: 'all 0.2s',
    userSelect: 'none',
  }),
  dropIcon: { fontSize: '2.5rem', marginBottom: '8px' },
  dropText: { fontSize: '1rem', color: '#555', margin: '4px 0' },
  dropHint: { fontSize: '0.8rem', color: '#999', marginTop: '6px' },
  statusBar: {
    display: 'flex',
    alignItems: 'center',
    gap: '10px',
    padding: '10px 14px',
    background: '#f0f4ff',
    borderRadius: '8px',
    margin: '16px 0',
    fontSize: '0.9rem',
  },
  spinner: {
    width: '18px',
    height: '18px',
    border: '2px solid #ccc',
    borderTop: '2px solid #0066cc',
    borderRadius: '50%',
    animation: 'fc-spin 0.8s linear infinite',
    flexShrink: 0,
  },
  clearBtn: {
    marginLeft: 'auto',
    background: 'none',
    border: '1px solid #bbb',
    borderRadius: '6px',
    padding: '3px 10px',
    cursor: 'pointer',
    fontSize: '0.8rem',
    color: '#555',
  },
  errorBox: {
    background: '#fff0f0',
    border: '1px solid #f99',
    borderRadius: '8px',
    padding: '12px 16px',
    color: '#c00',
    fontSize: '0.9rem',
    margin: '16px 0',
  },
  pdfPages: {
    display: 'flex',
    flexDirection: 'column',
    gap: '12px',
    marginTop: '12px',
  },
  pdfPage: {
    display: 'block',
    maxWidth: '100%',
    boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
    borderRadius: '4px',
  },
  pptxContainer: {
    marginTop: '12px',
    overflowX: 'auto',
  },
};

// ─── Component ────────────────────────────────────────────────────────────────

export default function FileConverter() {
  const [dragging, setDragging] = useState(false);
  const [fileName, setFileName] = useState('');
  const [status, setStatus] = useState('idle'); // idle | loading | done | error
  const [errorMsg, setErrorMsg] = useState('');
  const [pdfPages, setPdfPages] = useState(null);     // array of { pageNum, dataUrl, width }
  const pptxRef = useRef(null);
  const inputRef = useRef(null);

  const reset = () => {
    setFileName('');
    setStatus('idle');
    setErrorMsg('');
    setPdfPages(null);
    if (pptxRef.current) pptxRef.current.innerHTML = '';
  };

  const processFile = useCallback(async (file) => {
    if (!file) return;
    const name = file.name.toLowerCase();
    const isPptx = name.endsWith('.pptx');
    const isPdf = name.endsWith('.pdf');

    if (!isPptx && !isPdf) {
      alert('Please upload a .pptx or .pdf file.');
      return;
    }

    reset();
    setFileName(file.name);
    setStatus('loading');

    try {
      if (isPdf) {
        const pages = await convertPdf(file);
        setPdfPages(pages);
        setStatus('done');
      } else {
        const resultNode = await convertPptx(file);
        if (pptxRef.current) {
          pptxRef.current.innerHTML = '';
          // Reset the off-screen positioning from the render container
          resultNode.style.position = '';
          resultNode.style.left = '';
          resultNode.style.top = '';
          resultNode.style.pointerEvents = '';
          resultNode.style.width = '';
          pptxRef.current.appendChild(resultNode);
        }
        setStatus('done');
      }
    } catch (err) {
      setErrorMsg(err.message || 'Conversion failed.');
      setStatus('error');
    }
  }, []);

  const onFileChange = (e) => processFile(e.target.files[0]);

  const onDrop = useCallback((e) => {
    e.preventDefault();
    setDragging(false);
    processFile(e.dataTransfer.files[0]);
  }, [processFile]);

  const onDragOver = (e) => { e.preventDefault(); setDragging(true); };
  const onDragLeave = () => setDragging(false);

  return (
    <div style={S.wrapper}>
      {/* Keyframe injection */}
      <style>{`@keyframes fc-spin { to { transform: rotate(360deg); } }`}</style>

      {/* Drop Zone */}
      <div
        style={S.dropZone(dragging)}
        onClick={() => inputRef.current?.click()}
        onDrop={onDrop}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
      >
        <div style={S.dropIcon}>📂</div>
        <p style={S.dropText}>Drag &amp; drop a file here, or click to browse</p>
        <p style={S.dropHint}>Supported: .pptx, .pdf</p>
        <input
          ref={inputRef}
          type="file"
          accept=".pptx,.pdf"
          style={{ display: 'none' }}
          onChange={onFileChange}
        />
      </div>

      {/* Status bar */}
      {status !== 'idle' && (
        <div style={S.statusBar}>
          {status === 'loading' && <div style={S.spinner} />}
          {status === 'done'    && <span>✅</span>}
          {status === 'error'   && <span>❌</span>}
          <span>
            {status === 'loading' && `Converting ${fileName}…`}
            {status === 'done'    && `${fileName} — ready`}
            {status === 'error'   && `Failed: ${fileName}`}
          </span>
          <button style={S.clearBtn} onClick={reset}>Clear</button>
        </div>
      )}

      {/* Error detail */}
      {status === 'error' && (
        <div style={S.errorBox}>{errorMsg}</div>
      )}

      {/* PDF output */}
      {status === 'done' && pdfPages && (
        <div style={S.pdfPages}>
          {pdfPages.map((p) => (
            <img
              key={p.pageNum}
              src={p.dataUrl}
              alt={`Page ${p.pageNum}`}
              style={{ ...S.pdfPage, width: p.width }}
            />
          ))}
        </div>
      )}

      {/* PPTX output */}
      <div ref={pptxRef} style={S.pptxContainer} />
    </div>
  );
}
