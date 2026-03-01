# AtrocidadesRSS - Requirements v1

## v1 Requirements

### Gerador - Sistema de Curadoria (GEN)

#### API & Backend
- [ ] **GEN-01**: API RESTful para inserção manual de casos via .NET Core 10
- [ ] **GEN-02**: Validação de dados obrigatórios antes da inserção
- [ ] **GEN-03**: Validação de consistência (datas, relacionamentos, formatos)
- [ ] **GEN-04**: Interface web SPA para inserção de casos
- [ ] **GEN-05**: Interface web SPA para edição de casos existentes
- [ ] **GEN-06**: Sistema de curadoria e aprovação de registros
- [ ] **GEN-07**: Marcador de "Verificado" por curador
- [ ] **GEN-08**: Histórico de alterações (audit log) por caso

#### Coleta de Dados
- [ ] **GEN-09**: RSS feed aggregator para levantar potenciais casos
- [ ] **GEN-10**: Extração de dados de threads do Reddit (subreddit)
- [ ] **GEN-11**: Sistema de aprovação/rejeição de casos levantados automaticamente
- [ ] **GEN-12**: Upload/associação de evidências (links, documentos)
- [ ] **GEN-13**: Tags e categorização de casos

#### Exportação
- [ ] **GEN-14**: Geração automática de código de referência (ATRO-AAAA-NNNN)
- [ ] **GEN-15**: Compilação do banco PostgreSQL para snapshot SQL
- [ ] **GEN-16**: Sistema de versionamento de snapshots (v1.sql, v2.sql, etc.)
- [ ] **GEN-17**: Configuração via appsettings.json (banco, conexão, etc.)

#### Histórico de Alterações
- [ ] **GEN-18**: Sistema de histórico por campo (CaseFieldHistory)
- [ ] **GEN-19**: Registro de todas as alterações de cada campo (valor anterior, valor novo, data, curador)
- [ ] **GEN-20**: Campo de nível de confiança (0-100%) para cada informação
- [ ] **GEN-21**: UI para visualização do histórico de alterações de cada campo
- [ ] **GEN-22**: UI para diff visual entre versões de um campo

### Leitor - Aplicação Pública (RDR)

#### SPA Blazor WebAssembly
- [ ] **RDR-01**: Aplicação SPA Blazor executável localmente no navegador
- [ ] **RDR-02**: Download da base de dados completa via torrent
- [ ] **RDR-03**: Verificação de novas versões via torrent
- [ ] **RDR-04**: Carregamento de banco SQL local
- [ ] **RDR-05**: Configuração via appsettings.json (torrent tracker, etc.)

#### Interface de Busca
- [ ] **RDR-06**: Busca por nome (acusado/vítima) com fuzzy matching
- [ ] **RDR-07**: Filtros por tipo de crime
- [ ] **RDR-08**: Filtros por estado/localização
- [ ] **RDR-09**: Filtros por período de tempo
- [ ] **RDR-10**: Filtros por status judicial
- [ ] **RDR-11**: Combinação de múltiplos filtros simultâneos
- [ ] **RDR-12**: Paginação de resultados
- [ ] **RDR-13**: Ordenação por diferentes campos

#### Visualização de Casos
- [ ] **RDR-14**: Visualização detalhada de cada caso com todos os campos
- [ ] **RDR-15**: Tratamento de conteúdo sensível (IsSensitiveContent boolean)
- [ ] **RDR-16**: Warning para conteúdo sensível antes de exibir
- [ ] **RDR-17**: Exibição de fontes e links originais
- [ ] **RDR-18**: Exibição de evidências (links para fotos/dados)
- [ ] **RDR-19**: Exibição de informações jurídicas (nº processo, status, etc.)
- [ ] **RDR-20**: Exibição de metadados (data registro, verificado, tags)
- [ ] **RDR-21**: Exibição de nível de confiança (0-100%) para cada campo
- [ ] **RDR-22**: Timeline de histórico de alterações por campo
- [ ] **RDR-23**: Diff visual entre versões de cada campo
- [ ] **RDR-24**: Interface responsiva (mobile/desktop)

#### UI/UX
- [ ] **RDR-25**: Loading states para operações assíncronas
- [ ] **RDR-26**: Error handling para falhas de download/parse
- [ ] **RDR-27**: Navegação por breadcrumbs/back button

### Banco de Dados (DB)

#### Schema & Modelos
- [ ] **DB-01**: Tabela `Cases` com todos os campos definidos no PROJECT.md
- [ ] **DB-02**: Tabela `CrimeType` (tipo de crime)
- [ ] **DB-03**: Tabela `CaseType` (modalidade - tentativa/consumado)
- [ ] **DB-04**: Tabela `JudicialStatus` (status judicial)
- [ ] **DB-05**: Tabela `Sources` (fontes/reportagens)
- [ ] **DB-06**: Tabela `Evidence` (evidências vinculadas)
- [ ] **DB-07**: Tabela `Tags` (categorização)
- [ ] **DB-08**: Relacionamento many-to-many entre Cases e Tags
- [ ] **DB-09**: Tabela `CaseFieldHistory` (histórico ilimitado por campo)
- [ ] **DB-10**: Campos de nível de confiança em tabelas principais

#### Índices & Performance
- [ ] **DB-11**: Índices para busca eficiente por nome (acusado/vítima)
- [ ] **DB-12**: Índices para filtragem por tipo de crime
- [ ] **DB-13**: Índices para filtragem por estado/localização
- [ ] **DB-14**: Índices para filtragem por data do crime
- [ ] **DB-15**: Índices para filtragem por status judicial
- [ ] **DB-16**: Índices compostos para queries combinadas

#### Integridade & Manutenção
- [ ] **DB-17**: Constraints de integridade referencial entre tabelas
- [ ] **DB-18**: NOT NULL constraints para campos obrigatórios
- [ ] **DB-19**: DEFAULT values para campos opcionais
- [ ] **DB-20**: Migrações para versionamento do schema
- [ ] **DB-21**: Backup/restore procedures
- [ ] **DB-22**: Geração de snapshot SQL exportável

### Configuração (CFG)

#### App Settings
- [ ] **CFG-01**: appsettings.json para configuração de conexão PostgreSQL
- [ ] **CFG-02**: appsettings.json para configuração de paths de arquivos
- [ ] **CFG-03**: appsettings.json para configuração de torrent (tracker, ports)
- [ ] **CFG-04**: Suporte a appsettings.Development.json
- [ ] **CFG-05**: Validação de configurações obrigatórias no startup
- [ ] **CFG-06**: Documentação de todas as opções de configuração

---

## v2 Requirements (Deferred)

### Gerador
- [ ] Importação em massa de dados de formatos externos (CSV, Excel)
- [ ] Sistema de notificações para curadores
- [ ] Dashboard de estatísticas de curadoria
- [ ] Exportação de dados para múltiplos formatos

### Leitor
- [ ] API pública para integração de terceiros (v2 feature)
- [ ] Sistema de autenticação de usuários
- [ ] Contas de usuário com preferências salvas
- [ ] Sistema de comentários/discussão por caso
- [ ] Estatísticas e dashboards públicos
- [ ] Conteúdo de vídeo completo (streaming)

### Infraestrutura
- [ ] Funcionalidade de seed torrent (usuários podem servir)
- [ ] CDN para distribuição de snapshots
- [ ] Monitoramento de saúde do sistema
- [ ] Automação de deploy

---

## Out of Scope (Explicit Exclusions)

- [ ] **API pública no MVP** - Será implementada em v2
- [ ] **Sistema de autenticação** - Acesso aberto a todos, sem login
- [ ] **Conteúdo de vídeo completo** - Vídeos isolados para implementação futura
- [ ] **Funcionalidade de seed torrent no MVP** - Apenas download, seed vem depois
- [ ] **Exportação de dados** - Fora do escopo inicial
- [ ] **Sistema de notificações** - Usuários verificam atualizações manualmente via torrent
- [ ] **Estatísticas e dashboards** - Feature v2
- [ ] **Sistema de comentários** - Não haverá interação entre usuários por caso

---

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| GEN-01 | TBD | Not Mapped |
| GEN-02 | TBD | Not Mapped |
| GEN-03 | TBD | Not Mapped |
| GEN-04 | TBD | Not Mapped |
| GEN-05 | TBD | Not Mapped |
| GEN-06 | TBD | Not Mapped |
| GEN-07 | TBD | Not Mapped |
| GEN-08 | TBD | Not Mapped |
| GEN-09 | TBD | Not Mapped |
| GEN-10 | TBD | Not Mapped |
| GEN-11 | TBD | Not Mapped |
| GEN-12 | TBD | Not Mapped |
| GEN-13 | TBD | Not Mapped |
| GEN-14 | TBD | Not Mapped |
| GEN-15 | TBD | Not Mapped |
| GEN-16 | TBD | Not Mapped |
| GEN-17 | TBD | Not Mapped |
| GEN-18 | TBD | Not Mapped |
| GEN-19 | TBD | Not Mapped |
| GEN-20 | TBD | Not Mapped |
| GEN-21 | TBD | Not Mapped |
| GEN-22 | TBD | Not Mapped |
| RDR-01 | TBD | Not Mapped |
| RDR-02 | TBD | Not Mapped |
| RDR-03 | TBD | Not Mapped |
| RDR-04 | TBD | Not Mapped |
| RDR-05 | TBD | Not Mapped |
| RDR-06 | TBD | Not Mapped |
| RDR-07 | TBD | Not Mapped |
| RDR-08 | TBD | Not Mapped |
| RDR-09 | TBD | Not Mapped |
| RDR-10 | TBD | Not Mapped |
| RDR-11 | TBD | Not Mapped |
| RDR-12 | TBD | Not Mapped |
| RDR-13 | TBD | Not Mapped |
| RDR-14 | TBD | Not Mapped |
| RDR-15 | TBD | Not Mapped |
| RDR-16 | TBD | Not Mapped |
| RDR-17 | TBD | Not Mapped |
| RDR-18 | TBD | Not Mapped |
| RDR-19 | TBD | Not Mapped |
| RDR-20 | TBD | Not Mapped |
| RDR-21 | TBD | Not Mapped |
| RDR-22 | TBD | Not Mapped |
| RDR-23 | TBD | Not Mapped |
| RDR-24 | TBD | Not Mapped |
| RDR-25 | TBD | Not Mapped |
| RDR-26 | TBD | Not Mapped |
| RDR-27 | TBD | Not Mapped |
| DB-01 | TBD | Not Mapped |
| DB-02 | TBD | Not Mapped |
| DB-03 | TBD | Not Mapped |
| DB-04 | TBD | Not Mapped |
| DB-05 | TBD | Not Mapped |
| DB-06 | TBD | Not Mapped |
| DB-07 | TBD | Not Mapped |
| DB-08 | TBD | Not Mapped |
| DB-09 | TBD | Not Mapped |
| DB-10 | TBD | Not Mapped |
| DB-11 | TBD | Not Mapped |
| DB-12 | TBD | Not Mapped |
| DB-13 | TBD | Not Mapped |
| DB-14 | TBD | Not Mapped |
| DB-15 | TBD | Not Mapped |
| DB-16 | TBD | Not Mapped |
| DB-17 | TBD | Not Mapped |
| DB-18 | TBD | Not Mapped |
| DB-19 | TBD | Not Mapped |
| DB-20 | TBD | Not Mapped |
| DB-21 | TBD | Not Mapped |
| DB-22 | TBD | Not Mapped |
| CFG-01 | TBD | Not Mapped |
| CFG-02 | TBD | Not Mapped |
| CFG-03 | TBD | Not Mapped |
| CFG-04 | TBD | Not Mapped |
| CFG-05 | TBD | Not Mapped |
| CFG-06 | TBD | Not Mapped |

*Traceability section auto-populated by roadmap creation*

---

*Last updated: March 1, 2026 after requirements definition*