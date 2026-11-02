using OpenTelemetry;
using System.Diagnostics;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class SafeTraceProcessor(TelemetryCollectionOptions collection) : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        var database = activity.Source.Name == "Npgsql";
        var allowed = database ? collection.DatabaseTraceAttributes : collection.HttpTraceAttributes;
        foreach (var tag in activity.TagObjects.Where(tag => !allowed.Contains(tag.Key)).ToArray())
            activity.SetTag(tag.Key, null);

        foreach (var item in activity.Baggage.ToArray())
            activity.SetBaggage(item.Key, null);
        activity.TraceStateString = null;

        foreach (ref var activityEvent in activity.EnumerateEvents())
        {
            var tags = new ActivityTagsCollection(activityEvent.Tags.Where(tag =>
                tag.Value is not null && collection.ExceptionAttributes.Contains(tag.Key)));
            activityEvent = new ActivityEvent(activityEvent.Name, activityEvent.Timestamp, tags);
        }

        if (database)
            activity.DisplayName = collection.DatabaseSpanName;
        else
        {
            var method = TelemetryRequest.Method(activity.GetTagItem("http.request.method") as string, collection);
            var route = activity.GetTagItem("http.route") as string ?? "unmatched";
            activity.SetTag("http.request.method", method);
            activity.SetTag("http.route", route);
            activity.DisplayName = $"{method} {route}";
        }

        if (activity.Status == ActivityStatusCode.Error)
            activity.SetStatus(ActivityStatusCode.Error, database
                ? collection.DatabaseErrorMessage
                : collection.HttpErrorMessage);
    }
}
