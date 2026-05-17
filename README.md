## 1. Architettura Core e Gestione Dati

Il sistema si basa su un'architettura **Event Sourcing** con pattern **CQRS** (separazione tra modello di scrittura e lettura), implementata in modo speculare su due stack tecnologici.

* **Dual-Backend Speculare (C# / Java):** Due implementazioni indipendenti e intercambiabili (ASP.NET Core 8 e Spring Boot 3.5) che condividono lo stesso storage transazionale. Lo switching tra i backend è gestito tramite profili Docker Compose.
* **Event Store (Write Model):** La tabella `domain_events` su PostgreSQL funge da *Sorgente di Verità* append-only. Ogni mutazione è un evento immutabile. Lo stato corrente di un aggregato (es. `Account`) viene ricostruito in memoria riapplicando la cronologia dei suoi eventi.
* **Tabelle di Proiezione (Read Model):** Tabelle relazionali (`accounts`, `orders`, `transactions`) ottimizzate per le letture. Nelle Fasi 1-3, l'aggiornamento avviene in modo **sincrono** nella stessa transazione PostgreSQL dell'evento (Forte Consistenza), garantendo il riallineamento immediato del read model al costo di una doppia operazione di scrittura.
* **Allineamento Schema:** C# (EF Core) è il *Master* delle migrazioni. Java si limita a validare lo schema all'avvio (`ddl-auto=validate`), con sincronizzazione garantita dalla CI pipeline.

---

## 2. Struttura del Monorepo e Infrastruttura DevOps

Il progetto è organizzato come un **Monorepo** per centralizzare i servizi e automatizzare l'ambiente di sviluppo locale.

### Mappatura della Soluzione

```text
nexus-engine/
├── backend-csharp/     # ASP.NET Core 8, Swashbuckle (Swagger)
├── backend-java/       # Spring Boot 3.5.14, JPA, Java 21
├── frontend/           # React + TypeScript, Vite, Nginx
├── docker-compose.yml  # Orchestrazione locale con profili per backend
├── Makefile            # Shortcut mnemonici per comandi Docker complessi
└── global.json         # Blocco dell'SDK .NET 8 per consistenza ambientale

```

### Componenti Infrastrutturali

* **Dockerfile dedicati:** Implementano build *multi-stage* per generare immagini di runtime minimali e isolate.
* **Docker Compose:** Configura le reti virtuali interne e mappa le porte, permettendo l'avvio dell'intero ecosistema con un solo comando grazie ai profili.
* **Nginx (`nginx.conf`):** Serve i file statici del frontend React e funge da *Reverse Proxy*, smistando le richieste HTTP verso il backend attivo (C# o Java) e risolvendo i problemi di CORS.
* **Makefile:** Semplifica la CLI di progetto (es. `make up` o `make build`) evitando la digitazione di stringhe Docker chilometriche.

---

## 3. Principi di Domain-Driven Design (DDD) e Persistenza

L'applicazione segue rigorosamente la **Dependency Rule**: le dipendenze puntano esclusivamente verso l'interno (il Domain è isolato e non conosce l'Infrastructure).

* **Domain:** Contiene le regole di business e i vincoli finanziari. È scritto in C# puro, senza riferimenti a ORM o database.
* **Entities:** Oggetti definiti da un'identità univoca persistente nel tempo (UUID), come `Account`, `Order` e `Transaction`. Si distinguono dai *Value Objects* che non hanno ID.
* **Configurations (`IEntityTypeConfiguration<T>`):** Classi nello strato *Infrastructure* che istruiscono EF Core su come mappare esplicitamente le entità del dominio sul database tramite Fluent API. Evitano le convenzioni automatiche per implementare ottimizzazioni avanzate e mantengono il dominio pulito. Vengono caricate automaticamente nel `DbContext` tramite Reflection.

---

## 4. Scelte Tecniche Rilevanti e Problemi Risolti

### Decisioni di Ingegnerizzazione del Software

| Elemento | Scelta Tecnica | Motivazione Architetturale |
| --- | --- | --- |
| **Valori Monetari** | `NUMERIC(18,2)` | Evitare tassativamente gli errori di arrotondamento dei tipi `float` o `double` in contesti finanziari. |
| **Payload/Response** | `JSONB` (PostgreSQL) | Permette l'indicizzazione e la compressione nativa, risultando più efficiente del testo generico. |
| **Concorrenza** | `UNIQUE(aggregate_id, aggregate_version)` | Meccanismo di *Optimistic Locking* sull'Event Store: impedisce a scritture concorrenti di sovrascrivere lo stesso stato. |
| **Integrità Referenziale** | `onDelete: Restrict` | Divieto assoluto di cancellazione a cascata sui record finanziari. |
| **Performance Java** | `open-in-view=false` | Rilascio immediato della connessione al database dopo lo strato transazionale. |

### Risoluzione Anomalie in Fase di Setup

* **Allineamento SDK:** Bloccato l'ambiente C# a .NET 8 tramite `global.json` per sovrascrivere eventuali SDK locali .NET 10 instabili.
* **Discrepanze API:** Rimosso il pacchetto nativo OpenAPI di .NET 8 non compatibile ed esteso l'uso di Swashbuckle per la generazione di Swagger.
* **Corruzione file:** Riscritto manualmente `nginx.conf` a causa di problemi di encoding generati da PowerShell.
* **Stabilità DB:** Introdotto un `start_period: 10s` nell'healthcheck di PostgreSQL per evitare il crash dei servizi dipendenti durante il cold start del database.
