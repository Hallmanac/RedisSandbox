using System;

namespace RedisSandbox.Console.Core
{
    public interface IBaseEntity
    {
        int Id { get; set; }
        DateTimeOffset CreatedOn { get; set; }
        DateTimeOffset LastModifiedOn { get; set; } 
    }
}