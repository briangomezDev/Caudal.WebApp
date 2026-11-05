FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero los proyectos: si solo cambia el código, no se vuelven a descargar los paquetes.
COPY Caudal.Domain/Caudal.Domain.csproj Caudal.Domain/
COPY Caudal.Infraestructure/Caudal.Infraestructure.csproj Caudal.Infraestructure/
COPY Caudal.Infraestructure.PostgreSQL/Caudal.Infraestructure.PostgreSQL.csproj Caudal.Infraestructure.PostgreSQL/
COPY Caudal.Infraestructure.Mailing/Caudal.Infraestructure.Mailing.csproj Caudal.Infraestructure.Mailing/
COPY Caudal.Application/Caudal.Application.csproj Caudal.Application/
COPY Caudal.BlazorApp/Caudal.BlazorApp.csproj Caudal.BlazorApp/
RUN dotnet restore Caudal.BlazorApp/Caudal.BlazorApp.csproj

COPY . .
# Sin --no-restore: el restore anterior no ve los .razor y dejaría fuera blazor.web.js.
RUN dotnet publish Caudal.BlazorApp/Caudal.BlazorApp.csproj -c Release -o /app
RUN test -f /app/wwwroot/_framework/blazor.web.js

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# Aquí se monta el volumen de las claves: sin ellas, un reinicio invalida las sesiones y los enlaces de confirmación.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app

USER $APP_UID
ENV HOME=/home/app
ENTRYPOINT ["dotnet", "Caudal.BlazorApp.dll"]
