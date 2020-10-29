using Core;
using Core.Model;
using Core.Model.Constants;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Database
{
    public class PositionDatabase : IPositionDatabase
    {
        protected LiteDatabase _db;

        public PositionDatabase(string dbPath)
        {
            _db = new LiteDatabase(dbPath);
        }

        public IList<Position> GetStoredPositions()
        {
            return _db.GetCollection<Position>().FindAll().ToList();
        }

        public IList<PositionDelta> GetStoredDeltas()
        {
            return _db.GetCollection<PositionDelta>().FindAll().ToList();
        }

        public void UpdatePositionsAndDeltas(IList<Position> livePositions, IList<PositionDelta> positionDeltas)
        {
            if (positionDeltas.Count > 0)
            {
                _db.GetCollection<Position>().DeleteAll();
                _db.GetCollection<Position>().InsertBulk(livePositions);
                _db.GetCollection<PositionDelta>().InsertBulk(positionDeltas);
            }
        }
    }
}
