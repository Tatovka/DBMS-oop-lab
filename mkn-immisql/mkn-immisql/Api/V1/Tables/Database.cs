using System;
using System.Collections.Generic;
using System.Linq;

namespace MknImmiSql.Api.V1.Tables;

public static class Database
{
    private static Dictionary<String, Table> _tables = new Dictionary<String, Table>();

    public static bool TryAddTable(String name, Table t) => _tables.TryAdd(name, t);
    
    public static bool TryDropTable(String name) => _tables.Remove(name);

    public static String[] ListTables => _tables.Keys.ToArray();
    
    public static bool TryGetTable(String name, out Table? table) => _tables.TryGetValue(name, out table);

}