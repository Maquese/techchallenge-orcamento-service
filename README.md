# techchallenge-orcamento-service

## Pagamento de orçamento (DEV)

A API processa pagamentos com cartão pela Orders API do Mercado Pago em modo
automático. O orçamento precisa estar aprovado; o valor é obtido do orçamento
persistido e não é aceito do cliente.

Configure um Access Token de **teste** do Mercado Pago no ambiente antes de
executar a API:

```powershell
$env:MercadoPago__AccessToken = "TEST-seu-access-token"
```

O endpoint `POST /api/Orcamento/PagamentoEfetuado` recebe o orçamento, o token do
cartão gerado no cliente com o SDK oficial do Mercado Pago, o método de pagamento,
parcelas, e-mail do pagador e uma chave UUID de idempotência. O número do cartão
e o CVV não devem ser enviados à API; eles devem ser tokenizados no cliente e o
token não é armazenado.

A resposta contém os identificadores e o status retornados pelo Mercado Pago. O
orçamento só é marcado como pago quando o pagamento retorna `processed` com
detalhe `accredited`; pagamentos recusados não impedem uma nova tentativa.
Reutilize a mesma chave UUID ao repetir a mesma chamada após timeout; após uma
recusa, gere uma chave nova para iniciar outra tentativa.
Consulte [OrcamentoService.http](./src/OrcamentoService/OrcamentoService.http)
para um exemplo de chamada.

Referências oficiais:

- [Integração de cartão com o Mercado Pago](https://www.mercadopago.com.br/developers/en/docs/checkout-api-orders/payment-integration/card)
- [Processamento automático de Orders](https://www.mercadopago.com.br/developers/en/docs/checkout-api-orders/integration-model)