# AE.2 — Plano final de confiança OIDC

**22/10/2026 — plano aprovado e aplicado; AE.2 concluída.** Continuação da [transição inicial aplicada](AE2_TRANSICAO_OIDC_2026-10-22.md). Aplicado exclusivamente o binário aprovado, com hash conferido antes do apply.

## Plano aplicado

**0 criações, 3 alterações in-place, 0 exclusões.** Retira somente subjects do nome antigo GerenciamentoMecanicaAutenticacao da confiança destas roles:

| Recurso Terraform | Role | Contextos preservados do novo nome |
|---|---|---|
| aws_iam_role.auth_ci | mecanica-auth-ci-github | pull_request, develop e main |
| aws_iam_role.pipeline["hom-auth"] | mecanica-hom-auth-github | Environment hom |
| aws_iam_role.pipeline["prd-auth"] | mecanica-prd-auth-github | Environment prd |

Mantém exclusivamente repo:pknfelps@114608389/GerenciamentoMecanicaServerless@1375105446 com os sufixos existentes. Mesmo owner/repository ID, principal OIDC, audience sts.amazonaws.com e restrições de branch/Environment. Comparação estruturada confirmou que somente a lista de subjects muda; policies de permissão, outros atributos das roles, recursos e outputs permanecem inalterados.

## Evidências e arquivos

- [Plano textual](../artifacts/ae2/oidc-final.txt).
- [Revisão estruturada](../artifacts/ae2/oidc-final-review.json).
- Binário local ignorado pelo Git: artifacts/ae2/oidc-final.tfplan.
- SHA-256: `5e7e5582a71ce98873669dbe7b72228b04fc9f14cea225baadadb05b55689f49`.
- [Auth-ci #16 aprovado](https://github.com/pknfelps/GerenciamentoMecanicaServerless/actions/runs/39812817320): autenticação OIDC do novo nome confirmada no develop publicado, commit 01225acd7c45fe03a48ee7e6b228c069b77b4bdd. Não houve publish/provision/destroy.
- Depois da renomeação da raiz: restore/build Release sem avisos/erros, 269 testes aprovados, zero falhas/ignorados e SAM validate/build aprovados. EmailFunctionTests é somente estrutura sem casos funcionais; não foi contabilizado como cobertura do envio.

Evidências em artifacts são locais e não serão publicadas pelo Git. Plano consumido: não reaplicar. Mudanças futuras exigem outro plano salvo, revisão e aprovação próprios.

## Conferência e encerramento

Aplicação concluída: **0 criações, 3 alterações e 0 exclusões**. As três trust policies foram consultadas diretamente na AWS e comparadas ao plano aprovado: subjects antigos ausentes, novo nome exclusivo e demais condições/principal/audience preservados. O IAM devolve o subject único hom/prd como string; a comparação normalizou somente esse campo para lista antes de conferir o documento inteiro.

Plano posterior com os mesmos tfvars canônicos e Account ID New Relic fornecido ao processo retornou **No changes**, código detalhado 0. Evidências locais: artifacts/ae2/oidc-final-apply.log, oidc-final-deployed-review.json, deployed-final-*.json e oidc-final-post-apply-plan.txt.

AE.2 concluída e checklist extra atualizado. As alterações locais de estrutura/documentação continuam para PR do mantenedor; nenhum commit/push, publish/provision/destroy ou outro workflow remoto foi executado nesta finalização. A próxima etapa do plano extra é AE.3 — banco, mediante solicitação própria; desafios, SMTP e envio não foram implementados na AE.2.
