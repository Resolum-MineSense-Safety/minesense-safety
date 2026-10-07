# Edge agent simulator (cabin gateway). Build context: src/edge
FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    EDGE_BUFFER_PATH=/app/data/offline-buffer.json

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY main.py .
COPY minesense_edge/ minesense_edge/

RUN useradd --create-home --uid 10001 edge \
    && mkdir -p /app/data \
    && chown -R edge:edge /app/data
USER edge

# The offline buffer survives container restarts when /app/data is a volume
VOLUME ["/app/data"]

CMD ["python", "main.py"]
