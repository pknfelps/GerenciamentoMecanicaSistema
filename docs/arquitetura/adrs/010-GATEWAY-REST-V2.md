> O Gateway publica request-code/verify-code (POST), OpenAPI auth 2.0.0 e permissões de invocação para a versão numérica. A rota customers/validate foi retirada; não há Authorizer ou endpoint público email.

# ADR 010 — Gateway REST Regional com VPC Link V2

**Estado:** aceito em 12/10/2026, implementado e validado em hom; ambiente posteriormente removido.

**Contexto:** integrar API privada e função por contratos fixos, com ciclo de vida separado da base.

**Decisão vigente:** unidade gateway/ e estado/workflows próprios no repositório infra. REST Regional com stage hom/prd; VPC Link V2 em subnets privadas usa SG reservado da base. SG permite Gateway → NLB TCP 80 e avaliação inbound PrivateLink fica **on**. NLB/ASG/NodePort pertencem à base, sem controller.

Terraform projeta OpenAPI 3.0 verificados: HTTP_PROXY para a API e AWS_PROXY para versão numérica Lambda; normaliza nomes de parâmetros variáveis mantendo mapeamento ao backend. Não publica health/ANY/rota genérica. Remove modelos/security Bearer incompatíveis do documento importado, preservando os contratos originais completos. Autorização JWT/perfil/vínculo permanece na API, sem authorizer.

Base/auth são consumidos por SSM público; contratos por S3 com checksum/commit, sem Secrets ou estado remoto. API/auth novos exigem novo plano Gateway quando muda contrato/versão. Mantenedor informa environment/api_commit/auth_commit. Plan → aprovação sem AWS → apply do mesmo plano/SHA/contratos, artefato por ID e sete dias, sem replan.

**Logs e tracing:** access logs JSON/sete dias contêm IDs, método/modelo de rota, status/latências/tamanho e xrayTraceId. Sem corpo/headers/query, execution logs OFF, data tracing/cache/métricas detalhadas por método desligados. X-Ray é configurado por variável/tfvars: hom true, prd false; padrão da variável false. A Lambda tem tracing nativo sempre ativo, independentemente dessa variável. CloudWatch/polling New Relic são diagnóstico com limites aceitos, sem união automática OTLP/X-Ray.

Bootstrap possui service-linked role, role de logs e configuração regional persistentes; Gateway administra seu log group, integração, permissão de invocação e sete parâmetros gateway/v2. Endpoint é publicado em SSM e muda ao recriar; não versionar URL transitória como endereço disponível permanente.

**Consequências e comprovação:** Gateway deve ser atualizado para nova versão numérica auth; acesso saudável depende de backends/banco. Descartá-lo antes de API/auth. Entrada pública, respostas/JWT e logs foram conferidos em hom; não estender a prd ou prometer cobertura completa dos logs.

[Configuração do Gateway](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/gateway) · [Contratos](../CONTRATOS_ENTRE_REPOSITORIOS.md).

Revisão documental de 21/10/2026. Implementação e validações de hom não comprovam prd ou testes dispensados.

<details>
<summary>Histórico anterior à revisão E7 — registros preservados, sem vigência operacional</summary>

Os estados locais, planos pendentes e alternativas abaixo registram o momento em que foram escritos. A decisão e o alcance atuais estão acima; este histórico não autoriza operações nem reabre pendências aceitas.

**Estado:** aceito em 12/10/2026; código validado localmente, implantação e comprovação HTTP na AWS pendentes.

**Contexto:** API e autenticação já publicam contratos OpenAPI 3.0 imutáveis no S3. A base fornece NLB interno TCP 80 e a autenticação publica ARN qualificado por versão numérica no SSM v2. O Gateway deve ser criado sem acoplar a base aos artefatos de aplicação.

**Decisão:** unidade Terraform gateway no repositório de infraestrutura, estado separado por ambiente e workflows manuais próprios. REST API Regional com stage hom/prd; VPC Link V2 nas duas subnets privadas de workloads. Base possui SG reservado Gateway, saída TCP 80 ao SG NLB e ingresso NLB dessa origem. A avaliação de ingresso PrivateLink fica on. Isso substitui a exceção off prevista para VPC Link V1 no ADR 008; NLB/ASG/NodePort permanecem.

Terraform compõe uma definição AWS por jsondecode/jsonencode dos contratos verificados. API usa HTTP_PROXY com caminhos explícitos; POST /customers/validate usa AWS_PROXY para a versão numérica da Lambda. Não publicar health checks, ANY ou rota genérica. A projeção omite modelos e security Bearer incompatíveis com o importador REST; os contratos originais continuam completos. Autorização JWT permanece na API, sem authorizer Gateway ou alteração de regras de negócio.

Provisionamento/descarte: plan → aprovação protegida sem AWS → apply do plano salvo, no mesmo commit, com contratos no mesmo artefato GitHub por sete dias. Mantenedor coordena operações sequenciais entre componentes. Gateway consome SSM público base/auth e contratos S3; não lê Secrets nem estado de outro repositório.

Bootstrap mantém service-linked role, role restrita de logs e configuração regional CloudWatch persistentes. Gateway administra log group com sete dias e acesso JSON limitado a IDs, método, rota parametrizada, status, latência e tamanho. Execution logs/data tracing/cache/X-Ray desabilitados; E5 posterior. OIDC Gateway limitado ao repositório infra e ambiente correspondente.

**Consequências:** a base pode existir sem a API, mas o Gateway exige os contratos e a versão auth publicada; tráfego saudável exige ambos os backends e banco. Mudanças de contrato/versão exigem novo plano Gateway. Descartar Gateway antes de auth/API/banco/base e conferir liberação de ENIs antes de remover a rede. Preservar nomes/backends e descrição histórica do SG NLB, evitando substituição de recursos existentes.

**Validação local:** Terraform fmt/validate, workflows actionlint/ShellCheck, contratos S3/checksums, composição de 27 caminhos/37 operações hom/prd, políticas Access Analyzer e simulação IAM. O plano bootstrap tem 10 criações/4 alterações/nenhuma exclusão; aplicação pendente. A rede/backends hom estão descartados; importação AWS, encaminhamento sem prefixo stage, respostas HTTP, logs e compatibilidade JWT API/Lambda serão conferidos após recriação/deploy. E3/E2.12 permanecem abertas.

Rastreabilidade: D01/D09, E3.3/E3.4/E3.8/E3.11. [Contratos](../CONTRATOS_ENTRE_REPOSITORIOS.md) · [Configuração do Gateway](https://github.com/pknfelps/GerenciamentoMecanicaInfraestrutura/blob/develop/gateway) · [VPC Link V2 REST](https://docs.aws.amazon.com/apigateway/latest/developerguide/set-up-private-integration.html) · [Limites OpenAPI AWS](https://docs.aws.amazon.com/apigateway/latest/developerguide/api-gateway-known-issues.html).

</details>
