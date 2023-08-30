namespace GourmetClient.Model
{
    using System;

    public record BillingPosition(
        DateTime Date,
        bool ContainsTimeInformation,
        BillingPositionType PositionType,
        string PositionName,
        int Count,
        double SumCost);
}
