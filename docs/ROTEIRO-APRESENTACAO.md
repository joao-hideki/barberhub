# Roteiro da apresentação: BarberHub

> Pensado para cerca de **10 minutos**, mais as perguntas. Se o tempo for menor, corte os blocos marcados com *(opcional)*.

## Antes da aula (checklist)

- [ ] Docker Desktop **aberto**
- [ ] Na pasta do projeto, rodar `docker compose down -v` e depois `docker compose up --build`, para começar a demo com os dados limpos
- [ ] Abrir no navegador: `http://localhost:8080` (aba 1) e `http://localhost:8080/b/barbearia-do-ze` em **aba anônima** (aba 2, o "cliente")
- [ ] Deixar um terminal aberto na pasta do projeto, para mostrar `dotnet test` e `git log`
- [ ] Rodar `dotnet test` uma vez antes, para baixar a imagem do Postgres e não esperar na hora

## 1. Problema e proposta (1,5 min)

- "Barbearia pequena marca horário no caderno ou no WhatsApp: tem horário duplicado, cliente esquecido e o dono não sabe quanto faturou."
- **Público:** donos de barbearias de 1 a 5 barbeiros e os clientes delas.
- **Proposta SaaS:** cada barbearia assina, ganha um link de agendamento e um painel. Todas usam o mesmo sistema, mas os dados de cada uma são isolados (multi-tenant).
- **Cobrança:** Básico R$ 49,90 (1 barbeiro) e Pro R$ 99,90 (até 5), com 14 dias grátis. Mostrar os planos na landing page.

## 2. Demonstração do fluxo principal (4 min)

1. **Cliente (aba anônima)** em `/b/barbearia-do-ze`: escolher "Corte + barba", "Zé" e amanhã → **Ver horários**.
   - Mostrar que os horários ocupados **não aparecem** e que o último horário é 18:00 (serviço de 60 min, fecha às 19h).
2. Escolher um horário, digitar o nome e um **telefone fixo** (ex.: `11 3265-4321`). O sistema recusa: "precisa ser celular com DDD".
3. Corrigir para um celular → aparece a **tela de conferência** com o número formatado → **Confirmar**.
4. Voltar à lista de horários: o horário escolhido **sumiu**.
5. **Dono (aba 1)**: entrar com `demo@barberhub.com` / `demo123` → **Agenda** do dia → mostrar o agendamento novo.
   - Clicar em **WhatsApp**: abre com a mensagem pronta. Depois **Confirmar** → **Concluir**.
6. **Painel**: o faturamento do mês aumentou com o preço do serviço.
7. **Assinatura**: pagar com o cartão `4111 1111 1111 0000` (**recusado**) e depois com `4111 1111 1111 1111` (**aprovado**). Mostrar o histórico.
8. *(opcional)* **Barbeiros**: em uma conta nova no plano Básico, o 2º barbeiro é bloqueado.

## 3. Arquitetura e tecnologias (1,5 min)

Mostrar o diagrama do relatório (seção 2):

- **Interface:** Razor Pages, ou seja, HTML gerado no servidor, sem API separada. É mais simples para um MVP, e eu domino C#.
- **Lógica:** serviços de domínio (`AgendamentoService` etc.) com injeção de dependência. As telas só chamam os serviços.
- **Dados:** PostgreSQL via EF Core, com migrations aplicadas quando o app sobe.
- **Docker Compose:** qualquer pessoa roda com um comando, sem instalar banco.
- **Gateway:** interface `IGatewayPagamento` com implementação simulada. Para produção, troca-se uma linha do `Program.cs`.

## 4. Técnicas: TDD (1,5 min)

- No terminal: `git log --oneline --reverse | Select-Object -First 12` (PowerShell) ou `git log --oneline --reverse | head -12`.
  - Mostrar os pares **red** (teste falhando) → **green** (código mínimo).
- Abrir `tests/BarberHub.Tests/AgendamentoServiceTests.cs` e ler um teste em voz alta, por exemplo `Horario_que_sobrepoe_atendimento_do_mesmo_barbeiro_e_recusado`.
- Rodar `dotnet test` → **70 testes** passando, com **PostgreSQL real** (Testcontainers).
- Citar as outras técnicas: multi-tenant (teste "barbearia não altera atendimento de outra"), senha com hash, relógio controlado nos testes.

## 5. Uso de IA (1 min)

- Ferramenta: **Claude Code** (Claude Opus 5.5).
- Ajudou no planejamento do escopo, na programação em TDD, nos testes, na solução de problemas e na documentação.
- **Exemplo concreto:** quando eu levantei o risco de o cliente digitar o telefone errado, a IA propôs a proteção em camadas (validação, conferência e limite de horários), que virou regra testada.
- **Outro exemplo:** revisando os prints numa tela de notebook, eu vi o rodapé sobreposto; a correção fez as seções crescerem com o conteúdo.
- "Revisei todo o código e sei explicar cada parte."

## 6. Dificuldades e próximos passos (30 s)

- **Dificuldades:** regras de horário (sobreposição, caber no expediente), manter a disciplina do TDD, cobrança sem credenciais.
- **Próximos passos:** gateway real, código de confirmação (OTP) e lembrete por WhatsApp, folgas por barbeiro, publicação em nuvem.

## Perguntas prováveis do professor

| Pergunta | Resposta curta |
|---|---|
| Por que Razor e não React + API? | Para um MVP, a renderização no servidor tem menos peças, eu domino C# e o foco ficou nas regras de negócio. Se surgir um app mobile, os serviços já estão separados e dá para expor uma API depois. |
| Como garante que uma barbearia não vê dados da outra? | O Id da barbearia vem do cookie de login, e todo serviço filtra por `BarbeariaId`. Há testes específicos para isso. |
| E se dois clientes tentarem o mesmo horário juntos? | O serviço verifica a sobreposição antes de gravar. Em produção, adicionaríamos uma constraint de exclusão no PostgreSQL (`EXCLUDE USING gist`) para cobrir a concorrência. |
| Onde fica a senha do banco? | No arquivo `.env`, que não vai para o git. O repositório tem só o `.env.example`. A senha dos usuários é guardada com hash PBKDF2. |
| Por que o cliente não tem login? | Atrito: quanto menos passos, mais agendamentos. Ele é identificado pelo celular, com validação, tela de conferência e limite de 2 horários em aberto. |
| O que é mockado nos testes? | Só o gateway de pagamento, que é um sistema externo. O banco é um PostgreSQL real em container. |
