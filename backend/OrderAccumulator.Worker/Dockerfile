FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY OrderProcessingFix.sln .
COPY OrderAccumulator.Domain/OrderAccumulator.Domain.csproj OrderAccumulator.Domain/
COPY OrderAccumulator.Application/OrderAccumulator.Application.csproj OrderAccumulator.Application/
COPY OrderAccumulator.Worker/OrderAccumulator.Worker.csproj OrderAccumulator.Worker/

RUN dotnet restore OrderAccumulator.Worker/OrderAccumulator.Worker.csproj

COPY OrderAccumulator.Domain/ OrderAccumulator.Domain/
COPY OrderAccumulator.Application/ OrderAccumulator.Application/
COPY OrderAccumulator.Worker/ OrderAccumulator.Worker/

RUN dotnet publish OrderAccumulator.Worker/OrderAccumulator.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y netcat-traditional
COPY --from=build /app/publish .

EXPOSE 5001

ENTRYPOINT ["dotnet", "OrderAccumulator.Worker.dll"]
