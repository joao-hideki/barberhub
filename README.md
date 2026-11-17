# ✂ BarberHub

**SaaS de agendamento online para barbearias.** O cliente da barbearia escolhe serviço, barbeiro e horário por um link público, sem precisar criar conta. O dono acompanha a agenda, confirma os horários pelo WhatsApp com um clique e vê o faturamento do mês. A barbearia paga uma mensalidade para usar o sistema.

> MVP desenvolvido para o trabalho prático de SaaS da FATEC.

## Integrantes

- João Hideki
- _[Nome do integrante]_
- _[Nome do integrante]_

## O problema

Barbearias pequenas ainda marcam horários no caderno ou por mensagens no WhatsApp. Com isso surgem **horários duplicados**, **clientes esquecidos**, tempo perdido respondendo "tem horário amanhã?" e **nenhuma visão do faturamento**.

**Público-alvo:** donos de barbearias pequenas e médias (de 1 a 5 barbeiros) e os clientes delas.

## Funcionalidades do MVP

| Quem | O que faz |
|---|---|
| Dono da barbearia | Cria a conta (14 dias grátis), cadastra **serviços com preço e duração**, **barbeiros** e o **expediente** |
| Cliente | Agenda pela **página pública** `/b/nome-da-barbearia`: escolhe serviço → barbeiro → dia → horário livre, informa nome e celular e **confere os dados** antes de confirmar |
| Dono / barbeiro | Vê a **agenda do dia**, chama o cliente no **WhatsApp** com a mensagem pronta e marca como confirmado, concluído ou cancelado |
| Dono | **Painel** com atendimentos do dia, concluídos e faturamento do mês |
| Dono | **Assinatura**: escolhe o plano e paga com cartão (gateway **simulado**) |

### Regras de negócio (todas cobertas por testes)

- Um atendimento ocupa a agenda do barbeiro por **[início, início + duração do serviço)**, e dois atendimentos nunca se sobrepõem.
- O horário precisa **caber inteiro no expediente**, não pode estar **no passado** e segue **blocos de 15 minutos**.
- O cliente é identificado pelo **celular** (com DDD válido e 9º dígito). O mesmo celular em formatos diferentes é o mesmo cliente. Ele não pode ter dois horários ao mesmo tempo e pode ter no máximo **2 horários em aberto** por barbearia (proteção contra trote).
- O **preço fica gravado no agendamento**: reajustar o serviço não muda atendimentos antigos nem o faturamento.
- **Multi-tenant**: uma barbearia nunca enxerga nem altera dados de outra.
- **Limite de barbeiros por plano**: Básico 1, Pro 5. Não é possível trocar para um plano menor que o número de barbeiros ativos.
- Com a **assinatura vencida**, a página pública deixa de aceitar agendamentos.
- O pagamento durante o período grátis **não faz perder os dias grátis**: o mês pago começa depois deles.

## Modelo de negócio (como o SaaS é cobrado)

| Plano | Preço | Barbeiros |
|---|---|---|
| Básico | R$ 49,90/mês | 1 |
| Pro | R$ 99,90/mês | até 5 |

Os dois planos têm 14 dias grátis sem cartão, assinatura mensal no cartão e possibilidade de cancelar quando quiser.

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Interface | ASP.NET Core **Razor Pages** (renderização no servidor) + Bootstrap 5 |
| Lógica | C# / **.NET 10**, serviços de domínio com injeção de dependência |
| Dados | **PostgreSQL 17** com **Entity Framework Core 10** (migrations) |
| Autenticação | Cookie do ASP.NET Core e senha com hash PBKDF2 (`PasswordHasher`) |
| Testes | **xUnit** + **Testcontainers** (PostgreSQL real no Docker) + `FakeTimeProvider` |
| Infraestrutura | **Docker** e Docker Compose |

## Como executar

### Pré-requisito

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e **aberto**.

### Passo a passo

```bash
# 1. Clonar
git clone https://github.com/joao-hideki/barberhub.git
cd barberhub

# 2. Criar o arquivo de configuração local (a senha do banco NÃO fica no repositório)
cp .env.example .env          # no Windows (cmd): copy .env.example .env
#    abra o .env e troque POSTGRES_PASSWORD por uma senha qualquer

# 3. Subir banco + aplicação
docker compose up --build
```

Depois, abra **http://localhost:8080**.

Na primeira execução, o sistema cria as tabelas e uma barbearia de demonstração:

| | |
|---|---|
| Login do dono | `demo@barberhub.com` / `demo123` |
| Página do cliente | http://localhost:8080/b/barbearia-do-ze |
| Cartão aprovado (simulação) | `4111 1111 1111 1111`, validade futura (ex.: `12/30`), CVV `123` |
| Cartão recusado (simulação) | qualquer número terminado em `0000` |

Para parar: `Ctrl+C`, ou `docker compose down`. Para apagar também os dados, use `docker compose down -v`.

### Rodar os testes

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download) e o **Docker aberto**. Os testes sobem um PostgreSQL descartável sozinhos.

```bash
dotnet test
```

## Metodologia: TDD (Test-Driven Development)

As regras de negócio foram escritas **teste primeiro**, em ciclos *red → green*:

1. **red**: escreve um teste para um comportamento e confirma que ele **falha**.
2. **green**: escreve o mínimo de código para o teste **passar**.

Cada etapa virou um commit, então o histórico prova a ordem:

```bash
git log --oneline --reverse
# ... red: horário livre é agendado bloqueando a duração do serviço
# ... green: horário livre é agendado bloqueando a duração do serviço
# ... red: não sobrepõe atendimentos do mesmo barbeiro
# ... green: não sobrepõe atendimentos do mesmo barbeiro
```

São **70 testes** em 6 arquivos, todos passando pela interface pública dos serviços e com banco PostgreSQL real. A única dependência substituída nos testes é o **gateway de pagamento**, por ser um sistema externo.

## Arquitetura

```
Navegador ──HTTP──▶ Razor Pages (Pages/)            ← interface (HTML gerado no servidor)
                        │ chama
                        ▼
                    Serviços (Services/)             ← lógica e regras de negócio (testadas)
                    CadastroService · CatalogoService
                    AgendamentoService · AssinaturaService ──▶ IGatewayPagamento (simulado)
                        │ EF Core
                        ▼
                    PostgreSQL (container Docker)    ← armazenamento
```

```
src/BarberHub.Web/
  Domain/       entidades e regras puras (Barbearia, Servico, Agendamento, Telefone, Planos...)
  Services/     casos de uso (cadastro, catálogo, agenda, assinatura)
  Pagamentos/   porta IGatewayPagamento + GatewayPagamentoSimulado
  Data/         AppDbContext, migrations e dados de demonstração
  Pages/        telas Razor (públicas, /Painel protegido por login, /b/{slug} do cliente)
tests/BarberHub.Tests/   testes xUnit (Testcontainers)
docs/                    relatório (PDF) e roteiro da apresentação
```

Mais detalhes estão no [relatório do projeto](docs/BarberHub-Relatorio.pdf).

## Uso de IA

O desenvolvimento usou o **Claude Code** (modelo Claude Opus 5.5, da Anthropic) para planejamento, programação em TDD, testes e documentação. Todo o código foi revisado pelo grupo. O relatório traz os detalhes e exemplos concretos.

## Próximos passos

- Integração real com um gateway (Mercado Pago, Asaas ou Stripe), trocando só a implementação de `IGatewayPagamento`.
- Confirmação automática por WhatsApp/SMS com código (OTP) e lembrete 1 dia antes.
- Agenda individual por barbeiro (folgas e intervalo de almoço).
- Publicação em nuvem (ex.: Azure App Service + PostgreSQL gerenciado).
