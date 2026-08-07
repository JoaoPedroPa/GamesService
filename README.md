# 🧪 FCG Games - Projeto de Testes

Este projeto contém os **testes automatizados do GamesService** da aplicação FIAP Cloud Games (FCG).

O objetivo é validar as principais regras de negócio do serviço de jogos, garantindo maior confiabilidade durante alterações no código e permitindo a execução automática dos testes através da pipeline de CI.

---

# 🧱 Estrutura dos Testes

Os testes são focados principalmente na camada de aplicação do **GamesService**, utilizando mocks para isolar dependências externas.

Entre os componentes testados estão:

- **GameService**
- Regras de criação de jogos
- Validações de dados de entrada
- Comunicação com repositórios
- Integração lógica com o PaymentsService
- Indexação e busca de jogos
- Publicação/armazenamento de eventos

---

## 🧩 Tecnologias utilizadas

- .NET 9
- xUnit
- Moq
- Coverlet
- ReportGenerator
- GitHub Actions (CI)

---

# ⚙️ Estratégia de Testes

## 1. Testes Unitários

Os testes unitários validam o comportamento das regras de negócio de forma isolada.

Dependências externas são substituídas por **Mocks**, permitindo testar apenas a responsabilidade da classe em análise.

### Benefícios:

- Execução rápida
- Isolamento das regras de negócio
- Detecção antecipada de regressões
- Maior segurança durante refatorações
- Facilidade de manutenção

---

## 2. Mock de Dependências

O projeto utiliza **Moq** para simular dependências utilizadas pelo `GameService`.

Exemplos:

- `IGamesRepository`
- `IPaymentsClient`
- `IGameSearchRepository`
- `IEventStore`

Dessa forma, os testes não dependem de banco de dados, Elasticsearch, APIs externas ou outros microsserviços para validar as regras internas do serviço.

---

## 3. Validação de Exceções

Também são testados cenários inválidos para garantir que o serviço retorne os erros esperados.

Exemplo:

```csharp
var exception = await Assert.ThrowsAsync<ArgumentException>(
    () => _gameService.CreateAsync(request)
);
```

Esse teste valida que uma operação inválida lança uma `ArgumentException`.

---

# 📊 Cobertura de Código

A pipeline executa os testes com coleta de cobertura.

A cobertura permite identificar quais partes do código estão sendo exercitadas pelos testes automatizados.

O relatório é gerado utilizando:

- **Coverlet** para coleta de cobertura
- **ReportGenerator** para geração do relatório visual

---

# 🔄 Integração Contínua

O projeto utiliza **GitHub Actions** para executar automaticamente o processo de validação.

A pipeline realiza as seguintes etapas:

1. Baixa o código do repositório
2. Configura o .NET
3. Restaura as dependências
4. Compila a solução
5. Executa os testes com cobertura
6. Instala o ReportGenerator
7. Gera o relatório visual de cobertura
8. Exibe informações de cobertura no Summary
9. Salva os resultados dos testes como artifact
10. Salva o relatório de cobertura como artifact

---

## ✅ Resultado esperado da pipeline

Quando todos os testes passam, o job **Build and Test** é concluído com sucesso.

No passo:

```text
Executar testes com cobertura
```

é possível visualizar o resultado semelhante a:

```text
Passed! - Failed: 0, Passed: X, Skipped: 0, Total: X
```

Caso algum teste falhe, a pipeline também será marcada como falha.

---

# 🚀 Como executar os testes

## Pré-requisitos

- .NET 9 SDK
- Visual Studio 2022, VS Code ou terminal

---

## Executar todos os testes

Na raiz da solução:

```bash
dotnet test
```

---

## Executar os testes em modo Release

```bash
dotnet test --configuration Release
```

---

## Executar testes com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

# 📁 Relatórios da Pipeline

Após a execução da pipeline no GitHub Actions, os arquivos gerados podem ser encontrados na seção **Artifacts** da execução.

São disponibilizados:

- Resultados dos testes
- Relatório visual de cobertura

O relatório de cobertura pode ser aberto localmente para visualizar quais classes, métodos e linhas foram cobertos pelos testes.

---

# 🛡️ Qualidade e Confiabilidade

A utilização de testes automatizados ajuda a garantir:

- Validação das principais regras de negócio
- Redução de regressões
- Maior segurança durante alterações
- Feedback rápido através da CI
- Melhor manutenibilidade do código

---

# 🔗 Projeto Relacionado

🎮 **GamesService**

Microsserviço responsável pelo gerenciamento dos jogos da plataforma FCG.

O projeto de testes deve ser executado em conjunto com a solução do GamesService para validar seu comportamento durante o processo de desenvolvimento e integração contínua.

---

## ▶️ Execução pela Pipeline

Além da execução local, os testes são executados automaticamente pelo GitHub Actions em eventos configurados no workflow, como alterações enviadas para a branch principal e Pull Requests.

Isso garante que mudanças no código sejam validadas antes de serem integradas ao projeto.

Link do video no Youtube 👉 https://www.youtube.com/watch?v=1EplBnhhvUU
