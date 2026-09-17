# Notificações, métricas e persistência — D06/D07/D08

[Índice central](../../../PLANO_FASE_3.md)

> Decisões preservadas com suas datas. Para estado/etapa atual, consultar o índice central; complementos posteriores prevalecem sobre pendências antigas. Ler somente o assunto necessário.

<a id="d06-1"></a>

**D06.1 — Notificações mantidas na API; migração como melhoria futura (aceita em 2026-09-14):**

- **Origem e escopo:** usuário determinou que a migração não será implementada nesta entrega e deve ser anotada como melhoria futura para apresentação. A menção contextual do PDF não será tratada como tarefa adicional de migração.
- **Comportamento vigente:** manter dispatcher/handlers e envio SMTP no processo da API. Preservar funcionamento na nova rede e validar notificações nos fluxos de OS; observar falhas/latência com OpenTelemetry/New Relic. Não incluir função, fila ou infraestrutura de migração neste escopo.
- **Melhoria futura a apresentar:** separar o envio de notificações da API, avaliando processamento assíncrono/serverless para reduzir o impacto da latência e indisponibilidade do provedor no fluxo de OS. Acionamento, garantias de entrega, retentativas, idempotência, tratamento de falhas e custos serão definidos nessa evolução; não há escolha antecipada de serviços ou arquitetura.
- **Evidência na entrega:** incluir a decisão de escopo na documentação/RFC/ADR e um item de evolução futura na apresentação, distinguindo o envio atual implementado da migração proposta. Registrar benefícios esperados e principais pontos a definir, sem apresentar a melhoria como concluída.
- **Estado:** D06/P03 resolvidas e E0.6 concluída como definição; documentação/apresentação final pendentes em E7.11. E0 permanece em andamento.

<a id="d07-1"></a>

**D07.1 — Métricas e regras de cálculo (aceitas em 2026-09-14):**

**Origem:** usuário aceitou as propostas e solicitou adicionar o indicador de tempo total de atendimento, da abertura da OS até a conclusão do serviço. Esse indicador é adicional ao tempo de finalização/espera pela entrega e não substitui nenhum dashboard previsto.

Base local: `WorkOrderStatus` contém Received, InDiagnosis, WaitingForApproval, WaitingForExecution, InExecution, Finished e Delivered. `Order` guarda DateCreated/DateFinished, sem timestamps de cada transição; recusa também leva a Finished, sem preencher DateFinished. Criação/conclusão usam DateTime.Now. Não é possível deduzir as durações intermediárias a partir desses campos.

| Indicador | Regra aceita | Tratamento |
|---|---|---|
| Volume diário de OS | Contar criações persistidas com sucesso, uma vez por OS, no dia da criação | Volume significa OS abertas; exclusão/recusa posterior não reduz a contagem histórica |
| Tempo de diagnóstico | Entrada em InDiagnosis até saída para WaitingForApproval | Excluir tempo em Received e espera por aprovação |
| Tempo de execução | Entrada em InExecution até saída para Finished por serviço concluído | Excluir WaitingForExecution; recusa sem execução não é duração zero |
| Tempo de finalização | Entrada em Finished até Delivered | Permanência aguardando entrega. Separar conclusão de serviço e orçamento recusado por motivo de encerramento |
| Tempo total de atendimento | Criação da OS até entrada em Finished por conclusão do serviço | Inclui recebimento, diagnóstico, espera de aprovação, espera de execução e execução; exclui espera pela entrega. Somente serviços concluídos entram na média; recusas, OS abertas e exclusões anteriores à conclusão ficam fora |
| Falhas de integração | Quantidade e taxa por integração/operação, contando tentativas concluídas, incluindo timeout/exceção | Taxa = tentativas com falha / tentativas totais; zero tentativas produz sem dados. Cada retry é nova tentativa; falha técnica separada de resultado de negócio |

Regras comuns aceitas:

- **Relógio:** instantes em UTC; dias de calendário em America/Sao_Paulo, com intervalo [00:00, 00:00 do dia seguinte). Medir tempo corrido, inclusive noites/fins de semana e períodos entre sessões de deploy; não presumir horário comercial ou pausa do relógio quando o ambiente estiver desligado. Não converter datas antigas sem conhecer o fuso de origem.
- **Média:** soma das durações válidas / número de permanências concluídas no status; atribuir a amostra ao período em que saiu do status. OS ainda naquele status ficam fora da média e podem aparecer separadamente como idade atual. Sem amostras = sem dados, não zero; armazenar duração em segundos e exibir em minutos/horas.
- **Média do tempo total:** soma dos tempos entre criação e conclusão efetiva do serviço / quantidade de serviços concluídos com datas válidas; atribuir a amostra ao dia/período da conclusão, em America/Sao_Paulo. Calcular pelos instantes inicial/final da própria OS, não pela soma das médias dos status. Não requer aguardar Delivered. Preservar amostras concluídas mesmo após exclusão posterior; não inventar datas ausentes ou fuso de registros antigos.
- **Recusas e exclusões:** separar o motivo de entrada em Finished para não misturar espera de retirada após recusa com finalização de serviço executado. Etapas não percorridas ou intervalos interrompidos por exclusão não entram na média. Preservar contagens e amostras históricas válidas já concluídas. Isso não exige novo status; representação do motivo e persistência serão detalhadas em D08.
- **Dados:** registrar transições persistidas com instante/motivo, consistentes com a atualização da OS, para obter início/fim e conferir dashboards. Não reconstruir histórico de versões anteriores; banco nasce vazio conforme D08.1. Modelo físico e inicialização serão definidos em D08.2. Definir exportação/reconciliação em E4/E5 para evitar duplicação/perda; não prometer entrega exatamente uma vez apenas por emitir OTLP após commit. IDs podem servir à correlação/deduplicação sem virar dimensões de métricas de alta cardinalidade.
- **Integrações iniciais:** PostgreSQL/Aurora, SMTP, Secrets Manager e falhas Gateway -> Lambda/API; tratar separadamente falha de integração e falha da operação de OS. Falha de e-mail após persistir OS não desfaz a criação nem reduz seu volume. CPF inválido, cliente inelegível e orçamento recusado são resultados de negócio, não indisponibilidade técnica. Evitar contar a mesma falha em cada camada como se fossem integrações distintas.
- **Operação e alertas (E0.5/E5):** latência por rota (média/p95), taxa de erro técnico, CPU/memória por pod/nó, disponibilidade nas janelas ativas. Para demonstração, alertar quando ocorrer pelo menos uma falha técnica de processamento de OS em cinco minutos, com alerta de notificação separado. Destino, recuperação e parâmetros específicos dos demais alertas serão detalhados na configuração E5; não assumir SLA de produção. Manter filtro obrigatório por ambiente.

**Estado:** regras de negócio e critério de alerta para demonstração aceitos; E0.5/D07 concluídas como definição. Persistência, instrumentação, dashboards e validação pendentes em D08/E4/E5/E6. Nenhuma alteração no domínio, dados ou instrumentação foi feita.

<a id="d08-1"></a>

**D08.1 — Banco educacional descartável, sem migração de dados (aceita em 2026-09-14):**

- **Origem:** usuário esclareceu que todo o banco é apagado para reduzir custos e não há necessidade de migrações, por se tratar de projeto educacional.
- **Escopo:** dispensar migração/conversão/preservação de dados de versões anteriores, backfill de histórico e adoção de framework de migrações incrementais. Substituir por script versionado que cria o esquema final em banco vazio e seeds reproduzíveis para testes/apresentação. Não há obrigação de preservar dados entre recriações do ambiente.
- **Inicialização:** Terraform provisiona Aurora; definir na pipeline/job quem executa o SQL de criação e os seeds antes dos testes e uso da API/Lambda. Não executar criação/reset a cada requisição ou em cada réplica da API. Scripts de criação não devem apagar dados implicitamente durante reinícios ou deploys comuns; destruição/recriação é etapa explícita do ciclo do ambiente.
- **Métricas:** a preservação de histórico após exclusão individual de OS em D07.1 aplica-se enquanto o banco existir. Destruir o banco encerra esse conjunto de dados; telemetria já enviada ao New Relic segue sua própria retenção. Separar demonstrações por período/execução para evitar misturar bases recriadas e documentar cobertura; não presumir reconstrução dos dados pelo New Relic.
- **Estado:** migrações de dados retiradas do plano de implementação; modelo físico, inicialização e validação de banco vazio continuam necessários. Esta atualização é documental, sem execução de exclusão de recursos/dados.

<a id="d08-2"></a>

**D08.2 — Persistência com histórico de status (aceita em 2026-09-14):**

**Origem:** usuário aceitou as sugestões de persistência e solicitou remover os campos de datas existentes, pois os instantes passam a ser obtidos pelo histórico. Remover também a duração persistida redundante; duração continua sendo informação calculada para os indicadores.

1. **Histórico da OS:** criar tabela `order_status_history` com ID do evento, ID da OS, estado anterior/novo, instante UTC e motivo quando aplicável. Registrar criação como entrada em Received (estado anterior ausente) e cada transição efetivada; manter o estado atual em orders. Gravar criação/atualização e histórico na mesma transação e validar transição concorrente para não duplicar intervalos. Ordenação deve ser determinística mesmo quando instantes coincidirem; concretizar restrições no SQL/ER.
2. **Datas e encerramento:** remover `date_created`, `date_finished` e `duration` da tabela orders e os respectivos campos redundantes de persistência. Data de abertura deriva do evento de criação; conclusão deriva da entrada em Finished com motivo de serviço concluído; entrega deriva da entrada em Delivered. Guardar motivo no evento de encerramento para distinguir recusa, sem criar outro status. Usar instantes UTC; ausência de evento de conclusão significa conclusão ainda inexistente, sem data artificial. Calcular durações a partir desses instantes.
3. **Exclusão individual e histórico:** preservar eventos/amostras válidas quando uma OS for excluída, sem impedir o DELETE já existente. Não aplicar cascade ao histórico nem apagar seus identificadores de correlação. Detalhar o relacionamento SQL para permitir a retenção independentemente da linha atual de orders. O descarte integral do banco continua permitido pelo ciclo D08.1.
4. **Clientes/usuários e índices:** clientes ativos por padrão, identidade por UUID e perfis Admin/Mechanic nos novos registros/seeds. Reaproveitar UNIQUE de customers.document já existente; incluir índice do histórico por OS/instante e definir restrições para estados/motivos. Manter formato de CPF compatível com o domínio e a consulta da Lambda.
5. **Inicialização e demonstração:** atualizar o SQL existente e definir executor/ordem de criação, credencial de leitura da Lambda e seeds mínimos (Admin, Mechanic, cliente, veículo, catálogo/estoque). Gerar as OS de demonstração pelos fluxos da aplicação para produzir métricas reais; script e código precisam usar o mesmo contrato de esquema.

**Impacto nas consultas e contratos:** o repositório atual usa date_created para ordenação e as respostas de OS expõem DateCreated, DateFinished e Duration. Adaptar consultas e projeções para calcular esses valores pelo histórico quando expostos, preservando informação útil sem duplicá-la em orders. Remover armazenamento/setters redundantes do domínio e ajustar construtores, mapeamentos, seeds e testes. Representar conclusão ausente explicitamente no contrato, documentando a eventual mudança para campo nullable. Evitar uma consulta adicional por OS nas listagens; obter projeções agregadas em conjunto. A ordenação por abertura continua usando o evento de criação. Não exigir nova rota pública de histórico apenas para calcular os indicadores.

**Estado:** D08.1/D08.2 aceitas como decisões de persistência. Implementação SQL/ER, consultas, contratos e testes pendente em E2/E4/E6; execução da inicialização e propriedade dos artefatos seguem em E0.1/E0.4. Sem migração de dados, conforme D08.1.
