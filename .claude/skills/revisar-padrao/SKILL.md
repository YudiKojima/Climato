---
name: revisar-padrao
description: Revisa o diff da branch atual contra os padrões do Climato (docs/architecture/review-checklist.md) e gera um relatório por severidade. Usar antes de qualquer commit e antes de abrir PR.
---

# Revisar padrão

Gera um relatório; **não altera arquivos**.

## 1. Escopo

```bash
BASE=$(git merge-base HEAD origin/main)
git diff --stat "$BASE"
git diff "$BASE"
git ls-files --others --exclude-standard
```

Considerar: commits da branch + alterações não commitadas + arquivos novos não rastreados (ler o conteúdo deles inteiro).

## 2. Regras

1. Ler `docs/architecture/review-checklist.md` inteiro.
2. Descobrir a fase atual: maior `NN` em `docs/features/NN-*/` com `design.md`. Regras com **Desde** maior que a fase atual só se aplicam se o diff introduz aquele assunto (ver "Vigência" no checklist).
3. Arquivos em `src/**`: ler as seções de `docs/architecture/backend.md` linkadas pelas regras BE aplicáveis. Arquivos em `web/**`: as de `docs/architecture/frontend.md` (regras FE).
4. Regras GE valem sempre.
5. Avaliar cada regra **somente sobre linhas adicionadas ou alteradas** e arquivos novos. Código não tocado não gera achado — mesmo que esteja na auditoria.
6. Se o diff resolve códigos da auditoria (`A*`), conferir se `docs/features/00-roteiro-portfolio/auditoria-inicial.md` foi marcado (GE-06 no commit final da fase).

## 3. Relatório

```markdown
## Revisão de padrões — <branch> (<N> arquivos) · fase atual: NN

### 🔴 Bloqueantes
| ID | Local | Problema | Sugestão |
|---|---|---|---|
| BE-02 | `src/Gateway/.../CityAppService.cs:20` | ... | ... |

### 🟡 Avisos
| ID | Local | Problema | Sugestão |

### 🔵 Sugestões
| ID | Local | Problema | Sugestão |

**Auditoria resolvida neste diff:** A8, A10 (ou "nenhuma")

**Veredito:** ✅ pode commitar · ou · ⛔ há N itens 🔴
```

Seções sem achados mostram "Nenhum". Cada achado cita `arquivo:linha`.

## 4. Depois do relatório

- ⛔: não commitar. Corrigir os 🔴 ou o usuário libera explicitamente cada item (citar o ID).
- ✅: seguir com o commit/PR que foi pedido.
