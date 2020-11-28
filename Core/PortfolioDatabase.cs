using Core.Model;
using Core.Model.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

namespace Core
{
    public abstract class PortfolioDatabase
    {
        public abstract IEnumerable<Position> GetStoredPositions();

        public abstract TimeSortedSet<PositionDelta> GetStoredDeltas();

        public abstract TimeSortedSet<FilledOrder> GetStoredOrders();

        public abstract void InsertOrders(IEnumerable<FilledOrder> orders);

        public abstract void DeleteOrders(IEnumerable<FilledOrder> orders);

        public abstract void InsertDelta(PositionDelta delta);

        public abstract void InsertDeltas(IEnumerable<PositionDelta> deltas);

        public abstract void InsertPosition(Position position);

        public abstract void UpdateAllPositions(IEnumerable<Position> positions);

        public abstract void DeletePosition(Position position);

        public abstract TimeSortedSet<FilledOrder> GetTodaysFilledOrders();

        protected abstract bool OrderAlreadyExists(FilledOrder order);

        protected abstract Position? GetPosition(string symbol);

        public TimeSortedSet<PositionDelta> ComputeDeltasAndUpdateTables(TimeSortedSet<FilledOrder> newOrders)
        {
            InsertOrders(newOrders);

            TimeSortedSet<PositionDelta> deltas = new TimeSortedSet<PositionDelta>();
            foreach (FilledOrder order in newOrders)
            {
                Position? oldPos = GetPosition(order.Symbol);
                if (oldPos == null) // NEW
                {
                    if (order.Instruction == InstructionType.SELL_TO_CLOSE)
                    {
                        PortfolioDatabaseException ex = new PortfolioDatabaseException("No existing position corresponding to sell order");
                        Log.Fatal(ex, "No existing position corresponding to sell order {@Order}- Symbol {Symbol}", order, order.Symbol);
                        throw ex;
                    }
                    PositionDelta delta = new PositionDelta(
                        DeltaType.NEW, 
                        order.Symbol, 
                        order.Quantity, 
                        order.Price, 
                        0,
                        order.Time);
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
                        order.Quantity / oldPos.LongQuantity,
                        order.Time);
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
                        order.Quantity / oldPos.LongQuantity,
                        order.Time);
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
            IEnumerable<Position> oldPositions = GetStoredPositions();

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

        public void UpdateOrders(IEnumerable<UpdatedFilledOrder> updatedFilledOrders)
        {
            IEnumerable<FilledOrder> oldOrders = updatedFilledOrders.Select(updated => updated.OldOrder);
            DeleteOrders(oldOrders);
            IEnumerable<FilledOrder> newOrders = updatedFilledOrders.Select(updated => updated.NewOrder);
            InsertOrders(newOrders);
        }

        public NewAndUpdatedFilledOrders IdentifyNewAndUpdatedOrders(TimeSortedSet<FilledOrder> liveOrders, double lookbackMinutes)
        {
            DateTime cutoffTime = DateTime.Now.AddMinutes(-lookbackMinutes);

            NewAndUpdatedFilledOrders recentOrdersResult = IdentifyRecentNewAndUpdatedOrders(liveOrders, cutoffTime);
            TimeSortedSet<FilledOrder> oldUnseenOrders = IdentifyOldUnseenOrders(liveOrders, cutoffTime);

            TimeSortedSet<FilledOrder> allNewOrders = new TimeSortedSet<FilledOrder>(recentOrdersResult.NewFilledOrders);
            foreach(FilledOrder oldUnseen in oldUnseenOrders)
            {
                allNewOrders.Add(oldUnseen);
            }

            return new NewAndUpdatedFilledOrders(allNewOrders, recentOrdersResult.UpdatedFilledOrders);
        }

        private NewAndUpdatedFilledOrders IdentifyRecentNewAndUpdatedOrders(TimeSortedSet<FilledOrder> liveOrders, DateTime lookAfterTime)
        {
            // After the matching process, any unmatched filled orders will be the new filled orders
            TimeSortedSet<FilledOrder> unmatchedRecentLiveOrders = new TimeSortedSet<FilledOrder>(
                liveOrders.Where(order => order.Time >= lookAfterTime));
            IList<UpdatedFilledOrder> updatedFilledOrders = new List<UpdatedFilledOrder>();

            // Only retrieve orders from the last "lookbackMinutes" minutes (call it X) for matching. This assumes that:
            // 1) Order times will not be updated AFTER X minutes
            // 2) It takes longer than X minutes for every visible order in the live portfolio to be pushed out of view
            TimeSortedSet<FilledOrder> unmatchedRecentDbOrders = new TimeSortedSet<FilledOrder>(
                GetTodaysFilledOrders().Where(order => order.Time >= lookAfterTime));

            // 1st pass: Match with exact time
            foreach (FilledOrder dbOrder in unmatchedRecentDbOrders.ToList())
            {
                FilledOrder? match = unmatchedRecentLiveOrders.FirstOrDefault(o => dbOrder.StrictEquals(o));
                if (match != null)
                {
                    unmatchedRecentLiveOrders.Remove(match);
                    unmatchedRecentDbOrders.Remove(dbOrder);
                }
            }
            // 2nd pass: Match using closest time
            foreach (FilledOrder dbOrder in unmatchedRecentDbOrders)
            {
                FilledOrder? match = unmatchedRecentLiveOrders.Where(o => dbOrder.EqualsIgnoreTime(o) && o.Time >= dbOrder.Time).FirstOrDefault();
                if (match != null)
                {
                    unmatchedRecentLiveOrders.Remove(match);
                    UpdatedFilledOrder updated = new UpdatedFilledOrder(dbOrder, match);
                    updatedFilledOrders.Add(updated);
                    Log.Information("Updated order {@OldOrder} to {@NewOrder}", dbOrder, match);
                }
                else
                {
                    PortfolioDatabaseException ex = new PortfolioDatabaseException("No live order matched to database order");
                    Log.Error(ex, "No live order matched to database order {@Order}- Symbol {Symbol}. Current live orders {@LiveOrders}", dbOrder, dbOrder.Symbol, liveOrders);
                    throw ex;
                }
            }

            TimeSortedSet<FilledOrder> newOrders = new TimeSortedSet<FilledOrder>(unmatchedRecentLiveOrders);
            return new NewAndUpdatedFilledOrders(newOrders, updatedFilledOrders);
        }

        private TimeSortedSet<FilledOrder> IdentifyOldUnseenOrders(TimeSortedSet<FilledOrder> liveOrders, DateTime lookBeforeTime)
        {
            TimeSortedSet<FilledOrder> unseenOrders = new TimeSortedSet<FilledOrder>();

            // Add any older olders that have not been seen before to the "newOrders" set.
            TimeSortedSet<FilledOrder> oldLiveOrders = new TimeSortedSet<FilledOrder>(
                liveOrders.Where(order => order.Time < lookBeforeTime));
            foreach(FilledOrder oldLiveOrder in oldLiveOrders.Where(o => !OrderAlreadyExists(o)))
            {
                unseenOrders.Add(oldLiveOrder);
            }
            if (oldLiveOrders.Count > 0 && unseenOrders.Contains(oldLiveOrders.First()))
            {
                Log.Warning("Oldest visible live order was found to be new. This may indicate that some orders were missed.");
            }
            return unseenOrders;
        }
    }
}
