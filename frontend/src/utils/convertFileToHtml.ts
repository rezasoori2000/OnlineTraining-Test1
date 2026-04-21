/**
 * convertFileToHtml.ts
 *
 * Converts a PDF or PPTX/PPT file to an HTML string.
 * Logic adapted from /contentconverter-example/fileconverter.jsx.
 *
 * PDF  → rendered via pdfjs-dist; each page becomes a base64 <img>.
 * PPTX → rendered via pptxjs (jQuery plugin); slides captured as innerHTML.
 */

import * as pdfjs from 'pdfjs-dist'

// Use the CDN worker matching the installed pdfjs-dist version
pdfjs.GlobalWorkerOptions.workerSrc = `https://unpkg.com/pdfjs-dist@${pdfjs.version}/build/pdf.worker.min.mjs`

const RENDER_SCALE = 1.5

// ── PDF ────────────────────────────────────────────────────────────────────────
//
// Strategy:
//   1.  Try PDF.js getTextContent() — works for digitally-created PDFs (Word/Office exports).
//       If extracted text is substantial (>150 chars) → wrap in <p> tags and return.
//       This text is readable in the Portal AND indexable by the embedding pipeline.
//
//   2.  Fallback — canvas rendering (image-only PDFs / display only, not indexed in Qdrant).

const MIN_TEXT_CHARS = 150 // below this we consider the PDF image-based

/**
 * Extract text layer from all pages using PDF.js getTextContent.
 * Returns empty string if the PDF has no embedded text.
 */
async function extractPdfTextLayer(pdf: pdfjs.PDFDocumentProxy): Promise<string> {
  const pageTexts: string[] = []
  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i)
    const content = await page.getTextContent()
    const pageText = content.items
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      .map((item: any) => item.str as string)
      .join(' ')
    pageTexts.push(pageText)
  }
  return pageTexts.join('\n\n').trim()
}

/** Render all PDF pages to canvas and return HTML with base64 <img> tags. */
async function renderPdfToImages(pdf: pdfjs.PDFDocumentProxy): Promise<string> {
  const imgTags: string[] = []
  for (let i = 1; i <= pdf.numPages; i++) {
    const page = await pdf.getPage(i)
    const viewport = page.getViewport({ scale: RENDER_SCALE })
    const canvas = document.createElement('canvas')
    canvas.width = viewport.width
    canvas.height = viewport.height
    const ctx = canvas.getContext('2d')
    if (!ctx) throw new Error('Could not get canvas context.')
    await page.render({ canvasContext: ctx, viewport }).promise
    imgTags.push(
      `<img src="${canvas.toDataURL('image/png')}" ` +
        `style="width:${viewport.width}px;max-width:100%;display:block;margin-bottom:8px;" ` +
        `alt="Page ${i}" />`,
    )
  }
  return `<div class="pdf-content">${imgTags.join('\n')}</div>`
}

/** Wrap plain text into semantic HTML paragraphs. */
function textToHtml(text: string): string {
  const paragraphs = text
    .split(/\n{2,}/)
    .map((p) => p.replace(/\n/g, ' ').trim())
    .filter((p) => p.length > 0)
    .map((p) => `<p>${p}</p>`)
  return `<div class="pdf-text-content">${paragraphs.join('\n')}</div>`
}

async function convertPdfToHtml(file: File): Promise<string> {
  const data = await file.arrayBuffer()
  const pdf = await pdfjs.getDocument({ data }).promise

  // ── Step 1: try text layer (Word / Office exports always have one) ────────
  const textLayerContent = await extractPdfTextLayer(pdf)
  if (textLayerContent.length >= MIN_TEXT_CHARS) {
    return textToHtml(textLayerContent)
  }

  // ── Step 2: fallback — canvas images (display only, not indexable) ────────
  console.warn(
    '[convertPdfToHtml] No text layer found. Falling back to image rendering. ' +
      'Content will NOT be indexed in the RAG search database.',
  )
  return renderPdfToImages(pdf)
}

// ── PPTX ───────────────────────────────────────────────────────────────────────

let pptxLibsLoaded = false

function loadScript(src: string): Promise<void> {
  return new Promise((resolve, reject) => {
    if (document.querySelector(`script[src="${src}"]`)) {
      resolve()
      return
    }
    const script = document.createElement('script')
    script.src = src
    script.onload = () => resolve()
    script.onerror = () => reject(new Error(`Failed to load script: ${src}`))
    document.head.appendChild(script)
  })
}

function loadCss(href: string): void {
  if (document.querySelector(`link[href="${href}"]`)) return
  const link = document.createElement('link')
  link.rel = 'stylesheet'
  link.href = href
  document.head.appendChild(link)
}

async function ensurePptxLibs(): Promise<void> {
  if (pptxLibsLoaded) return

  // CSS required for correct slide rendering
  loadCss('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/css/pptxjs.css')
  loadCss('https://cdnjs.cloudflare.com/ajax/libs/nvd3/1.8.6/nv.d3.min.css')

  // jQuery 1.11.3 — PPTXjs was built for jQuery 1.x, does NOT work with 3.x
  await loadScript('https://code.jquery.com/jquery-1.11.3.min.js')

  // JSZip v2 (local copies in public/libs)
  await loadScript('/libs/jszip.js')
  await loadScript('/libs/jszip-inflate.js')
  await loadScript('/libs/jszip-deflate.js')
  await loadScript('/libs/jszip-load.js')

  // Real filereader.js from CDN (PPTXjs needs this to read File objects)
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/filereader.js@master/filereader.js')

  // D3 + NVD3 for chart slides
  await loadScript('https://cdnjs.cloudflare.com/ajax/libs/d3/3.5.17/d3.min.js')
  await loadScript('https://cdnjs.cloudflare.com/ajax/libs/nvd3/1.8.6/nv.d3.min.js')

  // Dingbat font helper
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/js/dingbat.js')

  // PPTXjs core (local)
  await loadScript('/libs/pptxjs.js')

  // Slideshow plugin
  await loadScript('https://cdn.jsdelivr.net/gh/meshesha/PPTXjs@master/js/divs2slides.js')

  pptxLibsLoaded = true
}

function convertPptxToHtml(file: File): Promise<string> {
  return ensurePptxLibs().then(
    () =>
      new Promise((resolve, reject) => {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const $ = (window as any).$ as
          | ((el: Element) => { pptxToHtml: (opts: Record<string, unknown>) => void })
          | undefined

        if (!$) {
          reject(new Error('jQuery failed to load.'))
          return
        }

        const containerId = `pptx-${Date.now()}-${Math.random().toString(36).slice(2)}`
        const container = document.createElement('div')
        container.id = containerId
        Object.assign(container.style, {
          position: 'absolute',
          left: '-9999px',
          top: '0',
          width: '1280px',
          pointerEvents: 'none',
        })
        document.body.appendChild(container)

        // Create a blob URL so pptxjs can fetch the file via JSZipUtils (bundled in pptxjs.js)
        const blobUrl = URL.createObjectURL(file)
        let settled = false

        const cleanup = () => {
          clearTimeout(timer)
          URL.revokeObjectURL(blobUrl)
          if (container.parentNode) document.body.removeChild(container)
        }

        // 20 s timeout — accept partial render
        const timer = setTimeout(() => {
          if (!settled) {
            settled = true
            const html = container.innerHTML
            cleanup()
            resolve(`<div class="pptx-content ">${html}</div>`)
          }
        }, 20_000)

        try {
          $(container).pptxToHtml({
            pptxFileUrl: blobUrl,
            fileInputId: null,
            slidesScale: '100%',
            slideMode: false,
            keyBoardShortCut: false,
            afterRender: () => {
              if (!settled) {
                settled = true
                const html = container.innerHTML
                cleanup()
                resolve(`<div class="pptx-content ">${html}</div>`)
              }
            },
          })
        } catch (err) {
          settled = true
          cleanup()
          reject(err instanceof Error ? err : new Error('PPTX conversion failed.'))
        }
      }),
  )
}

// ── Public API ─────────────────────────────────────────────────────────────────

export async function convertFileToHtml(file: File): Promise<string> {
  const ext = file.name.split('.').pop()?.toLowerCase() ?? ''

  if (ext === 'pdf') return convertPdfToHtml(file)
  if (ext === 'ppt' || ext === 'pptx') return convertPptxToHtml(file)

  throw new Error(`Unsupported file type ".${ext}". Only PDF and PPT/PPTX are accepted.`)
}
