using System;
using System.Collections.Generic;
using HbApiTester.Tasks;
using SQLite;

namespace HbApiTester.sqlite3
{
    public class DbOperations
    {
        public const string dbName = "/db/tasksdb.sqlite3";

        public DbOperations()
        {
            Console.WriteLine("DbOperations() : establishing connection...");

            using (var conn = new SQLiteConnection(dbName))
            {
                Console.WriteLine("DbOperations() : connection established! Creating/checking tables...");
                conn.CreateTable<TestTask>();
                conn.CreateTable<TestTaskWithDelayedResult>();
                conn.CreateTable<TestResponse>();
            }
            
            Console.WriteLine("DbOperations() : OK!");
        }

        public void InsertTask<T>(T task)
            where T : TestTask
        {
            try
            {
                Console.WriteLine("InsertTask() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    task.TaskId = (task.TaskId == Guid.Empty) ? Guid.NewGuid() : task.TaskId;
                    Console.WriteLine("InsertTask() : connection established!");
                    conn.Insert(task);
                    conn.Commit();
                    Console.WriteLine("InsertTask() : saved...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertTask() : exception: " + ex.Message);                
            }
        }

        public void InsertResponse(TestResponse response)
        {
            try
            {
                Console.WriteLine("InsertResponse() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    response.ResponseId = (response.ResponseId == Guid.Empty) ? Guid.NewGuid() : response.ResponseId;
                    Console.WriteLine("InsertResponse() : connection established!");
                    conn.Insert(response);
                    conn.Commit();
                    Console.WriteLine("InsertResponse() : saved...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InsertResponse() : exception: " + ex.Message);                
            }
        }
        
        public T GetTask<T>( Func<T, bool> expr ) 
            where T : TestTask, new() 
        {
            try
            {
                Console.WriteLine("GetTask() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    return conn.Table<T>().FirstOrDefault(t => expr(t));
                }
            } catch (Exception ex)
            {
                Console.WriteLine("GetTask() : exception: " + ex.Message);                
            }

            return null;
        }
        
        public IEnumerable<T> GetTasks<T>( Func<T, bool> expr ) 
            where T : TestTask, new() 
        {
            try
            {
                Console.WriteLine("GetTasks() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    return conn.Table<T>().Where(t => expr(t));
                }
            } catch (Exception ex)
            {
                Console.WriteLine("GetTasks() : exception: " + ex.Message);                
            }

            return null;
        }


        public TestResponse GetResponse(Func<TestResponse, bool> expr)
        {
            try
            {
                Console.WriteLine("GetResponse() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    return conn.Table<TestResponse>().FirstOrDefault(t => expr(t));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetResponse() : exception: " + ex.Message);
            }

            return null;
        }
        
        public IEnumerable<TestResponse> GetResponses(Func<TestResponse, bool> expr)
        {
            try
            {
                Console.WriteLine("GetResponses() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    return conn.Table<TestResponse>().Where(t => expr(t));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetResponses() : exception: " + ex.Message);
            }

            return null;
        }

        public void DeleteTask<T>(Func<T, bool> expr)
            where T : TestTask, new()
        {
            try
            {
                Console.WriteLine("GetResponse() : establishing connection...");
                using (var conn = new SQLiteConnection(dbName))
                {
                    conn.Table<T>().Delete(t => expr(t));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetResponse() : exception: " + ex.Message);
            }
        }
        
    }
}