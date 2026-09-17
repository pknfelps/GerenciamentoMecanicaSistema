# Snapshot do banco para desenvolvimento local

O esquema/seeds pertence ao [repositório de banco](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados). Esta cópia permite executar Docker Compose com apenas o checkout da aplicação. Não editar `Init.sql` aqui: alterar a origem e importar um commit fixo.

`source.json` registra origem, commit completo, caminho e SHA-256. O SQL usa UTF-8 sem BOM e LF; o CI verifica sua integridade sem acessar outro repositório. A verificação offline detecta divergência da cópia com o manifesto; a importação lê o conteúdo do commit no clone de origem.

Na raiz da aplicação, com PowerShell 7:

```powershell
./scripts/Sync-DatabaseSnapshot.ps1 -Verify
./scripts/Sync-DatabaseSnapshot.ps1 -DatabaseRepository ../GerenciamentoMecanicaBancoDados -Commit <SHA-completo>
```

O script só lê um commit já disponível no clone local: não faz fetch, não executa SQL e não altera volumes. O PostgreSQL do Compose aplica o SQL somente na primeira inicialização de um volume vazio. Esta distribuição preserva o esquema da Fase 2; alterações de clientes/histórico da Fase 3 pertencem a E2/E4.
