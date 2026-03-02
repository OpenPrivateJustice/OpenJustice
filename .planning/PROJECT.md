# OpenJustice

## What This Is

A decentralized database of Brazilian historical crimes with full history tracking and confidence scores. The project consists of:
- **Generator (private)**: .NET Core 10 system for curating cases with API, web UI, RSS/Reddit scraping
- **Reader (public)**: Blazor WASM SPA for searching, filtering, and viewing cases with torrent-based distribution

**Core Value**: Decentralized, censorship-resistant database with complete historical transparency.

---

## Current State (v1.0 MVP Shipped)

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

### Active (Next Milestone)

- [ ] GEN-01 through GEN-03: Case API validation
- [ ] GEN-06 through GEN-08: Curation workflow, verified marker, audit log
- [ ] GEN-12, GEN-13: Evidence upload, tags
- [ ] GEN-14: Reference code generation (ATRO-YYYY-NNNN)
- [ ] GEN-16, GEN-17: Snapshot versioning, full config
- [ ] RDR-01: Blazor WASM executable
- [ ] RDR-05 through RDR-13: Search, filters, pagination, sorting
- [ ] RDR-21: Confidence display
- [ ] RDR-24 through RDR-27: Responsive UI, loading states, error handling
- [ ] DB-01 through DB-15, DB-17 through DB-22: Database schema completion

### Out of Scope

- API pública no MVP — será implementada em v2
- Sistema de autenticação — acesso aberto
- Conteúdo de vídeo completo — feature futura
- Funcionalidade de seed torrent no MVP — apenas download

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

*Last updated: March 2, 2026 after v1.0 milestone*
