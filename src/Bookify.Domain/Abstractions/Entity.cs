namespace Bookify.Domain.Abstractions
{
    public abstract class Entity
    {
        // make a list of events
        private readonly List<IDomainEvent> _domainEvents = new();
        protected Entity(Guid id)
        {
            Id = id;
        }
        public Guid Id { get; set; }

        public IReadOnlyList<IDomainEvent> GetDomainEvents()


        { return _domainEvents.ToList(); }



        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }


        // raise when sth happened in domain layer
        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }


        protected Entity()
        {
            
        }



    }
}
