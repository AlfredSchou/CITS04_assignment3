using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;

public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;
    }

    public void Run()
    {

        var server = new TcpListener(IPAddress.Loopback, _port);
        server.Start();

        Console.WriteLine($"Server is running on port {_port}...");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine($"Client connected to {client.Client.RemoteEndPoint}");
            Task.Run(() => HandleClient(client)); // Makes a new thread for each new task
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            string msg = ReadFromStream(stream);
            Console.WriteLine("Message from client: " + msg);

            //2 + 5
            if (msg == "{}")
            {
                var response = new Response
                {
                    Status = "missing method missing date"
                };

                var json = ToJson(response);
                WriteToStream(stream, json);
            }
            else
            {
                var request = FromJson(msg);

                if (request == null)
                {

                }

                //3
                // Allowed methods are "create", "read", "update", "delete", "echo"
                string[] validMethods = ["create", "read", "update", "delete", "echo"];

                if (!validMethods.Contains(request.Method))
                {
                    var response = new Response
                    {
                        Status = "illegal method"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //4
                string[] crudMethods = ["create", "read", "update", "delete"];

                if (crudMethods.Contains(request.Method) && string.IsNullOrEmpty(request.Path))
                {
                    var response = new Response
                    {
                        Status = "missing resource"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //6
                // Unix time test
                if (!IsUnixTime(request.Date))
                {
                    var response = new Response
                    {
                        Status = "illegal date"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //7
                // Handling methods requiring a body
                string[] methodsRequiringBody = { "create", "update", "echo" };

                if (methodsRequiringBody.Contains(request.Method) && string.IsNullOrEmpty(request.Body))
                {
                    var response = new Response
                    {
                        Status = "missing body"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //8
                //Handling for seeing if its a valid json body structure
                if (request.Method == "update" && !IsValidJson(request.Body))
                {
                    var response = new Response
                    {
                        Status = "illegal body"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //9
                // The handling for echoing the body
                if (request.Method == "echo")
                {
                    var response = new Response
                    {
                        Status = "ok",
                        Body = request.Body
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //Testing API

                //Path tests

                //10
                // Handling for seeeing if paths are valid for the api

                if (!request.Path.StartsWith("/api/categories"))
                {
                    var response = new Response
                    {
                        Status = "4 Bad Request"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }


                //11
                // Does path have a invalid ID
                if (request.Path.Contains("/api/categories/"))
                {
                    string lastSegment1 = request.Path.Split('/').Last();
                    if (!int.TryParse(lastSegment1, out _))
                    {
                        var response = new Response
                        {
                            Status = "4 Bad Request"
                        };
                        var json = ToJson(response);
                        WriteToStream(stream, json);
                        return;
                    }
                }

                //12
                // Handling that ensure that create method has a valid ID
                if (request.Method == "create" && (request.Path.Contains("/api/categories/")))
                {
                    string lastSegment2 = request.Path.Split('/').Last();
                    if (int.TryParse(lastSegment2, out _))
                    {
                        var response = new Response
                        {
                            Status = "4 Bad Request"
                        };
                        var json = ToJson(response);
                        WriteToStream(stream, json);
                        return;
                    }
                }

                //13 
                // Handling for ensuring that update has a valid path ID
                if (request.Method == "update" && request.Path == "/api/categories")
                {
                    var response = new Response
                    {
                        Status = "4 Bad Request"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //14
                // Ensure del-method has a valid path ID
                if (request.Method == "delete" && request.Path == "/api/categories")
                {
                    var response = new Response
                    {
                        Status = "4 Bad Request"
                    };
                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;
                }

                //15
                // Handle "read" method for "/api/categories"
                if (request.Method == "read" && request.Path == "/api/categories")
                {

                    var categories = new List<object>
                        {
                            new { cid = 1, name = "Beverages" },
                            new { cid = 2, name = "Condiments" },
                            new { cid = 3, name = "Confections" }
                        };

                    var response = new Response
                    {
                        Status = "1 Ok",
                        Body = JsonSerializer.Serialize(categories)
                    };

                    var json = ToJson(response);
                    WriteToStream(stream, json);
                    return;  // Return after sending the response
                }


                //16
                // Handle "read" method for "/api/categories/ AND an id
                if (request.Method == "read" && request.Path.StartsWith("/api/categories/"))
                {
                    string lastSegment3 = request.Path.Split('/').Last();
                    if (!int.TryParse(lastSegment3, out int categoryId))
                    {
                        var response = new Response
                        {
                            Status = "4 Bad Request"
                        };
                        var json = ToJson(response);
                        WriteToStream(stream, json);
                        return;
                    }

                    // The categoires that it can fall under
                    var categories = new List<object>
                        {
                            new { cid = 1, name = "Beverages" },
                            new { cid = 2, name = "Condiments" },
                            new { cid = 3, name = "Confections" }
                        };

                    // lambda for finding the category 1
                    var category = categories.FirstOrDefault(c => ((int)c.GetType().GetProperty("cid").GetValue(c)) == categoryId);

                    if (category != null)
                    {
                        var response = new Response
                        {
                            Status = "1 Ok",
                            Body = JsonSerializer.Serialize(category)
                        };
                        var json = ToJson(response);
                        WriteToStream(stream, json);
                        return;
                    }
                    else
                    {
                        var response = new Response
                        {
                            Status = "4 Bad Request"
                        };
                        var json = ToJson(response);
                        WriteToStream(stream, json);
                        return;
                    }
                }


            }
        }
        catch { }
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }

    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson(string element)
    {
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    // Util method for seeing if a date can parse as unix time
    private bool IsUnixTime(string date)
    {
        // Try to parse the string as unix which is a int
        if (long.TryParse(date, out long unixTime))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            if (dateTimeOffset >= DateTimeOffset.UnixEpoch && dateTimeOffset <= DateTimeOffset.UtcNow)
            {
                return true;
            }
        }
        return false;
    }

    // another util method for checking wether a string is a valid json stucture
    private bool IsValidJson(string strInput)
    {
        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || // For object
            (strInput.StartsWith("[") && strInput.EndsWith("]")))   // For array
        {
            try
            {
                var obj = JsonSerializer.Deserialize<object>(strInput);
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
}