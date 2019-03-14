FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as base
WORKDIR /tmp
COPY . .
RUN dotnet build HttpStaticServer.sln -c Release -f netcoreapp2.2

FROM mcr.microsoft.com/dotnet/core/runtime:2.2
WORKDIR /tmp

COPY --from=base /tmp/HttpStaticServer/bin/Release/netcoreapp2.2 .
COPY ./httpd.conf /etc/httpd.conf
COPY ./httptest /var/www/html/httptest

CMD ["dotnet", "/tmp/HttpStaticServer.dll"]
EXPOSE 80
