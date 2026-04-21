"""
Marker OCR sidecar service.

Exposes a single endpoint:
    POST /marker  — accepts a PDF as multipart 'pdf_file', returns extracted markdown text.

Marker pipeline:
    1. Detect whether the page has a usable text layer (pdftext / surya OCR if not)
    2. Run layout detection and reading-order sorting (surya)
    3. Clean and format blocks (tables, equations, headers, etc.)
    4. Optionally use an LLM to improve table/math accuracy (disabled by default to keep startup fast)
    5. Return markdown string

Environment variables (all optional):
    MARKER_LLM_SERVICE   — if set to "ollama", enables LLM-enhanced accuracy
    OLLAMA_BASE_URL      — ollama URL (default: http://ollama:11434)
    OLLAMA_MODEL         — ollama model for refinement (default: llama3.2)
    TORCH_DEVICE         — force cpu/cuda/mps (auto-detected by default)
"""

import os
import io
import logging
import traceback
from pathlib import Path
import tempfile

from fastapi import FastAPI, File, HTTPException, UploadFile
from fastapi.responses import JSONResponse

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
logger = logging.getLogger(__name__)

app = FastAPI(title="Marker OCR Service", version="1.0.0")

# ── Lazy-load marker models once on first request ─────────────────────────────
_converter = None
_artifact_dict = None


def get_converter():
    global _converter, _artifact_dict
    if _converter is not None:
        return _converter

    logger.info("Loading Marker models (first request)...")
    from marker.converters.pdf import PdfConverter
    from marker.models import create_model_dict
    from marker.config.parser import ConfigParser

    llm_service_name = os.getenv("MARKER_LLM_SERVICE", "")

    config: dict = {
        "output_format": "markdown",
        # Force OCR on all pages — ensures scanned PDFs are processed correctly
        "force_ocr": True,
        # Strip existing low-quality OCR text and re-OCR with Surya
        "strip_existing_ocr": False,
    }

    llm_service = None
    if llm_service_name == "ollama":
        config["ollama_base_url"] = os.getenv("OLLAMA_BASE_URL", "http://ollama:11434")
        config["ollama_model"] = os.getenv("OLLAMA_MODEL", "llama3.2")
        llm_service_cls = "marker.services.ollama.OllamaService"
        config_parser = ConfigParser(config)
        _artifact_dict = create_model_dict()
        from marker.services.ollama import OllamaService
        llm_service = OllamaService(config_parser.generate_config_dict())
    else:
        config_parser = ConfigParser(config)
        _artifact_dict = create_model_dict()

    _converter = PdfConverter(
        config=config_parser.generate_config_dict(),
        artifact_dict=_artifact_dict,
        processor_list=config_parser.get_processors(),
        renderer=config_parser.get_renderer(),
        llm_service=llm_service,
    )
    logger.info("Marker models loaded successfully.")
    return _converter


# ── Routes ────────────────────────────────────────────────────────────────────

@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/marker")
async def extract_text(pdf_file: UploadFile = File(...)):
    """
    Accept a PDF file and return extracted text as markdown.
    Response: { "markdown": "...", "pages": N }
    """
    if not pdf_file.filename or not pdf_file.filename.lower().endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Only PDF files are accepted.")

    content = await pdf_file.read()
    if len(content) == 0:
        raise HTTPException(status_code=400, detail="Empty file.")

    logger.info("Processing PDF: %s (%d bytes)", pdf_file.filename, len(content))

    # Write to temp file — Marker operates on file paths
    with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as tmp:
        tmp.write(content)
        tmp_path = tmp.name

    try:
        converter = get_converter()
        rendered = converter(tmp_path)

        # rendered.markdown contains the extracted text with light markdown formatting
        markdown_text: str = rendered.markdown if hasattr(rendered, "markdown") else str(rendered)
        page_count: int = (
            len(rendered.metadata.get("page_stats", [])) if hasattr(rendered, "metadata") else 0
        )

        logger.info("Extracted %d chars from %d pages", len(markdown_text), page_count)
        return JSONResponse({"markdown": markdown_text, "pages": page_count})

    except Exception as exc:
        logger.error("Marker conversion failed:\n%s", traceback.format_exc())
        raise HTTPException(status_code=500, detail=f"OCR failed: {exc}") from exc
    finally:
        Path(tmp_path).unlink(missing_ok=True)
