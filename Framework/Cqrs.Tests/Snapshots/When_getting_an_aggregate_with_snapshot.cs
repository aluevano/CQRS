﻿using System;
using Cqrs.Domain;
using Cqrs.Domain.Factories;
using Cqrs.Authentication;
using Cqrs.Snapshots;
using Cqrs.Tests.Substitutes;
using NUnit.Framework;

namespace Cqrs.Tests.Snapshots
{
	[TestFixture]
	public class When_getting_an_aggregate_with_snapshot
	{
		private TestSnapshotAggregate _aggregate;

		[SetUp]
		public void Setup()
		{
			var eventStore = new TestInMemoryEventStore();
			var eventPublisher = new TestEventPublisher();
			var snapshotStore = new TestSnapshotStore();
			var snapshotStrategy = new DefaultSnapshotStrategy<ISingleSignOnToken>();
			var aggregateFactory = new AggregateFactory(new TestDependencyResolver());
			var snapshotRepository = new SnapshotRepository<ISingleSignOnToken>(snapshotStore, snapshotStrategy, new Repository<ISingleSignOnToken>(aggregateFactory, eventStore, eventPublisher), eventStore, aggregateFactory);
			var session = new UnitOfWork<ISingleSignOnToken>(snapshotRepository);

			_aggregate = session.Get<TestSnapshotAggregate>(Guid.NewGuid());
		}

		[Test]
		public void Should_restore()
		{
			Assert.True(_aggregate.Restored);
		}
	}
}
