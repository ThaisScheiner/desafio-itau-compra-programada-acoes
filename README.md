# Desafio Técnico - Sistema de Compra Programada de Ações

## Itaú Corretora - Engenharia de Software

---

## 1. Sobre o projeto

Este projeto implementa um **Sistema de Compra Programada de Ações** inspirado no desafio técnico da **Itaú Corretora**.

A proposta é permitir que clientes adiram a um plano de investimento recorrente em uma **cesta recomendada de 5 ações (Top Five)**. A partir dessa adesão, o sistema passa a:

- cadastrar clientes no produto
- registrar o valor mensal de aporte
- consolidar compras em uma conta master
- distribuir ativos proporcionalmente para as contas dos clientes
- manter resíduos na custódia master
- calcular preço médio por ativo
- acompanhar carteira e rentabilidade do cliente
- calcular eventos de imposto de renda
- executar rebalanceamentos quando a cesta é alterada ou quando há desvio de proporção

O objetivo principal foi construir uma solução **modular, organizada e aderente às regras de negócio do desafio**, usando uma arquitetura próxima do que seria esperado em um ambiente moderno de engenharia de software no mercado financeiro.

---

## 2. Objetivo da solução

O sistema busca resolver o seguinte problema de negócio:

> Como automatizar a compra recorrente de ações para múltiplos clientes, utilizando uma cesta recomendada, com controle de custódia, distribuição proporcional, cálculo de preço médio, geração de eventos tributários e suporte a rebalanceamento?

Na prática, a solução implementa:

- **adesão de clientes ao produto**
- **controle da cesta Top Five**
- **consulta/importação de cotações**
- **execução da compra programada**
- **controle de custódia master e filhote**
- **consulta de carteira e rentabilidade**
- **registro de IR dedo-duro**
- **registro de IR sobre vendas em rebalanceamento**
- **consumo de eventos via Kafka**
- **scheduler para automação de rotinas**

---

## 3. Arquitetura da solução

A solução foi organizada em **microserviços por domínio**, separando responsabilidades para deixar o sistema mais coeso, mais fácil de manter e mais simples de evoluir.

### Microserviços implementados

#### ClientesService
Responsável pelo ciclo de vida do cliente dentro do produto.

Principais responsabilidades:
- adesão ao produto
- saída do produto
- alteração do valor mensal
- listagem de clientes ativos
- consulta de carteira
- consulta de rentabilidade
- manutenção da conta gráfica filhote

#### CestasRecomendacaoService
Responsável pela gestão da cesta recomendada Top Five.

Principais responsabilidades:
- cadastrar nova cesta
- manter apenas uma cesta ativa por vez
- consultar cesta atual
- consultar histórico de cestas
- publicar evento quando a cesta é alterada

#### CotacoesService
Responsável pela gestão de cotações de mercado.

Principais responsabilidades:
- importação e leitura de dados de cotação
- suporte ao conceito do COTAHIST da B3
- consulta da cotação de fechamento mais recente por ticker
- persistência de cotações para consumo pelos demais serviços

#### CustodiasService
Responsável pelo controle das posições dos ativos.

Principais responsabilidades:
- movimentar ativos em conta master
- movimentar ativos em conta filhote
- calcular e atualizar preço médio
- consultar custódia por cliente
- consultar custódia master
- manter resíduos não distribuídos

#### MotorCompraService
É o coração do fluxo de compra programada.

Principais responsabilidades:
- buscar clientes ativos
- calcular 1/3 do valor mensal de cada cliente
- consolidar aportes
- consultar cesta vigente
- consultar cotações
- considerar saldo existente na conta master
- gerar ordens de compra
- distribuir ativos proporcionalmente
- registrar histórico de aportes
- integrar com o serviço de eventos de IR

#### RebalanceamentosService
Responsável por recalcular e movimentar carteiras quando necessário.

Principais responsabilidades:
- reagir à mudança da cesta via Kafka
- executar rebalanceamento por cliente
- registrar vendas realizadas no rebalanceamento
- calcular base de IR sobre vendas
- disparar integração com o EventosIRService
- executar rotina automática por scheduler

#### EventosIRService
Responsável por registrar e publicar eventos fiscais.

Principais responsabilidades:
- registrar IR dedo-duro
- registrar IR sobre venda de rebalanceamento
- persistir eventos fiscais
- publicar eventos no Kafka
- consultar eventos por cliente

---

## 4. Por que essa arquitetura foi escolhida

A escolha por microserviços foi feita porque o domínio do problema já é naturalmente dividido em contextos independentes:

- cliente
- cesta recomendada
- cotação
- compra programada
- custódia
- rebalanceamento
- fiscal

Essa separação traz alguns ganhos importantes:

- **responsabilidade clara por serviço**
- **menor acoplamento entre contextos de negócio**
- **facilidade de manutenção**
- **facilidade de testes e evolução**
- **melhor aderência a eventos assíncronos**

Mesmo sendo um projeto de desafio técnico, a arquitetura foi pensada para refletir práticas reais de engenharia em sistemas distribuídos.

---

## 5. Tecnologias utilizadas e por que foram usadas

### .NET / ASP.NET Core
Foi utilizado como stack principal de backend por atender diretamente ao requisito do desafio e por oferecer:

- boa estrutura para APIs REST
- integração madura com DI
- bom suporte a testes
- boa produtividade com Entity Framework Core
- facilidade para organizar projetos em camadas

### Entity Framework Core
Foi usado para persistência relacional porque acelera a modelagem, migrations e integração com MySQL.

Foi útil para:
- mapear entidades do domínio
- controlar migrations
- persistir dados com menor esforço estrutural

### MySQL
Foi escolhido por ser o banco relacional exigido no desafio e por se encaixar bem no modelo transacional do sistema.

Foi usado para persistir:
- clientes
- contas gráficas
- cestas
- cotações
- custódias
- aportes
- ordens de compra
- rebalanceamentos
- eventos fiscais

### Apache Kafka
Foi utilizado para comunicação assíncrona entre serviços.

Foi importante principalmente para:
- evento de alteração de cesta
- publicação de eventos de imposto de renda
- desacoplamento entre serviços
- simulação de integração orientada a eventos

### Docker / Docker Compose
Foi utilizado para facilitar a execução local do ambiente.

Permite subir rapidamente:
- MySQL
- Kafka
- OpenTelemetry Collector

Isso reduz fricção de setup e deixa o projeto mais fácil de reproduzir.

### Swagger / OpenAPI
Foi utilizado para:
- documentar endpoints
- testar manualmente fluxos do sistema
- demonstrar o projeto durante a apresentação

### xUnit, Moq e FluentAssertions
Foram utilizados nos testes automatizados.

- **xUnit**: framework de testes
- **Moq**: criação de mocks para dependências externas
- **FluentAssertions**: assertions mais legíveis e expressivas

### OpenTelemetry
Foi utilizado para adicionar **observabilidade** ao projeto, principalmente no `MotorCompraService`.

Com OpenTelemetry foi possível:
- rastrear o fluxo completo da compra programada
- medir duração das etapas críticas
- instrumentar chamadas HTTP entre microserviços
- expor métricas técnicas e métricas de negócio
- correlacionar logs com `traceId`

---

## 6. Como o sistema funciona na prática

O fluxo principal do sistema é o seguinte:

1. o cliente adere ao produto informando nome, CPF, e-mail e valor mensal
2. o sistema cria a conta gráfica filhote
3. o administrador cadastra uma cesta Top Five com 5 ativos e percentuais que somam 100%
4. as cotações dos ativos são disponibilizadas no CotacoesService
5. o MotorCompraService busca os clientes ativos e calcula **1/3 do valor mensal** de cada um
6. os valores são consolidados
7. o motor consulta a cesta atual
8. o motor consulta a cotação de fechamento de cada ativo
9. o motor considera saldo residual da conta master
10. o motor registra a compra consolidada
11. o sistema distribui os ativos proporcionalmente para os clientes
12. o CustodiasService atualiza posições e preço médio
13. resíduos permanecem na conta master
14. eventos de IR dedo-duro são registrados
15. se a cesta mudar, o RebalanceamentosService processa as vendas e compras necessárias
16. se houver venda acima de R$ 20.000,00 no mês com lucro líquido positivo, o EventosIRService registra o IR devido

---

## 7. Detalhamento do funcionamento por serviço

### 7.1 ClientesService
O ClientesService funciona como a porta de entrada do cliente no produto.

Ele permite:
- cadastrar cliente
- tornar cliente inativo sem apagar sua posição
- alterar valor mensal
- consultar carteira
- consultar rentabilidade

A carteira é montada integrando:
- posição de custódia do cliente
- cotação atual dos ativos
- histórico de aportes do motor de compra

A rentabilidade considera:
- valor atual da carteira
- custo da posição com base em preço médio
- P/L total
- evolução baseada no histórico de aportes

### 7.2 CestasRecomendacaoService
Esse serviço centraliza a regra da cesta Top Five.

Ao cadastrar uma nova cesta, o sistema:
- valida que há exatamente 5 ativos
- valida que a soma é 100%
- desativa a cesta anterior
- cria a nova cesta ativa
- publica um evento Kafka indicando mudança de composição

### 7.3 CotacoesService
Esse serviço centraliza a fonte de preço.

Ele foi estruturado para refletir a lógica de uso do COTAHIST:
- armazenar cotação por ticker e data
- retornar o último fechamento disponível
- servir como base para compra, carteira e rebalanceamento

### 7.4 MotorCompraService
Esse é o fluxo mais importante do sistema.

Em cada execução, ele:
- recebe uma data de referência
- verifica se já houve execução para a data
- busca clientes ativos
- calcula os aportes parciais
- soma o valor consolidado
- consulta a cesta atual
- consulta a última cotação de fechamento dos tickers
- calcula quantidade comprada por ativo
- considera posição existente na conta master
- registra a compra
- distribui os ativos
- registra histórico de aportes
- dispara integração com eventos fiscais

### 7.5 CustodiasService
Esse serviço mantém a verdade sobre as posições.

Ele controla:
- conta master
- contas filhotes dos clientes
- movimentação de compra e venda
- atualização de quantidade
- atualização de preço médio

Isso é essencial porque o desafio exige:
- distribuição proporcional
- manutenção de resíduos
- cálculo correto de preço médio

### 7.6 RebalanceamentosService
O rebalanceamento foi implementado para duas necessidades do desafio:
- mudança de composição da cesta
- ajuste por desvio de proporção

Quando a cesta muda, o serviço pode:
- identificar ativo vendido
- identificar ativo comprado
- consultar preços
- movimentar custódia
- registrar venda para cálculo de IR
- publicar evento de rebalanceamento executado

Também foi implementado um **scheduler** para rotinas automáticas relacionadas a rebalanceamento.

### 7.7 EventosIRService
Esse serviço centraliza a parte fiscal.

Ele registra:
- IR dedo-duro por operação distribuída
- IR sobre venda em rebalanceamento

Também publica mensagens no Kafka, permitindo desacoplamento e rastreabilidade dos eventos fiscais.

---

## 8. Endpoints principais

### ClientesService
- `POST /api/clientes/adesao`
- `POST /api/clientes/{clienteId}/saida`
- `PUT /api/clientes/{clienteId}/valor-mensal`
- `GET /api/clientes/ativos`
- `GET /api/clientes/{clienteId}/carteira`
- `GET /api/clientes/{clienteId}/rentabilidade`

### CestasRecomendacaoService
- `POST /api/admin/cesta`
- `GET /api/admin/cesta/atual`
- `GET /api/admin/cesta/historico`

### CotacoesService
- endpoint de importação de cotação/COTAHIST
- `GET /api/cotacoes/fechamento/ultimo?ticker=...`

### CustodiasService
- `POST /api/custodias/movimentar`
- `GET /api/custodias/cliente/{clienteId}`
- `GET /api/custodias/master`

### MotorCompraService
- `POST /api/motor/executar-compra`
- `GET /api/motor/aportes/{clienteId}`

### RebalanceamentosService
- endpoint de execução manual de rebalanceamento
- endpoint de consulta de rebalanceamentos por cliente

### EventosIRService
- `POST /api/eventos-ir/dedo-duro`
- `POST /api/eventos-ir/venda-rebalanceamento`
- `GET /api/eventos-ir/cliente/{clienteId}`

---

## 9. Como rodar o projeto

## 9.1 Pré-requisitos

É necessário ter instalado:

- .NET SDK
- Docker
- Docker Compose
- Git

---

## 9.2 Subir a infraestrutura

Na raiz do projeto, abra um terminal e execute:

```bash
docker compose up -d
```

Esse comando sobe a infraestrutura de apoio, incluindo:

- MySQL
- Kafka
- OpenTelemetry Collector

Se quiser reiniciar tudo do zero:

```bash
docker compose down -v
docker compose up --build -d
```

---

## 9.3 Como abrir o projeto para execução manual

A forma mais prática de rodar para demonstração é abrir **um terminal para cada microserviço**.

### Terminal 1 - ClientesService
```bash
dotnet run --project src/services/ClientesService/src/ClientesService.Api/ClientesService.Api/ClientesService.Api.csproj --urls http://localhost:5001
```

### Terminal 2 - CestasRecomendacaoService
```bash
dotnet run --project src/services/CestasRecomendacaoService/src/CestasRecomendacaoService.Api/CestasRecomendacaoService.Api/CestasRecomendacaoService.Api.csproj --urls http://localhost:5002
```

### Terminal 3 - CotacoesService
```bash
dotnet run --project src/services/CotacoesService/src/CotacoesService.Api/CotacoesService.Api/CotacoesService.Api.csproj --urls http://localhost:5003
```

### Terminal 4 - CustodiasService
```bash
dotnet run --project src/services/CustodiasService/src/CustodiasService.Api/CustodiasService.Api/CustodiasService.Api.csproj --urls http://localhost:5004
```

### Terminal 5 - MotorCompraService
```bash
dotnet run --project src/services/MotorCompraService/src/MotorCompraService.Api/MotorCompraService.Api/MotorCompraService.Api.csproj --urls http://localhost:5005
```

### Terminal 6 - EventosIRService
```bash
dotnet run --project src/services/EventosIRService/src/EventosIRService.Api/EventosIRService.Api/EventosIRService.Api.csproj --urls http://localhost:5006
```

### Terminal 7 - RebalanceamentosService
```bash
dotnet run --project src/services/RebalanceamentosService/src/RebalanceamentosService.Api/RebalanceamentosService.Api/RebalanceamentosService.Api.csproj --urls http://localhost:5007
```

---

## 9.4 Como acessar o Swagger

Depois que os serviços estiverem rodando, abra no navegador:

- `http://localhost:5001/swagger` → ClientesService
- `http://localhost:5002/swagger` → CestasRecomendacaoService
- `http://localhost:5003/swagger` → CotacoesService
- `http://localhost:5004/swagger` → CustodiasService
- `http://localhost:5005/swagger` → MotorCompraService
- `http://localhost:5006/swagger` → EventosIRService
- `http://localhost:5007/swagger` → RebalanceamentosService

---

## 9.5 Ordem recomendada para testar manualmente no Swagger

A ordem mais segura para testar o sistema é:

1. cadastrar clientes
2. cadastrar cesta Top Five
3. cadastrar/importar cotações
4. executar compra programada
5. consultar custódia dos clientes
6. consultar custódia master
7. consultar carteira
8. consultar rentabilidade
9. consultar eventos de IR
10. alterar cesta e observar rebalanceamento

---

## 10. Testes automatizados

Foram implementados testes automatizados para o **ClientesService**.

### O que foi testado
Os testes cobrem regras importantes do serviço, como:

- adesão válida de cliente
- validação de CPF inválido
- validação de valor mensal mínimo
- bloqueio de CPF duplicado
- alteração de valor mensal
- saída do produto
- montagem da carteira
- cálculo de rentabilidade

### Bibliotecas usadas
- **xUnit** para execução dos testes
- **Moq** para simular dependências externas
- **FluentAssertions** para assertions mais legíveis

### Como rodar o teste do ClientesService na raiz do projeto

```bash
dotnet test src/services/ClientesService/tests/ClientesService.Tests/ClientesService.Tests/ClientesService.Tests.csproj
```

Se quiser saída mais detalhada:

```bash
dotnet test src/services/ClientesService/tests/ClientesService.Tests/ClientesService.Tests/ClientesService.Tests.csproj --logger "console;verbosity=detailed"
```

### Por que esse teste é importante
Como o ClientesService depende de outros serviços, os testes usam mocks para simular integrações como:

- CustodiasService
- CotacoesService
- MotorCompraService

Isso permite validar a regra de negócio de forma isolada, sem depender de o sistema inteiro estar rodando.

---

## 11. Principais decisões técnicas

### Microserviços por domínio
Escolhi separar os contextos principais do problema em serviços diferentes porque isso melhora:
- organização
- legibilidade
- manutenibilidade
- desacoplamento

### Kafka para eventos
Kafka foi usado porque faz sentido em fluxos como:
- alteração de cesta
- registro de eventos fiscais
- integração assíncrona entre serviços

### Conta master e contas filhote
Essa decisão é central para o desafio, porque a compra precisa ser consolidada antes da distribuição.

### Persistência distribuída por serviço
Cada serviço mantém seus próprios dados principais, o que deixa a responsabilidade mais bem definida.

### Uso de scheduler
O scheduler foi adicionado para aproximar a solução de um fluxo mais automatizado, especialmente em rebalanceamento.

### Swagger em todos os serviços
Isso foi importante para:
- demonstrar o sistema
- facilitar validação manual
- deixar a API documentada

### Observabilidade concentrada no MotorCompraService
A observabilidade foi priorizada no `MotorCompraService` porque ele é o serviço mais crítico do fluxo. Ele orquestra a compra programada, conversa com vários serviços externos e concentra a principal regra de negócio do desafio. Por isso, foi o melhor ponto para demonstrar tracing, métricas e correlação de logs de forma objetiva.

---

## 12. Qualidade, organização e métodos utilizados

Durante o desenvolvimento, procurei aplicar:

- separação de responsabilidades
- organização por domínio
- uso de DTOs para contratos externos
- uso de exceptions de domínio para regras de negócio
- integração HTTP tipada entre serviços
- eventos para desacoplamento
- testes automatizados para regras críticas do cliente

Mesmo sendo um desafio técnico, a ideia foi deixar o projeto o mais próximo possível de uma solução profissional, sem perder clareza.

---

## 13. Observabilidade e telemetria

Além do fluxo funcional, o projeto recebeu uma camada de observabilidade com foco principal no `MotorCompraService`, já que esse serviço concentra a etapa mais sensível do sistema: a execução da compra programada, a consulta a múltiplas dependências externas, o cálculo das quantidades por ativo, a distribuição dos papéis e a integração com o serviço de eventos fiscais.

A observabilidade foi adicionada para permitir rastreamento completo da execução, medição de latência entre etapas, correlação de logs com requisições específicas e análise mais clara de falhas em um fluxo distribuído. Em vez de enxergar apenas mensagens soltas de log, a solução passa a mostrar a jornada da operação ponta a ponta.

A implementação foi feita com **OpenTelemetry**, utilizando tracing, métricas e exportação via **OTLP** para um **OpenTelemetry Collector** executado em Docker.

### Onde a observabilidade foi aplicada

A instrumentação foi concentrada principalmente no método `ExecutarCompra` do `MotorCompraService`, porque ele representa o fluxo principal do negócio.

Foram adicionados spans internos e métricas nas seguintes etapas:

- entrada da requisição `POST /api/motor/executar-compra`
- busca de clientes ativos
- carregamento da cesta vigente
- consulta das cotações
- leitura da custódia master inicial
- cálculo do plano por ticker
- distribuição dos ativos
- publicação dos eventos de IR
- leitura final da custódia master
- finalização da execução
- tratamento de exceções

Essa escolha faz sentido porque cada uma dessas etapas representa um ponto importante do domínio. Quando uma compra programada demora mais do que o esperado ou falha no meio do caminho, a instrumentação permite identificar exatamente em qual parte isso aconteceu.

### Tracing distribuído

Foi utilizado `ActivitySource` para criar spans customizados no `MotorCompraService`.

Com isso, o fluxo principal da compra programada passou a ter um span pai, e as subetapas mais importantes passaram a ser registradas como spans internos. Isso permite visualizar o encadeamento lógico da execução, com duração, contexto e metadados de negócio.

No fluxo de compra programada, o tracing foi usado para registrar informações como:

- data de referência da execução
- total de clientes processados
- valor total consolidado
- quantidade de ativos da cesta
- quantidade de ordens planejadas
- quantidade de distribuições realizadas
- total de movimentações em custódia
- quantidade de eventos de IR publicados
- resultado final da operação

Esse tracing foi aplicado justamente nas partes do fluxo em que o sistema toma decisões importantes ou depende de integrações externas. Assim, se houver timeout em um serviço, erro de cotação, falha de custódia ou lentidão na distribuição, o trace ajuda a localizar o problema com muito mais precisão.

### Métricas

Também foram adicionadas métricas customizadas para acompanhar o comportamento do motor.

Entre elas:

- contador de compras executadas com sucesso
- contador de execuções com erro
- histograma com a duração total da execução da compra programada

Essas métricas foram usadas porque ajudam a enxergar o sistema de forma agregada. Enquanto o tracing mostra uma execução específica, as métricas permitem acompanhar tendência, volume e comportamento geral do serviço.

No contexto do desafio, isso ajuda a demonstrar maturidade técnica porque o projeto não apenas executa o fluxo, mas também consegue medir estabilidade e desempenho do processo principal.

### Logs estruturados com `ILogger`

Além da telemetria, foram adicionados logs estruturados com `ILogger` em pontos estratégicos do fluxo.

Os logs foram posicionados especialmente:

- após buscar os clientes
- após carregar a cesta
- após montar o plano por ticker
- antes do commit final
- no sucesso da execução
- no tratamento de erro

Esses logs foram colocados nesses pontos porque representam marcos importantes do processo. Dessa forma, ao ler o log de uma execução, fica claro o que já foi carregado, o que foi calculado, qual foi o volume processado e em qual etapa ocorreu algum erro.

### Correlação com `traceId`

Foi utilizado `BeginScope` com um helper de contexto para incluir `traceId` e `spanId` nos logs.

Isso foi importante porque permite correlacionar os logs gerados pelo serviço com os spans da telemetria. Em um sistema com múltiplas chamadas HTTP entre microserviços, essa correlação facilita bastante o diagnóstico, já que uma mesma execução pode atravessar várias etapas e várias dependências.

Na prática, isso significa que, ao identificar um problema em um log, é possível localizar o mesmo fluxo dentro da telemetria e entender a jornada completa da operação.

### Instrumentação automática de ASP.NET Core e HttpClient

Além dos spans customizados, foi utilizada instrumentação automática do ASP.NET Core e do `HttpClient`.

Isso permitiu capturar automaticamente:

- requisições recebidas pelo serviço
- duração das chamadas HTTP feitas para outros microserviços
- status codes das respostas externas
- erros em integrações HTTP

Essa parte foi importante porque o `MotorCompraService` conversa com vários outros serviços durante a execução. Com essa instrumentação, o projeto consegue mostrar não só a lógica interna do motor, mas também o impacto das dependências externas no tempo total da compra programada.

### Exportação OTLP e Collector

A telemetria foi configurada para ser exportada via OTLP para um **OpenTelemetry Collector** rodando em Docker.

Nos testes locais, o collector foi configurado em modo `debug`, permitindo visualizar diretamente no terminal:

- spans do fluxo principal
- spans internos do motor
- chamadas HTTP para outros serviços
- métricas automáticas do ASP.NET Core
- métricas de `HttpClient`
- métricas customizadas do negócio

Isso foi suficiente para validar que a instrumentação estava funcionando corretamente e para comprovar o rastreamento ponta a ponta do fluxo.

### Por que isso é relevante no projeto

A observabilidade foi adicionada para aproximar o desafio de um cenário mais real de engenharia de software. Em um ambiente distribuído, principalmente em um contexto financeiro, não basta apenas o fluxo funcionar. Também é importante conseguir responder perguntas como:

- quanto tempo a execução da compra levou
- em qual dependência o fluxo ficou mais lento
- quantos eventos de IR foram gerados
- quantas movimentações em custódia ocorreram
- em qual etapa uma execução falhou
- qual `traceId` corresponde a um erro específico

Ao adicionar telemetria, métricas e logs estruturados, o projeto passa a demonstrar não apenas implementação funcional, mas também capacidade de diagnóstico e acompanhamento operacional, o que fortalece bastante a solução do ponto de vista de engenharia.

---

## 14. Possíveis melhorias futuras

Algumas melhorias que poderiam ser adicionadas em uma evolução futura:

- autenticação e autorização
- frontend para o cliente e painel administrativo
- expansão da observabilidade para `EventosIRService`, `RebalanceamentosService`, `CotacoesService` e demais microserviços
- integração visual da telemetria com ferramentas como Jaeger, Grafana ou Datadog
- mais cobertura de testes
- política de retry e circuit breaker mais padronizada em todos os serviços
- compensação de prejuízo em IR entre meses
- maior refinamento do histórico de evolução da carteira

---

## 15. Considerações finais

Este projeto foi desenvolvido com foco em:

- aderência ao desafio proposto
- separação de responsabilidades
- organização arquitetural
- demonstração de regras de negócio financeiras
- comunicação síncrona e assíncrona entre serviços

A solução cobre os principais fluxos exigidos pelo desafio e foi pensada para ser demonstrável, organizada e tecnicamente consistente.
