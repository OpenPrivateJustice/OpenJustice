# OpenJustice

## What This Is

A decentralized database of Brazilian historical crimes with full history tracking and confidence scores. The project consists of:
- **Generator (private)**: .NET Core 10 system for curating cases with API, web UI, RSS/Reddit scraping
- **Reader (public)**: Blazor WASM SPA for searching, filtering, and viewing cases with torrent-based distribution

**Core Value**: Decentralized, censorship-resistant database with complete historical transparency.

---

## Current Milestone: v2.0 BrazilExtractor

**Goal:** Create foundation for automated Brazilian court document extraction and criminal case identification.

**Target features:**
- Playwright scraping of TJGO publicacao page (projudi.tjgo.jus.br)
- PDF download from search results (15 per query, 30s interval)
- Local OCR to extract text from PDFs → .txt files
- Text analysis to identify criminal cases (crimes hediondos, homicide, traffic accidents as murder)
- Filter cases with "trânsito em julgado" (final judgment)
- Queue selected cases for admin evaluation in Generator.Web

**Shipped:** 2026-03-02
**Components:**
- Generator: API, Blazor UI, case management, curation workflow, RSS/Reddit discovery
- Reader: Blazor WASM SPA, torrent sync, local SQL, search/filters, case details with history timeline

**Tech Stack:**
- Database: PostgreSQL with EF Core
- Backend: .NET Core 10
- Frontend: Blazor WebAssembly
- Distribution: Torrent + SQL snapshots

---

## Context

**Codebase:** 
- Generator: ~2,500 LOC C# (API, services, Blazor UI)
- Reader: ~1,800 LOC C# (Blazor WASM components)
- Database: PostgreSQL with 8 tables, CaseFieldHistory for unlimited history

**Initial Testing Themes:**
- Users need clear case details with confidence scores
- History timeline is key value-add for verification
- Torrent distribution enables offline access

**Known Issues / Tech Debt:**
- Some Generator requirements still need full verification (GEN-01 through GEN-17)
- Reader search/filter UI needs user testing
- Build warnings in discovery services (RSS symbols)

---

## Requirements

### Validated (v1.0)

- ✓ GEN-04: Interface web SPA para inserção de casos — v1.0
- ✓ GEN-05: Interface web SPA para edição de casos existentes — v1.0
- ✓ GEN-09: RSS feed aggregator — v1.0
- ✓ GEN-10: Extração de dados de threads do Reddit — v1.0
- ✓ GEN-11: Sistema de aprovação/rejeição de casos — v1.0
- ✓ GEN-15: Compilação do banco PostgreSQL para snapshot SQL — v1.0
- ✓ GEN-18 through GEN-22: History system (full) — v1.0
- ✓ RDR-02 through RDR-04: Torrent download/sync — v1.0
- ✓ RDR-14 through RDR-20: Case details, sensitive content, sources — v1.0
- ✓ RDR-22, RDR-23: Timeline and diff — v1.0
- ✓ CFG-01 through CFG-06: Configuration system — v1.0
- ✓ DB-16: Composite indexes — v1.0

### Active (v2.0)

- [ ] EXTR-01: Create OpenJustice.BrazilExtractor project in solution
- [ ] EXTR-02: Playwright scraping of TJGO publicacao search (projudi.tjgo.jus.br)
- [ ] EXTR-03: PDF download from search results (15/query, 30s interval)
- [ ] EXTR-04: Local OCR text extraction from PDFs → .txt files
- [ ] EXTR-05: Text analysis for criminal case identification (crimes hediondos, homicide, traffic as murder)
- [ ] EXTR-06: Filter for "trânsito em julgado" (final judgment) cases
- [ ] EXTR-07: Queue cases for Generator.Web admin evaluation

### Out of Scope

- Other Brazilian courts (TJ/SP, TJ/RJ, TRFs) - future phases
- Real-time/streaming monitoring - batch processing only
- Automatic case registration - manual admin approval required
- Multi-country extractors - Brazil first, then expand

---

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| PostgreSQL como banco de dados | Suporte a dados relacionais complexos | ✅ v1.0 |
| .NET Core 10 para backend | Performance, suporte a longo prazo | ✅ v1.0 |
| Blazor WebAssembly para leitor | SPA moderno, execução local | ✅ v1.0 |
| Torrent para distribuição | Resistência à censura | ✅ v1.0 |
| Isolation de conteúdo sensível | IsSensitiveContent boolean | ✅ v1.0 |
| Sem API pública no MVP | Foco em distribuição estática | ✅ v1.0 |
| Sem autenticação | Acesso aberto | ✅ v1.0 |
| Gerador privado | Controle de qualidade | ✅ v1.0 |
| Histórico ilimitado por campo | CaseFieldHistory, append-only | ✅ v1.0 |
| Nível de confiança (0-100%) | Campo em cada dado | ✅ v1.0 |
| Timeline de alterações | UI no Blazor reader | ✅ v1.0 |
| Diff visual entre versões | UI no Blazor reader | ✅ v1.0 |
| Configuration via appsettings.json | IOptions pattern | ✅ v1.0 |
| Wire Reader to Generator API | Live history data | ✅ v1.0 |
| Wire torrent to SQL import | End-to-end flow | ✅ v1.0 |

---

## Visão Geral

OpenJustice é uma base de dados descentralizada de registros históricos de crimes cometidos no Brasil. O projeto consiste em dois componentes:

1. **Gerador (privado)**: Sistema para alimentar e curar a base de dados oficial
2. **Leitor (público)**: Aplicação SPA Blazor que permite acesso à base de dados com replicação via torrent

**Princípio fundamental**: Transparência absoluta. O OpenJustice é um repositório de fatos, sem julgamentos de valor — cabe aos usuários interpretar os dados.

---

*Last updated: March 2, 2026 after v2.0 milestone started*
