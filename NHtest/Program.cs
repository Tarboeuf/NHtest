using System.Collections.Generic;
using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;


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
}
