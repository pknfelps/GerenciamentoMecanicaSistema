# 🔧 Sistema de Gerenciamento de Mecânica

Sistema desenvolvido para o gerenciamento completo de uma mecânica, contemplando usuários, clientes, veículos, estoque, catálogo de serviços e ordens de serviço.

---

## 📋 Funcionalidades

- Gerenciamento de **usuários**
- Gerenciamento de **clientes**
- Gerenciamento de **veículos**
- Gerenciamento de **estoque**
- **Catálogo de serviços**
- **Ordens de serviço** com envio de e-mail ao concluir diagnóstico

---

## 🛠️ Tecnologias

- [.NET 10 / ASP.NET 10](https://dotnet.microsoft.com/)
- [PostgreSQL 16](https://www.postgresql.org/)
- [Docker](https://www.docker.com/)

---

## ✅ Pré-requisitos

Antes de começar, certifique-se de ter instalado em sua máquina:

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (inclui Docker Compose)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) com a carga de trabalho **ASP.NET e desenvolvimento Web**

> ⚠️ O Visual Studio é necessário para utilizar o perfil de execução via container. O ambiente foi desenvolvido para **Windows**.

---

## 🚀 Instalação

Clone o repositório:

```bash
git clone https://github.com/pknfelps/GerenciamentoMecanicaSistema
```

Não é necessária nenhuma configuração adicional. O ambiente já está completamente configurado no repositório.

---

## ▶️ Executando a aplicação

Siga os passos **na ordem abaixo**:

**1. Suba os containers com o Docker Compose:**

```bash
docker compose up
```

Isso irá inicializar o banco de dados PostgreSQL e o servidor SMTP local. O script `Init.sql` será executado automaticamente, criando todas as tabelas e populando os dados iniciais.

**2. Execute a aplicação pelo Visual Studio:**

Abra a solução no Visual Studio 2022, selecione o perfil de execução **`Container (Dockerfile)`** e inicie a aplicação.

---

## 🌐 Portas utilizadas

| Serviço       | URL / Porta                  |
|---------------|------------------------------|
| API           | `http://localhost:8080`      |
| PostgreSQL    | `localhost:5432`             |
| SMTP (serviço)| porta `2525`                 |
| SMTP (painel) | `http://localhost:3000`      |

---

## 🔑 Credenciais padrão

As credenciais abaixo são criadas automaticamente pelo `Init.sql` ao iniciar o banco de dados:

| Campo    | Valor       |
|----------|-------------|
| Nome     | `Admin`     |
| Senha    | `Admin@123` |
| Perfil   | `Admin`     |

---

## 🧪 Testando com o Postman

Os arquivos de configuração do Postman estão disponíveis no repositório. Para utilizá-los:

1. Abra o **Postman**
2. Clique em **Import**
3. Selecione os arquivos disponíveis na pasta `/postman` do repositório
4. As coleções e variáveis de ambiente já estarão configuradas e prontas para uso

> O `Init.sql` já registra um **usuário, cliente, veículo, serviço e material** para que todas as operações possam ser testadas diretamente.

---

## 📧 Testando o envio de e-mail

O ambiente inclui um servidor SMTP local para interceptar e visualizar os e-mails enviados pelo sistema.

Ao concluir o diagnóstico de uma ordem de serviço, o sistema dispara um e-mail automaticamente. Para visualizá-lo, acesse o painel do servidor SMTP:

```
http://localhost:3000
```

Todas as tentativas de envio serão registradas e exibidas neste painel.