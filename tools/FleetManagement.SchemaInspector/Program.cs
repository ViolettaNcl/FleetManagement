using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

// Read-only catalog comparison. No DDL, DML, migrations or connection strings in the report.
// Intended for restored copies and disposable CI databases, never for live adoption.
if(args.Length is <1 or >2 || (args.Length==2 && args[1]!="--report-only"))
    throw new ArgumentException("Usage: SchemaInspector <report.json> [--report-only]");
var provider=Environment.GetEnvironmentVariable("Fleet__Provider");
if(provider is not ("Sqlite" or "SqlServer"))throw new InvalidOperationException("Explicit provider required.");
string Required(string name)=>Environment.GetEnvironmentVariable(name) is {Length:>0} value?value:throw new InvalidOperationException("Missing "+name);
var left=Required("FLEET_SCHEMA_LEFT");
var right=Required("FLEET_SCHEMA_RIGHT");
if(string.Equals(left,right,StringComparison.Ordinal))throw new InvalidOperationException("Two separate databases are required.");
var reference=await Read(left);
var candidate=await Read(right);
var missing=reference.Except(candidate,StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
var unexpected=candidate.Except(reference,StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
var matches=missing.Length==0&&unexpected.Length==0;
await File.WriteAllTextAsync(args[0],JsonSerializer.Serialize(new{
    schema=1,provider,catalogMatches=matches,referenceEntries=reference.Count,candidateEntries=candidate.Count,
    missing,unexpected,historyAdoptionApproved=false,
    limitations="Catalog comparison only; excludes EF history/lock metadata, permissions, data, provider-specific features not queried and SQLite table check expressions. Never stamps migration history."
},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Schema catalog matches: {matches}; reference {reference.Count}, candidate {candidate.Count}; history adoption not approved.");
return matches||args.Length==2?0:1;

async Task<HashSet<string>> Read(string connection)
{
    await using DbConnection db=provider=="Sqlite"
        ?new SqliteConnection(new SqliteConnectionStringBuilder(connection){Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ConnectionString)
        :new SqlConnection(connection);
    await db.OpenAsync();
    var result=new HashSet<string>(StringComparer.Ordinal);
    async Task<List<string[]>> Query(string sql,string? name=null)
    {
        await using var command=db.CreateCommand();
        command.CommandText=sql;
        command.CommandTimeout=30;
        if(name is not null){
            var parameter=command.CreateParameter();parameter.ParameterName="$name";parameter.Value=name;command.Parameters.Add(parameter);
        }
        await using var reader=await command.ExecuteReaderAsync();
        var rows=new List<string[]>();
        while(await reader.ReadAsync()){
            var row=new string[reader.FieldCount];
            for(var i=0;i<row.Length;i++)row[i]=reader.IsDBNull(i)?"<NULL>":Convert.ToString(reader.GetValue(i),CultureInfo.InvariantCulture)??"";
            rows.Add(row);
        }
        return rows;
    }
    async Task Add(string kind,string sql,string? name=null)
    {
        foreach(var row in await Query(sql,name))result.Add(JsonSerializer.Serialize(new[]{kind,name??""}.Concat(row)));
    }
    if(provider=="Sqlite"){
        var tables=await Query("SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT IN ('__EFMigrationsHistory','__EFMigrationsLock')");
        foreach(var table in tables){
            var name=table[0];result.Add(JsonSerializer.Serialize(new[]{"table",name}));
            await Add("column","SELECT cid,name,type,[notnull],dflt_value,pk,hidden FROM pragma_table_xinfo($name)",name);
            await Add("foreign-key","SELECT id,seq,[table],[from],[to],on_update,on_delete,match FROM pragma_foreign_key_list($name)",name);
            var indexes=await Query("SELECT name,[unique],origin,partial FROM pragma_index_list($name)",name);
            foreach(var index in indexes){
                result.Add(JsonSerializer.Serialize(new[]{"index",name}.Concat(index)));
                await Add("index-column","SELECT seqno,cid,name,[desc],coll,[key] FROM pragma_index_xinfo($name)",index[0]);
                await Add("index-definition","SELECT sql FROM sqlite_schema WHERE type='index' AND name=$name AND sql IS NOT NULL",index[0]);
            }
        }
        await Add("view-trigger","SELECT type,name,tbl_name,sql FROM sqlite_schema WHERE type IN ('view','trigger')");
    }else{
        const string tables=" FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id ";
        const string filter=" WHERE t.is_ms_shipped=0 AND t.name<>'__EFMigrationsHistory'";
        await Add("table","SELECT s.name,t.name"+tables+filter);
        await Add("column","SELECT s.name,t.name,c.column_id,c.name,ty.name,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,c.collation_name,dc.definition,cc.definition,CONVERT(nvarchar(100),ic.seed_value),CONVERT(nvarchar(100),ic.increment_value)"+tables+" JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id"+filter);
        await Add("index","SELECT s.name,t.name,i.name,i.type,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.has_filter,i.filter_definition,ic.key_ordinal,ic.index_column_id,c.name,ic.is_descending_key,ic.is_included_column"+tables+" JOIN sys.indexes i ON i.object_id=t.object_id AND i.index_id>0 JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id"+filter);
        await Add("foreign-key","SELECT s.name,t.name,f.name,rs.name,rt.name,fc.constraint_column_id,pc.name,rc.name,f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted"+tables+" JOIN sys.foreign_keys f ON f.parent_object_id=t.object_id JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id JOIN sys.tables rt ON rt.object_id=f.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id JOIN sys.columns pc ON pc.object_id=t.object_id AND pc.column_id=fc.parent_column_id JOIN sys.columns rc ON rc.object_id=rt.object_id AND rc.column_id=fc.referenced_column_id"+filter);
        await Add("check","SELECT s.name,t.name,ck.name,ck.definition,ck.is_disabled,ck.is_not_trusted"+tables+" JOIN sys.check_constraints ck ON ck.parent_object_id=t.object_id"+filter);
        await Add("view-trigger","SELECT s.name,o.type,o.name,m.definition FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id LEFT JOIN sys.sql_modules m ON m.object_id=o.object_id WHERE o.is_ms_shipped=0 AND o.type IN ('V','TR')");
    }
    if(result.Count==0)throw new InvalidOperationException("No visible application schema; refusing empty catalog comparison.");
    return result;
}
