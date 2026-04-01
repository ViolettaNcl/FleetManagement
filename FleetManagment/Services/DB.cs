namespace Zachet
{
    internal static class DB
    {
        private static FleetManagementDBEntities1 _context;
        public static FleetManagementDBEntities1 Context
        {
            get => _context ?? (_context = new FleetManagementDBEntities1());
            set => _context = value;
        }
    }
}