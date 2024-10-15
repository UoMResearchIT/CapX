namespace PPMTool.Data
{
    public interface IChartItem
    {
        public bool IsHatched();

        /// <summary>
        /// There is a bug in ApexCharts that doesn't order the items in the series properly unless every series has the same number of elements.
        /// This method is to determine whether this chart item simply exists to get the sorting to work properly and shouldn't be drawn.
        /// If true then this won't be rendered when the chart draws by giving it zero thickness.
        /// </summary>
        /// <returns></returns>
        public bool IsFake();
    }
}
