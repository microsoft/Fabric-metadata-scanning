using Fabric_Metadata_Scanning;
using Newtonsoft.Json.Linq;

class Program
{
    private static SemaphoreSlim threadPool;
    private static WorkspaceInfoAPI_Handler workspaceInfoAPI;

    static async Task Main(string[] args)
    {
        try
        {
            Configuration_Handler configuration_handler = Configuration_Handler.Instance;
            configuration_handler.setConfigurationsFile(args[0]);

            Auth_Handler authHandler = new Auth_Handler();
            ModifiedAPI_Handler modifiedAPI = new ModifiedAPI_Handler();

            int threadsCount = Configuration_Handler.Instance.getConfig("shared", "threadsCount").Value<int>();
            threadPool = new SemaphoreSlim(threadsCount, threadsCount); ;

            string accessToken = await authHandler.authenticate();

            string workspacesFilePath = (string)await modifiedAPI.run(null);

            workspaceInfoAPI = new WorkspaceInfoAPI_Handler(workspacesFilePath);

            // Start < threadsCount > tasks, each trying to acquire a permit from the semaphore and run the APIs
            Task[] tasks = new Task[threadsCount];
            for (int i = 0; i < threadsCount; i++)
            {
                tasks[i] = Task.Run(() => runAPIs(i));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            Console.WriteLine("All tasks completed.");

        }
        catch (ScanningException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine($"You can use docs starting from here: {ex.HelpLink}");
            Console.WriteLine($"And the ReadMe file of this module: {ex.ReadMeLink}");
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        static async Task runAPIs(object? num)
        {
            threadPool.WaitAsync();
            string scanId;
            while (!Equals(scanId = (string)await workspaceInfoAPI.run(null), "Done"))
            {
                while (scanId == null)
                {
                    // Waiting for response
                    Console.WriteLine($"Thread number {num} is going to sleep .....");
                    Thread.Sleep(500);
                    Console.WriteLine($"Thread number {num} awake .....");
                }

                while (!Equals(await ScanStatusAPI_Handler.Instance.run(scanId), "Succeeded"))
                {
                    Console.WriteLine($"Thread number {num} is going to sleep (waiting for status) ...");
                    await Task.Delay(2500); // change waiting increasly
                    Console.WriteLine($"Thread number {num} is awake from waiting for status ...");
                }

                ScanResultAPI_Handler.Instance.run(scanId).Wait();

                try
                {
                    threadPool.Release();
                }
                catch
                {}
            }
        }
    }
}


