# OCR Tracking SQLite - Consultas de Auditoria

## Localização do Banco

O banco de dados SQLite está localizado em:
```
src/OpenJustice.BrazilExtractor.Web/data/ocr_tracking.db
```

## Schema do Banco

### Tabela: OcrPageRecords

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| Id | INTEGER | PK auto-increment |
| ExecutionDate | TEXT | Data da execução (YYYY-MM-DD) |
| PdfPath | TEXT | Caminho completo do PDF |
| PageNumber | INTEGER | Número da página (1-based) |
| Status | INTEGER | 0=Pending, 1=Success, 2=Failed, 3=Skipped |
| Provider | INTEGER | 0=LlamaCpp, 1=OpenAI |
| ImageHash | TEXT | SHA256 do PNG (opcional) |
| CharactersExtracted | INTEGER | Caracteres extraídos |
| ErrorMessage | TEXT | Mensagem de erro (se falhar) |
| StartedAt | TEXT | Timestamp de início (UTC) |
| CompletedAt | TEXT | Timestamp de conclusão (UTC) |
| CreatedAt | TEXT | Timestamp de criação (UTC) |
| UpdatedAt | TEXT | Timestamp de atualização (UTC) |

### Índices

- `IX_OcrPageRecords_CompositeKey` - Única (ExecutionDate + PdfPath + PageNumber)
- `IX_OcrPageRecords_ExecutionDate` - Por data de execução
- `IX_OcrPageRecords_Status` - Por status
- `IX_OcrPageRecords_PdfPath` - Por caminho do PDF
- `IX_OcrPageRecords_Pending` - Composto (ExecutionDate + Status)

---

## Exemplos de Consultas SQL

### 1. Verificar se uma página específica foi processada

```sql
SELECT * FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND PdfPath LIKE '%processo_12345.pdf' 
  AND PageNumber = 1 
  AND Status = 1;
```

### 2. Listar todas as páginas de um PDF específico

```sql
SELECT PageNumber, Status, Provider, CharactersExtracted, CompletedAt
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND PdfPath LIKE '%processo_12345.pdf'
ORDER BY PageNumber;
```

### 3. Resumo de status de um PDF

```sql
SELECT 
    COUNT(*) as total,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as success,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as failed,
    SUM(CASE WHEN Status != 1 THEN 1 ELSE 0 END) as pending
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND PdfPath LIKE '%processo_12345.pdf';
```

### 4. Resumo de status de um dia inteiro

```sql
SELECT 
    COUNT(*) as total,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as success,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as failed,
    SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) as skipped,
    SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) as pending
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02';
```

### 5. Listar páginas que falharam

```sql
SELECT PdfPath, PageNumber, ErrorMessage, CompletedAt
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND Status = 2
ORDER BY PdfPath, PageNumber;
```

### 6. Listar PDFs com processamento pendente

```sql
SELECT DISTINCT PdfPath
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND Status != 1;
```

### 7. Verificar progresso por provider

```sql
SELECT 
    Provider,
    COUNT(*) as total,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) as success,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) as failed
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02'
GROUP BY Provider;
```

### 8. Páginas processadas recentemente (últimas 24h)

```sql
SELECT * FROM OcrPageRecords 
WHERE CompletedAt >= datetime('now', '-1 day')
ORDER BY CompletedAt DESC
LIMIT 50;
```

### 9. Contagem de caracteres extraídos por PDF

```sql
SELECT 
    PdfPath,
    SUM(CharactersExtracted) as total_chars,
    COUNT(*) as total_pages
FROM OcrPageRecords 
WHERE ExecutionDate = '2026-03-02' 
  AND Status = 1
GROUP BY PdfPath
ORDER BY total_chars DESC;
```

---

## Usando SQLite no Terminal

### Abrir o banco

```bash
sqlite3 <caminho>/data/ocr_tracking.db
```

### Executar uma consulta

```bash
sqlite3 <caminho>/data/ocr_tracking.db "SELECT * FROM OcrPageRecords WHERE ExecutionDate = '2026-03-02' LIMIT 10;"
```

### Exportar para CSV

```bash
sqlite3 -header -csv <caminho>/data/ocr_tracking.db "SELECT * FROM OcrPageRecords WHERE ExecutionDate = '2026-03-02';" > output.csv
```

---

## Status Enum Reference

| Valor | Nome | Descrição |
|-------|------|-----------|
| 0 | Pending | Processamento pendente ou em andamento |
| 1 | Success | Processado com sucesso |
| 2 | Failed | Falha no processamento |
| 3 | Skipped | Página ignorada (ex: limite atingido) |

## Provider Enum Reference

| Valor | Nome | Descrição |
|-------|------|-----------|
| 0 | LlamaCpp | Llama.cpp com visão |
| 1 | OpenAI | API OpenAI Vision |
