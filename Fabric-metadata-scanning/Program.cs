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
            configuration_handler.setConfigurationsFile(args);
            await Auth_Handler.Instance.authenticate();
            ModifiedAPI_Handler modifiedAPI = new ModifiedAPI_Handler();

            int threadsCount = Configuration_Handler.Instance.getConfig("shared", "threadsCount").Value<int>();
            threadPool = new SemaphoreSlim(threadsCount, threadsCount); ;

            string workspacesFilePath = (string)await modifiedAPI.run();
            if (Equals(workspacesFilePath,null)) // No workspaces found.
            {
                return;
            }
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
            await threadPool.WaitAsync();
            string scanId;
            while (!Equals(scanId = (string)await workspaceInfoAPI.run(), "Done"))
            {
                while (scanId == null)
                {
                    // Waiting for response
                    Thread.Sleep(500);
                }

                while (!Equals(await ScanStatusAPI_Handler.Instance.run(scanId), "Succeeded"))
                {
                    await Task.Delay(2500);
                }

                ScanResultAPI_Handler.Instance.run(scanId).Wait();
            }
            try
            {
                threadPool.Release();
            }
            catch (SemaphoreFullException ex) 
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}


