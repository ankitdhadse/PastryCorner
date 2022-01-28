using System;
using System.Collections.Generic;
using System.Text;

namespace PastryCorner.Domain.Models
{
    using System.Diagnostics.CodeAnalysis;
    using MongoDB.Bson;

    [ExcludeFromCodeCoverage]
    public class BaseBsonModel
    {
        public ObjectId Id { get; set; }
    }
}
