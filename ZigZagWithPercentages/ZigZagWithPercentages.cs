using cAlgo.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ZigZagWithPercentages : Indicator
    {
        private class SwingPoint
        {
            public int Index { get; set; }
            public double Price { get; set; }

            public SwingPoint()
            {
                Index = 0;
                Price = double.MaxValue;
            }

            public SwingPoint(int index, double price)
            {
                Index = index;
                Price = price;
            }
        }

        #region Fields

        // Bar Index
        private int _index;

        // Direction of the ZigZag
        private Direction direction = Direction.Down;

        // Price of recent High
        private double high;

        // Price of recent Low
        private double low;

        private bool isBullish;

        private SwingPoint swingPoint;
        private List<SwingPoint> swingPointList;

        private enum Direction
        {
            Up,
            Down
        }

        #endregion Fields

        #region Parameters

        [Parameter("Percentage Move", DefaultValue = 3.0, Step = 0.5, MinValue = 0, Group = "Zig Zag")]
        public double PercentageMove { get; set; }

        [Parameter("Show Percentage?", DefaultValue = true, Group = "Zig Zag")]
        public bool ShowPercentChange { get; set; }

        [Parameter("Show Price?", DefaultValue = false, Group = "Zig Zag")]
        public bool ShowPrice { get; set; }

        #endregion Parameters

        #region Output

        [Output("ZigZag", LineColor = "White", Thickness = 2, PlotType = PlotType.Line)]
        public IndicatorDataSeries Result { get; set; }

        #endregion Output

        public override void Calculate(int index)
        {
            _index = index;

            low = Bars.LowPrices.LastValue;
            high = Bars.HighPrices.LastValue;

            if (Bars.ClosePrices.Count < 2)
                return;

            switch (direction)
            {
                case Direction.Up:
                    BullishZigZag();
                    break;

                case Direction.Down:
                default:
                    BearishZigZag();
                    break;
            }
        }

        protected override void Initialize()
        {
            swingPoint = new SwingPoint();
            swingPointList = new List<SwingPoint>();
        }

        #region Append Text

        public string GetZigZagText(double currentValue, double previousValue)
        {
            StringBuilder sb = new StringBuilder();

            if (ShowPrice)
                AppendPrice(sb);

            if (ShowPercentChange)
                AppendPercentageChange(currentValue, previousValue, sb);

            return sb.ToString();
        }

        private void AppendPercentageChange(double currentValue, double previousValue, StringBuilder sb)
        {
            if (ShowPrice)
                sb.AppendLine();

            string sign = isBullish ? "+" : "-";
            string percentage = (isBullish ? CalculatePercentageChange(currentValue, previousValue) : CalculatePercentageChange(previousValue, currentValue)).ToString("F2");

            sb.Append(string.Format("({0}{1}%)", sign, percentage));
        }

        private void AppendPrice(StringBuilder sb)
        {
            sb.Append(swingPoint.Price);
        }

        private double CalculatePercentageChange(double previous, double current)
        {
            double change = Math.Abs(current - previous);
            return 100 * (double)change / previous;
        }

        #endregion Append Text

        #region Create Zig Zag

        public void ShowZigZagLabel()
        {
            // Get last value
            double currentValue = swingPointList[swingPointList.Count - 1].Price;
            // Get second last value
            double previousValue = swingPointList[swingPointList.Count - 2].Price;

            isBullish = currentValue > previousValue;
            string objName = "ZigZagLabel" + swingPoint.Index;
            Color textColour = isBullish ? "Green" : "Red";
            string labelText = GetZigZagText(currentValue, previousValue);

            ChartText text = Chart.DrawText(name: objName, text: labelText, barIndex: swingPoint.Index, y: swingPoint.Price, color: textColour);

            text.VerticalAlignment = isBullish ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            text.HorizontalAlignment = HorizontalAlignment.Center;
        }

        private void BearishZigZag()
        {
            double percentChange = (1.0 + PercentageMove * 0.01);

            bool moveExtremum = low <= swingPoint.Price;
            bool changeDirection = high >= swingPoint.Price * percentChange;

            if (moveExtremum)
                MoveExtremum(low);
            else if (changeDirection)
            {
                SetExtremum(high);
                direction = Direction.Up;
            }
        }

        private void BullishZigZag()
        {
            double percentChange = (1.0 - PercentageMove * 0.01);

            bool moveExtremum = high >= swingPoint.Price;
            bool changeDirection = low <= swingPoint.Price * percentChange;

            if (moveExtremum)
                MoveExtremum(high);
            else if (changeDirection)
            {
                SetExtremum(low);
                direction = Direction.Down;
            }
        }

        private void MoveExtremum(double price)
        {
            Result[swingPoint.Index] = double.NaN;

            if (swingPointList.Count > 0)
                swingPointList.Remove(swingPointList[swingPointList.Count - 1]);

            Chart.RemoveObject("ZigZagLabel" + swingPoint.Index);

            SetExtremum(price);
        }

        private void SetExtremum(double price)
        {
            swingPoint = new SwingPoint(_index, price);
            swingPointList.Add(swingPoint);
            Result[swingPoint.Index] = swingPoint.Price;

            if (swingPointList.Count < 2)
                return;

            ShowZigZagLabel();
        }

        #endregion Create Zig Zag
    }
}