FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Shop.Api/Shop.Api.csproj Shop.Api/
RUN dotnet restore Shop.Api/Shop.Api.csproj
COPY Shop.Api/ Shop.Api/
RUN dotnet publish Shop.Api/Shop.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Shop.Api.dll"]
