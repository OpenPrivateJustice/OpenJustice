# MEMORY

## 2026-03-03

### Erro
Assumi que a paginação avançaria após `QueryIntervalSeconds`, mas `MaxResultsPerQuery` (15) estava limitando a coleta antes da próxima página.

### Padrão
Conflito entre regra de paginação e limite de resultados pode mascarar o comportamento esperado de navegação entre páginas.

### Regra preventiva
Sempre validar se parâmetros de cap (`MaxResultsPerQuery`) estão compatíveis com o cenário de paginação. Para extração completa, usar `0 = ilimitado` e tratar isso explicitamente no código e na validação.
