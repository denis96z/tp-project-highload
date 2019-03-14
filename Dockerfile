FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine
WORKDIR /tmp

COPY . .
RUN dotnet build HttpStaticServer.sln -c Release -f netcoreapp2.2 \
    && mv ./httpd.conf /etc/httpd.conf

WORKDIR /tmp/HttpStaticServer/bin/Release/netcoreapp2.2

CMD ["dotnet", "./HttpStaticServer.dll"]
EXPOSE 80
