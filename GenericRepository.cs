public class GenericRepository<TEntity> where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

   
     public void Update(TEntity entity, params Expression<Func<TEntity, object>>[] navigationProperties)
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

    // Load the navigation properties and perform the updates
    foreach (var navigationProperty in navigationProperties)
    {
        var propertyInfo = (PropertyInfo)((MemberExpression)navigationProperty.Body).Member;
        var propertyValue = propertyInfo.GetValue(entity);

        if (propertyValue is ICollection collection)
        {
            foreach (var item in collection)
            {
                var itemEntry = _context.Entry(item);

                if (itemEntry.State == EntityState.Detached)
                {
                    var primaryKey = _context.Model.FindEntityType(item.GetType()).FindPrimaryKey();
                    var keyValues = primaryKey.Properties.Select(p => p.PropertyInfo.GetValue(item)).ToArray();

                    if (_context.Find(item.GetType(), keyValues) != null)
                    {
                        itemEntry.State = EntityState.Modified;
                    }
                    else
                    {
                        itemEntry.State = EntityState.Added;
                    }
                }
            }
        }
        else
        {
            var relatedEntityEntry = _context.Entry(propertyValue);
            if (relatedEntityEntry.State == EntityState.Detached)
            {
                _dbSet.Attach((TEntity)propertyValue);
            }

            relatedEntityEntry.State = EntityState.Modified;
        }
    }

    // Save changes to the database
    _context.SaveChanges();
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
/*
// Create an instance of the DbContext (e.g., YourDbContext)
using var context = new YourDbContext();

// Create an instance of the GenericRepository for the Author entity
var authorRepository = new GenericRepository<Author>(context);

// Retrieve the author from the database (assuming you have an authorId)
var author = context.Authors
    .Include(a => a.Books)
    .Include(a => a.Publisher)
    .FirstOrDefault(a => a.AuthorId == authorId);

// Modify the author, related books, and publisher
author.Name = "Updated Author Name";
author.Books.First().Title = "Updated Book Title";
author.Publisher.Name = "Updated Publisher Name";

// Call the Update method to update the author, related books, and publisher
authorRepository.Update(author, a => a.Books, a => a.Publisher);

*/