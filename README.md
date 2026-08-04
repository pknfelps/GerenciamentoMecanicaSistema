# 🔧 Sistema de Gerenciamento de Mecânica

API para gerenciamento de usuários, clientes, veículos, estoque, catálogo de serviços e ordens de serviço.

## 🛠️ Tecnologias

- .NET 10 / ASP.NET Core 10
- PostgreSQL 16
- Docker e Docker Compose
- smtp4dev para captura local dos e-mails

## ▶️ Execução local com Docker Compose

### ✅ Pré-requisitos

- Docker Desktop com Docker Compose v2
- Portas `8080`, `5432`, `3000` e `2525` disponíveis, ou alteradas no arquivo `.env`

### Configuração

Crie o arquivo local de variáveis a partir do exemplo:

```bash
cp .env.example .env
```

No PowerShell:

```powershell
Copy-Item .env.example .env
```

Revise principalmente `POSTGRES_PASSWORD` e `JWT_KEY`. O arquivo `.env` é ignorado pelo Git.

### 🚀 Inicialização

Construa a imagem e suba todo o ambiente:

```bash
docker compose up --build -d
```

O Compose inicia:

- a API;
- o PostgreSQL;
- o smtp4dev.

A API só é iniciada depois que o PostgreSQL passa no health check. Na primeira criação do volume do banco, o script [Init.sql](InitDb/Init.sql) cria as tabelas e os dados iniciais.

Verifique o estado dos serviços:

```bash
docker compose ps
```

Consulte os logs da API:

```bash
docker compose logs -f api
```

### 🌐 Endereços locais

| Serviço | Endereço padrão |
|---|---|
| API | `http://localhost:8080` |
| Swagger UI | `http://localhost:8080/swagger` |
| Health check | `http://localhost:8080/health/ready` |
| PostgreSQL | `localhost:5432` |
| Painel do smtp4dev | `http://localhost:3000` |
| SMTP | `localhost:2525` |

As portas podem ser alteradas no `.env` sem modificar o Compose.

### Encerramento

Para parar os containers preservando os dados:

```bash
docker compose down
```

Para recriar completamente o banco e o armazenamento do smtp4dev:

```bash
docker compose down --volumes
docker compose up --build -d
```

O uso de `--volumes` remove permanentemente os dados locais dos volumes do projeto.

## 🔑 Dados iniciais

O script de inicialização registra um usuário administrativo:

| Campo | Valor |
|---|---|
| Nome | `Admin` |
| Senha | `Admin@123` |
| Perfil | `Admin` |

Também são criados cliente, veículo, serviço e material para testes das APIs.

## 🧪 Postman

As collections e o ambiente estão na pasta [postman](postman). Importe os arquivos no Postman depois que o ambiente estiver saudável.

## 📧 E-mails locais

As notificações de orçamento e atualização de status são enviadas para o smtp4dev. Elas não saem do ambiente local e podem ser visualizadas em `http://localhost:3000`.
