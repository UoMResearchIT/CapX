using Microsoft.EntityFrameworkCore;

namespace PPMTool.Services
{
    /// <summary>
    /// Class to encapsulate a change to an entity with values represented as strings
    /// </summary>
    public class EntityDiff
    {
        public int EntityId { get; }
        public EntityState State { get; }
        public string PropertyName { get; }
        public string OriginalValue { get; }
        public string CurrentValue { get; }

        public EntityDiff(int entityId, EntityState state, string propertyName, string originalValue, string currentValue)
        {
            EntityId = entityId;
            State = state;
            PropertyName = propertyName;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }
    }
}
