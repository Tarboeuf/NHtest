using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Data;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.Engine;
using NHibernate.Hql.Ast;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Impl;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Linq.Visitors;
using NHibernate.Loader.Criteria;
using NHibernate.Multi;
using NHibernate.Persister.Entity;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Util;

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

            using (var transaction = session.BeginTransaction())
            {
                var customer1 = new Customer1 { CustomerCode = "123", Name = "1", Prop1 = "1", Age = 1 };
                session.SaveOrUpdate(customer1);

                var customer2 = new Customer2 { CustomerCode = "123", Name = "2", Prop2 = "2", Age = 2 };
                session.SaveOrUpdate(customer2);
                transaction.Commit();
            }

            var query = QueryOver.Of<Customer>().Where(c => c.Name == "1")
                .And(c => ((Customer1)c).Age == 1)
                .And(c => ((Customer2)c).Age == 2)
                .Select(c => c.Name);

            foreach (var customer in query.GetExecutableQueryOver(session).List())
            {
                Console.WriteLine($"{customer}");
            }
        }

        private static QueryOver<Customer, Customer> GetDetachedQuery(Customer customer)
        {
            var detachedQuery = QueryOver.Of<Customer>().Where(c => c.Name == customer.Name).Select(c => c.Name);
            return detachedQuery;
        }

        static string FormatSql(string sql)
        {
            return sql;
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
    public class Customer
    {
        public virtual int Id { get; set; }

        public virtual string CustomerCode { get; set; }

        public virtual string Name { get; set; }
    }
    public class Customer1 : Customer
    {
        public virtual int Age { get; set; }
        public virtual string Prop1 { get; set; }
    }
    public class Customer2 : Customer
    {
        public virtual int Age { get; set; }

        public virtual string Prop2 { get; set; }

    }
    public class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Id(x => x.Id)
                .GeneratedBy.Identity();

            Map(x => x.CustomerCode);
            Map(x => x.Name);
            Table("tblCustomer");
        }
    }
    public class Customer1Map : SubclassMap<Customer1>
    {
        public Customer1Map()
        {
            KeyColumn("Id");
            Map(x => x.Age);
            Map(x => x.Prop1);
            Table("tblCustomer1");
        }
    }

    public class Customer2Map : SubclassMap<Customer2>
    {
        public Customer2Map()
        {
            KeyColumn("Id");

            Map(x => x.Age);
            Map(x => x.Prop2);
            Table("tblCustomer2");
        }
    }
    public class LoggingInterceptor : EmptyInterceptor
    {
        public override SqlString OnPrepareStatement(SqlString sql)
        {

            Console.Write("NHibernate: ");
            Console.WriteLine(sql);

            return sql;
        }
    }
}
