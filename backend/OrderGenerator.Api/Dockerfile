FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY OrderProcessingFix.sln .
COPY OrderGenerator.Infrastructure/OrderGenerator.Infrastructure.csproj OrderGenerator.Infrastructure/
COPY OrderGenerator.Api/OrderGenerator.Api.csproj OrderGenerator.Api/

RUN dotnet restore OrderGenerator.Api/OrderGenerator.Api.csproj

COPY OrderGenerator.Infrastructure/ OrderGenerator.Infrastructure/
COPY OrderGenerator.Api/ OrderGenerator.Api/

RUN dotnet publish OrderGenerator.Api/OrderGenerator.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderGenerator.Api.dll"]
