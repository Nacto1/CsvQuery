﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Community.CsharpSqlite;

namespace CsvQuery
{
    public static class DataStorage
    {
        // For now only in-memroy DB, perhaps later you could have a config setting for saving to disk
        private static SQLiteDatabase db = new SQLiteDatabase(":memory:");

        private static Dictionary<int, string> _createdTables = new Dictionary<int, string>();

        private static int _currentActiveBufferId = 0;
        private static int _lastCreatedTableName = 0;

        public static void SetActiveTab(int bufferId)
        {
            if (_currentActiveBufferId != bufferId && _createdTables.ContainsKey(bufferId))
            {
                if (_currentActiveBufferId != 0)
                {
                    db.ExecuteNonQuery("DROP VIEW this");
                    
                }
                db.ExecuteNonQuery("CREATE VIEW this AS SELECT * FROM " + _createdTables[bufferId]);
                _currentActiveBufferId = bufferId;
            }
        }

        public static string SaveData(int bufferId, List<string[]> data, bool? hasHeader)
        {
            string tableName;
            if (_createdTables.ContainsKey(bufferId))
            {
                tableName = _createdTables[bufferId];
                db.ExecuteNonQuery("DROP TABLE IF EXISTS " + tableName);
            }
            else
            {
                tableName = "T" + ++_lastCreatedTableName;
            }

            int columns = data[0].Length;
            // Figure out column types. For now just Int/Decimal/String
            // TODO
            var types = new List<string>();
            bool first = true;
            foreach (var cols in data)
            {
                if (first)
                {
                    first = false;
                    if (!hasHeader.HasValue || hasHeader.Value)
                    {
                        
                    }
                }
            }
            
            // Create SQL by string concat - look out for SQL injection! (although rather harmless since it's all your own data)
            var createQuery = new StringBuilder("CREATE TABLE " + tableName + "(");
            if (hasHeader ?? false)
            {
                var colnames = new List<string>();
                first = true;
                foreach (var colName in data[0])
                {
                    var columnNameClean = Regex.Replace(colName, @"[^\w_]", "");
                    if (colnames.Contains(columnNameClean))
                    {
                        var c = 1;
                        var fixedName = columnNameClean + c;
                        while (colnames.Contains(columnNameClean))
                            fixedName = columnNameClean + ++c;
                        columnNameClean = fixedName;
                    }
                    if (first) first = false;
                    else createQuery.Append(", ");
                    colnames.Add(columnNameClean);
                    createQuery.AppendFormat("{0} CHAR", columnNameClean);
                }
            }
            else
            {
                // Just create Col1, Col2, Col3 etc
                first = true;
                for (int index = 0; index < data[0].Length; index++)
                {
                    if (first) first = false;
                    else createQuery.Append(", ");
                    createQuery.AppendFormat("{0} CHAR", "Col" + index);
                }
            }
            createQuery.Append(")");
            db.ExecuteNonQuery(createQuery.ToString());



            var insertQuery = new StringBuilder("INSERT INTO ");
            insertQuery.Append(tableName);
            insertQuery.Append(" VALUES (?");
            for (int i = 1; i < columns; i++)
                insertQuery.Append(",?");
            insertQuery.Append(")");
            
            var stmt = new SQLiteVdbe(db, insertQuery.ToString());
            first = true;
            foreach (var stringse in data)
            {
                if (first)
                {
                    first = false;
                    if(hasHeader ?? false == true)
                        continue;
                }
                stmt.Reset();
                int index = 0;
                foreach (var s in stringse)
                {
                    stmt.BindText(++index,s);
                }
                stmt.ExecuteStep();
            }
            stmt.Close();

            return tableName;
        }

        public static List<string[]> ExecuteQuery(string query)
        {
            var result = new List<string[]>();
            var c1 = new SQLiteVdbe(db, query);
            while (c1.ExecuteStep() != Sqlite3.SQLITE_DONE)
            {
                var columns = c1.ResultColumnCount();
                var data = new List<string>();
                for (int i = 0; i < columns; i++)
                {
                    data.Add(c1.Result_Text(i));
                }
                result.Add(data.ToArray());
            }
            c1.Close();
            return result;
        }

        public static void ExecuteNonQuery(string query)
        {
            db.ExecuteNonQuery(query);
        }
    }
}
