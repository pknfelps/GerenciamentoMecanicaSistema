namespace Domain.Interface.Order
{
    public enum ServiceOrderStatus
    {
        Received,
        InDiagnosis,
        WaitingForApproval,
        WaitingForExecution,
        InExecution,
        Finished,
        Delivered
    }
}
