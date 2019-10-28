using JPProject.Domain.Core.Interfaces;
using JPProject.Admin.Domain.Interfaces;
using JPProject.EntityFrameworkCore.Context;

namespace JPProject.Admin.Infra.Data.UoW
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly JpProjectContext _context;

        public UnitOfWork(JpProjectContext context)
        {
            _context = context;
        }

        public bool Commit()
        {
            return _context.SaveChanges() > 0;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
