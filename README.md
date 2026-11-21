# 🚀 Prova II - Sistemas Web II: Ecossistema Distribuído (Web API + Desktop + WebSite + xUnit)

## 📖 Sobre o Projeto
Solução distribuída em **.NET 8** desenvolvida para a **Prova II** da disciplina de Sistemas Web II (CBTSWE2 — IFSP Campus Cubatão), aplicando a arquitetura REST e TDD da Aula 09. Toda a camada de persistência e regras de negócio fica isolada na **Web API (`Prova2.API`)**, consumida exclusivamente via HTTP/JSON pelo **Cliente Desktop Windows Forms (`Prova2.Desktop`)** para administração de usuários e pelo **WebSite ASP.NET Core MVC (`Prova2.Web`)** para operação autenticada de produtos, com auditoria completa de ações.

## 🎯 Requisitos da Prova II e Diferenciais Implementados
1. **Banco de Dados Relacional (`SQLite` + EF Core):** Persistência das tabelas `Usuarios` (`Id`, `Nome`, `Senha` em hash SHA-256, `Status`), `Produtos` (`Id`, `Nome`, `Preco` float, `Status`, `IdUsuarioCadastro`, `IdUsuarioUpdate`) e `Logs` (`LogAuditoria`), com chaves estrangeiras `ON DELETE RESTRICT` e provisionamento automático via `Database.EnsureCreated()`.
2. **Web API RESTful (`Prova2.API`):** CRUD completo (`GET`, `GET/{id}`, `POST`, `PUT/{id}`, `DELETE/{id}`) para `Usuarios` e `Produtos`, autenticação (`POST /api/v1/Usuarios/login`) e consulta de auditoria (`GET /api/v1/Logs`), utilizando envelopes padronizados (`PagedResponse`, `SingleResponse`, `Response`), logging estruturado e **Swagger UI** com documentação XML.
3. **Aplicação Desktop (`Prova2.Desktop`):** Cliente Windows Forms (`.NET 8.0-windows`) dedicado à gestão completa de usuários consumindo a Web API, com filtros por nome/status, alternância rápida de status, campo de senha mascarado (`*`) com botão de visualização (olho) e **checklist interativo em tempo real** de requisitos de senha forte.
4. **WebSite MVC Autenticado (`Prova2.Web`):** Portal em Bootstrap 5 (`#0A3D62`) protegido por **Cookie Authentication**. Apenas usuários ativos acessam o CRUD de Produtos, gravando automaticamente `IdUsuarioCadastro` na inclusão e `IdUsuarioUpdate` em qualquer edição ou alteração de status.
5. **Segurança e Auditoria Avançada:**
   - **Módulo de Logs Unificado:** Registra cronologicamente acessos (`LOGIN_SUCESSO`, `LOGIN_FALHA`, `LOGIN_BLOQUEADO`), criações, edições, mudanças de status e exclusões originadas do WebSite, Desktop ou API.
   - **Revogação Instantânea de Sessão:** Validação contínua via `CookieAuthenticationEvents.OnValidatePrincipal`; ao inativar um usuário no Desktop, sua sessão no WebSite é encerrada imediatamente na próxima requisição.
   - **Política de Senha Forte:** Exige mínimo de 8 caracteres, letra maiúscula, minúscula, número e caractere especial (validada tanto no Desktop quanto no servidor via `SenhaForteAttribute`).
   - **Proteção contra Lockout:** Impede inativar ou excluir o único usuário ativo restante no sistema.
   - **KPIs e Exportação CSV:** Cards de métricas no catálogo de produtos e exportação em `.csv` (UTF-8 com BOM) para Produtos e Logs de Auditoria.
6. **Testes Unitários (`Prova2.Tests`):** Suíte **xUnit** com `DbContextMocker` e `Microsoft.EntityFrameworkCore.InMemory` cobrindo **13 cenários** de `UsuariosController`, `ProdutosController` e `LogsController`.

## 🏛️ Arquitetura da Solução (`Prova2.sln`)
```text
Prova2/
├── Prova2.sln
├── README.md
├── .gitignore
│
├── Prova2.API/                     # Web API REST (.NET 8) — http://localhost:5100
│   ├── Controllers/
│   │   ├── UsuariosController.cs   # CRUD de Usuários, Login, Lockout e Política de Senha
│   │   ├── ProdutosController.cs   # CRUD de Produtos com Auditoria e Logs
│   │   └── LogsController.cs       # Consulta filtrada de Logs de Auditoria
│   ├── Models/
│   │   ├── Domain.cs               # Entidades, PasswordHelper, SenhaForteAttribute e DbContext
│   │   ├── Extensions.cs           # Consultas LINQ, Paginação e RegistrarLogAsync
│   │   ├── Requests.cs             # DTOs de entrada (Post/Put/Login)
│   │   └── Responses.cs            # Envelopes IResponse/IPagedResponse e ToHttpResponse()
│   ├── Properties/launchSettings.json
│   ├── appsettings.json
│   └── Program.cs
│
├── Prova2.Desktop/                 # Cliente Desktop WinForms (.NET 8.0-windows)
│   ├── Models/UsuarioDtos.cs
│   ├── Services/UsuarioApiClient.cs
│   ├── MainForm.cs                 # Gestão de Usuários, Checklist de Senha Forte e Link API
│   └── Program.cs
│
├── Prova2.Web/                     # WebSite ASP.NET Core MVC (.NET 8) — http://localhost:5200
│   ├── Controllers/
│   │   ├── AccountController.cs    # Login / Logout via Cookie Auth
│   │   ├── ProdutosController.cs   # CRUD, KPIs, Toggle Status e Exportação CSV
│   │   ├── LogsController.cs       # Histórico de Auditoria e Exportação CSV
│   │   └── HomeController.cs       # Créditos da Equipe
│   ├── Models/ApiDtos.cs           # ViewModels e parser pt-BR para Preço (float)
│   ├── Services/Prova2ApiClient.cs # Consumo HTTP da Prova2.API
│   └── Views/                      # Account, Produtos, Logs, Home e Shared (_Layout)
│
└── Prova2.Tests/                   # Testes Unitários xUnit (13 testes — Padrão Aula 09)
    ├── DbContextExtensions.cs
    ├── DbContextMocker.cs
    ├── UsuariosControllerUnitTest.cs
    └── ProdutosControllerUnitTest.cs
```

## 🛣️ Endpoints da Web API (`http://localhost:5100`)

| Entidade | Verbo | Endpoint | Descrição |
|----------|-------|----------|-----------|
| Usuários | `GET` | `/api/v1/Usuarios` | Lista paginada com filtros opcionais (`nome`, `status`) |
| Usuários | `GET` | `/api/v1/Usuarios/{id}` | Consulta usuário por ID |
| Usuários | `POST` | `/api/v1/Usuarios` | Cadastra usuário validando senha forte e hash SHA-256 |
| Usuários | `PUT` | `/api/v1/Usuarios/{id}` | Atualiza dados/status (com trava contra lockout do último ativo) |
| Usuários | `DELETE` | `/api/v1/Usuarios/{id}` | Remove usuário sem vínculos de auditoria |
| Usuários | `POST` | `/api/v1/Usuarios/login` | Autentica usuário ativo e registra evento de sessão nos Logs |
| Produtos | `GET` | `/api/v1/Produtos` | Lista paginada com dados de autoria de cadastro e atualização |
| Produtos | `GET` | `/api/v1/Produtos/{id}` | Consulta produto por ID com metadados de auditoria |
| Produtos | `POST` | `/api/v1/Produtos` | Cadastra produto registrando `IdUsuarioCadastro` e log |
| Produtos | `PUT` | `/api/v1/Produtos/{id}` | Atualiza produto/status registrando `IdUsuarioUpdate` e log |
| Produtos | `DELETE` | `/api/v1/Produtos/{id}` | Remove produto e registra exclusão nos Logs |
| Logs | `GET` | `/api/v1/Logs` | Lista histórico de auditoria com filtros (`entidade`, `acao`, `origem`, `busca`) |

## 🚀 Como Executar o Projeto

### 1. Inicialização Simultânea no Visual Studio
1. Abra `Prova2.sln` no Visual Studio 2022 ou Visual Studio 2026.
2. Clique com o botão direito na solução > **Configurar Projetos de Inicialização...**
3. Selecione **Vários projetos de inicialização** e defina **Iniciar** na seguinte ordem:
   - `Prova2.API` (`http://localhost:5100/swagger`)
   - `Prova2.Desktop` (Aplicação de Gestão de Usuários)
   - `Prova2.Web` (`http://localhost:5200`)
4. Pressione **`F5`**. O banco `banco_prova2.db` será criado e populado automaticamente.

### 2. Credenciais Iniciais (Seed)
Para avaliação inicial (antes de criar novos usuários pelo aplicativo Desktop):
- **Usuário:** `admin` | **Senha:** `Admin@123` (Ativo)
- **Usuário:** `operador` | **Senha:** `Operador@123` (Ativo)

### 3. Executar os Testes Unitários
No Visual Studio, acesse **Teste > Executar Todos os Testes** (ou execute `dotnet test` na raiz da solução) para validar os **13 testes unitários** em memória.

## 👨‍💻 Autores
**Stiven Richardy Silva Rodrigues**  
*Estudante de Análise e Desenvolvimento de Sistemas | IFSP — Campus Cubatão*  
GitHub: [@Stiven-Richardy](https://github.com/Stiven-Richardy)

**Guilherme Mendes de Sousa**  
*Estudante de Análise e Desenvolvimento de Sistemas | IFSP — Campus Cubatão*  
GitHub: [@Guilh3rme-M3ndes](https://github.com/Guilh3rme-M3ndes)

## 📚 Referências
- Aula 09 (Prática: Web API REST, Swagger XML, xUnit e InMemoryDatabase) e Enunciado da Prova II — Prof. Me. Wellington Tuler Moraes (IFSP Campus Cubatão).
- Documentação Oficial Microsoft: ASP.NET Core 8, Entity Framework Core, Windows Forms e xUnit.
