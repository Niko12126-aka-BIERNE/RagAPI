# RagAPI

A local RAG (Retrieval-Augmented Generation) API built with ASP.NET Core, llama.cpp, and Qdrant.

## Prerequisites

- Docker Desktop
- .NET 10 SDK
- An Anthropic API key (optional — only needed if using Claude as the LLM provider)

## Setup

### 1. Download models

Create a `GgufModels` folder inside the `RagAPI` project folder and download the following models:

**LLM**
- Model: `Qwen3-8B-Q4_K_M.gguf`
- Download: https://huggingface.co/Aldaris/Qwen3-8B-Q4_K_M-GGUF

**Embedding**
- Model: `qwen3-embedding-4b-q4_k_m.gguf`
- Download: https://huggingface.co/enacimie/Qwen3-Embedding-4B-Q4_K_M-GGUF

**Vision**
- Model: `Qwen2.5-VL-7B-Instruct-Q4_K_M.gguf`
- Download: https://huggingface.co/ggml-org/Qwen2.5-VL-7B-Instruct-GGUF

**Vision projector**
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

### 2. Configure API key (optional)

If using the Anthropic provider, add your API key to `secrets.json`:
```json
{
  "RagConfigurations": {
    "AnthropicApiKey": "your-key-here"
  }
}
```

### 3. Run with Docker
```bash
docker compose up --build
```

The API will be available at `http://localhost:5000/swagger`

### 4. Run locally for development

Update `appsettings.json` to use `localhost` URLs (already the default) and hit F5 in Visual Studio.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/rag/index` | Upload and index a file |
| GET | `/api/rag/query` | Query across indexed files |
| GET | `/api/rag/files` | List indexed files |
| GET | `/health` | Health check |

## Authentication

All endpoints require an `X-Api-Key` header. Set the key in `appsettings.json` under `Auth:ApiKey`.

## Supported file types

`.txt`, `.md`, `.pdf`, `.docx`, `.jpg`, `.jpeg`, `.png`, `.bmp`, `.tiff`

## LLM Providers

Switch providers in `appsettings.json` by setting `LlmProvider` to either `llamaserver` or `anthropic`.