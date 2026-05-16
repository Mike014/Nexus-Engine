.PHONY: up-csharp up-java down logs-csharp logs-java db-shell

up-csharp:
	docker compose --profile csharp up --build
# 	Questo comando valida la sintassi del docker-compose.yml senza avviare niente. Se non da' errori, l'infrastruttura base e' corretta.

up-java:
	docker compose --profile java up --build

down:
	docker compose --profile csharp --profile java down

logs-csharp:
	docker compose --profile csharp logs -f backend-csharp

logs-java:
	docker compose --profile java logs -f backend-java

db-shell:
	docker compose exec postgres psql -U nexus -d nexusdb