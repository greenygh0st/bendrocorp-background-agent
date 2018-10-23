using System;
using System.Data;
using System.Reflection;
using System.Threading;

namespace BendroCorpBackgroundAgent
{
    public class Runner
    {
        public void Run()
        {
            while (true)
            {
                // get a list of the task managers
                PgUnicorn pg = new PgUnicorn();

                DataTable taskManagers = pg.DataTableOfSql("select * from task_managers where enabled = true");

                foreach (DataRow manager in taskManagers.Rows)
                {
                    DateTime lastRun = DateTime.Parse(manager["next_run"].ToString());
                    if (lastRun < DateTime.Now)
                    {
                        try
                        {
                            // get the task name from the manager object
                            string taskName = manager["task_name"].ToString();
                            // get the Tasks object
                            Type thisType = typeof(Tasks);
                            // see if we can find the method
                            MethodInfo theMethod = thisType.GetMethod(taskName);
                            if (theMethod != null)
                            {
                                theMethod.Invoke(null, new object[] { "Har har" });
                                string updateTask = $"update task_managers set last_run = now(), next_run = displace_date(now()::timestamp, {manager["every"].ToString()}, {manager["recur"].ToString()}) where id = {manager["id"].ToString()}";
                                pg.ExecuteNonQueryOfSql(updateTask);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Occured while trying to process task: " + ex.Message);
                        }
                    }
                }
                Thread.Sleep(10000); // sleep for 10 seconds
            }
        }
    }
}
