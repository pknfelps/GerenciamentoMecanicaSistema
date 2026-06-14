namespace Domain.Interface.Order
{
    public enum WorkOrderStatus
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
