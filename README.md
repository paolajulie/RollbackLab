# RollbackLab: Automação de Rollback com Árvore de Decisão

Este repositório contém o laboratório local da pesquisa de pós-graduação **"A esteira subiu uma versão que quebrou produção"**. O objetivo é projetar, formalizar e validar em código uma esteira de automação de mitigação e rollback guiada por árvore de decisão, onde cada folha (**A** a **H**) representa uma ação automática ou uma parada segura para intervenção humana.

O projeto foi construído inteiramente com o **.NET SDK**, sem qualquer dependência externa de nuvem, Docker, orquestradores ou bancos de dados reais.

---

## 1. Como Instalar o .NET SDK

Para compilar, testar e executar a solução, é necessário ter o .NET SDK instalado:

1. Baixe o instalador oficial do .NET SDK LTS para seu sistema operacional (Windows, Linux ou macOS) em:
   👉 [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
2. Após a instalação, confirme no terminal que o comando `dotnet` está disponível executando:
   ```bash
   dotnet --version
   ```
   *(O projeto é compatível com o target framework LTS do SDK instalado).*

---

## 2. Comandos Principais e Opções de Execução

Você pode executar o laboratório de duas maneiras: **nativamente via .NET SDK** (Opção A) ou **via Docker** (Opção B, sem precisar instalar o SDK).

### Opção A: Execução Nativa (.NET SDK)

Abra o terminal na pasta raiz da solução e utilize os comandos da CLI do .NET:

#### Compilação da Solução
```bash
dotnet build
```

#### Execução dos Testes Automatizados
Executa a suíte de testes xUnit (256 combinações do produto cartesiano, 8 cenários, consistência e testes fail-safe):
```bash
dotnet test
```

Para visualizar a saída detalhada com a contagem de decisões por folha:
```bash
dotnet test --logger "console;verbosity=normal"
```

#### Execução do Simulador
Executa o simulador temporal dos 8 cenários (A–H) com linha do tempo e tabela final:
```bash
dotnet run --project src/RollbackLab.Simulador
```

#### Exportação para CSV
```bash
dotnet run --project src/RollbackLab.Simulador -- --csv
```

---

### Opção B: Execução via Docker (Sem Instalar .NET SDK)

Caso prefira rodar em contêiner ou não tenha o .NET 10 SDK instalado na sua máquina, use o `Dockerfile` multi-stage incluído no projeto:

#### 1. Construir a Imagem (Compila e roda os 21 testes automaticamente durante o build)
```bash
docker build -t rollback-lab .
```
*(Se algum teste falhar, o build do Docker é interrompido com erro, garantindo a mesma governança de uma esteira real de CI/CD).*

#### 2. Executar o Simulador
```bash
docker run --rm rollback-lab
```

#### 3. Executar o Simulador exportando o CSV para sua máquina local
```bash
docker run --rm -v ${PWD}:/app/out rollback-lab --csv /app/out/resultados.csv
```

---

## 3. Estrutura de Pastas e Projetos

A arquitetura segue o isolamento em camadas limpas, utilizando contratos (portas) e fakes manuais puros:

```text
RollbackLab/
├── RollbackLab.sln                    # Arquivo da solução .NET
├── Dockerfile                         # Build multi-stage com testes automatizados e simulador
├── .dockerignore                      # Arquivos e pastas ignorados no contexto do Docker
├── README.md                          # Documentação técnica e guia do laboratório
├── resultados.csv                     # Arquivo gerado pelo simulador com a flag --csv
├── src/
│   ├── RollbackLab.Core/              # Biblioteca de classes com o domínio e esteira
│   │   ├── Modelos.cs                 # Enums (Migracao, Folha, Acao) e records (Estado, Decisao)
│   │   ├── ArvoreDecisao.cs           # Função pura com as regras de decisão (Folhas A-H)
│   │   ├── Portas.cs                  # Interfaces de integração (IManifesto, IRegistry, etc.)
│   │   └── Controlador.cs             # Orquestrador sequencial da esteira com fail-safe
│   └── RollbackLab.Simulador/         # Aplicação console de simulação temporal
│       ├── Custos.cs                  # Custos unitários de tempo configuráveis (constantes TimeSpan)
│       ├── RelogioVirtual.cs          # Implementação de IRelogio sem Thread.Sleep/Task.Delay
│       ├── SimuladorCenarios.cs       # Execução dos 8 cenários, linhas do tempo e relatório
│       └── Program.cs                 # Ponto de entrada da CLI (suporte a argumentos e --csv)
└── tests/
    └── RollbackLab.Tests/             # Suíte de testes xUnit sem bibliotecas de mock
        ├── Fakes.cs                   # Implementação de fakes manuais e FabricaDeFakes
        ├── ArvoreDecisaoTests.cs      # Teste do produto cartesiano (256 estados) e invariantes I1-I8
        ├── CenariosTests.cs           # Validação direta dos 8 cenários do documento (A-H)
        ├── ConsistenciaTests.cs       # Comparação Controlador vs Árvore em 256 estados
        ├── FailSafeTests.cs           # Validação da tolerância a falhas para cada porta
        └── SimuladorTests.cs          # Testes unitários do motor de simulação e exportação CSV
```

---

## 4. Árvore de Decisão e Mapeamento das Folhas (A–H)

A árvore avalia as condições do ambiente rigorosamente na seguinte ordem de precedência:

| Ordem | Regra / Condição Avaliada | Folha | Modo | Ações Executadas | Justificativa / Racional de Engenharia |
|:---:|:---|:---:|:---:|:---|:---|
| 1 | `!CorrelacionaComDeploy` | **H** | Humano | `NotificarPlantao` | Sem correlação comprovada com o deploy; não altera produção para evitar adicionar riscos desconhecidos. |
| 2 | `Migracao ∈ {Destrutiva, Desconhecida}` | **D** | Humano | `TravarDeploys`, `NotificarPlantao` | Voltar aplicação quebra a v2.3; down-migration ou restore de banco perderia pedidos, violando **RPO = 0**. |
| 3 | `TemFlag && FlagResolve` | **A** | Automático | `TravarDeploys`, `DesligarFlag`, `AbrirIncidente` | Desativação da feature flag contornou o problema sem necessidade de reverter binários ou pods. |
| 4 | `!ImagemExiste` | **E** | Humano | `TravarDeploys`, *(DesligarFlag)*, `NotificarPlantao` | Imagem da versão anterior ausente no registry; proíbe recompilação a quente por falta de auditoria. |
| 5 | `!PedidosCompativeis` | **F** | Humano | `TravarDeploys`, *(DesligarFlag)*, `NotificarPlantao` | Pedidos gravados durante a falha ficariam ilegíveis pela v2.3; rollback corromperia dados de negócio. |
| 6 | `!RollbackRecuperou` | **G** | Humano | `TravarDeploys`, *(DesligarFlag)*, `ExecutarRollback`, `ExecutarSmokeTest`, `NotificarPlantao` | Rollback e smoke test rodaram, mas métricas não normalizaram em 5 min; causa externa provável, impede loops. |
| 7 | `Migracao == Aditiva` | **C** | Automático | `TravarDeploys`, *(DesligarFlag)*, `ExecutarRollback`, `ExecutarSmokeTest`, `BloquearVersao`, `AbrirIncidente` | Rollback executado com sucesso com migração aditiva retrocompatível e pedidos íntegros. |
| 7 | `Migracao == Nenhuma` | **B** | Automático | `TravarDeploys`, *(DesligarFlag)*, `ExecutarRollback`, `ExecutarSmokeTest`, `BloquearVersao`, `AbrirIncidente` | Rollback executado com sucesso em versão sem alterações de banco de dados. |

> **Nota de Segurança (RPO = 0):** O enum `Acao` intencionalmente **não** contém nenhuma ação de "restaurar banco", "trocar infra" ou "hotfix automático". Qualquer intervenção no banco de dados além de migração aditiva retrocompatível exige parada obrigatória para análise humana.

---

## 5. Aviso Metodológico: Tempos Simulados por Composição

> ⚠️ **AVISO METODOLÓGICO SOBRE OS TEMPOS:**  
> Os tempos apresentados pelo simulador são **estritamente SIMULADOS por composição aditiva de custos de processo**, e **não** medidos via relógio físico da máquina ou tempos de espera real. O código utiliza um `RelogioVirtual` desacoplado, sendo terminantemente proibido o uso de chamadas bloqueantes como `Thread.Sleep` ou `Task.Delay`. Cada número reflete a soma determinística dos custos das operações de engenharia (análise de métricas, espera de estabilização estatística, validação de compatibilidade e tempo de transição de pods) configurados em `Custos.cs`.

### Tabela de Custos Unitários (`Custos.cs`)
- **Decisão inicial:** 30 s (consultas de manifesto, registry e correlação; cobrada 1 vez no início)
- **Janela de amostragem de Feature Flag:** 3 min (tempo para acumular amostras de tráfego)
- **Conferência da Feature Flag:** 30 s (avaliação de erro/sucesso após janela)
- **Execução do Rollback:** 2 min (troca do deployment / drenagem e prontidão de pods estáveis)
- **Smoke Test sintético:** 30 s (testes sintéticos pós-deploy em rotas vitais)
- **Janela de estabilização pós-rollback:** 5 min (observabilidade para confirmar normalização do negócio)
- **Validação de pedidos:** 30 s (cobrada apenas se `Migracao == Aditiva`)
- **Notificação e passagem de plantão:** 30 s (cobrada em qualquer parada para humano)

---

## 6. Resultados Consolidados dos 8 Cenários

A tabela a seguir resume os resultados obtidos pelo simulador para cada um dos 8 cenários padrão da pesquisa:

| Cenário | Folha | Tipo | Tempo Simulado | Ref. Documento | Divergência | Composição do Tempo Simulado |
|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| **A** | **A** | Automático | **04:00** (4,0 min) | ≈ 4,0 min | Nenhuma | `Decisao (30s) + EsperaFlag (3m) + ConferenciaFlag (30s)` |
| **B** | **B** | Automático | **08:00** (8,0 min) | ≈ 8,0 min | Nenhuma | `Decisao (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m)` |
| **C** | **C** | Automático | **08:30** (8,5 min) | ≈ 8,5 min | Nenhuma | `Decisao (30s) + ValidacaoPedidos (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m)` |
| **D** | **D** | Humano | **01:00** (1,0 min) | ≈ 1,0 min | Nenhuma | `Decisao (30s) + Notificacao (30s)` |
| **E** | **E** | Humano | **01:00** (1,0 min) | ≈ 1,0 min | Nenhuma | `Decisao (30s) + Notificacao (30s)` |
| **F** | **F** | Humano | **01:00** (1,0 min) | ≈ 1,0 min | Nenhuma | `Decisao (30s) + Notificacao (30s)` |
| **G** | **G** | Humano | **08:30** (8,5 min) | ≈ 8,0 min | **+0,5 min** | `Decisao (30s) + Rollback (2m) + SmokeTest (30s) + Estabilizacao (5m) + Notificacao (30s)` |
| **H** | **H** | Humano | **01:00** (1,0 min) | ≈ 1,0 min | Nenhuma | `Decisao (30s) + Notificacao (30s)` |

### Análise da Divergência no Cenário G
No documento acadêmico de referência, o tempo estimado para o Cenário G é citado como **≈ 8 min**. Contudo, aplicando a soma exata dos custos unitários exigidos pelo laboratório:
$$\text{Decisão (30s)} + \text{Rollback (2m)} + \text{SmokeTest (30s)} + \text{Estabilização (5m)} + \text{Notificação (30s)} = 8\text{ min } 30\text{ s (8,5 min)}$$
Como a parada para humano acarreta o custo de notificação do plantão (+30s) após o término dos 5 minutos de monitoramento da estabilização sem recuperação, o tempo exato modelado resulta em **8,5 min**. O valor **não** foi adulterado artificialmente, preservando a fidelidade do modelo matemático.

---

## 7. Decisões de Implementação e Políticas Fail-Safe

Conforme a especificação, o `Controlador` adota salvaguardas rigorosas para garantir que falhas operacionais não causem comportamentos anômalos em produção:

1. **Proteção contra exceções de portas:** Nenhuma exceção lançada pelas interfaces derruba o controlador:
   - Falha em `ICorrelacao` $\rightarrow$ Assume sem correlação $\rightarrow$ **Folha H**.
   - Falha em `IManifesto` $\rightarrow$ Assume `Migracao.Desconhecida` $\rightarrow$ **Folha D**.
   - Falha em `IFlags` $\rightarrow$ Assume "sem flag" e prossegue para verificação de registry.
   - Falha em `IRegistry` $\rightarrow$ Assume imagem ausente $\rightarrow$ **Folha E**.
   - Falha em `IPedidos` $\rightarrow$ Assume incompatibilidade $\rightarrow$ **Folha F**.
   - Falha durante Rollback, Smoke Test ou Métricas $\rightarrow$ Interrompe esteira e aciona plantão $\rightarrow$ **Folha G**.
2. **Idempotência e limites de atuação:**
   - `ExecutarRollback`: Executa no **máximo 1 vez** (evita loops destrutivos).
   - `DesligarFlag`: Executa no **máximo 1 vez**.
   - `TravarNovosDeploys`: Executa **exatamente 1 vez se houve correlação**, ou **0 vezes se não houve correlação**.
3. **Ausência de frameworks de terceiros:** Não são utilizados pacotes externos (Moq, AutoFixture, FluentAssertions). Todos os fakes foram escritos à mão em C# com a classe `FabricaDeFakes`.
