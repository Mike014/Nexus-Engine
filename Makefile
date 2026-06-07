.PHONY: up-csharp down logs-csharp db-shell

up-csharp:
	docker compose --profile csharp up --build

down:
	docker compose --profile csharp down

logs-csharp:
	docker compose --profile csharp logs -f backend-csharp

db-shell:
	docker compose exec postgres psql -U nexus -d nexusdb