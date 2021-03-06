﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Simple.Data.MongoDB
{
    [Export("MongoDb", typeof(Adapter))]
    internal class MongoAdapter : Adapter
    {
        private MongoDatabase _database;
        private readonly IExpressionFormatter _expressionFormatter;

        static MongoAdapter()
        {
            BsonSerializer.RegisterSerializationProvider(new DynamicSerializationProvider());
        }

        public MongoAdapter()
        {
            _expressionFormatter = new ExpressionFormatter(this);
        }

        public override int Delete(string tableName, SimpleExpression criteria)
        {
            return new MongoAdapterDeleter(this, _expressionFormatter)
                .Delete(GetCollection(tableName), criteria);
        }

        public override IDictionary<string, object> FindOne(string tableName, SimpleExpression criteria)
        {
            return new MongoAdapterFinder(this, _expressionFormatter)
                .FindOne(GetCollection(tableName), criteria);
        }

        public override IEnumerable<IDictionary<string, object>> Find(string tableName, SimpleExpression criteria)
        {
            return new MongoAdapterFinder(this, _expressionFormatter)
                .Find(GetCollection(tableName), criteria);
        }

        public override IDictionary<string, object> Get(string tableName, params object[] keyValues)
        {
            return FindOne(tableName, new SimpleExpression("_id", keyValues[0], SimpleExpressionType.Equal));
        }

        public override IDictionary<string, object> GetKey(string tableName, IDictionary<string, object> record)
        {
            var result = new Dictionary<string, object>();
            var idPair = MongoIdKeys.FindId(record);
            if (idPair.HasValue)
            {
                result.Add(idPair.Value.Key, idPair.Value.Value);
            }

            return result;
        }

        public override IList<string> GetKeyNames(string tableName)
        {
            return new List<string> { "_id" };
        }

        public override IDictionary<string, object> Insert(string tableName, IDictionary<string, object> data, bool resultRequired)
        {
            return new MongoAdapterInserter(this)
                .Insert(GetCollection(tableName), data);
        }

        public override bool IsExpressionFunction(string functionName, params object[] args)
        {
            return ExpressionFormatter._functions.Contains(functionName);
        }

        public override IEnumerable<IDictionary<string, object>> RunQuery(SimpleQuery query, out IEnumerable<SimpleQueryClauseBase> unhandledClauses)
        {
            return new MongoAdapterFinder(this, _expressionFormatter)
                .Find(GetCollection(query.TableName), query, out unhandledClauses);
        }

        public override int Update(string tableName, IDictionary<string, object> data, SimpleExpression criteria)
        {
            return new MongoAdapterUpdater(this, _expressionFormatter)
                .Update(GetCollection(tableName), data, criteria);
        }

        internal MongoDatabase GetDatabase()
        {
            return _database;
        }

        protected override void OnSetup()
        {
            var settingsKeys = ((IDictionary<string, object>)Settings).Keys;
            if (settingsKeys.Contains("ConnectionString"))
                _database = MongoDatabase.Create(Settings.ConnectionString);
            else if (settingsKeys.Contains("Settings"))
                _database = MongoDatabase.Create(Settings.Settings, Settings.DatabaseName);

            if (_database == null)
                throw new SimpleDataException("Invalid setup for MongoDb. Either a ConnectionString should be provided, or MongoServerSettings and a DatabaseName");
        }

        private MongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            return this.GetDatabase().GetCollection(collectionName);
        }
    }
}