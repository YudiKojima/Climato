# 0005 — Mapster no lugar do AutoMapper

**Data:** 2026-10-16
**Status:** Aceito
**Fase:** 01-higiene-fundacao

## Contexto

O Gateway usava AutoMapper 14 para dois mapeamentos (`City → CityDto`, `User → UserDto`). A partir da v15 o AutoMapper passou a ter licença comercial; a v14, gratuita, ficou congelada, sem atualização para o .NET 10 e com alerta de vulnerabilidade publicado em 2025.

## Decisão

Usar **Mapster** (licença MIT), configurado por classes `IRegister` em `Application/Mappings/` e injetado como `MapsterMapper.IMapper`, em modo runtime.

## Alternativas consideradas

- **Mapeamento manual (métodos de extensão)** — zero dependências e explícito; preterido para manter um mapeador declarativo que escale se os DTOs crescerem.
- **Manter AutoMapper 14** — sem trabalho agora, mas congelado, com alerta de segurança e risco de licença.
- **Mapster com geração de código (`Mapster.Tool`)** — sem reflexão em runtime, mas exige passo extra de build; não compensa para dois mapeamentos. Evolução possível se o número de DTOs crescer.

## Consequências

- Dependências: `Mapster` e `Mapster.DependencyInjection` só na `Gateway.Application`.
- Mapeamentos novos entram em `Application/Mappings/` (regra BE-19 do checklist).
- A troca para geração de código no futuro não muda os chamadores (`IMapper.Map<T>`).
