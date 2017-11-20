using Microsoft.Azure.Devices.ProtocolGateway.StorageClient.Implementation.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProtocolGateway.StorageClient.Implementation
{

    public class SQLStorageProvider : IStorageProvider
    {
        private string connectionString;
        DeviceContext context;
        string deviceid;

        public SQLStorageProvider(string connectionString, string deviceID)
        {
            try
            {
                this.connectionString = connectionString;
                this.deviceid = deviceID;

                DbContextOptions<DeviceContext> options = new DbContextOptions<DeviceContext>();
                context = new DeviceContext(options)
                {
                    dbcontextConnectionString = connectionString
                };

                try
                {
                    //context.Database.Migrate();
                    context.Database.EnsureCreated(); // should not do anything if exists
                }
                catch (SqlException ex)
                {

                    if (ex.Number == 1801)
                    {
                        //database already exists
                    }
                    else
                    {
                        Console.WriteLine("SQLException: " + ex.Message);
                    }
                }


            }
            catch (Exception ex)
            {

                Console.WriteLine("Exception: " + ex.Message);
            }
        }

        public Task AbandonAsync(string messageId)
        {
            Console.WriteLine("AbandonAsync:" + messageId);
            return Task.FromResult<bool>(true);
        }

        public Task CloseAsync()
        {
            context.Database.CloseConnection();
            return Task.FromResult<bool>(true);
        }


        public async Task OpenAsync()
        {

            await context.Database.OpenConnectionAsync();
        }

        public async Task<byte[]> ReceiveAsync(TimeSpan timeValue)
        {
            //byte[] result = Encoding.ASCII.GetBytes("HelloThere");            
            DateTime startDT = new DateTime();

            while ((timeValue == TimeSpan.MaxValue) || (DateTime.Now < startDT.Add(timeValue)))
            {
                //TODO: Async wait from scan from database
                var outMsg = await context.Outgoing.FirstOrDefaultAsync(u => u.ToDevice.DeviceID == this.deviceid);
                if (outMsg == null)
                {
                    Thread.Sleep(1000);
                    await Task.Yield();                    
                }
                else
                {
                    Thread.Sleep(1000);
                    return Encoding.ASCII.GetBytes(outMsg.Body);
                }
            }

            return null;
        }


        public Task SendEventAsync(string messageId, string to, string userId, byte[] messageBody)
        {
            string msg = Encoding.ASCII.GetString(messageBody);
            Console.WriteLine("SendEventAsync - messageId: {0}, To: {1}, UserId: {2}, Message: {3} ", messageId, to, userId, msg);
            return Task.FromResult<bool>(true);

        }

        public Task CompleteAsync(string messageId)
        {
            Console.WriteLine("CompleteAsync:" + messageId);
            return Task.FromResult<bool>(true);
        }
        public Task RejectAsync(string messageId)
        {
            Console.WriteLine("RejectAsync:" + messageId);
            return Task.FromResult<bool>(true);
        }

    }
}
