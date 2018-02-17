namespace Log4net.NetCore.Data
{
    public static class DBInitializer
    {
        public static void Initialize(Log4netDBContext context)
        {
            context.Database.EnsureCreated();
        }
    }
}
