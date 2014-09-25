using System;

namespace RedisSandbox.Console.Core
{
    public abstract class BaseEntity : IBaseEntity
    {
        private DateTimeOffset _createdOn;
        public int Id { get; set; }

        public DateTimeOffset CreatedOn
        {
            get
            {
                _createdOn = (_createdOn == default(DateTimeOffset) || Id == 0) ? DateTimeOffset.UtcNow : _createdOn;
                return _createdOn;
            }
            set
            {
                _createdOn = value;
            }
        }

        public DateTimeOffset LastModifiedOn { get; set; }
    }
}