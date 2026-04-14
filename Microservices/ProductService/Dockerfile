FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharedModels/SharedModels.csproj SharedModels/
COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore ProductService/ProductService.csproj

COPY SharedModels/ SharedModels/
COPY ProductService/ ProductService/
RUN dotnet publish ProductService/ProductService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
