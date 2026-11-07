# techchallenge-orcamento-service

## Pagamento de orçamento (DEV)

A API cria o pagamento diretamente pela Payments API (`POST /v1/payments`) do
Mercado Pago; ela não cria uma Order. O orçamento precisa estar aprovado e o
valor é obtido do orçamento persistido, não do cliente.

Para testar, configure o Access Token de teste da aplicação no ambiente antes
de executar a API (não armazene a credencial em arquivos versionados):

```powershell
$env:MercadoPago__AccessToken = "TEST-seu-access-token"
```

O endpoint `POST /api/Orcamento/PagamentoEfetuado` recebe o orçamento, o token do
cartão gerado no cliente com o SDK oficial do Mercado Pago, o método de pagamento,
parcelas, e-mail do pagador e uma chave UUID de idempotência. O número do cartão
e o CVV não devem ser enviados à API; eles devem ser tokenizados no cliente e o
token não é armazenado.

A resposta contém o ID e o status do pagamento retornados pelo Mercado Pago. O
orçamento só é marcado como pago quando o status é `approved`; pagamentos
recusados não impedem uma nova tentativa.
Reutilize a mesma chave UUID ao repetir a mesma chamada após timeout; após uma
recusa, gere uma chave nova para iniciar outra tentativa.
O estado `processing` é salvo no DynamoDB antes da chamada ao provedor; quando
o Mercado Pago responde, seus identificadores e status substituem esse estado e
são persistidos junto com o orçamento.
Consulte [OrcamentoService.http](./src/OrcamentoService/OrcamentoService.http)
para um exemplo de chamada.

Referência oficial: [Payments API](https://www.mercadopago.com.br/developers/pt/reference/payments/_payments/post).