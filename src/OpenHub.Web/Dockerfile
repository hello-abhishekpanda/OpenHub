FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/OpenHub.Web/OpenHub.Web.csproj -c Release -o /app
FROM nginx:1.27-alpine
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 80
