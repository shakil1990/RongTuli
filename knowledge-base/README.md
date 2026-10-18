# RongTuli Knowledge Base (AI / RAG)

Content and configuration used for retrieval-augmented generation (e.g. a shopping assistant answering questions about products, shipping, returns and cash-on-delivery).

## Pipeline

```
sources/  ->  ingest (clean + chunk)  ->  processed/  ->  embed  ->  index/  ->  retrieve  ->  LLM
```

| Folder | Purpose | Committed |
|--------|---------|-----------|
| `sources/` | Human-authored source documents (Markdown with front matter) | yes |
| `config/` | Chunking, embedding and retrieval settings | yes |
| `schemas/` | JSON Schema for document metadata | yes |
| `eval/` | Golden question/answer set for retrieval and answer quality | yes |
| `scripts/` | Ingest, embed and evaluate scripts | yes |
| `processed/` | Cleaned documents and chunks (generated) | no |
| `index/` | Vector store artifacts / exports (generated) | no |

## Authoring rules

- One topic per file, `kebab-case.md`, under the matching `sources/` subfolder.
- Every file starts with front matter validated by `schemas/document.schema.json`.
- Write self-contained passages; avoid "see above". Keep sections short and headed.
- No secrets, customer data or personal information.
- Changing a source requires re-running ingest and `eval/`.

## Storage

The planned production store is PostgreSQL with `pgvector` (Neon), so the index lives with the existing database. `index/` is for local dev and exports only.
