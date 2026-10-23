# Revisão documental E7 — 21/10/2026

## Escopo e resultado

Concluída a revisão documental E7.1–E7.5 dos quatro repositórios, contra código/configuração e evidências existentes. E5/E6 permanecem encerradas no escopo hom acordado. O ambiente foi destruído pelo mantenedor; não houve novo teste AWS, provisionamento, publicação, commit, push ou PR nesta revisão.

| Repositório | Documentos e correções |
|---|---|
| Sistema | README, índice arquitetural, quatro arquivos de diagramas, três RFCs, dez ADRs, acesso/JWT, catálogo entre repositórios, consumo do pacote e justificativa do banco |
| Autenticação | README: contrato, camadas, bootstrap/runtime, consumo real do pacote sem scripts removidos, W3C, SAM/VersionId e operação |
| BancoDados | README e SCHEMA: execução local/Job, credenciais, relações reais, histórico/grants e validações já concluídas |
| Infraestrutura | README: unidades, rede, pipelines, ordem de ciclos, Collectors, CloudWatch, New Relic publicado e limites aceitos |

Os ADRs distinguem a decisão vigente dos registros anteriores, preservados em seções históricas recolhíveis. Pendências descritas nessas seções se referem à data do registro, não ao estado final. Aurora Serverless é uma escolha histórica substituída por RDS provisionado; a restrição observada foi desta conta. A futura migração das notificações para processamento assíncrono permanece em ADR 006, sem afirmar implementação nem concluir o item de vídeo E7.11.

## Divergências corrigidas

- O fluxo de abertura confirma a transação, aguarda o handler de notificação e só depois registra a criação pelo filtro HTTP. Não existe fila/outbox ou entrega única nesta implementação.
- SMTP funciona localmente; hom/prd simulam o envio. A simulação não significa entrega ao destinatário.
- API/Lambda usam W3C; X-Ray nativo/polling permanece separado. A união não foi corrigida, nem é uma pendência reaberta da E5 aceita.
- As métricas de integração contam acessos lógicos concluídos, após as retentativas internas do SDK. Falhas SQL não equivalem necessariamente a HTTP 500 e cancelamentos ficam separados.
- O catálogo descreve somente parâmetros efetivamente publicados. Não existe publicador `/api/v2/` no código entregue; SSM é configuração, não prova de prontidão.
- O ER inclui dez tabelas/53 colunas. Histórico sem FK para orders, itens sem PK e marcador sem PK são limites reais. Grants de DML da API no histórico não impõem append-only.
- Novas credenciais e inicialização declarativa não são descritas como pendentes depois das comprovações existentes. ZIP SAM fixa VersionId; templates operacionais têm ciclo distinto dos contratos imutáveis.
- Os três dashboards/cinco alertas já publicados não são mais apresentados como apenas preparados localmente. Descartar AWS não elimina dashboards, bootstrap ou todos os custos/resíduos históricos.

## Conferência local

- Comparação direta do ER com `BancoDados/sql/Init.sql`: dez tabelas, 53 colunas, PKs/FKs/UNIQUE correspondentes; nenhum SQL alterado.
- Sete blocos Mermaid renderizados pelo CLI 12.0.0 com Chrome headless local; diagrama de componentes e ER inspecionados visualmente. Uma descrição com ponto e vírgula no diagrama de autenticação foi corrigida após erro de parsing.
- Links relativos e referências GitHub entre os quatro repositórios conferidos contra os arquivos locais. Essa conferência não prova publicação dos novos arquivos ou acesso público aos links depois de um PR.
- `git diff --check` nos quatro repositórios; alterações desta revisão restritas a Markdown. Não foram executados testes .NET, Docker, SAM ou Terraform: código/configuração executável não mudou.
- Cópias anteriores, hashes, conferência do esquema e renders ficam em `artifacts/e7-doc-review-20261021`, ignorado pelo Git; não são requisito para ler a documentação versionada.

Revisões de origem: Sistema `9d1c0625e7c0ada58a110e2f833b21853d3a0b61`; Autenticação `01225acd7c45fe03a48ee7e6b228c069b77b4bdd`; BancoDados `5ef95df52c037d940ffad07a07175fea0209ab0a`; Infraestrutura `333e2a8feb33385ddb248076f486f6fe1a295140`. Alterações documentais anteriores do Sistema foram preservadas.

## Limites e continuidade

Prd/coexistência, carga, disparos de CPU/memória, rollback e concorrência de publicação não foram comprovados, conforme decisões anteriores. Workflows manuais diferem do R06 original; a revisão desse requisito foi adiada. Nenhuma dispensa é apresentada como teste aprovado ou atendimento integral dos requisitos.

E7 continua em andamento: faltam vídeo, publicação do vídeo, PDF e conferência/submissão final. Por decisão posterior do mantenedor, o acesso deve ocorrer por repositórios públicos, substituindo o convite a `soat-architecture`; a conferência foi dispensada e E7.6 encerrada por escopo, sem comprovação da visibilidade ou alteração remota. A publicação destes ajustes nos quatro repositórios também permanece com o mantenedor. O vídeo deve explicar o processamento atual das notificações e sua evolução futura; a documentação dessa decisão já existe.

Fontes: [arquitetura](arquitetura/README.md), [banco/modelo](arquitetura/BANCO_E_MODELO.md), [encerramento E5](ENCERRAMENTO_E5_2026-10-21.md), [encerramento E6/custos](ENCERRAMENTO_E6_E_CUSTOS_2026-10-21.md), [proteções/pipelines](EVIDENCIAS_CICD_E_PROTECOES_2026-10-21.md) e [artefatos/VersionId](VALIDACAO_PROTECAO_ARTEFATOS_2026-10-21.md).

## Visibilidade pública dos repositórios

Em 21/10/2026, por solicitação posterior do mantenedor, a consulta somente leitura de metadados do GitHub confirmou os quatro repositórios com `visibility=public` e `archived=false`:

| Repositório | Visibilidade |
|---|---|
| [Sistema](https://github.com/pknfelps/GerenciamentoMecanicaSistema) | public |
| [Autenticação](https://github.com/pknfelps/GerenciamentoMecanicaAutenticacao) | public |
| [BancoDados](https://github.com/pknfelps/GerenciamentoMecanicaBancoDados) | public |
| [Infraestrutura](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura) | public |

Fonte: metadados de cada repositório pela API GitHub. Isso comprova a visibilidade atual e substitui a dispensa de conferência registrada anteriormente. Não houve alteração de visibilidade, convite ou permissão. A comprovação não significa que as alterações documentais locais já foram publicadas.
