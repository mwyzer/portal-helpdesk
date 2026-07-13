FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/AIHelpdesk.Api/AIHelpdesk.Api.csproj", "AIHelpdesk.Api/"]
COPY ["src/AIHelpdesk.Application/AIHelpdesk.Application.csproj", "AIHelpdesk.Application/"]
COPY ["src/AIHelpdesk.Domain/AIHelpdesk.Domain.csproj", "AIHelpdesk.Domain/"]
COPY ["src/AIHelpdesk.Infrastructure/AIHelpdesk.Infrastructure.csproj", "AIHelpdesk.Infrastructure/"]
COPY ["src/AIHelpdesk.Contracts/AIHelpdesk.Contracts.csproj", "AIHelpdesk.Contracts/"]
RUN dotnet restore "AIHelpdesk.Api/AIHelpdesk.Api.csproj"
COPY src/ .
WORKDIR /src/AIHelpdesk.Api
RUN dotnet publish "AIHelpdesk.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIHelpdesk.Api.dll"]
