﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Regalo.Core;

namespace Regalo.EventSourcing.Raven.Tests.Unit
{
    [TestFixture]
    public class PersistenceTests
    {
        [Test]
        public void Loading_GivenEmptyStore_ShouldReturnNull()
        {
            // Arrange
            IDocumentStore documentStore = new EmbeddableDocumentStore { RunInMemory = true };
            documentStore.Initialize();
            IRepository<Customer> repository = new RavenRepository<Customer>(documentStore);

            // Act
            Customer customer = repository.Get("customer1");

            // Assert
            Assert.Null(customer);
        }

        [Test]
        public void Saving_GivenNewAggregate_ShouldAllowReloading()
        {
            // Arrange
            IDocumentStore documentStore = new EmbeddableDocumentStore { RunInMemory = true };
            documentStore.Initialize();
            IRepository<Customer> repository = new RavenRepository<Customer>(documentStore);

            // Act
            var customer = new Customer();
            string id = customer.Id;
            repository.Save(customer);
            customer = repository.Get(id);

            // Assert
            Assert.NotNull(customer);
            Assert.AreEqual(id, customer.Id);
        }
    }

    public class Customer : AggregateRoot
    {
        public Customer()
        {
            Record(new CustomerCreated(Guid.NewGuid().ToString()));
        }

        private void Apply(CustomerCreated evt)
        {
            Id = evt.AggregateId;
        }
    }

    public class CustomerCreated : Event
    {
        public CustomerCreated(string id) : base(id)
        {
            
        }
    }
}