﻿using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Regalo.Core;
using Regalo.Core.EventSourcing;
using Regalo.Core.Tests.Unit;
using Regalo.RavenDB.Tests.Unit.DomainModel.Customers;
using Regalo.Testing;

namespace Regalo.RavenDB.Tests.Unit
{
    [TestFixture]
    public class PersistenceTests
    {
        private IDocumentStore _documentStore;
        private Mock<IVersionHandler> _versionHandlerMock;

        [SetUp]
        public void SetUp()
        {
            //_documentStore = new EmbeddableDocumentStore { RunInMemory = true };
            _documentStore = new DocumentStore
            {
                Url = "http://localhost:8080/",
                DefaultDatabase = "Regalo.RavenDB.Tests.UnitPersistenceTests"
            };
            _documentStore.Initialize();

            _versionHandlerMock = new Mock<IVersionHandler>();
            _versionHandlerMock.Setup(x => x.GetVersion(It.IsAny<Event>())).Returns<Event>(x => x.Version);
            _versionHandlerMock.Setup(x => x.SetParentVersion(It.IsAny<Event>(), It.IsAny<Guid?>())).Callback<object, Guid?>((x, v) => ((Event)x).ParentVersion = v);
            Resolver.SetResolvers(type =>
            {
                if (type == typeof(IVersionHandler)) return _versionHandlerMock.Object;
                if (type == typeof(ILogger)) return new NullLogger();
                throw new InvalidOperationException(string.Format("No type of {0} registered.", type));
            },
            type => null);
        }

        [TearDown]
        public void TearDown()
        {
            Conventions.SetFindAggregateTypeForEventType(null);

            Resolver.ClearResolvers();

            _documentStore.Dispose();
            _documentStore = null;
        }

        [Test]
        public void Loading_GivenEmptyStore_ShouldReturnNull()
        {
            // Arrange
            var versionHandlerMock = new Mock<IVersionHandler>();
            IEventStore store = new RavenEventStore(_documentStore, versionHandlerMock.Object);

            // Act
            IEnumerable<object> events = store.Load(Guid.NewGuid());

            // Assert
            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void Saving_GivenSingleEvent_ShouldAllowReloading()
        {
            // Arrange
            var versionHandlerMock = new Mock<IVersionHandler>();
            IEventStore store = new RavenEventStore(_documentStore, versionHandlerMock.Object);

            // Act
            var id = Guid.NewGuid();
            var evt = new CustomerSignedUp(id);
            store.Store(id, evt);
            var events = store.Load(id);

            // Assert
            Assert.NotNull(events);
            CollectionAssert.AreEqual(
                new object[] { evt },
                events,
                "Events reloaded from store do not match those generated by aggregate.");
        }

        [Test]
        public void Saving_GivenEventWithGuidProperty_ShouldAllowReloadingToGuidType()
        {
            // Arrange
            var versionHandlerMock = new Mock<IVersionHandler>();
            IEventStore store = new RavenEventStore(_documentStore, versionHandlerMock.Object);

            var customer = new Customer();
            customer.Signup();

            var accountManager = new AccountManager();
            var startDate = new DateTime(2012, 4, 28);
            accountManager.Employ(startDate);

            customer.AssignAccountManager(accountManager.Id, startDate);

            store.Store(customer.Id, customer.GetUncommittedEvents());

            // Act
            var acctMgrAssignedEvent = (AssignedAccountManager)store.Load(customer.Id).LastOrDefault();

            // Assert
            Assert.NotNull(acctMgrAssignedEvent);
            Assert.AreEqual(accountManager.Id, acctMgrAssignedEvent.AccountManagerId);
        }

        [Test]
        public void Saving_GivenEvents_ShouldAllowReloading()
        {
            // Arrange
            IEventStore store = new RavenEventStore(_documentStore, _versionHandlerMock.Object);

            // Act
            var customer = new Customer();
            customer.Signup();
            store.Store(customer.Id, customer.GetUncommittedEvents());
            var events = store.Load(customer.Id);

            // Assert
            Assert.NotNull(events);
            CollectionAssert.AreEqual(customer.GetUncommittedEvents(), events, "Events reloaded from store do not match those generated by aggregate.");
        }


        [Test]
        public void Saving_GivenNoEvents_ShouldDoNothing()
        {
            // Arrange
            var versionHandlerMock = new Mock<IVersionHandler>();
            IEventStore store = new RavenEventStore(_documentStore, versionHandlerMock.Object);

            // Act
            var id = Guid.NewGuid();
            store.Store(id, Enumerable.Empty<object>());
            var events = store.Load(id);

            // Assert
            CollectionAssert.IsEmpty(events);
        }

        [Test]
        public void GivenAggregateWithMultipleEvents_WhenLoadingSpecificVersion_ThenShouldOnlyReturnRequestedEvents()
        {
            // Arrange
            IEventStore store = new RavenEventStore(_documentStore, _versionHandlerMock.Object);
            var customerId = Guid.NewGuid();
            var storedEvents = new object[]
                              {
                                  new CustomerSignedUp(customerId), 
                                  new SubscribedToNewsletter("latest"), 
                                  new SubscribedToNewsletter("top")
                              };
            store.Store(customerId, storedEvents);
            
            // Act
            var events = store.Load(customerId, ((Event)storedEvents[1]).Version);

            // Assert
            CollectionAssert.AreEqual(storedEvents.Take(2), events, "Events loaded from store do not match version requested.");
        }

        [Test]
        public void GivenAggregateWithMultipleEvents_WhenLoadingSpecificVersionThatNoEventHas_ThenShouldFail()
        {
            // Arrange
            IEventStore store = new RavenEventStore(_documentStore, _versionHandlerMock.Object);
            var customerId = Guid.NewGuid();
            var storedEvents = new object[]
                              {
                                  new CustomerSignedUp(customerId), 
                                  new SubscribedToNewsletter("latest"), 
                                  new SubscribedToNewsletter("top")
                              };
            store.Store(customerId, storedEvents);

            // Act / Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Load(customerId, Guid.Parse("00000000-0000-0000-0000-000000000001")));
        }

        [Test]
        public void Saving_GivenEventMappedToAggregateType_ThenShouldSetRavenCollectionName()
        {
            IEventStore store = new RavenEventStore(_documentStore, _versionHandlerMock.Object);

            Conventions.SetFindAggregateTypeForEventType(
                type =>
                {
                    if (type == typeof(CustomerSignedUp))
                    {
                        return typeof(Customer);
                    }

                    return typeof(EventStream);
                });

            var customerId = Guid.NewGuid();
            var storedEvents = new object[]
                              {
                                  new CustomerSignedUp(customerId), 
                                  new SubscribedToNewsletter("latest"), 
                                  new SubscribedToNewsletter("top")
                              };
            store.Store(customerId, storedEvents);

            using (var session = _documentStore.OpenSession())
            {
                var eventStream = session.Load<EventStream>(customerId.ToString());
                var entityName = session.Advanced.GetMetadataFor(eventStream)[Constants.RavenEntityName].ToString();

                Assert.That(entityName, Is.EqualTo("Customers"));
            }
        }
    }
}