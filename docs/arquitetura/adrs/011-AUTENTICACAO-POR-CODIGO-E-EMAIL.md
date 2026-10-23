# ADR 011 — Código de acesso e transporte SMTP centralizado

**23/10/2026 — decisão aceita, implementada e comprovada localmente na extra AE; publicação/comprovação AWS pendentes na AE.10.** Preserva os aceites oficiais anteriores E5/E6/E7. [Plano extra](../../PLANO_EXTRA_AUTENTICACAO_E_EMAIL.md).

## Decisão

Cliente solicita código com CPF/CNPJ; confirma desafio/código/documento para obter JWT Customer de dez minutos. Duas operações explícitas request-code/verify-code, sem envio/validação combinados implicitamente, Authorizer ou token por OS. O mesmo JWT permite consultar status antes/depois do orçamento e decidir somente as próprias OSs. Lambda valida cadastro/Active e consome código; API valida assinatura/perfil/Active/titularidade. UUID do cliente no JWT evita acoplamento da autenticação às OSs. CPF/CNPJ não entra no JWT ou orçamento; ClockSkew zero na API.

Um repositório GerenciamentoMecanicaServerless, projetos AuthFunction/EmailFunction e dois ZIPs/templates/stacks/roles/catálogos. Wrappers de CI/publish/provision/destroy separados reutilizam workflow_call nativo com allowlists e imutabilidade/VersionId. Roles específicas compartilham subject do Environment; arquivos separados não são isolamento OIDC por workflow.

API e auth montam o conteúdo e invocam EmailFunction por IAM no alias live do ambiente; somente EmailFunction implementa MIME/SMTP. Envio síncrono com limites, sem fila/outbox/retry próprio. Auth confirma Ready somente após captured; falha de notificação de OS após commit continua sem desfazer a persistência.

SMTP4dev privado no EKS, mesma rede/NLB da base e listener SMTP próprio; caixa efêmera e relay desabilitado, sem entrega externa. Operador lê a captura via port-forward autorizado para simular acesso à caixa, nunca por endpoint público/debug ou permissão JWT Mechanic. Cliente inicia a solicitação; mecânico não gera acesso. O local usa SAM/Invoke v1 e mesma captura; auth mantém bootstrap produtivo hom/prd, com injeção apenas no ensaio/testes.

Banco vazio recebe desafios/limites no Init.sql; sem migrations ou CHECKs/FKs/cascatas novos. Regras/locks/consumo em C#, cliente inexistente sem gravação/envio/limpeza, limites duráveis por UUID elegível, salt/verificador PBKDF2. Código oito dígitos/cinco minutos/cinco tentativas; cooldown 60 s/cinco pedidos por hora; limpeza oportunista limitada a 100 expirados por tabela.

## Limites e alternativas

Emissão por documento isolado e Lambda Authorizer foram substituídos: não cobriam uma sessão uniforme para status/decisão e identidade forte do cliente. Código devolvido publicamente ou acesso gerado pelo mecânico não foram adotados. Não há email real, painel próprio, refresh/revogação central, sessão por OS, envio independente com fila ou garantia única.

Captura não comprova propriedade do e-mail pessoal. Caixa desaparece em reinícios; código pode estar no banco sem estar consultável na caixa, ou captura ocorrer antes de timeout. Pedir novo código respeitando limites; sem recuperação automática. SQL é descartável com o banco; nenhuma rotina reconstrói desafios/mensagens.

## Evidências e implantação

[AE.9](../../AE9_VALIDACAO_LOCAL_E_DOCUMENTACAO.md) registra testes e o ensaio com handler real/SQL/grants, SDK real, SAM/EmailFunction, SMTP4dev e API. Bootstrap AWS foi injetado, sem novo fallback produtivo ou executável permanente. Isso não comprova IAM, Gateway, VPC/NLB, cold start/congelamento/capacidade reais ou prd.

Esta decisão substitui o transporte/simulação do [ADR 006](006-NOTIFICACOES.md) e a emissão de cliente por documento isolado do [ADR 001](001-IDENTIDADE.md), mantendo seus registros históricos e regras internas. [Contratos](../ACESSO_E_AUTENTICACAO.md) · [Componentes](../diagramas/COMPONENTES.md).
