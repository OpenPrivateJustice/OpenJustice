# OpenJustice BrazilExtractor - OCR Tracking com EF Core + SQLite

## Entregáveis implementados

- Persistência de OCR por página em SQLite via EF Core.
- Serviço de domínio `IOcrTrackingService` (sem acesso direto ao `DbContext` nos OCR services).
- Integração com:
  - `LlamaCppVisionOcrService`
  - `OpenAiVisionOcrService`
  - `ExtractionManager` (scan OCR Only DB-first)
- Migrations EF criadas e aplicadas no startup com `Database.MigrateAsync()`.
- Logs explícitos de página já processada no banco e pulada.
- Append incremental no TXT por página preservado.
- `Run Extraction` continua sem OCR automático.

## Schema

Tabela: `OcrPageRecords`

- `Id` (PK)
- `ExecutionDate` (data da pasta)
- `PdfPath`
- `PageNumber`
- `Status` (`Pending`, `Success`, `Failed`, `Skipped`)
- `Provider` (`LlamaCpp`, `OpenAI`)
- `ImageHash` (SHA-256 opcional)
- `CharactersExtracted` (opcional)
- `ErrorMessage` (opcional)
- `StartedAt`, `CompletedAt`, `CreatedAt`, `UpdatedAt`

Índices:
- único: `(ExecutionDate, PdfPath, PageNumber)`
- `ExecutionDate`
- `Status`
- `PdfPath`
- `(ExecutionDate, Status)`

## Migrations

Arquivos:
- `Data/Migrations/20260303035156_InitialOcrTracking.cs`
- `Data/Migrations/20260303035156_InitialOcrTracking.Designer.cs`
- `Data/Migrations/OcrTrackingDbContextModelSnapshot.cs`

## Fluxo de retomada

1. OCR Only faz scan de PDFs do dia.
2. Para cada PDF, consulta o banco:
   - se já há histórico no banco: decisão é DB-first
   - se não há histórico: fallback legado por TXT (compatibilidade)
3. No OCR por página:
   - se página já está `Success` no banco, pula
   - registra `Pending` no início
   - ao concluir, atualiza para `Success` ou `Failed`
4. Cancelar/retomar continua apenas páginas pendentes pelo banco.

## Build validado

```bash
dotnet build src/OpenJustice.BrazilExtractor.Web/OpenJustice.BrazilExtractor.Web.csproj
```

Resultado: sucesso (sem erros).
