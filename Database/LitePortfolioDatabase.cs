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
    public class LitePortfolioDatabase : PortfolioDatabase
    {
        protected LiteDatabase _db;
        protected LiteDatabase _symbolsDb;

        public LitePortfolioDatabase(string dbPath, string symbolsDbPath)
        {
            _db = new LiteDatabase(dbPath);
            _symbolsDb = new LiteDatabase(symbolsDbPath);

            //TODO REMOVE!!!!!!!!!!!!!!!!!!!!!!!!!!
            //_db.GetCollection<Position>().DeleteMany(pos => pos.DateUpdated.Date == DateTime.Now.Date);
        }

        public override void Dispose()
        {
            _db.Dispose();
            _symbolsDb.Dispose();
        }

        public override IEnumerable<Position> GetStoredPositions()
        {
            return _db.GetCollection<Position>().FindAll();
        }

        public override TimeSortedCollection<PositionDelta> GetStoredDeltas()
        {
            return new TimeSortedCollection<PositionDelta>(_db.GetCollection<PositionDelta>().FindAll());
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

        public override TimeSortedCollection<FilledOrder> GetStoredOrders()
        {
            IEnumerable<FilledOrder> orders = _db.GetCollection<FilledOrder>().FindAll();
            return new TimeSortedCollection<FilledOrder>(orders);
        }

        public override bool OrderAlreadyExists(FilledOrder order)
        {
            // FindOne() using o.StrictEquals() throws a LiteDB exception dealing with
            // inability to convert to BSON expression.
            FilledOrder? existingOrder = _db.GetCollection<FilledOrder>().FindById(order.Id);
            return existingOrder != null;
        }

        public override Position? GetPosition(string symbol)
        {
            return _db.GetCollection<Position>().FindOne(pos => pos.Symbol == symbol);
        }

        public override void InsertOrders(IEnumerable<FilledOrder> orders)
        {
            _db.GetCollection<FilledOrder>().InsertBulk(orders);
        }

        public override void InsertOrder(FilledOrder order)
        {
            _db.GetCollection<FilledOrder>().Insert(order);
        }

        public override void UpdateAllPositions(IEnumerable<Position> positions)
        {
            _db.GetCollection<Position>().DeleteAll();
            _db.GetCollection<Position>().InsertBulk(positions);
        }

        public override TimeSortedCollection<FilledOrder> GetTodaysFilledOrders()
        {
            return new TimeSortedCollection<FilledOrder>(_db.GetCollection<FilledOrder>().Find(order => order.Time.Date == DateTime.Today));
        }

        public override void DeleteOrders(IEnumerable<FilledOrder> orders)
        {
            foreach(FilledOrder order in orders)
            {
                _db.GetCollection<FilledOrder>().Delete(order.Id);
                //_db.GetCollection<FilledOrder>().DeleteMany(o => o.StrictEquals(order));
            }
        }

        protected override void UpsertUsedUnderlyingSymbol(UsedUnderlyingSymbol usedSymbol)
        {
            _symbolsDb.GetCollection<UsedUnderlyingSymbol>().Upsert(usedSymbol);
        }

        protected override void InsertDelta(PositionDelta delta)
        {
            _db.GetCollection<PositionDelta>().Insert(delta);
        }

        public override IEnumerable<UsedUnderlyingSymbol> GetUsedUnderlyingSymbols(Func<UsedUnderlyingSymbol, bool> predicate)
        {
            return _symbolsDb.GetCollection<UsedUnderlyingSymbol>().FindAll().Where(predicate);
        }
    }
}
