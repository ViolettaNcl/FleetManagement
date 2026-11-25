#!/usr/bin/env bash
# CI-only fresh database migration rehearsal. No production connection accepted.
set -euo pipefail
provider="$1"
case "$provider" in Sqlite|SqlServer) ;; *) exit 2;; esac
: "${RUNNER_TEMP:?Requires a disposable CI runner}"
root="$(cd "$(dirname "$0")/.." && pwd)"
work="$(mktemp -d "$RUNNER_TEMP/fleet-migration.XXXXXX")"
out="$RUNNER_TEMP/migration-$provider"
mkdir -p "$work/src" "$out"
cp -R "$root/src/FleetManagement.Core" "$root/src/FleetManagement.Api" "$work/src/"
proj="$work/src/FleetManagement.Api/FleetManagement.Api.csproj"
# Design-time dependencies exist only in the temporary generation copy.
dotnet add "$proj" package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet tool install dotnet-ef --version 10.0.0 --tool-path "$work/tools"
export Fleet__Provider="$provider"
export ConnectionStrings__Fleet="Data Source=$work/new.db"
export Fleet__ApiKey="$(python3 -c 'import secrets; print(secrets.token_hex(32))')"
export Fleet__BootstrapAdminEmail=baseline@example.test
export Fleet__BootstrapAdminPassword="Baseline!aA$(python3 -c 'import secrets; print(secrets.token_hex(24))')"
echo "::add-mask::$Fleet__ApiKey"
echo "::add-mask::$Fleet__BootstrapAdminPassword"
sql_container=""
pid=""
cleanup(){
    if [ -n "$pid" ]; then kill "$pid" 2>/dev/null || true; wait "$pid" 2>/dev/null || true; fi
    if [ -n "$sql_container" ]; then docker rm -f "$sql_container" >/dev/null 2>&1 || true; fi
    rm -rf "$work"
}
trap cleanup EXIT
if [ "$provider" = SqlServer ]; then
    export MSSQL_SA_PASSWORD="Qa!aA1$(python3 -c 'import secrets; print(secrets.token_hex(24))')"
    echo "::add-mask::$MSSQL_SA_PASSWORD"
    sql_container="fleet-migration-ci"
    docker run -d --name "$sql_container" -e ACCEPT_EULA=Y -e MSSQL_PID=Developer -e MSSQL_SA_PASSWORD -p 127.0.0.1:1433:1433 mcr.microsoft.com/mssql/server:2022-latest
    ready=false
    for i in $(seq 1 60); do
        if docker exec "$sql_container" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" -b >/dev/null 2>&1; then ready=true; break; fi
        sleep 2
    done
    test "$ready" = true
    export ConnectionStrings__Fleet="Server=127.0.0.1,1433;Database=FleetFreshMigrationCi;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
fi
# Explicit design factory prevents application startup/seed during scaffolding.
cat > "$work/src/FleetManagement.Api/MigrationDesignFactory.cs" <<'EOF'
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace FleetManagement.Api;
public sealed class MigrationDesignFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args)
    {
        var provider=Environment.GetEnvironmentVariable("Fleet__Provider");
        var connection=Environment.GetEnvironmentVariable("ConnectionStrings__Fleet")??throw new InvalidOperationException("Explicit CI connection required.");
        var options=new DbContextOptionsBuilder<FleetDbContext>();
        if(provider=="Sqlite")options.UseSqlite(connection);
        else if(provider=="SqlServer")options.UseSqlServer(connection);
        else throw new InvalidOperationException("Provider required.");
        return new FleetDbContext(options.Options,new HttpContextAccessor());
    }
}
EOF
ef="$work/tools/dotnet-ef"
"$ef" migrations add InitialV3 --project "$proj" --output-dir Migrations
"$ef" migrations script 0 InitialV3 --project "$proj" --output "$out/initial.sql"
"$ef" database update --project "$proj"
"$ef" database update --project "$proj"
cp -R "$work/src/FleetManagement.Api/Migrations" "$out/"
export FLEET_SCHEMA_LEFT="$ConnectionStrings__Fleet"
export Fleet__InitializeDatabase=true Fleet__SeedDemo=true Fleet__UpgradeV2Schema=false
dotnet run --project "$proj" --no-launch-profile --urls http://127.0.0.1:5095 > "$work/server.log" 2>&1 &
pid=$!
ready=false
for i in $(seq 1 90); do
    if curl -fsS http://127.0.0.1:5095/health >/dev/null 2>&1; then ready=true; break; fi
    if ! kill -0 "$pid" 2>/dev/null; then break; fi
    sleep 1
done
if [ "$ready" != true ]; then cat "$work/server.log"; exit 1; fi
python3 - <<'PY'
import os,json,urllib.request
base="http://127.0.0.1:5095"
req=urllib.request.Request(base+"/api/auth/login",data=json.dumps({"email":os.environ["Fleet__BootstrapAdminEmail"],"password":os.environ["Fleet__BootstrapAdminPassword"]}).encode(),headers={"Content-Type":"application/json"})
with urllib.request.urlopen(req,timeout=15) as response:
    token=json.load(response)["accessToken"]
def read(path):
    req=urllib.request.Request(base+path,headers={"Authorization":"Bearer "+token})
    with urllib.request.urlopen(req,timeout=15) as response:
        return json.load(response)
assert len(read("/api/fleet")["vehicles"])==4
for name in ("maintenance","shifts","reservations"):
    assert read("/api/operations/"+name)==[]
print("PASS: fresh EF migration applied twice, current API initialization, Identity and fleet records")
PY
# Compare migration-created catalog to a separate current EnsureCreated database.
kill "$pid"; wait "$pid" || true; pid=""
if [ "$provider" = Sqlite ]; then
    export ConnectionStrings__Fleet="Data Source=$work/ensure-created.db"
else
    export ConnectionStrings__Fleet="${ConnectionStrings__Fleet/Database=FleetFreshMigrationCi;/Database=FleetEnsureCreatedCi;}"
fi
export FLEET_SCHEMA_RIGHT="$ConnectionStrings__Fleet"
dotnet run --project "$proj" --no-launch-profile --urls http://127.0.0.1:5095 > "$work/ensure-server.log" 2>&1 &
pid=$!
ready=false
for i in $(seq 1 90); do
    if curl -fsS http://127.0.0.1:5095/health >/dev/null 2>&1; then ready=true; break; fi
    if ! kill -0 "$pid" 2>/dev/null; then break; fi
    sleep 1
done
if [ "$ready" != true ]; then cat "$work/ensure-server.log"; exit 1; fi
kill "$pid"; wait "$pid" || true; pid=""
dotnet run --project "$root/tools/FleetManagement.SchemaInspector" -c Release -- "$out/schema-parity.json"
printf '%s\n' "Generated for fresh $provider database only. Catalog parity checked against separate EnsureCreated database. Existing database adoption is not authorized." > "$out/README.txt"
