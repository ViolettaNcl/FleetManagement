namespace Zachet
{
    internal static class DB
    {
        private static FleetManagementDBEntities _context;
        public static FleetManagementDBEntities Context
        {
            get => _context ?? (_context = new FleetManagementDBEntities());
            set => _context = value;
        }
    }
}