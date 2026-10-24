# ADR 006 — Conteúdo na origem e transporte centralizado

**Consolidação de 23/10/2026:** [ADR 011](011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md) é a decisão vigente; implementação/comprovação local nas AE.5–AE.9 e [comprovação AWS/hom concluída na AE.10](../../AE10_VALIDACAO_HOM_2026-10-23.md). O aceite anterior de SMTP local/simulação hom é histórico e não foi reaberto.

API preserva handler/EmailService/IEmailSender e regras/conteúdo das mensagens. Auth monta o código de acesso. Ambas invocam EmailFunction síncrona por IAM; somente a função email tem MIME/MailKit/SMTP, sem duplicar transporte nos consumidores. Alias live do ambiente/contrato v1, sem endpoint público, retry próprio ou captura de conteúdo em logs.

Captura SMTP4dev privada/efêmera é usada em todos os modos, sem entrega externa/relay ou volume persistente. Operador consulta a caixa via port-forward autorizado; não recebe código pela resposta de autenticação ou por endpoint do mecânico. mode=smtp indica captura, não entrega pessoal; histórico simulated preservado.

Notificação após commit da OS continua sendo aguardada e sua falha tratada não desfaz a operação. Auth só ativa desafio após captured; falha tenta invalidar sem reembolsar limite. Sem outbox/fila, recuperação automática, processamento independente da requisição ou entrega única. Interrupção/timeout pode perder ou duplicar mensagem. [Sequência](../diagramas/ABERTURA_OS.md) e [AE.9](../../AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md).

## Evolução futura para a E7.11

O transporte em função serverless já foi implementado. A evolução futura é tornar o processamento das notificações de OS independente da resposta HTTP, reduzindo a espera e permitindo tratamento controlado de indisponibilidade. Ainda exige decisões sobre publicação confiável após commit, idempotência, retentativas, mensagens que falham, operação e custos. Não há fila/outbox ou tecnologia adicional escolhida nesta entrega. Usar Task/await ou invocar Lambda sincronamente não equivale a esse desacoplamento. A autenticação mantém sua regra de só liberar o desafio após captura; não presumir mudança automática para o mesmo fluxo futuro.

Essa diferença deve ser explicada no vídeo. O texto está consolidado; gravação/publicação continuam pendentes. [Consolidação da entrega](../../CONSOLIDACAO_ENTREGA_2026-10-23.md).

<details>
<summary>Histórico da decisão anterior — E4/E5/E6, sem vigência no código AE</summary>

# ADR 006 — Notificações na API e envio simulado na AWS

**Estado:** aceito e validado em hom. Formalização original: 16/09/2026; decisão de SMTP local/simulação AWS incorporada na E4.

**Contexto:** o handler de eventos já consulta dados, monta e envia notificações no processo da API. Migração para outro serviço ampliaria o escopo.

**Decisão vigente:** manter handler/EmailService/IEmailSender. Sem Runtime__Environment, SMTP/MailKit e smtp4dev local. Em hom/prd, LoggingEmailSender retorna após registrar somente `Envio de e-mail simulado.`; não configura cliente/EmailSettings SMTP nem entrega ao destinatário. Seleção é pelo modo runtime validado, não pela máquina ou ASPNETCORE_ENVIRONMENT.

Após commit, OrdersService publica e aguarda o handler; este consulta cliente/veículo e monta a mensagem. Falhas são tratadas com mensagem fixa/tipo, sem stack trace/dados de destinatário e sem desfazer a OS. Métrica de notificação registra a tentativa concluída, inclusive falha de consulta/montagem, por tipo/outcome/mode. Sucesso simulated não significa entrega de negócio.

**Alternativa futura:** mover o trabalho para processamento serverless/assíncrono, desacoplando tempo da resposta e permitindo retentativas próprias. Ainda será necessário decidir fila/evento, idempotência, retries, entrega e operação; nenhuma tecnologia foi escolhida nem implementada. Não chamar o handler atual de processamento assíncrono independente apenas por usar Task/await.

**Consequências e prova:** sem outbox/reenvio/garantia única. Falha tratada após commit não desfaz a OS, mas interrupção pode perder a notificação. Fluxos simulados e falha controlada/alerta foram conferidos em hom; envio SMTP real na AWS foi retirado do escopo. A melhoria futura precisa ser apresentada na documentação/vídeo da E7.11. [Sequência atual](../diagramas/ABERTURA_OS.md).

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados. [Arquitetura](../README.md) · [Evidências E6](../../REVISAO_E6_2026-10-21.md).

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** aceito. **Formalização:** 2026-09-16. [Índice](../README.md)

**Contexto:** o envio de notificações já ocorre por eventos tratados na API. A migração desse fluxo não foi considerada obrigatória para a entrega e ampliaria a implementação.

**Decisão:** preservar envio SMTP na API e apresentar migração para processamento serverless/assíncrono como melhoria futura. A função implementada nesta fase é a validação de CPF. Não foi escolhido broker, fila ou serviço de notificação futuro.

**Alternativa:** migrar agora permitiria tratar envio independentemente da requisição, mas exigiria definir entrega, retries, idempotência e operação de novos componentes.

**Consequências:** envio continua consumindo tempo no processamento atual; sua falha após commit não desfaz a OS. Instrumentar tentativas/erros separadamente e remover dados pessoais dos logs de integração. Não alegar que notificações já são assíncronas ou serverless na apresentação.

**Comprovação:** preservar comportamento funcional e verificar falha de SMTP sem duplicação/reversão de OS em E4/E6; documentar melhoria em E7.11. [Sequência de abertura](../diagramas/ABERTURA_OS.md).

</details>

## Extra AE.7 — implementação local de 22/10/2026

[Envio centralizado e acesso Customer](../../AE7_API_ENVIO_E_ACESSO_CUSTOMER.md): API usa Invoke v1 em todos os modos, sem transporte SMTP próprio ou logging de simulação. Cadastro Active/titularidade são conferidos nas operações Customer, orçamento aceita somente approved booleano e retorna decisão após commit, e ClockSkew é zero. Falhas de notificação continuam tratadas após persistência. Captura SMTP4dev não comprova entrega externa; mode=smtp identifica a captura central. Preparado/comprovado localmente com 1.212 testes; publicação/Gateway e aceite AWS permanecem nas etapas extras posteriores, sem reabrir aceites oficiais.

</details>
