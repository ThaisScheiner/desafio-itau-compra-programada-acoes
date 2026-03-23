# Front-End - Sistema de Compra Programada de Ações

## Angular 21 + Angular Material

---

## 1. Sobre o projeto

Este projeto é o **frontend** do sistema de **Compra Programada de Ações**, construído para consumir a arquitetura de microserviços desenvolvida no backend em **.NET**.

A aplicação foi criada com foco em simular uma experiência próxima de um produto real do mercado financeiro, permitindo visualizar informações importantes do fluxo de investimento programado, como:

- clientes ativos
- detalhes do cliente
- carteira consolidada
- rentabilidade
- cestas recomendadas
- motor de compra
- observabilidade dos serviços

A proposta do frontend é transformar os dados expostos pelos microserviços em uma interface clara, navegável e visualmente próxima de um sistema corporativo.

---

## 2. Objetivo da interface

O objetivo desta aplicação é fornecer uma camada visual para os serviços do sistema, permitindo:

- consultar clientes e seus dados financeiros
- visualizar carteira e rentabilidade individual
- navegar pelas cestas recomendadas
- acompanhar o estado do motor de compra
- monitorar saúde e telemetria básica dos serviços
- demonstrar integração entre frontend Angular e backend distribuído

Além da funcionalidade, o frontend também foi pensado para demonstrar:

- organização por módulos
- separação de responsabilidades
- consumo de APIs REST
- construção de telas com foco em legibilidade
- experiência de navegação mais próxima de produtos reais

---

## 3. Tecnologias utilizadas e por que foram usadas

### Angular 21
Framework principal do frontend, escolhido por:
- estrutura robusta para aplicações SPA
- organização modular
- integração nativa com rotas, formulários e HTTP
- escalabilidade para projetos maiores

### TypeScript
- tipagem estática
- maior segurança no consumo das APIs
- melhor manutenção do código

### Angular Material
- construção rápida de UI
- componentes prontos (tabelas, botões, etc.)
- consistência visual

### SCSS
- melhor organização de estilos
- reutilização e manutenção mais fácil

### HttpClient
- comunicação com APIs REST do backend

### Angular Router
- navegação entre páginas

---

## 4. Estrutura do projeto

- src/app/core → models e services
- src/app/features → módulos por domínio
- src/app/layout → layout e navegação
- src/app/app.routes.ts → rotas

---

## 5. Funcionalidades

- Dashboard
- Clientes (listagem, detalhe, cadastro)
- Carteira
- Rentabilidade
- Cestas
- Motor de compra
- Observabilidade (trace, span, correlation id)

---

## 6. Integração com backend

Consome microserviços locais via HTTP:

- http://localhost:5001
- http://localhost:5002
- http://localhost:5003
- http://localhost:5004
- http://localhost:5005
- http://localhost:5006
- http://localhost:5007

Backend precisa estar rodando.

---

## 7. Como rodar

### Pré-requisitos
- Node.js
- npm
- Angular CLI

### Instalar dependências
```bash
npm install
```

### Rodar aplicação
```bash
ng serve
```

Acesse:
http://localhost:4200

---

## 8. Rotas principais

- /dashboard
- /clientes
- /clientes/novo
- /clientes/:clienteId
- /cestas
- /motor-compra
- /observabilidade

---

## 9. Arquitetura

- organização por domínio
- core centralizado
- layout desacoplado
- consumo tipado de API

---

## 10. Melhorias futuras

- gráficos
- histórico de aportes
- autenticação
- interceptors
- responsividade
- tratamento de erros

---

## 11. Objetivo técnico

- Angular aplicado em projeto real
- integração com microserviços
- leitura de dados financeiros
- observabilidade

---

## 12. Considerações finais

Projeto desenvolvido para simular um sistema real de investimentos, com foco em arquitetura, integração e experiência do usuário.


