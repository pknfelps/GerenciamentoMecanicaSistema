# ADR 003 — Decisão histórica de Aurora e histórico transacional

**Estado:** escolha Aurora/auto-pause substituída pelo [ADR 007](007-RDS-FREE-PLAN.md) em 02/10/2026; histórico transacional e banco descartável implementados/validados em hom. Registro original: 16/09/2026.

**Contexto e mudança:** a proposta original usava Aurora Serverless v2 para uso intermitente. A criação na conta falhou com FreeTierRestrictionError, e o mantenedor escolheu RDS PostgreSQL provisionado. Não descrever Aurora, ACUs ou auto-pause como recursos entregues. A escolha final e suas consequências estão no [modelo/justificativa](../BANCO_E_MODELO.md).

**Decisão preservada:** orders mantém estado/orçamento, sem datas/duração redundantes. order_status_history registra UUID da OS, sequência, estados, UTC e motivo. Criação/transição/histórico e efeitos de estoque pertencem à transação; bloqueio da OS evita decisão com estado anterior. Sem FK do histórico para orders, exclusão individual preserva eventos; destruição do banco encerra esse histórico.

**Alternativas e limites:** duplicar datas na OS manteria fontes temporais concorrentes. Migração incremental/backfill e preservação de bancos legados foram dispensados para o banco educacional vazio/recriável. Não há CHECK/UNIQUE de sequência ou índice do histórico no SQL atual; a aplicação valida o encadeamento e os grants não impõem append-only. Sem garantia de desempenho em alto volume.

**Comprovação:** SQL direto, projeções, transações, recusa, retenção após DELETE e indicadores conhecidos conferidos em hom, com cenários locais adicionais delimitados. ER completo em [DADOS](../diagramas/DADOS.md); regras de medição na [RFC 003](../rfcs/003-DADOS-E-OBSERVABILIDADE.md).

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** escolha Aurora/auto-pause substituída pelo [ADR 007](007-RDS-FREE-PLAN.md) em 2026-10-02; decisões de histórico transacional e banco descartável preservadas. **Formalização original:** 2026-09-16. [Índice](../README.md)

**Contexto:** PostgreSQL existente, foco educacional/serverless e uso intermitente. Os indicadores exigem tempos por etapa e tratamento correto de recusas, exclusões e períodos desligados.

**Decisão:** Aurora PostgreSQL Serverless v2 Standard, um writer, capacidade inicial 0–2 ACUs e pausa elegível após 300 segundos, com versão compatível fixada na implementação. Manter probes/pooling da API; Lambda sem pooling, sem RDS Proxy. Banco descartável com SQL/seeds versionados. Histórico de status transacional substitui datas/duração redundantes persistidas em orders e preserva eventos de OS excluídas individualmente.

**Alternativas:** RDS PostgreSQL provisionado foi considerado e continua sendo comparação válida de custo. Banco no cluster não atende à escolha de serviço gerenciado. Migração incremental/backfill e manutenção de campos temporais duplicados foram dispensados para o banco educacional descartável.

**Consequências:** não há garantia de Aurora sempre mais barato: armazenamento, I/O, período ativo e serviços associados compõem o custo. Conexões/probes podem impedir pausa; encerrar consumidores fora da janela e fazer preflight antes da apresentação. SQL, projeções e consultas precisam refletir a nova fonte temporal; histórico não sobrevive à destruição completa do banco.

**Comprovação:** recriação em banco vazio, conectividade/readiness após ativação, transações e indicadores conhecidos em E2/E4/E6. ER e comparação final permanecem em E7. [RFC 003](../rfcs/003-DADOS-E-OBSERVABILIDADE.md).

</details>
