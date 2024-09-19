using System;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using PermitSDK;
using PermitSDK.Models;

namespace PermitOnboardingApp
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:4000/";
        public static string pageData ="<p>User {0} is {1} to {2} {3}</p>";
        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerResponse resp = ctx.Response;

                // in a real app, you would typically decode the user id from a JWT token
                UserKey user = new UserKey("sjackson@mailinator.com", "SAML", "Jackson", "sjackson@mailinator.com");
                // init Permit SDK
                string clientToken = "permit_key_b33r3KW2ifk74PcJ9O7XGctqQ8DtXoZC3j8pSQd4Z86rH6zz8wnrx019Me5dNqd2PjI5dXnkEhW4UQ4Ujdac6k";
                Permit permit = new Permit(
                    clientToken,
                    "http://localhost:7766",
                    "default",
                    true
                );
                // After we created this user in the previous step, we also synced the user's identifier
                // to permit.io servers with permit.write(permit.api.syncUser(user)). The user identifier
                // can be anything (email, db id, etc) but must be unique for each user. Now that the
                // user is synced, we can use its identifier to check permissions with `permit.check()`.
                bool permitted = await permit.Check(user.key, "create", "API");
                if (permitted)
                {
                    var permitRoles = await permit.Api.ListRoles();

                    foreach (var role in permitRoles)
                    {
                        // Replace 'role.Name' and 'role.Id' with the actual properties available in the role object
                        Console.WriteLine($"Role ID: {role.Id}, Role Name: {role.Name}");
                    }
                    
                    await SendResponseAsync(resp, 200, String.Format(pageData, user.firstName + user.lastName, "Permitted", "create", "API"));
                }
                else
                {
                    await SendResponseAsync(resp, 403, String.Format(pageData, user.firstName + user.lastName, "NOT Permitted", "create", "API"));
                }

            }
        }
        public static async Task SendResponseAsync(HttpListenerResponse resp, int returnCode, string responseContent)
        {
            byte[] data = Encoding.UTF8.GetBytes(responseContent);
            resp.StatusCode = returnCode;
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }

        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
            listener.Close();
        }
    }
}