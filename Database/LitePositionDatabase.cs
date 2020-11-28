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
    public class LitePositionDatabase : PortfolioDatabase
    {
        protected LiteDatabase _db;

        public LitePositionDatabase(string dbPath)
        {
            _db = new LiteDatabase(dbPath);

            //TODO REMOVE!!!!!!!!!!!!!!!!!!!!!!!!!!
            //_db.GetCollection<Position>().DeleteMany(pos => pos.DateUpdated.Date == DateTime.Now.Date);
        }

        public override IEnumerable<Position> GetStoredPositions()
        {
            return _db.GetCollection<Position>().FindAll();
        }

        public override TimeSortedSet<PositionDelta> GetStoredDeltas()
        {
            return new TimeSortedSet<PositionDelta>(_db.GetCollection<PositionDelta>().FindAll());
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
                Exception ex = new PortfolioDatabaseException("Cannot insert position since one already exists");
                Log.Fatal(ex, "Tried to insert {@Position} but one already exists in the database: {@CurrentPosition}- Symbol {Symbol}", position, currentPos, position.Symbol);
                throw ex;
            }
            _db.GetCollection<Position>().Insert(position);
        }

        public override void DeletePosition(Position position)
        {
            _db.GetCollection<Position>().DeleteMany(pos => pos.Symbol == position.Symbol);
        }

        public override TimeSortedSet<FilledOrder> GetStoredOrders()
        {
            IEnumerable<FilledOrder> orders = _db.GetCollection<FilledOrder>().FindAll();
            return new TimeSortedSet<FilledOrder>(orders);
        }

        protected override bool OrderAlreadyExists(FilledOrder order)
        {
            // FindOne() using o.StrictEquals() throws a LiteDB exception dealing with
            // inability to convert to BSON expression.
            FilledOrder? existingOrder = _db.GetCollection<FilledOrder>().FindById(order.Id);
            return existingOrder != null;
        }

        protected override Position? GetPosition(string symbol)
        {
            return _db.GetCollection<Position>().FindOne(pos => pos.Symbol == symbol);
        }

        public override void InsertOrders(IEnumerable<FilledOrder> orders)
        {
            _db.GetCollection<FilledOrder>().InsertBulk(orders);
        }

        public override void UpdateAllPositions(IEnumerable<Position> positions)
        {
            _db.GetCollection<Position>().DeleteAll();
            _db.GetCollection<Position>().InsertBulk(positions);
        }

        public override void InsertDeltas(IEnumerable<PositionDelta> deltas)
        {
            _db.GetCollection<PositionDelta>().InsertBulk(deltas);
        }

        public override TimeSortedSet<FilledOrder> GetTodaysFilledOrders()
        {
            return new TimeSortedSet<FilledOrder>(_db.GetCollection<FilledOrder>().Find(order => order.Time.Date == DateTime.Today));
        }

        public override void DeleteOrders(IEnumerable<FilledOrder> orders)
        {
            foreach(FilledOrder order in orders)
            {
                _db.GetCollection<FilledOrder>().Delete(order.Id);
                //_db.GetCollection<FilledOrder>().DeleteMany(o => o.StrictEquals(order));
            }
        }
    }
}
