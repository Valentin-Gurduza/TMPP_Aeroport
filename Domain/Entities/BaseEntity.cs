using System;

namespace TMPP_Aeroport.Domain.Entities
{
    // Încapsulare: Proprietățile sunt definite cu accesori protejați/publici corespunzători.
    // Aceasta este clasa de bază (părinte) pentru toate entitățile din sistem.
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
