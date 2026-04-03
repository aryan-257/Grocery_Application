FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
VOLUME /app/data
ENV ConnectionStrings__Default="Data Source=/app/data/freshmart.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]
