namespace GameServerManager.Client
{
    public static class ChartHelper
    {
        public static T[] AddSeriesData<T>(T[] data, T newValue, int maxData)
        {
            if (data.Length == maxData)
            {
                Array.Copy(data, 1, data, 0, data.Length - 1);
            }
            else
            {
                Array.Resize(ref data, Math.Min(data.Length + 1, maxData));
            }

            data[^1] = newValue;
            return data;
        }
    }
}
