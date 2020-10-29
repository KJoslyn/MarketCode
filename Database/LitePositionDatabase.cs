using Core;
using Core.Model;
using LiteDB;
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

        public override void UpdatePositionsAndDeltas(IList<Position> livePositions, IList<PositionDelta> positionDeltas)
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
