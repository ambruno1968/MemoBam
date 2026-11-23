# Regole di Progetto e Workspace — MemoBam

Queste regole si applicano automaticamente a tutte le sessioni e chat aperte su questo workspace (`d:\Repository\MemoBam`).

## 1. Punto di ripristino su GitHub prima di ogni modifica
- **PRIMA** di creare, modificare o eliminare qualsiasi file del progetto, verifica `git status`.
- Se sono presenti modifiche non ancora committate o non pushate, crea prima un **commit di ripristino** e fai **push su GitHub (`origin main`)** in modo che lo stato precedente sia sempre recuperabile.
- Se il working tree è già pulito e allineato con `origin/main`, annota l'ultimo commit come punto di ripristino prima di procedere.

## 2. Pubblicazione e aggiornamento versione solo su richiesta esplicita
- **NON aggiornare la versione** (`sw.js`, `README.md`, `CHANGELOG.md`, tag Git) e **NON pubblicare/pushare** modifiche su GitHub a meno che l'utente non lo richieda esplicitamente.
- Quando l'utente richiede esplicitamente un rilascio o una pubblicazione, incrementa la versione secondo **Semantic Versioning (SemVer)** e aggiorna `sw.js`, `README.md` e `CHANGELOG.md`.
