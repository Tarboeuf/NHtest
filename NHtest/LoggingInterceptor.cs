using System;
using System.IO;
using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NHibernate;
using NHibernate.SqlCommand;

namespace NHtest;

public class LoggingInterceptor : EmptyInterceptor
{
    TSql120Parser _parser = new(false);
    Sql120ScriptGenerator _generator = new (new SqlScriptGeneratorOptions()
    {
        KeywordCasing = KeywordCasing.Uppercase,
        IncludeSemicolons = true,
        NewLineBeforeFromClause = true,
        NewLineBeforeOrderByClause = true,
        NewLineBeforeWhereClause = true,
        AlignClauseBodies = true
    });
    public override SqlString OnPrepareStatement(SqlString sql)
    {
        Console.WriteLine("NH: ");
        var parsedQuery = _parser.Parse(new StringReader(ToString(sql)), out var errors);

        _generator.GenerateScript(parsedQuery, out var formattedQuery);
        Console.WriteLine(formattedQuery);

        return base.OnPrepareStatement(sql);
    }

    public string ToString(SqlString sql)
    {
        StringBuilder builder = new StringBuilder();
        int inc = 0;
        foreach (var part in sql)
        {
            if (part is Parameter)
            {
                builder.Append($"@p{inc++}");
            }
            else
            {
                builder.Append(part);
            }
        }
        return builder.ToString();
    }
}