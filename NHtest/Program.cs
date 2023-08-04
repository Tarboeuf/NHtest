using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using Microsoft.SqlServer.TransactSql.ScriptDom;


namespace NHtest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sessionFactory = CreateSessionFactory();

            var interceptor = new LoggingInterceptor();
            using var session = sessionFactory.WithOptions()
                .Interceptor(interceptor).OpenSession();

            using var transaction = session.BeginTransaction();
            var customer = new Customer { Addresses = new List<Address>{ new(), new()}};
            session.SaveOrUpdate(customer);
            transaction.Commit();
        }
        

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                .Database(
                    SQLiteConfiguration.Standard
                        .UsingFile("firstProject.db")
                )
                .Mappings(m =>
                {
                    m.FluentMappings.AddFromAssemblyOf<Program>();
                })
                .ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            // delete the existing db on each run
            if (File.Exists("firstProject.db"))
                File.Delete("firstProject.db");

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
            new SchemaExport(config)
                .Create(true, true);
        }
    }

    internal class TokenInfo
    {
        public int Start { get; internal set; }
        public int End { get; internal set; }
        public bool IsPairMatch { get; internal set; }
        public bool IsExecAutoParamHelp { get; internal set; }
        public string Sql { get; internal set; }
        public Tokens Token { get; internal set; }
    }

    public class Customer
    {
        public virtual int Id { get; set; }

        public virtual IList<Address> Addresses { get; set; }
    }

    public class Address
    {
        public virtual int AddressId { get; set; }
    }

    public class AddressMap : ClassMap<Address>
    {
        public AddressMap()
        {
            Id(x => x.AddressId)
                .GeneratedBy.Identity();
            
            Table("tblAddress");
        }
    }

    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Id(x => x.Id)
                .GeneratedBy.Identity();

            HasMany(t => t.Addresses)
                .KeyColumn("Id")
                .Cascade.AllDeleteOrphan();
            Table("tblCustomer");
        }
    }

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
}
