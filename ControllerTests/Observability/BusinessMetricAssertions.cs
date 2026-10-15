using OpenTelemetry.Metrics;

namespace ControllerTests.Observability;

public static class BusinessMetricAssertions
{
    public static long Count(IEnumerable<Metric> metrics, string name, params (string Key, string Value)[] tags)
    {
        long count = 0;
        foreach (var metric in metrics.Where(metric => metric.Name == name))
            foreach (ref readonly var point in metric.GetMetricPoints())
            {
                var attributes = new Dictionary<string, object?>();
                foreach (var tag in point.Tags) attributes[tag.Key] = tag.Value;
                if (tags.All(tag => attributes.TryGetValue(tag.Key, out var value) && Equals(value, tag.Value)))
                    count += name.EndsWith(".duration", StringComparison.Ordinal) ? point.GetHistogramCount() : point.GetSumLong();
            }
        return count;
    }

    public static double Sum(IEnumerable<Metric> metrics, string name)
    {
        double sum = 0;
        foreach (var metric in metrics.Where(metric => metric.Name == name))
            foreach (ref readonly var point in metric.GetMetricPoints()) sum += point.GetHistogramSum();
        return sum;
    }
}
