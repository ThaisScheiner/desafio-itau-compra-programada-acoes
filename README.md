# Sistema de Compra Programada de Ações

## Visão Geral

Este projeto implementa um **sistema de investimento automatizado em
ações**, inspirado no modelo de **compra programada de ativos**
utilizado por plataformas financeiras.

A solução permite que clientes invistam mensalmente em uma **cesta
recomendada de ações**, realizando compras automáticas, controle de
custódia, rebalanceamento de carteira e cálculo de impostos.

A arquitetura foi construída utilizando **microserviços com .NET**,
comunicação entre serviços via **APIs REST** e **eventos Kafka**, e
persistência em **MySQL**.

O sistema foi projetado para ser **escalável, resiliente e orientado a
eventos**, refletindo práticas modernas utilizadas em ambientes de
produção no mercado financeiro.

------------------------------------------------------------------------

# Objetivo do Projeto

O objetivo deste projeto é simular uma plataforma de investimentos que
permita:

-   clientes aderirem a um plano de investimento mensal
-   executar compras automáticas de ações
-   manter controle de custódia
-   distribuir ativos entre clientes
-   realizar rebalanceamento de carteira
-   calcular eventos de imposto de renda
-   registrar eventos financeiros

O sistema busca resolver o seguinte problema:

> Como automatizar a compra recorrente de ativos para diversos clientes
> utilizando uma cesta recomendada, garantindo consistência de custódia
> e controle tributário.

------------------------------------------------------------------------

# Arquitetura da Solução

A solução foi construída seguindo uma arquitetura de **microserviços
orientada a domínio**, onde cada serviço é responsável por uma parte
específica do sistema.

## Serviços da Arquitetura

### ClientesService

Responsável por gerenciar clientes e seus planos de investimento.

Funcionalidades:

-   adesão ao plano
-   saída do plano
-   alteração do valor de aporte mensal
-   consulta de carteira
-   consulta de rentabilidade
-   listagem de clientes ativos

------------------------------------------------------------------------

### CestasRecomendacaoService

Gerencia as **cestas de investimento recomendadas**.

Funcionalidades:

-   criação de nova cesta
-   definição de cesta ativa
-   histórico de cestas
-   consulta da cesta atual

Cada cesta contém **5 ativos com seus respectivos pesos percentuais**.

------------------------------------------------------------------------

### CotacoesService

Responsável por armazenar e consultar **cotações de mercado**.

Funcionalidades:

-   importação de dados COTAHIST
-   consulta da última cotação de um ativo
-   atualização de preços

------------------------------------------------------------------------

### MotorCompraService

Este é o **motor principal do sistema**, responsável por executar as
compras programadas.

Funções do motor:

-   calcular aporte total dos clientes
-   obter cesta recomendada
-   consultar cotações
-   calcular quantidade de ativos
-   consolidar compras
-   distribuir ativos para os clientes
-   atualizar custódia

------------------------------------------------------------------------

### CustodiasService

Responsável pelo controle de **posições de ativos**.

Possui dois tipos de contas:

-   Conta MASTER (conta do sistema)
-   Conta FILHOTE (contas dos clientes)

Funções:

-   movimentação de ativos
-   atualização de posição
-   cálculo de preço médio
-   consulta de posições por cliente
-   consulta da conta master

------------------------------------------------------------------------

### RebalanceamentosService

Responsável por **rebalancear carteiras** quando a composição da cesta
muda.

Funções:

-   cálculo de vendas necessárias
-   execução de vendas
-   geração de lucro/prejuízo
-   cálculo de imposto

------------------------------------------------------------------------

### EventosIRService

Gerencia eventos tributários.

Tipos de eventos:

-   IR dedo-duro
-   IR sobre venda em rebalanceamento

Eventos são publicados no **Kafka**.

------------------------------------------------------------------------

# Comunicação entre serviços

A comunicação ocorre de duas formas:

## APIs REST

Para consultas e comandos síncronos.

Exemplos:

GET /api/admin/cesta/atual\
GET /api/cotacoes/fechamento/ultimo\
POST /api/custodias/movimentar

------------------------------------------------------------------------

## Eventos Kafka

Para integração assíncrona.

Exemplos de eventos:

-   IR gerado
-   venda em rebalanceamento
-   eventos financeiros

Kafka permite desacoplamento entre serviços.

------------------------------------------------------------------------

# Tecnologias Utilizadas

## Backend

-   .NET
-   ASP.NET Core
-   Entity Framework Core

## Banco de Dados

-   MySQL

## Mensageria

-   Apache Kafka

## Containerização

-   Docker
-   Docker Compose

## APIs

-   RESTful APIs
-   Swagger / OpenAPI

------------------------------------------------------------------------

# Infraestrutura com Docker

O ambiente do projeto utiliza **Docker** para facilitar a execução
local.

Serviços containerizados:

-   MySQL
-   Kafka
-   Zookeeper
-   APIs .NET

Isso permite que todo o sistema seja executado localmente sem
necessidade de instalação manual de dependências.

------------------------------------------------------------------------

# Como Rodar o Projeto

## Pré-requisitos

Instalar:

-   Docker
-   Docker Compose
-   .NET SDK

------------------------------------------------------------------------

## Subir a infraestrutura

Na raiz do projeto:

docker-compose up -d

Isso irá iniciar:

-   MySQL
-   Kafka
-   Zookeeper

------------------------------------------------------------------------

## Rodar os serviços

Cada microserviço pode ser executado com:

dotnet run

Ou via Docker dependendo da configuração.

------------------------------------------------------------------------

# Fluxo de Execução do Sistema

O fluxo principal ocorre da seguinte forma:

1.  Clientes aderem ao plano de investimento
2.  Uma cesta de ativos é definida
3.  Cotações são importadas
4.  O motor executa compras
5.  Ativos são distribuídos aos clientes
6.  Custódias são atualizadas
7.  Eventos tributários são gerados
8.  Rebalanceamentos podem ocorrer

------------------------------------------------------------------------

# Exemplo de Execução de Compra

Endpoint:

POST /api/motor/executar-compra

Resposta simplificada:

totalClientes: 2\
totalConsolidado: 3466.67\
planoPorTicker: PETR4, VALE3, ITUB4\
distribuições por cliente

------------------------------------------------------------------------

# Decisões de Arquitetura

## Arquitetura de Microserviços

Separação por domínio:

-   clientes
-   custódia
-   cotação
-   compra
-   rebalanceamento
-   eventos tributários

Isso permite maior escalabilidade e manutenção.

------------------------------------------------------------------------

## Uso de Kafka

Kafka foi utilizado para:

-   desacoplar serviços
-   permitir processamento assíncrono
-   registrar eventos financeiros

------------------------------------------------------------------------

## Conta MASTER

Foi implementada uma conta master para:

-   consolidar compras
-   evitar múltiplas ordens de mercado
-   distribuir ativos posteriormente aos clientes

------------------------------------------------------------------------

## Controle de Custódia

Cada cliente possui sua própria posição de ativos, com controle de:

-   quantidade
-   preço médio

------------------------------------------------------------------------

# Segurança e Resiliência

O sistema implementa:

-   tratamento de erros
-   circuit breaker
-   retry
-   timeout de chamadas HTTP

Isso garante maior robustez em comunicação entre serviços.

------------------------------------------------------------------------

# Possíveis Melhorias Futuras

Algumas melhorias poderiam ser implementadas:

-   autenticação e autorização
-   testes automatizados
-   observabilidade
-   métricas e monitoramento
-   interface frontend

------------------------------------------------------------------------

# Considerações Finais

Este projeto demonstra a implementação de um sistema distribuído para
gestão de investimentos automatizados, utilizando tecnologias modernas e
práticas comuns em arquiteturas financeiras baseadas em microserviços.

A solução foi projetada com foco em:

-   separação de responsabilidades
-   comunicação assíncrona
-   escalabilidade
-   organização por domínio
