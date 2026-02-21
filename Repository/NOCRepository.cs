namespace ProductivityApi.Repository
{
    public class NOCRepository
    {
        private readonly string _conStr;
        public NOCRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

    }
}
