FROM microsoft/dotnet:2.1-sdk-alpine AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY src/mock-ae/*.csproj ./src/mock-ae/
COPY src/Tests.mock-ae/*.csproj ./src/Tests.mock-ae/
RUN dotnet restore


# copy everything else and build app
COPY . .
WORKDIR /app/src/mock-ae
RUN dotnet build


#Not sure what this is.  Builds an image but then what?
FROM build AS testrunner
WORKDIR /app/Tests.mock-ae
ENTRYPOINT ["dotnet", "test", "--logger:trx"]


FROM build AS test
WORKDIR /app/src/Tests.mock-ae
RUN dotnet test


FROM build AS publish
WORKDIR /app/src/mock-ae
RUN dotnet publish -c Release -o out


FROM microsoft/dotnet:2.1-runtime-alpine AS runtime
WORKDIR /app
COPY --from=publish /app/src/mock-ae/out ./
ENTRYPOINT ["dotnet", "Mattersight.mock.ba.ae.dll"]