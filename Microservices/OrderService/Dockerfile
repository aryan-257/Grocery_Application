FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy SharedModels first (referenced project)
COPY SharedModels/SharedModels.csproj SharedModels/
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj

COPY SharedModels/ SharedModels/
COPY OrderService/ OrderService/
RUN dotnet publish OrderService/OrderService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
