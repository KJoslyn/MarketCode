﻿using Core;
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

        public override void UpdatePositionsAndDeltas(IList<Position> livePositions, IList<PositionDelta> positionDeltas)
        {
            if (positionDeltas.Count > 0)
            {
                _db.GetCollection<Position>().DeleteAll();
                _db.GetCollection<Position>().InsertBulk(livePositions);
                _db.GetCollection<PositionDelta>().InsertBulk(positionDeltas);
            }
        }

        public override void InsertDelta(PositionDelta delta)
        {
            _db.GetCollection<PositionDelta>().Insert(delta);
        }

        public override void InsertPosition(Position position)
        {
            Position currentPos = _db.GetCollection<Position>().FindOne(pos => pos.Symbol == position.Symbol);
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
    }
}
