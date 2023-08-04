using System;
using System.Windows;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using System.Linq.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using UnaryExpression = System.Linq.Expressions.UnaryExpression;
using static System.Runtime.InteropServices.JavaScript.JSType;


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

                var customer2 = new CustomerTest { CustomerCode = "123", Name = "2", Prop2 = "2", Age = 2, Addresses = new List<Address>{ new(), new()}};
                session.SaveOrUpdate(customer2);
                transaction.Commit();
            }
            QueryOver(session);
            //Linq(session);
            Criteria(session);
        }

        private static void Linq(ISession session)
        {
            var l = session.Query<Customer>()
                .Where(c => c.Name == "1")
                .Where(c => ((Customer1)c).Age == 1 || ((CustomerTest)c).Age == 1)
                .Select(c => c.Id)
                .ToList();
            Console.WriteLine(l.Count);
        }

        private static void Criteria(ISession session)
        {
            var crit = session.CreateCriteria<Customer>("c")
                .SetProjection(Projections.Property("Id"))
                .CreateEntityAlias("cust2", Restrictions.EqProperty("c.Id", "cust2.Id"), JoinType.LeftOuterJoin, typeof(CustomerTest).FullName)
                //.CreateEntityAlias("cust2", Restrictions.EqProperty("c.Id", "cust2.Id"), JoinType.LeftOuterJoin, typeof(Customer2).FullName)
                .Add(Restrictions.EqProperty("Age", "cust2.Age"));

            Console.WriteLine(crit.List().Count);
        }

        private static void QueryOver(ISession session)
        {
            Customer cust = null;

            var query = NHibernate.Criterion.QueryOver.Of(() => cust);
            query.Where(Equal<Customer, Customer1, int>(query, c => c.Age, 1, cust))
                .Where(Equal<Customer, CustomerTest, int>(query, c => c.Age, 2, cust));

            //query.UnderlyingCriteria.CreateAlias("tblCustomerTest", "test").Add(Restrictions.Eq("test.Age", 123));
            foreach (var customer in query.Select(c => c.Id).GetExecutableQueryOver(session).List<int>())
            {
                Console.WriteLine($"{customer}");
            }
        }




        private static ICriterion Equal<TRoot, TSubType, TKey>(QueryOver<TRoot, TRoot> query, Expression<Func<TSubType, object>> property, TKey value, Customer cust)
            where TRoot : Customer
            where TSubType : TRoot
        {
            Type subType = typeof(TSubType);
            string name = subType.Name.ToLower();

            query.UnderlyingCriteria
                .CreateEntityAlias(
                    name, 
                    Restrictions.EqProperty(Projections.Property(() => cust.Id), $"{name}.Id"), 
                    JoinType.LeftOuterJoin,
                    subType.FullName);

            var propExpression = ((MemberExpression)((UnaryExpression)property.Body).Operand);

            return Restrictions.Eq($"{name}.{propExpression.Member.Name}", value);
        }




        private static QueryOver<Customer, Customer> GetDetachedQuery(Customer customer)
        {
            var detachedQuery = NHibernate.Criterion.QueryOver.Of<Customer>().Where(c => c.Name == customer.Name).Select(c => c.Name);
            return detachedQuery;
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

        public virtual string CustomerCode { get; set; }

        public virtual string Name { get; set; }
    }
    public class Customer1 : Customer
    {
        public virtual int Age { get; set; }
        public virtual string Prop1 { get; set; }
    }
    public abstract class Customer2 : Customer
    {
        public virtual string Prop2 { get; set; }

    }
    public class Customer21 : Customer2
    {
        public virtual int Age { get; set; }
        public virtual int CustomerId { get; set; }

    }
    public class CustomerTest : Customer21
    {
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
            
            Map(x => x.Prop2);
            Table("tblCustomer2");
        }
    }
    public class Customer21Map : SubclassMap<Customer21>
    {
        public Customer21Map()
        {
            KeyColumn("CustomerId");

            Map(x => x.Age);
            Table("tblCustomer21");
        }
    }
    public class CustomerTestMap : SubclassMap<CustomerTest>
    {
        public CustomerTestMap()
        {
            KeyColumn("CustomerId");
            HasMany(t => t.Addresses)
                .KeyColumn("Id")
                .Cascade.AllDeleteOrphan();
            Table("tblCustomerTest");
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
