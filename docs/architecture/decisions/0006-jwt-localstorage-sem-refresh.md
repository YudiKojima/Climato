# 0006 — JWT no localStorage, sem refresh token

**Data:** 2026-10-18
**Status:** Aceito
**Fase:** 02-seguranca

## Contexto

A "sessão" era só o `userId` no `localStorage`, enviado na query e nunca validado (A4, A7). A Fase 02 precisa de autenticação real sem inflar o escopo de um projeto de portfólio com um único front SPA.

## Decisão

O Gateway emite um **access token JWT** (HS256, 60 min configuráveis) em `/auth/register` e `/auth/login`. O web guarda o token no `localStorage` e o envia como `Authorization: Bearer`. **Não há refresh token**: expirou, a API responde 401 e o usuário faz login de novo.

## Alternativas consideradas

- **Access + refresh token rotativo** — sessão longa sem novo login, com revogação; exige tabela, endpoint `/auth/refresh` e lógica de renovação no front. Quase dobra o escopo da fase.
- **Cookie `HttpOnly` + `SameSite`** — token fora do alcance de JavaScript (protege contra XSS), mas exige proteção CSRF e configuração de cookie entre origens (web e Gateway em portas/domínios diferentes no deploy gratuito).

## Consequências

- Um XSS consegue ler o token. Mitigação: React escapa a saída por padrão, o projeto não usa `dangerouslySetInnerHTML`, e o token expira em 60 min.
- Não há logout no servidor: um token vazado vale até expirar.
- Revisitar se o app ganhar sessões longas, múltiplos clientes ou dados sensíveis — caminho natural é refresh token em cookie `HttpOnly`.
