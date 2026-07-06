namespace Repository.Interface
{
    public interface ITransactionManager
    {
        Task ExecuteInTransaction(Func<Task> operation);
        Task<T> ExecuteInTransaction<T>(Func<Task<T>> operation);
    }
}
