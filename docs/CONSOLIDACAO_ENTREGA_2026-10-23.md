# Consolidação da entrega — Fase 3

**23/10/2026.** Estado atual após o encerramento da extra AE. Este documento reúne as fontes da entrega; os checklists permanecem nos arquivos das etapas do plano local. Não substitui resultados históricos nem representa nova validação remota.

## Estado e fontes

| Escopo | Estado e evidência |
|---|---|
| Repositórios, CI/CD, infraestrutura e Gateway — E1–E3 | Implementados e comprovados no escopo de hom; checklists reconciliados com E5/E6 e AE. Não certifica prd ou gatilho automático após merge |
| API, autorização e histórico — E4 | Concluída em hom; regras Customer atualizadas pela AE. [Revisão integrada anterior](REVISAO_E6_2026-10-21.md) e [fluxo atual](AE10_VALIDACAO_HOM_2026-10-23.md) |
| Observabilidade — E5 | [Encerrada por aceite](ENCERRAMENTO_E5_2026-10-21.md); limitações e testes dispensados preservados |
| Evidências operacionais e custos — E6 | [Encerrada no escopo acordado em 21/10](ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md). Descarte/custos daquela data são históricos; hom foi recriado para AE/apresentação |
| Autenticação por código e envio central — AE | [Fluxo AWS](AE10_VALIDACAO_HOM_2026-10-23.md) e [SQL, inativação e limites](AE10_CONFERENCIAS_FINAIS_HOM_2026-10-23.md) concluídos em hom |
| Documentação — E7.1–E7.6 | Revisão de 21/10 complementada com a arquitetura AE, evidências de 23/10 e nome Serverless. Visibilidade pública comprovada em 21/10; renomeação de 22/10 preserva identidade do repositório |
| Entrega final — E7.7–E7.11 | Vídeo, publicação, PDF e conferência/submissão ainda pendentes. Conteúdo documental da evolução de notificações consolidado; apresentação no vídeo ainda falta |

## Quatro repositórios da entrega

| Repositório | Responsabilidade |
|---|---|
| [GerenciamentoMecanicaSistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema) | API, regras de OS, autorização, contratos e documentação arquitetural |
| [GerenciamentoMecanicaServerless](https://github.com/pknfelps/GerenciamentoMecanicaServerless) | AuthFunction e EmailFunction, dois SAMs/stacks, ZIPs e workflows próprios/reutilizáveis |
| [GerenciamentoMecanicaBancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados) | RDS, esquema/seeds para banco vazio, Job de inicialização e grants |
| [GerenciamentoMecanicaInfraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura) | Bootstrap, EKS/rede/NLB/SMTP4dev, Gateway e Collectors; New Relic persistente |

Autenticação é o nome histórico do repositório agora chamado Serverless; não constitui um quinto repositório. Links/SHAs de evidências datadas permanecem como histórico.

## Arquitetura a apresentar

- Cliente solicita código com CPF/CNPJ, lê-se a captura privada no SMTP4dev para o ensaio e confirma documento/desafio/código. AuthFunction consulta o cadastro e emite JWT Customer por UUID, sem CPF ou OS no token. [Contrato de acesso](arquitetura/ACESSO_E_AUTENTICACAO.md).
- JWT Customer permite status e decisão de orçamento somente da própria OS, enquanto Active. Aprovação/recusa exige approved explícito e retorna 200/Approved ou Refused. Login interno Admin/Mechanic permanece na API.
- API/auth montam o conteúdo e invocam EmailFunction síncrona por IAM. Ela concentra MIME/SMTP e captura no SMTP4dev privado, sem relay externo. A caixa é efêmera; desafios/limites ficam no PostgreSQL. [ADR 011](arquitetura/adrs/011-AUTENTICACAO-POR-CODIGO-E-EMAIL.md).
- Conteúdo, regras e transações da OS permanecem na API; falha tratada de notificação após commit não desfaz a operação. Não há fila, outbox, retry próprio ou garantia de entrega única.
- OTel API/auth e Collectors alimentam New Relic; X-Ray segue integração separada. Não apresentar os dois caminhos como um único trace. EmailFunction possui log JSON técnico, sem nova suíte OTel própria.
- CI valida PRs. Publicação/provisionamento têm disparo manual; base/banco/Gateway/New Relic preservam plano salvo → aprovação → apply. A função de envio não tem endpoint público. [RFC de entrega](arquitetura/rfcs/002-ENTREGA.md).

## E7.11 — Implementado e evolução futura

**Implementado:** transporte centralizado em uma função serverless, compartilhado por API e autenticação, com IAM por ambiente e captura SMTP4dev privada. Não apresentar essa centralização como proposta ainda não executada.

**Evolução futura:** desacoplar notificações de OS da resposta HTTP com processamento assíncrono independente. Motivação: reduzir o tempo aguardado na requisição e tratar indisponibilidade do transporte. Benefícios esperados dependem de decidir publicação confiável após commit, idempotência, retentativas limitadas, tratamento de mensagens que falham, operação e custos. Nenhuma fila/outbox ou tecnologia adicional foi escolhida/implementada nessa entrega. A confirmação do código deve continuar respeitando a regra de só liberar desafio após captura; não aplicar automaticamente a mesma mudança de comportamento à autenticação. [ADR 006](arquitetura/adrs/006-NOTIFICACOES.md).

O conteúdo documental está preparado. A explicação no vídeo permanece pendente na E7.11, vinculada à gravação E7.7.

## Limites que precisam permanecer na entrega

Prd, coexistência, carga, disparos de CPU/memória, rollback e concorrência de publicação não foram comprovados nos cenários dispensados. A diferença entre workflow_dispatch e o gatilho pós-merge do R06 original foi adiada pelo mantenedor; não afirmar atendimento integral desse requisito. Configuração hom/prd não comprova operação de prd.

E5 mantém limites de coleta/exportação, logs Gateway parciais e X-Ray separado de OTLP. Captura SMTP4dev não comprova posse de caixa pessoal ou entrega externa. Não há recuperação automática, entrega única, capacidade máxima comprovada ou preservação de mensagens/banco após seu descarte.

O relatório de custos de 21/10 não é uma fatura atual nem confirma ausência de recursos hoje. O ambiente hom foi reprovisionado e permaneceu disponível ao final da AE. Nova gravação, destruição ou conferência financeira exige sua própria execução/autorização; esta consolidação não operou AWS/GitHub/New Relic.

## Próxima etapa

Preparar e gravar o vídeo de até 15 minutos, publicar o link, produzir o PDF único e conferir links/integridade antes da submissão. Mostrar o funcionamento e os limites reais; não anunciar envio externo, deploy automático pós-merge ou união X-Ray/OTLP. Nenhum vídeo, PDF ou registro de submissão foi criado nesta consolidação. Commits/PRs e publicação dos documentos continuam sob coordenação do mantenedor.

## Conferência desta consolidação

Trinta documentos revisados nos quatro checkouts, incluindo o plano local/checklists ignorados pelo Git; 508 referências locais ou caminhos GitHub mapeados para arquivos existentes, sem destinos ausentes. Estrutura Markdown e checklists conferidos; E7.7–E7.11 continuam abertos no alcance descrito. Isso é conferência local de destinos, sem alegar nova validação de acesso remoto aos links. Relatório em `artifacts/e7-consolidation-20261023/documentation-review.json`.

Somente Markdown foi alterado; sem mudanças em código, contratos executáveis, SQL, manifestos, Terraform ou workflows. Não foram repetidos testes de aplicação nem executadas operações remotas para esta revisão documental. O plano vivo e `docs/plano-fase-3/` continuam locais/ignorados; READMEs, arquitetura e relatórios versionáveis devem ser publicados pelo mantenedor.
