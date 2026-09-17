# ADR 003 — Aurora Serverless e histórico transacional de OS

**Estado:** aceito como arquitetura; implementação pendente. **Formalização:** 2026-09-16. **Origem:** [D01.2](../../plano-fase-3/decisoes/INFRAESTRUTURA.md), [D07/D08](../../plano-fase-3/decisoes/NEGOCIO_E_PERSISTENCIA.md). [Índice](../README.md)

**Contexto:** PostgreSQL existente, foco educacional/serverless e uso intermitente. Os indicadores exigem tempos por etapa e tratamento correto de recusas, exclusões e períodos desligados.

**Decisão:** Aurora PostgreSQL Serverless v2 Standard, um writer, capacidade inicial 0–2 ACUs e pausa elegível após 300 segundos, com versão compatível fixada na implementação. Manter probes/pooling da API; Lambda sem pooling, sem RDS Proxy. Banco descartável com SQL/seeds versionados. Histórico de status transacional substitui datas/duração redundantes persistidas em orders e preserva eventos de OS excluídas individualmente.

**Alternativas:** RDS PostgreSQL provisionado foi considerado e continua sendo comparação válida de custo. Banco no cluster não atende à escolha de serviço gerenciado. Migração incremental/backfill e manutenção de campos temporais duplicados foram dispensados para o banco educacional descartável.

**Consequências:** não há garantia de Aurora sempre mais barato: armazenamento, I/O, período ativo e serviços associados compõem o custo. Conexões/probes podem impedir pausa; encerrar consumidores fora da janela e fazer preflight antes da apresentação. SQL, projeções e consultas precisam refletir a nova fonte temporal; histórico não sobrevive à destruição completa do banco.

**Comprovação:** recriação em banco vazio, conectividade/readiness após ativação, transações e indicadores conhecidos em E2/E4/E6. ER e comparação final permanecem em E7. [RFC 003](../rfcs/003-DADOS-E-OBSERVABILIDADE.md).
