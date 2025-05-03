namespace MknImmiSql.Api.V1;

public class ServiceContext
{
    static ServiceContext _instance;
    public TerminateToken TerminationToken { get; }
    public static ServiceContext GetInstance()
    {
        if (_instance == null) _instance = new ServiceContext();
        return _instance;
    }
    private ServiceContext()
    {
        TerminationToken = new TerminateToken();
    }
}
