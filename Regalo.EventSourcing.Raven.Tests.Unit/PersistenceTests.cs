﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
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
            Conventions.SetAggregatesMustImplementApplymethods(true);
            IDocumentStore documentStore = new EmbeddableDocumentStore { RunInMemory = true };
            documentStore.Initialize();
            IRepository<Customer> repository = new RavenRepository<Customer>(documentStore);

            // Act
            var customer = new Customer();
            customer.Signup();
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
        public void Signup()
        {
            Record(new CustomerSignedUp(Guid.NewGuid().ToString()));
        }

        private void Apply(CustomerSignedUp evt)
        {
            Id = evt.AggregateId;
        }
    }

    public class CustomerSignedUp : Event
    {
        public string AggregateId { get; set; }

        public CustomerSignedUp(string customerId)
        {
            AggregateId = customerId;
        }

        public bool Equals(CustomerSignedUp other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.AggregateId, AggregateId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(CustomerSignedUp)) return false;
            return Equals((CustomerSignedUp)obj);
        }

        public override int GetHashCode()
        {
            return (AggregateId != null ? AggregateId.GetHashCode() : 0);
        }
    }
}