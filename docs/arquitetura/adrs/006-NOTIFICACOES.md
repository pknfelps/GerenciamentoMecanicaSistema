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
