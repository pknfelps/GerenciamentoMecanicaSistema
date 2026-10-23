# ADR 007 — RDS PostgreSQL no ambiente educacional

**Estado:** decisão aceita em 02/10/2026; RDS 16.15 e fluxo integrado comprovados em hom. Substitui Aurora/auto-pause do [ADR 003](003-BANCO-E-HISTORICO.md), preservando histórico/banco descartável.

**Contexto histórico:** a criação Aurora Serverless v2 desta conta falhou com FreeTierRestrictionError. O mantenedor rejeitou mudar de plano para viabilizar aquela topologia e escolheu RDS PostgreSQL db.t3.micro. Isso registra a decisão tomada; não define elegibilidade geral de todas as contas ou tarifas atuais.

**Decisão implementada:** uma instância RDS PostgreSQL 16.15/db.t3.micro Single-AZ, 20 GiB gp3 criptografados, porta 5432 e acesso privado pelos SGs dos consumidores. Minor fixo/auto-upgrade desabilitado, backup de um dia, sem réplica, deletion_protection=false e skip_final_snapshot=true. TLS VerifyFull com CA RDS incluída nos runtimes/Job; Lambda sem pooling e sem RDS Proxy. Ver [justificativa/modelo](../BANCO_E_MODELO.md).

SSM database/v2 publica endpoint/porta/database/ssl-mode/schema/hash e referências de credenciais, inclusive instance-arn; não confundir com cluster-name/arn do EKS. Job inicializa banco vazio/marker e grants. API não usa administrador nem inicializa DDL no startup.

**Alternativas e consequências:** preservar PostgreSQL/SQL/Dapper evita remodelar as transações existentes. RDS provisionado não pausa/escala a zero automaticamente. O ciclo econômico escolhido é destruir/recriar dados educacionais, não manter armazenamento/backups gratuitos ou alta disponibilidade produtiva. db.t3.micro/nó único foram avaliados em fluxos funcionais, sem prova de capacidade sob carga.

**Comprovação:** disponibilidade, rede/TLS, esquema/Job/grants, API/Lambda/JWT, histórico SQL e descarte hom registrados na [E6](../../REVISAO_E6_2026-10-21.md). [Encerramento/custos](../../ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md) documenta snapshot manual antigo e persistências; o mantenedor confirmou a exclusão desse snapshot em 22/10/2026, sem nova consulta AWS pelo agente. Não se alega ausência total de cobrança ou de dados históricos. Prd/coexistência e carga não testados. Não executar recuperação/limpeza de estado como parte desta revisão.

Referência: [RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html). O registro original de transição fica no histórico abaixo.

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** aceito como decisão de arquitetura em 2026-10-02; implementação e validação pendentes. [Índice](../README.md). Substitui somente a escolha Aurora/auto-pause do [ADR 003](003-BANCO-E-HISTORICO.md); suas decisões sobre histórico transacional e banco descartável permanecem.

**Contexto:** o deploy aprovado de Aurora Serverless v2 em hom falhou em `CreateDBCluster` com `FreeTierRestrictionError`. A conta Free Plan permite apenas Aurora Express, que não atende à topologia de VPC privada, subnet group e security groups do projeto. O usuário rejeitou migrar para Paid Plan e escolheu RDS PostgreSQL `db.t3.micro`.

**Decisão:** uma instância RDS for PostgreSQL `db.t3.micro` Single-AZ por ambiente, em subnets privadas de duas AZs, com DB subnet group e acesso 5432 limitado aos SGs reais dos consumidores. Manter criptografia em repouso, Secrets Manager, TLS com validação completa do certificado, SQL/seeds versionados, probes e health checks atuais. Proposta inicial para Terraform: 20 GiB gp3, backup de um dia, sem réplica, proteção contra exclusão desativada e sem snapshot final para dados de demonstração descartáveis. Fixar o minor PostgreSQL 16 somente após consultar a oferta da região e os parâmetros aceitos pela instância. O contrato do banco passará a publicar `instance-arn`; os campos `cluster-name` e `cluster-arn` da base continuam representando o EKS.

**Operação e custos:** RDS não pausa automaticamente. Uma parada manual é temporária e não interrompe cobrança de armazenamento/backups; após até sete dias a instância reinicia automaticamente. Fora das janelas de teste, destruir e recriar banco e base na ordem documentada. Créditos e prazo do Free Plan são compartilhados com EKS, NAT e outros serviços; medir consumo real. O tamanho micro exige testar CPU, memória, conexões, HPA e concorrência da Lambda antes de fechar capacidade. Inicialmente sem RDS Proxy; revisar pooling da Lambda com dados de teste.

**Transição:** o apply anterior deixou cinco recursos no estado de hom: `terraform_data.base_release`, DB subnet group, SG e duas regras de ingresso. Revisar e aprovar o plano exato de destroy pelo workflow antigo; verificar estado vazio e ausência dos recursos na AWS. Só depois revisar Terraform, policies IAM, scripts, workflow e checks de ausência para `aws_db_instance`. Preservar aprovação, recapture da base e separação hom/prd. Não alterar state manualmente em caso de divergência; investigar antes de continuar.

**Comprovação:** plan limpo para cada ambiente, disponibilidade da versão/classe e IAM efetivo; apply aprovado, endpoint privado e TLS `VerifyFull`, SQL/seeds e `database/v1/release` validados; teste integrado da API/Lambda e destruição sem remanescentes. O [registro de replanejamento](../../plano-fase-3/RESULTADO_E2_2_REPLANEJAMENTO_RDS.md) acompanha pendências.

Fontes: [RDS e Free Tier](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Welcome.html), [criação em VPC](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_CreateDBInstance.html), [parada temporária](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_StopInstance.html), [Free Plan](https://docs.aws.amazon.com/awsaccountbilling/latest/aboutv2/free-tier-plans.html).

</details>
