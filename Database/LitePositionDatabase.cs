using Core;
using Core.Model;
using LiteDB;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Database
{
    public class LitePositionDatabase : PositionDatabase
    {
        protected LiteDatabase _db;

        public LitePositionDatabase(string dbPath)
        {
            _db = new LiteDatabase(dbPath);
        }

        public override IList<Position> GetStoredPositions()
        {
            return _db.GetCollection<Position>().FindAll().ToList();
        }

        public override IList<PositionDelta> GetStoredDeltas()
        {
            return _db.GetCollection<PositionDelta>().FindAll().ToList();
        }

        public override void InsertDelta(PositionDelta delta)
        {
            _db.GetCollection<PositionDelta>().Insert(delta);
        }

        public override void InsertPosition(Position position)
        {
            Position? currentPos = GetPosition(position.Symbol);
            if (currentPos != null)
            {
                Exception ex = new PositionDatabaseException("Cannot insert position since one already exists");
                Log.Error(ex, "Tried to insert {@Position} but one already exists in the database: {@CurrentPosition}", position, currentPos);
                throw ex;
            }
            _db.GetCollection<Position>().Insert(position);
        }

        public override void DeletePosition(Position position)
        {
            _db.GetCollection<Position>().DeleteMany(pos => pos.Symbol == position.Symbol);
        }

        public override IList<FilledOrder> GetStoredOrders()
        {
            return _db.GetCollection<FilledOrder>().FindAll().ToList();
        }

        protected override bool OrderAlreadyExists(FilledOrder order)
        {
            FilledOrder existingOrder = _db.GetCollection<FilledOrder>()
                .FindOne(o => o.Time == order.Time && o.Symbol == order.Symbol);
            return existingOrder != null;
        }

        protected override Position? GetPosition(string symbol)
        {
            return _db.GetCollection<Position>().FindOne(pos => pos.Symbol == symbol);
        }

        protected override void InsertOrders(IList<FilledOrder> orders)
        {
            _db.GetCollection<FilledOrder>().InsertBulk(orders);
        }

        public override void UpdateAllPositions(IList<Position> positions)
        {
            _db.GetCollection<Position>().DeleteAll();
            _db.GetCollection<Position>().InsertBulk(positions);
        }

        public override void InsertDeltas(IList<PositionDelta> deltas)
        {
            _db.GetCollection<PositionDelta>().InsertBulk(deltas);
        }
    }
}
