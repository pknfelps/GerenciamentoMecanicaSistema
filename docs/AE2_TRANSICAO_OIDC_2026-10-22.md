# AE.2 — Plano salvo de transição OIDC

**22/10/2026 — plano inicial aprovado e aplicado.** Parte administrativa do [plano extra AE](PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md), independente dos checklists oficiais. Resultado confirmado: 0 criações, 3 alterações e 0 exclusões; confiança implantada conferida.

## Resultado e escopo

**0 criações, 3 alterações in-place e 0 exclusões.** Somente assume_role_policy das roles abaixo muda; policies de permissão, roles runtime, provider, buckets, catálogos, outputs e demais recursos não recebem alterações planejadas.

| Role | Nome adicional permitido |
|---|---|
| mecanica-auth-ci-github | Mesmo repositório com nome GerenciamentoMecanicaServerless, em pull_request e refs develop/main |
| mecanica-hom-auth-github | Mesmo repositório com nome GerenciamentoMecanicaServerless, Environment hom |
| mecanica-prd-auth-github | Mesmo repositório com nome GerenciamentoMecanicaServerless, Environment prd |

Prefixo adicional exato: repo:pknfelps@114608389/GerenciamentoMecanicaServerless@1375105446. Mantém owner/repository ID e confiança antiga durante a transição. Audience sts.amazonaws.com, branches/Environments e demais condições permanecem. Sem wildcard ou ampliação de permissões.

O plano inicial usou uma variável temporária de transição e tfvars adicionais para aceitar os dois nomes durante a migração. Após a renomeação e CI aprovado, essa configuração temporária foi retirada do código local e o prefixo canônico passou a Serverless. Sua retirada da AWS foi concluída pelo [plano final aprovado/aplicado](AE2_FINALIZACAO_OIDC_2026-10-22.md). O Account ID New Relic foi fornecido somente ao processo; não houve leitura de chaves.

## Arquivos locais do plano

- Binário: artifacts/ae2/oidc-transition.tfplan no repositório da API, ignorado pelo Git.
- Revisão textual: artifacts/ae2/oidc-transition.txt.
- Conferência estruturada: artifacts/ae2/oidc-transition-review.json.
- SHA-256 do binário: a0bb486730d9d35ad08b74691d11bf49f86205b3b6c63d87f91de1bc67a5fbd9.

Aplicado exclusivamente o binário acima, após aprovação e conferência do SHA-256. Plano consumido: não reaplicar. As três roles aceitaram o novo nome mantendo temporariamente o antigo; policies de permissão permaneceram intactas.

## Organização e validação local concluídas

Solução GerenciamentoMecanicaServerless.slnx, AuthFunction/AuthFunctionTests, estrutura EmailFunction/EmailFunctionTests, templates/auth.yaml e samconfig.auth.toml preparados. Nomes de assembly/handler, recursos AWS e conteúdo de 17 fontes C# preservados; somente CodeUri mudou no SAM. OpenAPI idêntico.

Restore/build Release sem avisos/erros; 269 testes existentes aprovados, zero falhas/ignorados, incluindo PostgreSQL/Testcontainers. SAM validate/build, publish linux-x64, actionlint com ShellCheck e Terraform fmt/init sem backend/validate aprovados. Arquivos de certificado/observabilidade/runtimeconfig conferidos; publish estrutural email sem SQL/JWT/domínio auth/testes. Email ainda não tem handler/envio ou testes funcionais e não foi apresentado como função pronta.

Console Terraform offline confirmou as alterações de subjects antes da geração do plano real. A revisão do JSON do plano salvo confirmou somente as três atualizações de confiança esperadas. Evidências de build/teste em artifacts/ae2 do repositório de autenticação.

## Migração e próxima aprovação

GitHub, pasta local e remote renomeados para GerenciamentoMecanicaServerless, com repository ID 1375105446 e visibilidade pública preservados. Ruleset ativo, PR obrigatório, checks configuration/dotnet, bloqueios de exclusão/force push e Environments conferidos sem alteração.

[Auth-ci #16](https://github.com/pknfelps/GerenciamentoMecanicaServerless/actions/runs/39812817320) executado uma única vez em develop, mediante autorização: sucesso, configuração/dotnet aprovados e autenticação OIDC confirmada. O commit remoto 01225acd7c45fe03a48ee7e6b228c069b77b4bdd é anterior às alterações locais de caminhos; portanto esse run comprova a identidade remota, enquanto os testes locais comprovam a estrutura nova.

[Plano final de remoção do nome antigo](AE2_FINALIZACAO_OIDC_2026-10-22.md) aprovado e aplicado: 0 criações, 3 alterações e 0 exclusões; subjects antigos ausentes e plano posterior sem diferenças. AE.2 concluída. AE.3–AE.10 e publicação da estrutura local continuam separadas.
