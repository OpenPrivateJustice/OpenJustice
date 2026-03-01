# AtrocidadesRSS

## Visão Geral

AtrocidadesRSS é uma base de dados descentralizada de registros históricos de crimes cometidos no Brasil. O projeto consiste em dois componentes:

1. **Gerador (privado)**: Sistema para alimentar e curar a base de dados oficial
2. **Leitor (público)**: Aplicação SPA Blazor que permite acesso à base de dados com replicação via torrent

**Princípio fundamental**: Transparência absoluta. O AtrocidadesRSS é um repositório de fatos, sem julgamentos de valor — cabe aos usuários interpretar os dados.

## Propósito

- **Preservação histórica**: Manter registro detalhado de crimes no Brasil
- **Transparência**: Disponibilizar informações completas e verificáveis
- **Descentralização**: Replicação via torrent para garantir acesso contínuo e resistência à censura
- **Documentação**: Criar um histórico público acessível a todos os brasileiros

## Escopo do MVP

### Componentes em escopo

#### Gerador (privado)
- Interface web para inserção manual de casos
- Sistema de curadoria e aprovação de registros
- Levantamento automático de potenciais casos via scraping de RSS feeds (incluindo subreddit)
- Validação de dados antes da inserção
- Compilação do banco de dados PostgreSQL para snapshot SQL
- Geração de versão atualizada para distribuição

#### Leitor (público)
- Aplicação SPA Blazor (executa localmente no navegador do usuário)
- Interface de busca e filtros por:
  - Nome do acusado/vítima
  - Tipo de crime
  - Estado/localização
  - Período de tempo
  - Status judicial
- Visualização detalhada de cada caso com todos os campos disponíveis
- Download da base de dados completa via torrent
- Verificação de novas versões via torrent
- Tratamento de conteúdo sensível (IsSensitiveContent boolean)
- Videos não incluídos no MVP (isolados para implementação futura)

### Componentes fora do escopo (MVP v1)

- Exportação de dados para formatos externos
- API pública para consulta
- Sistema de autenticação de usuários
- Conteúdo de vídeo completo
- Funcionalidade de seed torrent (apenas download)

## Requisitos Técnicos

### Tecnologias

| Componente | Tecnologia |
|------------|------------|
| Banco de dados | PostgreSQL |
| Backend (Gerador) | .NET Core 10 |
| Frontend (Leitor) | Blazor WebAssembly SPA |
| Distribuição | Torrent + snapshot SQL |
| Hospedagem | Configurável via appsettings.json |

### Estrutura do Banco de Dados

Campos principais para cada registro de crime:

**Identificação**
- ID único (auto-generated)
- Código de referência (ATRO-AAAA-NNNN)
- Data do registro no sistema
- Data do crime
- Data da denúncia/reportagem
- Última atualização

**Vítima**
- Nome (anonimizado ou completo)
- Gênero
- Idade na época do crime
- Nacionalidade
- Profissão/ocupação (se relevante)
- Relação com o acusado

**Acusado**
- Nome completo
- Nome social (se aplicável)
- Gênero
- Idade na época do crime
- Nacionalidade
- Profissão/ocupação
- CPF/RG (se disponível)
- Endereço/localização
- Relacionamento com vítima

**Detalhes do Crime**
- Tipo de crime (assassinato, estupro, pedofilia, etc.)
- Subtipo/especificação
- Data e hora estimada
- Local exato (endereço, cidade, estado)
- Coordenadas geográficas (opcional)
- Descrição detalhada dos fatos
- Modalidade (tentativa/consumado)
- Nº de vítimas
- Nº de acusados
- Arma utilizada (se aplicável)
- Motivação (se conhecida)
- Premeditação (sim/não/desconhecido)

**Informações Jurídicas**
- Status judicial (investigação, processo, condenado, absolvido, arquivado)
- Nº do processo
- Vara/órgão judiciário
- Comarca/estado
- Fase atual do processo
- Data da denúncia
- Data da sentença
- Pena aplicada (se condenado)
- Recursos pendentes

**Fontes e Evidências**
- Fonte primária (nome do subreddit, thread URL)
- Data da postagem
- Link para discussão original
- Nº de upvotes/comentários
- Evidências mencionadas
- Testemunhas identificadas
- Perícias realizadas
- Notas de curadoria

**Classificação**
- Tags (tipos de crime, locais, etc.)
- Categoria principal
- IsSensitiveContent (boolean)
- Verificado (sim/não)
- Status de anonimização

## Decisões de Arquitetura

### Descentralização via Torrent
- O leitor baixa a base de dados completa via torrent
- Verificação de versão via torrent para detectar atualizações
- Usuário pode se tornar seed após download completo
- Arquivo SQL serve como snapshot do banco PostgreSQL

### Separação Gerador/Leitor
- Gerador permanece privado (uso exclusivo do curador)
- Leitor é open source (disponível via GitHub)
- Código fonte do leitor totalmente acessível

### Transparência como princípio
- Todos os dados brutos disponíveis
- Sem filtragem ou censura de conteúdo
- Status judicial claramente indicado para cada caso
- Fontes originais vinculadas a cada registro

### Configuração flexível
- Todas as configurações de hospedagem em appsettings.json
- Suporte a diferentes ambientes de deploy

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| PostgreSQL como banco de dados | Suporte a dados relacionais complexos, consultas flexíveis | Implementado no MVP |
| .NET Core 10 para backend | Performance, suporte a longo prazo, ecossistema maduro | Implementado no MVP |
| Blazor WebAssembly para leitor | SPA moderno, execução local do usuário, código open source | Implementado no MVP |
| Torrent para distribuição | Resistência à censura, descentralização, eficiência | Implementado no MVP |
| Isolation de conteúdo sensível | Proteção de usuários, controle de exposição | IsSensitiveContent boolean implementado |
| Sem API pública no MVP | Foco em distribuição estática, simplicidade | Fora do escopo MVP |
| Sem autenticação | Acesso aberto a todos, transparência total | Fora do escopo MVP |
| Sem conteúdo de vídeo no MVP | Complexidade técnica, storage issues | Fora do escopo MVP |
| Gerador privado | Controle de qualidade, curadoria humana | Implementado como sistema privado |

---

*Last updated: March 1, 2026 after initialization*