using System;
using System.Collections.Generic;
using System.Text;

namespace PastryCorner.Domain.Interfaces
{
    using System.Data;

    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
