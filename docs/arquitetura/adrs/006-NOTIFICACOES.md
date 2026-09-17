# ADR 006 — Manter notificações na API nesta entrega

**Estado:** aceito. **Formalização:** 2026-09-16. **Origem:** [D06.1](../../plano-fase-3/decisoes/NEGOCIO_E_PERSISTENCIA.md), decisão de escopo do usuário. [Índice](../README.md)

**Contexto:** o envio de notificações já ocorre por eventos tratados na API. A migração desse fluxo não foi considerada obrigatória para a entrega e ampliaria a implementação.

**Decisão:** preservar envio SMTP na API e apresentar migração para processamento serverless/assíncrono como melhoria futura. A função implementada nesta fase é a validação de CPF. Não foi escolhido broker, fila ou serviço de notificação futuro.

**Alternativa:** migrar agora permitiria tratar envio independentemente da requisição, mas exigiria definir entrega, retries, idempotência e operação de novos componentes.

**Consequências:** envio continua consumindo tempo no processamento atual; sua falha após commit não desfaz a OS. Instrumentar tentativas/erros separadamente e remover dados pessoais dos logs de integração. Não alegar que notificações já são assíncronas ou serverless na apresentação.

**Comprovação:** preservar comportamento funcional e verificar falha de SMTP sem duplicação/reversão de OS em E4/E6; documentar melhoria em E7.11. [Sequência de abertura](../diagramas/ABERTURA_OS.md).
