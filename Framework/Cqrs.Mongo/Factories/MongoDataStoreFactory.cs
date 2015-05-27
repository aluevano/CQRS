﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using cdmdotnet.Logging;
using Cqrs.Mongo.DataStores.Indexes;
using Cqrs.Mongo.Serialisers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Cqrs.Mongo.Factories
{
	/// <summary>
	/// A factory for obtaining DataStore collections from Mongo
	/// </summary>
	public class MongoDataStoreFactory
	{
		private static IDictionary<Type, IList<object>> IndexTypesByEntityType { get; set; }

		protected ILogger Logger { get; private set; }

		protected IMongoDataStoreConnectionStringFactory MongoDataStoreConnectionStringFactory { get; private set; }

		public MongoDataStoreFactory(ILogger logger, IMongoDataStoreConnectionStringFactory mongoDataStoreConnectionStringFactory)
		{
			Logger = logger;
			MongoDataStoreConnectionStringFactory = mongoDataStoreConnectionStringFactory;
		}

		static MongoDataStoreFactory()
		{
			var typeSerializer = new TypeSerialiser();
			MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(typeof(Type), typeSerializer);
			MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializer(Type.GetType("System.RuntimeType"), typeSerializer);

			IndexTypesByEntityType = new Dictionary<Type, IList<object>>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var mongoIndexTypes = assembly
					.GetTypes()
					.Where
					(
						type =>
							(
								// base is NOT an abstract index
								(
									type.BaseType != null &&
									type.BaseType.IsGenericType &&
									typeof(MongoIndex<>).IsAssignableFrom(type.BaseType.GetGenericTypeDefinition())
								)
								||
								// base is an abstract index
								(
									type.BaseType != null &&
									type.BaseType.BaseType != null &&
									type.BaseType.BaseType.IsGenericType &&
									typeof(MongoIndex<>).IsAssignableFrom(type.BaseType.BaseType.GetGenericTypeDefinition())
								)
								||
								// a deeper inheritance model
								(
									type.BaseType != null &&
									type.BaseType.BaseType != null &&
									type.BaseType.BaseType.BaseType != null &&
									type.BaseType.BaseType.BaseType.IsGenericType &&
									typeof(MongoIndex<>).IsAssignableFrom(type.BaseType.BaseType.BaseType.GetGenericTypeDefinition())
								)
								||
								// an even deeper inheritance model
								(
									type.BaseType != null &&
									type.BaseType.BaseType != null &&
									type.BaseType.BaseType.BaseType != null &&
									type.BaseType.BaseType.BaseType.BaseType != null &&
									type.BaseType.BaseType.BaseType.BaseType.IsGenericType &&
									typeof(MongoIndex<>).IsAssignableFrom(type.BaseType.BaseType.BaseType.BaseType.GetGenericTypeDefinition())
								)
							)
							&& !type.IsAbstract
					);
				foreach (Type mongoIndexType in mongoIndexTypes)
				{
					Type genericType = mongoIndexType;
					while (genericType != null && !genericType.IsGenericType)
					{
						genericType = genericType.BaseType;
					}
					genericType = genericType.GetGenericArguments().Single();

					IList<object> indexTypes;
					if (!IndexTypesByEntityType.TryGetValue(genericType, out indexTypes))
						IndexTypesByEntityType.Add(genericType, indexTypes = new List<object>());
					object mongoIndex = Activator.CreateInstance(mongoIndexType, true);
					indexTypes.Add(mongoIndex);
				}
			}
		}

		protected virtual MongoCollection<TEntity> GetCollection<TEntity>()
		{
			var mongoClient = new MongoClient(MongoDataStoreConnectionStringFactory.GetMongoConnectionString());
			MongoServer mongoServer = mongoClient.GetServer();
			MongoDatabase mongoDatabase = mongoServer.GetDatabase(MongoDataStoreConnectionStringFactory.GetMongoDatabaseName());

			return mongoDatabase.GetCollection<TEntity>(typeof(TEntity).FullName);
		}

		protected virtual void VerifyIndexes<TEntity>(MongoCollection<TEntity> collection)
		{
			Type entityType = typeof (TEntity);
			if (IndexTypesByEntityType.ContainsKey(entityType))
			{
				foreach (object untypedIndexType in IndexTypesByEntityType[entityType])
				{
					var mongoIndex = (MongoIndex<TEntity>)untypedIndexType;

					var indexKeysBuilder = new IndexKeysBuilder();
					if (mongoIndex.IsAcending)
						indexKeysBuilder = indexKeysBuilder.Ascending(mongoIndex.Selectors.ToArray());
					else
						indexKeysBuilder = indexKeysBuilder.Descending(mongoIndex.Selectors.ToArray());

					collection.CreateIndex
					(
						indexKeysBuilder,
						IndexOptions
							.SetUnique(mongoIndex.IsUnique)
							.SetName(mongoIndex.Name)
					);
				}
			}
		}
	}
}