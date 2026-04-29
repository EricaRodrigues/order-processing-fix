# Order Processing FIX

Sistema de geração e acumulação de ordens financeiras utilizando o protocolo **FIX 4.4** via **QuickFIX/n**.

> This is a challenge by [Coodesh](https://coodesh.com/)

---

## Sobre o projeto

O sistema é composto por duas aplicações que se comunicam via protocolo FIX 4.4:

- **OrderGenerator** — API REST em ASP.NET Core com frontend React. Recebe ordens via formulário web e as envia ao OrderAccumulator usando uma mensagem `NewOrderSingle`.
- **OrderAccumulator** — Worker Service em ASP.NET Core. Recebe as ordens, calcula a exposição financeira por símbolo e responde com um `ExecutionReport` indicando aceitação ou rejeição.

### Regra de negócio

A **exposição financeira** por símbolo é calculada como:

```
Exposição = Σ(preço × quantidade) das ordens de compra
          − Σ(preço × quantidade) das ordens de venda
```

O limite interno por símbolo é de **R$ 100.000.000 (cem milhões)**. Qualquer ordem que, em valor absoluto, ultrapasse esse limite é rejeitada com `ExecType = Rejected`. Ordens rejeitadas não são consideradas no cálculo de exposição.

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | C# / .NET 9 |
| Protocolo FIX | QuickFIX/n (FIX 4.4) |
| Frontend | React 18 + TypeScript + Tailwind CSS v4 |
| Containerização | Docker + Docker Compose |
| Testes | xUnit |

---

## Arquitetura

```
order-processing-fix/
├── backend/
│   ├── OrderAccumulator.Domain/        # Entidades de domínio (Order)
│   ├── OrderAccumulator.Application/   # Serviços de aplicação (ExposureService)
│   ├── OrderAccumulator.Worker/        # Entry point — FIX Acceptor
│   ├── OrderGenerator.Infrastructure/  # FIX Initiator (OrderClient)
│   └── OrderGenerator.Api/             # Entry point — API REST + Controllers
│   └── tests/
│       └── OrderAccumulator.Tests/     # Testes unitários (xUnit)
├── frontend/                           # React + TypeScript + Tailwind CSS
│   ├── src/
│   │   ├── components/OrderForm.tsx
│   │   ├── services/orderService.ts
│   │   └── types/order.ts
│   └── Dockerfile
├── docker-compose.yml
└── .gitignore
```

### Fluxo de comunicação

```
[Browser] → POST /api/orders → [OrderGenerator.Api]
                                       ↓
                           NewOrderSingle (FIX 4.4)
                                       ↓
                            [OrderAccumulator.Worker]
                                       ↓
                    Calcula exposição financeira por símbolo
                                       ↓
                          ExecutionReport (FIX 4.4)
                          ExecType = New | Rejected
                                       ↓
                 [OrderGenerator.Api] → Response JSON → [Browser]
```

---

## Como rodar

### Com Docker Compose

**Pré-requisitos:** [Docker](https://www.docker.com/) e Docker Compose instalados.

```bash
git clone https://github.com/EricaRodrigues/order-processing-fix.git
cd order-processing-fix

docker-compose up --build
```

Serviços disponíveis:

| Serviço | URL |
|---|---|
| Frontend | http://localhost:3000 |
| OrderGenerator API | http://localhost:8080 |
| OrderAccumulator FIX | porta 5001 (interna) |

---

### Rodando localmente (sem Docker)

**Pré-requisitos:** [.NET 9 SDK](https://dotnet.microsoft.com/download) e [Node.js 20+](https://nodejs.org/)

**Terminal 1 — OrderAccumulator:**
```bash
cd backend
dotnet run --project OrderAccumulator.Worker
```

**Terminal 2 — OrderGenerator:**
```bash
cd backend
dotnet run --project OrderGenerator.Api
```

**Terminal 3 — Frontend:**
```bash
cd frontend
npm install
npm run dev
```

Acesse em http://localhost:5173

> ⚠️ Ao rodar localmente, o frontend aponta para `http://localhost:5014` via proxy do Vite. Certifique-se de que o OrderGenerator está rodando nessa porta.

---

## Testes

```bash
cd backend
dotnet test
```

Cobertura:
- `ExposureServiceTests` — regras de exposição, limites positivo e negativo, múltiplos símbolos, concorrência
- `OrderServerTests` — aceitação, rejeição, ordens sequenciais, isolamento entre símbolos

---

## Formulário

| Campo | Valores aceitos |
|---|---|
| Símbolo | PETR4, VALE3, VIIA4 |
| Lado | Buy (Compra), Sell (Venda) |
| Quantidade | Inteiro positivo, menor que 100.000 |
| Preço | Decimal múltiplo de 0.01, menor que 1.000 |

---

## API

### `POST /api/orders`

**Request:**
```json
{
  "symbol": "PETR4",
  "side": "Buy",
  "quantity": 100,
  "price": 38.50
}
```

**Response — aceita:**
```json
{
  "accepted": true,
  "status": "New",
  "message": "Order accepted. Symbol: PETR4, Qty: 100, Price: 38.50"
}
```

**Response — rejeitada:**
```json
{
  "accepted": false,
  "status": "Rejected",
  "message": "Order rejected. Exposure limit exceeded for PETR4."
}
```

---

## Variáveis de ambiente

| Variável | Descrição | Padrão |
|---|---|---|
| `ACCUMULATOR_HOST` | Host do OrderAccumulator para conexão FIX | `localhost` |
| `ASPNETCORE_URLS` | URL de escuta da API | `http://+:8080` |