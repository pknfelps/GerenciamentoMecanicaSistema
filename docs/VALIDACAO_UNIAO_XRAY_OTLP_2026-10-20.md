# E5 — União X-Ray/OTLP no New Relic — 20/10/2026

**Resultado do critério de árvore única: falhou na configuração atual, nos dois caminhos selecionados.** A interface do New Relic marcou os traces OTLP da API e da Lambda como **Fragmented**, ambos com **Missing parent**. A consulta dos mesmos acontecimentos encontrou os segmentos nativos correspondentes, porém armazenados sob outros valores de trace.id/id/parent.id.

Isso confirma a lacuna de união automática; não é falha de propagação comprovada na API ou Lambda, nem ausência dos segmentos por amostragem nesta amostra. A relação é identificável comparando os componentes dos IDs, mas essa comparação manual não equivale a uma única árvore no produto.

## Escopo

Conferência somente de leitura, pela sessão autenticada New Relic US da conta 8626152, usando dados históricos de hom em **20/10/2026, 13:10–13:19 UTC**. Nenhum tráfego novo, login AWS, alteração de código/configuração, workflow ou recurso AWS foi executado.

API a8ba2615d9fe918fc161d975fabd406b54fd0ff2; autenticação versão 10/revisão 0446a19b4888921f1d9b4032d034de7d4009ab24, conforme a rodada integrada de origem.

Foram consultados somente IDs, nomes e origem dos spans; não foram abertos corpos, credenciais ou documentos de segmentos. Os nomes nativos podem conter URL de infraestrutura, diferentemente dos nomes OTLP sanitizados; não foram reproduzidos integralmente neste relatório.

## Consulta e totais

```sql
SELECT timestamp, `trace.id`, id, `parent.id`, name,
       `newRelic.ingestPoint`, `service.name`
FROM Span
WHERE `trace.id` IN (
  '6ad7686a14cde6997a691ac14fbf7813',
  '1-6ad7686a-14cde6997a691ac14fbf7813',
  '6ad7686b104d5dc750e1f27a021ac58e',
  '1-6ad7686b-104d5dc750e1f27a021ac58e'
)
SINCE '2026-10-20 13:10:00+0000'
UNTIL '2026-10-20 13:19:00+0000'
LIMIT 100
```

Resultado: **15 spans**, seis api.traces.otlp e nove xray.polling, distribuídos em quatro valores de trace.id:

| Chamada | Trace OTLP | Spans OTLP | Trace do polling | Spans nativos |
|---|---|---:|---|---:|
| POST Authentication | 6ad7686a14cde6997a691ac14fbf7813 | 3 | 1-6ad7686a-14cde6997a691ac14fbf7813 | 3 |
| POST /customers/validate | 6ad7686b104d5dc750e1f27a021ac58e | 3 | 1-6ad7686b-104d5dc750e1f27a021ac58e | 6 |

Os spans OTLP PostgreSQL encontram seus pais pelos IDs exatos dentro de cada trace OTLP. Já o pai externo do span HTTP/invocação só corresponde ao componente final do ID nativo, não ao campo integral armazenado.

## Gateway → API

Span OTLP HTTP c952ff161f839133, com parent.id **1e510261445ed9ac**.

O subsegmento correspondente da integração do Gateway existe no polling, com id:

```text
1-6ad7686a-14cde6997a691ac14fbf7813:1e510261445ed9ac
```

Os valores de parent.id OTLP e id nativo não são iguais; também diferem os valores integrais de trace.id. Os filhos PostgreSQL a128dcf9d15a11c9 e bdf66b85c807b6d1 apontam corretamente para c952ff161f839133.

Na interface Distributed tracing, mesma janela absoluta e filtro pelo trace OTLP, foi aberto o detalhe de POST Authentication:

- Trace ID conferido integralmente.
- **Spans: 3**, com API e PostgreSQL no mapa.
- Sinalização **Fragmented**.
- Linha do span de entrada: **mecanica-api Missing parent**.
- Nenhum segmento Gateway unido a essa árvore.

## Gateway/runtime → Lambda

Span OTLP de invocação 136a462e26d3a44c, com parent.id **f89d6ebac806cfa6**.

O segmento nativo correspondente existe no polling, com id:

```text
1-6ad7686b-104d5dc750e1f27a021ac58e:f89d6ebac806cfa6
```

Seus ancestrais nativos incluem integração do Gateway e serviço Lambda. Os filhos PostgreSQL 921059b406855f7f e 34f0a7dd1e9816d4 apontam corretamente para 136a462e26d3a44c.

Na interface, filtro pelo trace OTLP e detalhe de OtherTransaction/Function/mecanica-hom-auth (POST /customers/validate):

- Trace ID conferido integralmente.
- **Spans: 3**, com autenticação e PostgreSQL no mapa; Logs (1).
- Sinalização **Fragmented**.
- Linha da invocação: **mecanica-auth Missing parent**.
- Nenhum segmento nativo Gateway/Lambda unido a essa árvore.

Não é somente uma diferença visual na apresentação dos IDs: os campos de vínculo persistidos diferem e o produto sinaliza o pai ausente.

## Documentação consultada e interpretação

A [documentação de mapeamento OTLP](https://docs.newrelic.com/docs/opentelemetry/best-practices/opentelemetry-best-practices-traces/) define trace_id → trace.id, span_id → id e parent_span_id → parent.id. Os campos de protocolo têm precedência sobre atributos comuns. Portanto, acrescentar atributos homônimos não substitui, por si só, os identificadores efetivos.

A [documentação da integração X-Ray](https://docs.newrelic.com/docs/infrastructure/amazon-integrations/aws-integrations-list/aws-x-ray-monitoring-integration/) confirma ingestão dos segmentos como Span com newRelic.ingestPoint=xray.polling e processamento posterior à conclusão. As duas chamadas já possuem segmentos nativos na janela consultada; aguardar mais polling não explica o Missing parent observado.

Não foi encontrada nessas páginas uma opção documentada que normalize automaticamente os vínculos do polling para os campos OTLP. Isso não é uma afirmação de impossibilidade absoluta ou confirmação do fornecedor sobre todas as integrações disponíveis.

A conclusão é específica da configuração e dos dados inspecionados. Uma solução de normalização/ingestão compatível depende de análise própria; não alterar IDs da aplicação, duplicar coleta, fabricar spans ou criar vínculos artificiais como resultado desta validação.

## Registro e limites

- Propagação/continuidade dos componentes dos IDs: confirmada.
- Relações internas API/Lambda → PostgreSQL: confirmadas.
- Recebimento dos segmentos nativos selecionados: confirmado.
- Árvore única nativa + OTLP: **não atendida**, com evidência direta de Fragmented/Missing parent nos dois detalhes.
- Nenhuma configuração New Relic salva ou modificada; não foram criadas views persistentes.
- A aba temporária de conferência foi fechada; a aba original e a sessão do mantenedor foram preservadas.

E5.3/E5.14 permanecem parciais. Não repetir provisionamento, tráfego ou aguardar polling como se isso corrigisse a incompatibilidade dos vínculos já armazenados. Não generalizar a prd ou a outras modalidades de ingestão.

Evidência técnica sanitizada local: artifacts/e5-validacao-integrada/20261020-xray-otlp-union/validation-summary.json. Relatórios anteriores permanecem como histórico; esta etapa substitui “não investigado” por “critério verificado e não atendido” nos dois casos.

