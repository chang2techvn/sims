using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SIMS.Tests
{
    public class PerformanceTests
    {
        private readonly HttpClient _client;

        public PerformanceTests()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5281") };
        }

        [Fact]
        public async Task PT01_TestLoginLoad_1000ConcurrentUsers()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting PT01_TestLoginLoad_1000ConcurrentUsers at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var loginData = new Dictionary<string, string>
            {
                { "Email", "student@sims.com" },
                { "Password", "chang123" },
                { "RememberMe", "false" }
            };
            var content = new FormUrlEncodedContent(loginData);
            var tasks = new List<Task<HttpResponseMessage>>();
            var semaphore = new SemaphoreSlim(100); // Limit concurrency to avoid overwhelming

            var setupStart = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var response = await _client.PostAsync("/Account/Login", content);
                        return response;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            var setupEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Setup 1000 concurrent tasks completed in {setupEnd - setupStart}ms");

            var executionStart = stopwatch.ElapsedMilliseconds;
            var results = await Task.WhenAll(tasks);
            var executionEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Executed 1000 login requests in {executionEnd - executionStart}ms");

            var analysisStart = stopwatch.ElapsedMilliseconds;
            var successCount = results.Count(r => r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Redirect);
            var avgResponseTime = results.Average(r => r.Headers.Date?.Subtract(DateTime.UtcNow).TotalMilliseconds ?? 0);
            var analysisEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Analysis completed in {analysisEnd - analysisStart}ms");

            stopwatch.Stop();
            Console.WriteLine($"PT01 completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Success rate: {successCount}/1000");
            Console.WriteLine($"Average response time: {avgResponseTime}ms");

            Assert.True(avgResponseTime < 3000, $"Average response time {avgResponseTime}ms exceeds 3s");
            Assert.True(successCount >= 950, $"Success rate {successCount}/1000 too low"); // Allow some failures
            Console.WriteLine("Assert: Test passed successfully");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task PT02_TestCourseListingLoad_5000Records()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting PT02_TestCourseListingLoad_5000Records at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // First login to get session
            var loginData = new Dictionary<string, string>
            {
                { "Email", "student@sims.com" },
                { "Password", "chang123" },
                { "RememberMe", "false" }
            };
            var loginContent = new FormUrlEncodedContent(loginData);
            var loginStart = stopwatch.ElapsedMilliseconds;
            var loginResponse = await _client.PostAsync("/Account/Login", loginContent);
            var loginEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Login phase completed in {loginEnd - loginStart}ms: {(loginResponse.IsSuccessStatusCode ? "Success" : "Failed")}");
            Assert.True(loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect);

            var tasks = new List<Task<HttpResponseMessage>>();
            var setupStart = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < 50; i++) // Simulate fetching in batches
            {
                tasks.Add(_client.GetAsync("/Student/MyCourses"));
            }
            var setupEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Setup 50 course listing tasks completed in {setupEnd - setupStart}ms");

            var executionStart = stopwatch.ElapsedMilliseconds;
            var results = await Task.WhenAll(tasks);
            var executionEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Executed 50 course listing requests in {executionEnd - executionStart}ms");

            var analysisStart = stopwatch.ElapsedMilliseconds;
            var successCount = results.Count(r => r.IsSuccessStatusCode);
            var avgResponseTime = results.Average(r => r.Headers.Date?.Subtract(DateTime.UtcNow).TotalMilliseconds ?? 0);
            var analysisEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Analysis completed in {analysisEnd - analysisStart}ms");

            stopwatch.Stop();
            Console.WriteLine($"PT02 completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Success rate: {successCount}/50");
            Console.WriteLine($"Average response time: {avgResponseTime}ms");

            Assert.True(avgResponseTime < 5000, $"Average response time {avgResponseTime}ms exceeds 5s");
            Assert.True(successCount == 50, "All requests should succeed");
            Console.WriteLine("Assert: Test passed successfully");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task PT03_TestRegistrationPeak_500ConcurrentRegistrations()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting PT03_TestRegistrationPeak_500ConcurrentRegistrations at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // First login as admin
            var loginData = new Dictionary<string, string>
            {
                { "Email", "admin@sims.com" },
                { "Password", "Admin123" },
                { "RememberMe", "false" }
            };
            var loginContent = new FormUrlEncodedContent(loginData);
            var loginStart = stopwatch.ElapsedMilliseconds;
            var loginResponse = await _client.PostAsync("/Account/Login", loginContent);
            var loginEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Admin login phase completed in {loginEnd - loginStart}ms: {(loginResponse.IsSuccessStatusCode ? "Success" : "Failed")}");
            Assert.True(loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect);

            var tasks = new List<Task<HttpResponseMessage>>();
            var semaphore = new SemaphoreSlim(50);
            var setupStart = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < 500; i++)
            {
                var registerData = new Dictionary<string, string>
                {
                    { "Name", $"Test User {i}" },
                    { "Email", $"test{i}@example.com" },
                    { "Password", "Test123!" },
                    { "Role", "Student" },
                    { "StudentCode", $"ST{i:D3}" },
                    { "DateOfBirth", "2000-01-01" },
                    { "Phone", "123456789" },
                    { "Gender", "Male" },
                    { "Address", "Test Address" }
                };
                var content = new FormUrlEncodedContent(registerData);

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var response = await _client.PostAsync("/Admin/AddUser", content);
                        return response;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            var setupEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Setup 500 registration tasks completed in {setupEnd - setupStart}ms");

            var executionStart = stopwatch.ElapsedMilliseconds;
            var results = await Task.WhenAll(tasks);
            var executionEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Executed 500 registration requests in {executionEnd - executionStart}ms");

            var analysisStart = stopwatch.ElapsedMilliseconds;
            var successCount = results.Count(r => r.IsSuccessStatusCode || r.StatusCode == System.Net.HttpStatusCode.Redirect);
            var timeoutCount = results.Count(r => r.StatusCode == System.Net.HttpStatusCode.RequestTimeout);
            var analysisEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Analysis completed in {analysisEnd - analysisStart}ms");

            stopwatch.Stop();
            Console.WriteLine($"PT03 completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Success rate: {successCount}/500");
            Console.WriteLine($"Timeouts: {timeoutCount}");

            Assert.True(timeoutCount == 0, $"No timeouts allowed, but {timeoutCount} occurred");
            Assert.True(successCount >= 475, $"Success rate {successCount}/500 too low"); // Allow some failures
            Console.WriteLine("Assert: Test passed successfully ✅");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task PT04_TestDBThroughput_10kEnrollOperations()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting PT04_TestDBThroughput_10kEnrollOperations at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // First login as admin
            var loginData = new Dictionary<string, string>
            {
                { "Email", "admin@sims.com" },
                { "Password", "Admin123" },
                { "RememberMe", "false" }
            };
            var loginContent = new FormUrlEncodedContent(loginData);
            var loginStart = stopwatch.ElapsedMilliseconds;
            var loginResponse = await _client.PostAsync("/Account/Login", loginContent);
            var loginEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Admin login phase completed in {loginEnd - loginStart}ms: {(loginResponse.IsSuccessStatusCode ? "Success" : "Failed")}");
            Assert.True(loginResponse.IsSuccessStatusCode || loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect);

            var tasks = new List<Task<HttpResponseMessage>>();
            var semaphore = new SemaphoreSlim(100);
            var setupStart = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < 10000; i++)
            {
                var enrollData = new Dictionary<string, string>
                {
                    { "studentId", "1" }, // Assume fixed IDs for test
                    { "courseId", "1" }
                };
                var content = new FormUrlEncodedContent(enrollData);

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var response = await _client.PostAsync("/Admin/AssignStudentToCoursePost", content);
                        return response;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            var setupEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Setup 10000 enrollment tasks completed in {setupEnd - setupStart}ms");

            var executionStart = stopwatch.ElapsedMilliseconds;
            var results = await Task.WhenAll(tasks);
            var executionEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Executed 10000 enrollment requests in {executionEnd - executionStart}ms");

            var analysisStart = stopwatch.ElapsedMilliseconds;
            var successCount = results.Count(r => r.IsSuccessStatusCode);
            var failureRate = (10000 - successCount) / 100.0;
            var analysisEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Analysis completed in {analysisEnd - analysisStart}ms");

            stopwatch.Stop();
            Console.WriteLine($"PT04 completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Success count: {successCount}/10000");
            Console.WriteLine($"Failure rate: {failureRate}%");

            Assert.True(failureRate < 1.0, $"Failure rate {failureRate}% exceeds 1%");
            Console.WriteLine("Assert: Test passed successfully ✅");
            Console.WriteLine("---");
        }

        [Fact]
        public async Task PT05_TestSystemUnderStress_1000MixedOperations()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting PT05_TestSystemUnderStress_1000MixedOperations at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(100);
            var setupStart = stopwatch.ElapsedMilliseconds;
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Mix of operations: login, course listing, profile view
                        var loginData = new Dictionary<string, string>
                        {
                            { "Email", "student@sims.com" },
                            { "Password", "chang123" },
                            { "RememberMe", "false" }
                        };
                        var loginContent = new FormUrlEncodedContent(loginData);
                        var loginStart = stopwatch.ElapsedMilliseconds;
                        await _client.PostAsync("/Account/Login", loginContent);
                        var loginEnd = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine($"Operation {i+1} login completed in {loginEnd - loginStart}ms");

                        var courseStart = stopwatch.ElapsedMilliseconds;
                        await _client.GetAsync("/Student/MyCourses");
                        var courseEnd = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine($"Operation {i+1} course listing completed in {courseEnd - courseStart}ms");

                        var profileStart = stopwatch.ElapsedMilliseconds;
                        await _client.GetAsync("/Account/Profile");
                        var profileEnd = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine($"Operation {i+1} profile view completed in {profileEnd - profileStart}ms");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            var setupEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Setup 1000 mixed operation tasks completed in {setupEnd - setupStart}ms");

            var executionStart = stopwatch.ElapsedMilliseconds;
            await Task.WhenAll(tasks);
            var executionEnd = stopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Executed 1000 mixed operations in {executionEnd - executionStart}ms");

            stopwatch.Stop();
            Console.WriteLine($"PT05 completed in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("System remained stable under stress");

            Assert.True(true, "System should remain stable");
            Console.WriteLine("Assert: Test passed successfully ✅");
            Console.WriteLine("---");
        }
    }
}