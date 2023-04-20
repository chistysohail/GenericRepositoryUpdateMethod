public class GenericRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Attach the entity to the DbContext if it is not being tracked
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }

        // Mark the entity as modified
        _context.Entry(entity).State = EntityState.Modified;

        // Loop through all the navigation properties (child collections) and update them
        foreach (var navigationEntry in _context.Entry(entity).Navigations)
        {
            if (navigationEntry is CollectionEntry collectionEntry)
            {
                // Get the current and original values of the collection
                var currentItems = collectionEntry.CurrentValue;
                var originalItems = collectionEntry.GetDatabaseValues()?.GetValue<IEnumerable>() as IEnumerable<object>;

                // Create hash sets for comparison
                var currentSet = new HashSet<object>(currentItems, new ObjectIdentityComparer());
                var originalSet = originalItems == null ? new HashSet<object>(new ObjectIdentityComparer()) : new HashSet<object>(originalItems, new ObjectIdentityComparer());

                // Determine the items that were added, deleted, and modified
                var addedItems = currentSet.Except(originalSet).ToList();
                var deletedItems = originalSet.Except(currentSet).ToList();
                var modifiedItems = currentSet.Intersect(originalSet).ToList();

                // Handle added items
                foreach (var addedItem in addedItems)
                {
                    _context.Entry(addedItem).State = EntityState.Added;
                }

                // Handle deleted items
                foreach (var deletedItem in deletedItems)
                {
                    _context.Entry(deletedItem).State = EntityState.Deleted;
                }

                // Handle modified items
                foreach (var modifiedItem in modifiedItems)
                {
                    _context.Entry(modifiedItem).State = EntityState.Modified;
                }
            }
        }

        // Save changes to the database
        await _context.SaveChangesAsync(cancellationToken);
    }

    private class ObjectIdentityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}