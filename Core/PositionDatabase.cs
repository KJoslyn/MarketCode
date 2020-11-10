using Core.Model;
using Core.Model.Constants;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Core
{
    public abstract class PositionDatabase
    {
        public abstract IList<Position> GetStoredPositions();

        public abstract IList<PositionDelta> GetStoredDeltas();

        public abstract IList<FilledOrder> GetStoredOrders();

        protected abstract void InsertOrders(IList<FilledOrder> orders);

        public abstract void InsertDelta(PositionDelta delta);

        public abstract void InsertDeltas(IList<PositionDelta> deltas);

        public abstract void InsertPosition(Position position);

        public abstract void UpdateAllPositions(IList<Position> positions);

        public abstract void DeletePosition(Position position);

        protected abstract bool OrderAlreadyExists(FilledOrder order);

        protected abstract Position? GetPosition(string symbol);

        public IList<PositionDelta> ComputeDeltasAndUpdateTables(IList<FilledOrder> liveOrders)
        {
            IList<FilledOrder> newOrders = liveOrders.Where(order => !OrderAlreadyExists(order)).ToList();
            InsertOrders(newOrders);

            IList<PositionDelta> deltas = new List<PositionDelta>();
            foreach (FilledOrder order in newOrders)
            {
                Position? oldPos = GetPosition(order.Symbol);
                if (oldPos == null) // NEW
                {
                    if (order.Instruction == InstructionType.SELL_TO_CLOSE)
                    {
                        // Warning
                    }
                    PositionDelta delta = new PositionDelta(
                        DeltaType.NEW, 
                        order.Symbol, 
                        order.Quantity, 
                        order.Price, 
                        0);
                    deltas.Add(delta);

                    Position newPos = new Position(order.Symbol, order.Quantity, order.Price);
                    InsertPosition(newPos);
                }
                else if (order.Instruction == InstructionType.BUY_TO_OPEN) // ADD
                {
                    PositionDelta delta = new PositionDelta(
                        DeltaType.ADD,
                        order.Symbol,
                        order.Quantity,
                        order.Price,
                        order.Quantity / oldPos.LongQuantity);
                    deltas.Add(delta);

                    DeletePosition(oldPos);

                    int newQuantity = (int)oldPos.LongQuantity + order.Quantity;
                    float averagePrice = (oldPos.AveragePrice * oldPos.LongQuantity + order.Quantity * order.Price ) / newQuantity;
                    Position position = new Position(order.Symbol, newQuantity, averagePrice);
                    InsertPosition(position);
                }
                else if (order.Instruction == InstructionType.SELL_TO_CLOSE) // SELL
                {
                    PositionDelta delta = new PositionDelta(
                        DeltaType.SELL,
                        order.Symbol,
                        order.Quantity,
                        order.Price,
                        order.Quantity / oldPos.LongQuantity);
                    deltas.Add(delta);

                    DeletePosition(oldPos);

                    int newQuantity = (int)oldPos.LongQuantity - order.Quantity;
                    if (newQuantity > 0)
                    {
                        Position position = new Position(order.Symbol, newQuantity, oldPos.AveragePrice);
                        InsertPosition(position);
                    }
                }
            }
            InsertDeltas(deltas);
            return deltas;
        }

        public IList<PositionDelta> ComputeDeltasAndUpdateTables(IList<Position> livePositions)
        {
            IList<PositionDelta> deltas = new List<PositionDelta>();
            IList<Position> oldPositions = GetStoredPositions();

            foreach (Position livePos in livePositions)
            {
                Position? oldPos = oldPositions.Where(pos => pos.Symbol == livePos.Symbol).FirstOrDefault();
                if (oldPos == null)
                {
                    PositionDelta delta = new PositionDelta(
                        DeltaType.NEW,
                        livePos.Symbol,
                        livePos.LongQuantity,
                        livePos.AveragePrice,
                        0);
                    deltas.Add(delta);
                }
            }
            foreach (Position oldPos in oldPositions)
            {
                PositionDelta? delta = null;
                Position? livePos = livePositions.Where(pos => pos.Symbol == oldPos.Symbol).FirstOrDefault();
                if (livePos == null)
                {
                    delta = new PositionDelta(
                        DeltaType.SELL,
                        oldPos.Symbol,
                        oldPos.LongQuantity,
                        0, // Sell price is unknown
                        1);
                }
                else if (livePos.LongQuantity > oldPos.LongQuantity)
                {
                    float deltaContracts = livePos.LongQuantity - oldPos.LongQuantity;
                    float addPrice = (livePos.AveragePrice * livePos.LongQuantity - oldPos.AveragePrice * oldPos.LongQuantity) / deltaContracts;
                    delta = new PositionDelta(
                        DeltaType.ADD,
                        oldPos.Symbol,
                        deltaContracts,
                        addPrice,
                        deltaContracts / oldPos.LongQuantity);
                }
                else if (livePos.LongQuantity < oldPos.LongQuantity)
                {
                    float deltaContracts = oldPos.LongQuantity - livePos.LongQuantity;
                    delta = new PositionDelta(
                        DeltaType.SELL,
                        oldPos.Symbol,
                        deltaContracts,
                        0, // Sell price is unknown
                        deltaContracts / oldPos.LongQuantity); ;
                }

                if (delta != null)
                {
                    deltas.Add(delta);
                }
            }
            InsertDeltas(deltas);
            UpdateAllPositions(livePositions);

            return deltas;
        }
    }
}
