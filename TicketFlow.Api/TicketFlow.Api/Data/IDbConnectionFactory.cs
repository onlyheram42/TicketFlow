using System.Data;

namespace TicketFlow.Api.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
