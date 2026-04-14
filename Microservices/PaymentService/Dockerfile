FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SharedModels/SharedModels.csproj SharedModels/
COPY PaymentService/PaymentService.csproj PaymentService/
RUN dotnet restore PaymentService/PaymentService.csproj

COPY SharedModels/ SharedModels/
COPY PaymentService/ PaymentService/
RUN dotnet publish PaymentService/PaymentService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
