# ============================================================
#  Marker OCR Sidecar — Dockerfile
#  GPU-accelerated when NVIDIA Container Toolkit is available.
#  Falls back to CPU automatically if no GPU is present.
# ============================================================

# Use PyTorch base image — includes CUDA runtime for GPU acceleration
FROM pytorch/pytorch:2.3.1-cuda12.1-cudnn8-runtime

WORKDIR /app

# System deps for marker-pdf (poppler for PDF rendering, libgomp for OpenMP)
RUN apt-get update && apt-get install -y --no-install-recommends \
    poppler-utils \
    libgomp1 \
    libgl1-mesa-glx \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Install Python dependencies
# pip install in layers for better caching:
#   1. fastapi/uvicorn first (change rarely)
#   2. marker-pdf last (large, changes more)
COPY requirements.txt .
RUN pip install --no-cache-dir fastapi>=0.115.0 uvicorn[standard]>=0.30.0 python-multipart>=0.0.9
RUN pip install --no-cache-dir marker-pdf[full]

# Pre-download Marker/Surya model weights at build time
# so the container is ready immediately on first request.
# This makes the image larger (~3-4 GB) but avoids a slow cold start.
RUN python -c "from marker.models import create_model_dict; create_model_dict()" || true

COPY app.py .

# Marker uses /tmp for processing — ensure it's writable
ENV TMPDIR=/tmp
# Auto-select GPU if available; override with TORCH_DEVICE=cpu for CPU-only
ENV TORCH_DEVICE=auto

EXPOSE 8001

HEALTHCHECK --interval=30s --timeout=10s --start-period=120s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8001/health')"

ENTRYPOINT ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8001", "--workers", "1"]
