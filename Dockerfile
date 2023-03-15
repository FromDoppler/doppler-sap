FROM mcr.microsoft.com/dotnet/sdk:7.0.103-bullseye-slim AS restore
WORKDIR /src
COPY ./*.sln ./
COPY */*.csproj ./
COPY ./.config/dotnet-tools.json ./.config/
# Take into account using the same name for the folder and the .csproj and only one folder level
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet tool restore
RUN dotnet restore

FROM restore AS build
COPY . .
RUN dotnet format --verify-no-changes
RUN dotnet build -c Release

FROM build AS test
RUN dotnet test

FROM build AS publish
RUN dotnet publish "Doppler.Sap/Doppler.Sap.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0.4-bullseye-slim AS final
# We need these changes in openssl.cnf to access to our SQL Server instances in QA and INT environments
# See more information in https://stackoverflow.com/questions/56473656/cant-connect-to-sql-server-named-instance-from-asp-net-core-running-in-docker/59391426#59391426
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ARG version=unknown
RUN echo $version > /app/wwwroot/version.txt
ENTRYPOINT ["dotnet", "Doppler.Sap.dll"]
