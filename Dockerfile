FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY RollbackLab.sln ./
COPY src/RollbackLab.Core/RollbackLab.Core.csproj ./src/RollbackLab.Core/
COPY src/RollbackLab.Simulador/RollbackLab.Simulador.csproj ./src/RollbackLab.Simulador/
COPY tests/RollbackLab.Tests/RollbackLab.Tests.csproj ./tests/RollbackLab.Tests/
RUN dotnet restore

COPY . ./

RUN dotnet build -c Release --no-restore

RUN dotnet test -c Release --no-build --logger "console;verbosity=normal"

ENTRYPOINT ["dotnet", "run", "--project", "src/RollbackLab.Simulador", "-c", "Release", "--no-build", "--"]
