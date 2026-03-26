# RagAPI

A local RAG (Retrieval-Augmented Generation) API built with ASP.NET Core, llama.cpp, and Qdrant.

## Prerequisites

- Docker Desktop
- .NET 10 SDK
- An Anthropic API key (optional — only needed if using Claude as the LLM provider)

## Setup

### 1. Download models

Create a `GgufModels` folder inside the `RagAPI` project folder and download the following models:

**LLM** (only needed for local mode)
- Model: `Qwen3-8B-Q4_K_M.gguf`
- Download: https://huggingface.co/Aldaris/Qwen3-8B-Q4_K_M-GGUF

**Embedding** (always required)
- Model: `qwen3-embedding-4b-q4_k_m.gguf`
- Download: https://huggingface.co/enacimie/Qwen3-Embedding-4B-Q4_K_M-GGUF

**Vision** (only needed for local mode)
- Model: `Qwen2.5-VL-7B-Instruct-Q4_K_M.gguf`
- Download: https://huggingface.co/ggml-org/Qwen2.5-VL-7B-Instruct-GGUF

**Vision projector** (only needed for local mode)
- Model: `mmproj-Qwen2.5-VL-7B-Instruct-f16.gguf`
- Download: https://huggingface.co/ggml-org/Qwen2.5-VL-7B-Instruct-GGUF

Place all files in:
```
RagAPI/
└── GgufModels/
    ├── Qwen3-8B-Q4_K_M.gguf
    ├── qwen3-embedding-4b-q4_k_m.gguf
    ├── Qwen2.5-VL-7B-Instruct-Q4_K_M.gguf
    └── mmproj-Qwen2.5-VL-7B-Instruct-f16.gguf
```

### 2. Configure environment

Create a `.env` file in the solution root (same folder as `docker-compose.yml`):
```
ANTHROPIC_API_KEY=your-key-here
```

This file is gitignored and never committed. It is required even if you are not using Anthropic — just leave the value empty in that case.

For local development in Visual Studio, add your Anthropic API key to `secrets.json` instead:
```json
{
  "RagConfigurations": {
    "AnthropicApiKey": "your-key-here"
  }
}
```

### 3. Run with Docker

**Cloud mode** — uses Claude for LLM generation, runs only the embedding model locally (~3GB RAM):
```bash
docker compose up --build
```

**Local mode** — runs all models locally including LLM and vision (~13GB RAM):
```bash
docker compose --profile local up --build
```

The API will be available at `http://localhost:5000/swagger`

### 4. Switch LLM provider

In `appsettings.json` set `LlmProvider` to either `llamaserver` or `anthropic`:
```json
"LlmProvider": "anthropic"
```

Use `llamaserver` when running in local mode, `anthropic` when running in cloud mode.

### 5. Run locally for development

Set `LlmProvider` and URLs in `appsettings.json` to use `localhost` (already the default) and hit F5 in Visual Studio. Docker must still be running for Qdrant and llama-embed.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/rag/index` | Upload and index a file |
| GET | `/api/rag/query` | Query across indexed files |
| GET | `/api/rag/files` | List indexed files |
| GET | `/health` | Health check |

### Query parameters

`GET /api/rag/query?question=...&filename=...`

- `question` — required, the question to ask
- `filename` — optional, restricts search to a specific indexed file

## Authentication

All endpoints except `/health` and `/swagger` require an `X-Api-Key` header. Set the key in `appsettings.json` under `Auth:ApiKey`.

## Supported file types

| Type | Method |
|------|--------|
| `.txt`, `.md` | Direct text extraction |
| `.pdf` | Text extraction, falls back to OCR for scanned pages |
| `.docx` | Paragraph extraction |
| `.jpg`, `.jpeg`, `.png`, `.bmp`, `.tiff` | OCR for text images, vision LLM for photos |

## LLM Providers

| Provider | Setting | Notes |
|----------|---------|-------|
| llama-server | `llamaserver` | Fully local, requires local mode Docker profile |
| Anthropic Claude | `anthropic` | Requires API key in `.env` or `secrets.json` |

## Architecture

The system runs as five Docker containers:

| Container | Always runs | Profile |
|-----------|-------------|---------|
| qdrant | Yes | — |
| llama-embed | Yes | — |
| ragapi | Yes | — |
| llama-llm | No | `local` |
| llama-vision | No | `local` |