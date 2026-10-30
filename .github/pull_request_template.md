# 📋 Description

<!-- Briefly explain what problem this PR solves, what functionality it introduces, or what behavior it changes. -->

# 🗒️ Summary

<!-- Describe the main changes in a few sentences. -->

# 🚀 Changes Made

<!-- List the relevant changes. -->

- 
- 
- 

# 🔗 Ticket / User Story Reference

<!-- Provide the ID of the User Story (US) or Technical Task (TS). -->

| USID | TITLE | DESCRIPCION |
|------|-------|-------------|
|      |       |             | 

# 🛠️ Change Type

<!-- Mark all applicable options with an 'x'. -->

- [ ] ✨ New feature (`feat`)
- [ ] 🐛 Bug fix (`fix`)
- [ ] ♻️ Code refactoring (`refactor`)
- [ ] 🧪 Tests added or updated (`test`)
- [ ] 📝 Documentation update (`docs`)
- [ ] ⚙️ Configuration / CI-CD (`chore`)

# 🏗️ Architecture and Design

- [ ] The changes follow the domain-driven architecture and respect **Bounded Context** boundaries.
- [ ] Responsibilities are placed in the appropriate layer.
- [ ] No unnecessary dependencies have been introduced between layers or Bounded Contexts.
- [ ] Asynchronous operations in C# (.NET 8) correctly use `async/await`.
- [ ] No `async void` methods have been introduced unless strictly required for event handlers.

# 🧪 Testing

- [ ] Unit and/or integration tests have been added or updated as required.
- [ ] Tests use **xUnit**.
- [ ] Tests strictly follow the **AAA (Arrange, Act, Assert)** pattern.
- [ ] Successful scenarios (**Happy Path**) are covered.
- [ ] Error scenarios and edge cases are covered where applicable.
- [ ] All tests pass successfully locally.
- [ ] Existing tests have not been removed without valid justification.

## 📚 API and Documentation

- [ ] **Swagger / OpenAPI** documentation has been updated for the affected endpoints.
- [ ] HTTP request/response contracts are correctly documented.
- [ ] Relevant HTTP response status codes are documented.
- [ ] No undocumented **breaking changes** have been introduced.

## 🗄️ Database

<!-- Mark the applicable options. -->

- [ ] No database changes are required.
- [ ] Required database migrations have been created/updated.
- [ ] Database migrations have been tested successfully.
- [ ] Compatibility with existing data has been considered.

# 🔐 Security / DevSecOps

- [ ] No secrets, passwords, tokens, API keys, or credentials are exposed in the source code.
- [ ] No secrets have been added to version-controlled configuration files.
- [ ] Logs have been reviewed to ensure that sensitive information is not exposed.
- [ ] Input data is properly validated where applicable.
- [ ] No known security vulnerabilities or unsafe dependencies have been introduced.

# ⚠️Breaking Changes

<!-- Indicate whether this PR introduces breaking changes for other services, clients, or API consumers. -->

- [ ] No breaking changes.
- [ ] Breaking changes introduced.

**Details:**

<!-- Explain the breaking changes and how consumers should adapt. -->

# 📸 Screenshots / Evidence

<!-- Optional but recommended. Add screenshots of the local dashboard, Swagger, terminal logs, HTTP responses, etc. -->


# 📝 Notes for the Reviewer

<!-- Add any important considerations that the reviewer should be aware of. -->

# 👀 Reviewer Checklist

- [ ] The code is clear, maintainable, and consistent with project standards.
- [ ] The implementation meets the acceptance criteria of the ticket.
- [ ] The architecture and separation of responsibilities are appropriate.
- [ ] Tests adequately cover the implemented changes.
- [ ] No obvious security issues have been identified.
- [ ] Documentation has been updated where necessary.
- [ ] The PR is ready to be approved.

## User story / technical story

Closes: USxx / TSxx

## What changed

-

## Bounded context(s) affected

- [ ] Identity & Access
- [ ] Fatigue Detection
- [ ] Alerts
- [ ] Fleet Monitoring
- [ ] Incident Management
- [ ] Frontend / Edge / Infrastructure

## How it was tested

-

## Checklist (Definition of Done)

- [ ] Branch follows GitFlow and commits follow Conventional Commits with scope
- [ ] Business rules live in the domain layer and throw `DomainException` when broken
- [ ] xUnit tests added or updated (AAA pattern) and passing locally
- [ ] CI is green (build, tests, SonarQube)
- [ ] Acceptance criteria of the story are met
- [ ] Documentation updated (README, context map, `.http` files) when needed
