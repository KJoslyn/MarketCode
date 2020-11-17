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

            //TODO REMOVE!!!!!!!!!!!!!!!!!!!!!!!!!!
            //_db.GetCollection<Position>().DeleteMany(pos => pos.DateUpdated.Date == DateTime.Now.Date);
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
                Log.Fatal(ex, "Tried to insert {@Position} but one already exists in the database: {@CurrentPosition}- Symbol {Symbol}", position, currentPos, position.Symbol);
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
            FilledOrder? existingOrder = null;
            try
            {
                existingOrder = _db.GetCollection<FilledOrder>()
                    .FindOne(o => o.Time.Equals(order.Time) && o.Symbol == order.Symbol);
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return existingOrder != null;
        }

        protected override Position? GetPosition(string symbol)
        {
            return _db.GetCollection<Position>().FindOne(pos => pos.Symbol == symbol);
        }

        public override void InsertOrders(IList<FilledOrder> orders)
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

        public override IList<FilledOrder> GetTodaysFilledOrders()
        {
            return _db.GetCollection<FilledOrder>().Find(order => order.Time.Date == DateTime.Today).ToList();
        }
    }
}
