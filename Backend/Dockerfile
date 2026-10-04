FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.config Ims.slnx ./
COPY src/Ims.Domain/Ims.Domain.csproj src/Ims.Domain/
COPY src/Ims.Infrastructure/Ims.Infrastructure.csproj src/Ims.Infrastructure/
COPY src/Ims.Api/Ims.Api.csproj src/Ims.Api/
RUN dotnet restore src/Ims.Api/Ims.Api.csproj
COPY src/ src/
RUN dotnet publish src/Ims.Api/Ims.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app ./
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ims.Api.dll"]
