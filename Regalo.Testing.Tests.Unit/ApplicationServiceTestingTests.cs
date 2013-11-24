﻿using System;
using NUnit.Framework;
using Regalo.Core;
using Regalo.Core.Tests.DomainModel.SalesOrders;

namespace Regalo.Testing.Tests.Unit
{
    [TestFixture]
    public class ApplicationServiceTestingTests : ApplicationServiceTestBase<SalesOrder>
    {
        [SetUp]
        public void SetUp()
        {
            Resolver.SetResolvers(
                type =>
                {
                    if (type == typeof(IVersionHandler))
                    {
                        return new DefaultVersionHandler();
                    }

                    if (type == typeof(ILogger))
                    {
                        return new ConsoleLogger();
                    }

                    throw new InvalidOperationException(string.Format("No resolver registered for {0}", type));
                },
                type =>
                {
                    return null;
                });
        }

        [Test]
        public void GivenSalesOrderWithSingleOrderLine_WhenPlacingOrder_ThenShouldPlaceOrder()
        {
            Scenario.For<SalesOrder>(Context)
                    .HandledBy<PlaceSalesOrderCommandHandler>(CreateHandler())
                    .Given(SalesOrderTestDataBuilder.NewOrder().WithSingleLineItem())
                    .When(c => new PlaceSalesOrder(c.Id))
                    .Then((a, c) => new[] { new SalesOrderPlaced(a.Id) })
                    .Assert();
        }

        private PlaceSalesOrderCommandHandler CreateHandler()
        {
            return new PlaceSalesOrderCommandHandler(Context);
        }
    }
}